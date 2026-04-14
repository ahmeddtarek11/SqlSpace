import { useState, useEffect, useCallback, Component, type ReactNode, type ErrorInfo } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { BarChart3, RefreshCw, Sparkles, Send, Loader2 } from 'lucide-react'
import { Skeleton } from '@/components/ui/skeleton'
import { ChartCard } from '@/components/analytics/ChartCard'
import { ExpandedChartDialog } from '@/components/analytics/ExpandedChartDialog'
import { ReportsTab } from '@/components/analytics/reports/ReportsTab'
import { analyticsApi } from '@/api/analytics'
import { connectionsApi } from '@/api/connections'
import { accessApi } from '@/api/insights'
import { ingestArtifactForAskAi } from '@/lib/ask-ai'
import { useConnectionStore } from '@/stores/connection-store'
import { useReportsStore, selectConnectionReports } from '@/stores/reports-store'
import type { ChartDataResult, SaveChartRequest } from '@/types'

interface ChartDataMap {
  [chartId: string]: {
    data: Record<string, unknown>[]
    columns: string[]
    loading: boolean
    error: string | null
    executionTimeMs?: number
  }
}

function parseResultsJson(json: string | null): { rows: Record<string, unknown>[]; columns: string[] } {
  if (!json) return { rows: [], columns: [] }
  try {
    const parsed = JSON.parse(json)
    if (parsed.columns && parsed.rows) {
      const columns: string[] = parsed.columns
      const rows = (parsed.rows as unknown[][]).map((row) => {
        const obj: Record<string, unknown> = {}
        columns.forEach((col, i) => {
          obj[col] = row[i]
        })
        return obj
      })
      return { rows, columns }
    }
    if (Array.isArray(parsed)) return { rows: parsed, columns: parsed.length > 0 ? Object.keys(parsed[0]) : [] }
    return { rows: [], columns: [] }
  } catch {
    return { rows: [], columns: [] }
  }
}

class AnalyticsErrorBoundary extends Component<{ children: ReactNode }, { error: Error | null }> {
  state = { error: null as Error | null }
  static getDerivedStateFromError(error: Error) { return { error } }
  componentDidCatch(error: Error, info: ErrorInfo) { console.error('AnalyticsPage crash:', error, info) }
  render() {
    if (this.state.error) {
      return (
        <div className="p-8 text-red-400">
          <h2 className="text-lg font-bold mb-2">Analytics page crashed</h2>
          <pre className="text-sm bg-red-500/10 p-4 rounded-lg overflow-auto whitespace-pre-wrap">
            {this.state.error.message}{'\n'}{this.state.error.stack}
          </pre>
        </div>
      )
    }
    return this.props.children
  }
}

function AnalyticsPageInner() {
  const queryClient = useQueryClient()
  const { activeConnectionId, setActiveConnection } = useConnectionStore()

  const [generationMode, setGenerationMode] = useState<'chart' | 'report'>('chart')
  const [chartDataMap, setChartDataMap] = useState<ChartDataMap>({})
  const [prompt, setPrompt] = useState('')
  const [isGenerating, setIsGenerating] = useState(false)
  const [expandedChartId, setExpandedChartId] = useState<string | null>(null)
  const [askingAiChartId, setAskingAiChartId] = useState<string | null>(null)

  const { data: connections = [], isLoading: connectionsLoading } = useQuery({
    queryKey: ['connections'],
    queryFn: connectionsApi.list,
    staleTime: 60_000,
  })

  const selectedId = activeConnectionId ?? connections[0]?.connectionId ?? ''
  const reportsState = useReportsStore((s) => selectConnectionReports(selectedId)(s))
  const { generateDraft } = useReportsStore()

  const { data: charts = [], isLoading: chartsLoading } = useQuery({
    queryKey: ['analytics-charts', selectedId],
    queryFn: () => analyticsApi.getCharts(selectedId),
    enabled: !!selectedId,
    refetchOnMount: 'always' as const,
  })

  const { data: canAskAi = false } = useQuery({
    queryKey: ['connection-is-admin', selectedId],
    queryFn: () => accessApi.isAdmin(selectedId),
    enabled: !!selectedId,
    staleTime: 60_000,
    retry: false,
  })

  // Expanded chart derived state
  const expandedChart = expandedChartId ? charts.find((c) => c.id === expandedChartId) : null
  const expandedData = expandedChartId ? chartDataMap[expandedChartId] : null

  // Auto-refresh all charts on load
  const refreshAll = useCallback(async () => {
    if (!selectedId || charts.length === 0) return

    setChartDataMap((prev) => {
      const next = { ...prev }
      charts.forEach((c) => {
        next[c.id] = { data: prev[c.id]?.data ?? [], columns: prev[c.id]?.columns ?? [], loading: true, error: null }
      })
      return next
    })

    try {
      const results = await analyticsApi.refreshAllCharts(selectedId)
      setChartDataMap((prev) => {
        const next = { ...prev }
        results.forEach((r: ChartDataResult) => {
          const { rows, columns } = parseResultsJson(r.resultsJson)
          next[r.chartId] = {
            data: rows,
            columns,
            loading: false,
            error: r.success ? null : (r.errorMessage ?? 'Execution failed'),
            executionTimeMs: r.executionTimeMs,
          }
        })
        return next
      })
    } catch {
      setChartDataMap((prev) => {
        const next = { ...prev }
        charts.forEach((c) => {
          next[c.id] = { ...next[c.id], loading: false, error: 'Failed to refresh charts' }
        })
        return next
      })
    }
  }, [selectedId, charts])

  useEffect(() => {
    if (charts.length > 0) {
      refreshAll()
    }
  }, [charts.length, selectedId]) // eslint-disable-line react-hooks/exhaustive-deps

  // Generate chart from prompt: suggest charts → auto-save → refresh
  const handleGenerate = useCallback(async (userPrompt?: string) => {
    if (!selectedId || isGenerating) return
    const text = userPrompt?.trim()
    setIsGenerating(true)
    try {
      const suggestions = await analyticsApi.suggestCharts(selectedId, text || undefined, text ? 1 : 3)
      for (const s of suggestions) {
        const payload: SaveChartRequest = {
          title: s.title,
          description: s.description,
          sqlQuery: s.sql,
          chartType: s.chartType,
          chartConfigJson: s.chartConfigJson,
          insight: s.insight,
        }
        await analyticsApi.saveChart(selectedId, payload)
      }
      setPrompt('')
      queryClient.invalidateQueries({ queryKey: ['analytics-charts', selectedId] })
    } catch {
      // silent
    } finally {
      setIsGenerating(false)
    }
  }, [selectedId, isGenerating, queryClient])

  const isPromptBusy = generationMode === 'chart' ? isGenerating : reportsState.isGenerating

  const handleSubmitPrompt = useCallback(async () => {
    if (!selectedId || isPromptBusy) return
    const trimmed = prompt.trim()
    if (!trimmed) return

    if (generationMode === 'chart') {
      await handleGenerate(trimmed)
      return
    }

    await generateDraft(selectedId, trimmed)
  }, [selectedId, isPromptBusy, prompt, generationMode, handleGenerate, generateDraft])

  const deleteMutation = useMutation({
    mutationFn: (chartId: string) => analyticsApi.deleteChart(selectedId, chartId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['analytics-charts', selectedId] })
    },
  })

  const handleRefreshOne = useCallback(async (chartId: string) => {
    setChartDataMap((prev) => ({
      ...prev,
      [chartId]: { ...prev[chartId], loading: true, error: null, data: prev[chartId]?.data ?? [], columns: prev[chartId]?.columns ?? [] },
    }))

    try {
      const result = await analyticsApi.executeChart(selectedId, chartId)
      const { rows, columns } = parseResultsJson(result.resultsJson)
      setChartDataMap((prev) => ({
        ...prev,
        [chartId]: {
          data: rows,
          columns,
          loading: false,
          error: result.success ? null : (result.errorMessage ?? 'Execution failed'),
          executionTimeMs: result.executionTimeMs,
        },
      }))
    } catch {
      setChartDataMap((prev) => ({
        ...prev,
        [chartId]: { ...prev[chartId], loading: false, error: 'Failed to execute chart' },
      }))
    }
  }, [selectedId])

  const handleAskAiChart = useCallback(async (chartId: string) => {
    if (!selectedId || askingAiChartId || !canAskAi) return

    const chart = charts.find((item) => item.id === chartId)
    if (!chart) return

    const chartData = chartDataMap[chartId]

    setAskingAiChartId(chartId)
    try {
      await ingestArtifactForAskAi({
        source: 'quick-insight',
        connectionId: selectedId,
        title: chart.title,
        prompt: chart.originalPrompt,
        sql: chart.sqlQuery,
        explanation: chart.description,
        insight: chart.insight,
        rows: chartData?.data ?? [],
        metadata: {
          chartId: chart.id,
          chartType: chart.chartType,
          connectionName: chart.connectionName,
          rowCount: chartData?.data?.length ?? 0,
        },
      })
    } finally {
      setAskingAiChartId(null)
    }
  }, [selectedId, askingAiChartId, canAskAi, charts, chartDataMap])

  // Loading state
  if (connectionsLoading) {
    return (
      <div className="p-8 max-w-7xl mx-auto space-y-6">
        <Skeleton className="h-10 w-72 rounded-xl bg-white/5" />
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          {[1, 2, 3, 4].map((i) => <Skeleton key={i} className="h-72 rounded-2xl bg-white/5" />)}
        </div>
      </div>
    )
  }

  // No connections
  if (connections.length === 0) {
    return (
      <div className="h-full flex items-center justify-center p-8">
        <div className="text-center">
          <div className="w-14 h-14 rounded-2xl bg-sky-500/10 border border-sky-500/20 flex items-center justify-center mx-auto mb-4">
            <BarChart3 className="w-7 h-7 text-sky-400" />
          </div>
          <p className="text-zinc-200 font-medium">No connections yet</p>
          <p className="text-zinc-500 text-sm mt-1">Add a database connection to start creating analytics.</p>
        </div>
      </div>
    )
  }

  return (
    <div className="h-full flex flex-col overflow-hidden">

      {/* Header */}
      <div className="shrink-0 px-8 pt-6 pb-0 border-b border-white/10">
        <div className="max-w-7xl mx-auto">
          <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-4">
            <div>
              <h1 className="text-2xl font-bold text-white tracking-tight">Analytics</h1>
              <p className="text-zinc-400 mt-1 text-sm">AI-powered data insights for your database</p>
            </div>

            <div className="flex items-center gap-2 flex-wrap">
              {charts.length > 0 && (
                <button
                  onClick={refreshAll}
                  className="flex items-center gap-2 px-3 py-2 rounded-lg text-sm font-medium text-zinc-400 hover:text-zinc-200 bg-white/5 hover:bg-white/10 transition-colors"
                >
                  <RefreshCw className="w-4 h-4" />
                  Refresh All
                </button>
              )}

              <select
                value={selectedId}
                onChange={(e) => setActiveConnection(e.target.value)}
                className="bg-[#111113] border border-white/10 rounded-lg py-2 pl-3 pr-8 text-sm text-white focus:outline-none focus:border-sky-500 appearance-none"
              >
                {connections.map((c) => (
                  <option key={c.connectionId} value={c.connectionId}>{c.connectionName}</option>
                ))}
              </select>
            </div>
          </div>
        </div>
      </div>

      <div className="flex-1 overflow-y-auto">
        <div className="p-8 max-w-7xl mx-auto space-y-10">

          {/* Shared prompt input */}
          <div className="space-y-3">
            <div className="inline-flex rounded-lg border border-white/10 bg-[#111113] p-1">
              <button
                type="button"
                onClick={() => setGenerationMode('chart')}
                className={`px-3 py-1.5 rounded-md text-xs font-medium transition-colors ${
                  generationMode === 'chart'
                    ? 'bg-sky-500/20 text-sky-200'
                    : 'text-zinc-400 hover:text-zinc-200'
                }`}
              >
                Quick Insight
              </button>
              <button
                type="button"
                onClick={() => setGenerationMode('report')}
                className={`px-3 py-1.5 rounded-md text-xs font-medium transition-colors ${
                  generationMode === 'report'
                    ? 'bg-sky-500/20 text-sky-200'
                    : 'text-zinc-400 hover:text-zinc-200'
                }`}
              >
                Full Report
              </button>
            </div>

            <div className="flex items-center gap-3">
              <div className="flex-1 relative">
                <Sparkles className="absolute left-3.5 top-1/2 -translate-y-1/2 w-4 h-4 text-zinc-600 pointer-events-none" />
                <input
                  type="text"
                  value={prompt}
                  onChange={(e) => setPrompt(e.target.value)}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter' && !isPromptBusy) {
                      void handleSubmitPrompt()
                    }
                  }}
                  placeholder={
                    generationMode === 'chart'
                      ? "Describe a chart you want (e.g. 'revenue by month', 'top customers')"
                      : "Describe the full report you want (e.g. 'overall sales health and actions for this quarter')"
                  }
                  className="w-full bg-[#111113] border border-white/10 rounded-xl pl-10 pr-4 py-3 text-sm text-white placeholder:text-zinc-600 focus:outline-none focus:border-sky-500 transition-colors"
                  disabled={isPromptBusy}
                />
              </div>
              <button
                onClick={() => void handleSubmitPrompt()}
                disabled={isPromptBusy || !prompt.trim()}
                className="flex items-center gap-2 px-5 py-3 rounded-xl bg-sky-600 hover:bg-sky-500 disabled:opacity-50 disabled:cursor-not-allowed text-white text-sm font-medium transition-colors shrink-0"
              >
                {isPromptBusy ? (
                  <>
                    <Loader2 className="w-4 h-4 animate-spin" />
                    Generating...
                  </>
                ) : (
                  <>
                    <Send className="w-4 h-4" />
                    {generationMode === 'chart' ? 'Generate Chart' : 'Generate Full Report'}
                  </>
                )}
              </button>
            </div>
          </div>

          {/* Charts section */}
          <section className="space-y-4">
            <h2 className="text-lg font-semibold text-zinc-200">Charts</h2>
            {chartsLoading ? (
              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                {[1, 2, 3, 4].map((i) => <Skeleton key={i} className="h-72 rounded-2xl bg-white/5" />)}
              </div>
            ) : charts.length === 0 ? (
              <div className="flex flex-col items-center justify-center py-16 rounded-2xl border border-white/10 bg-white/2">
                <div className="w-14 h-14 rounded-2xl bg-sky-500/10 border border-sky-500/20 flex items-center justify-center mb-4">
                  <BarChart3 className="w-7 h-7 text-sky-400" />
                </div>
                <p className="text-zinc-200 font-medium">No charts yet</p>
                <p className="text-zinc-500 text-sm mt-1 max-w-md text-center px-6">
                  Use the prompt bar above in chart mode to generate your first visual.
                </p>
              </div>
            ) : (
              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                {charts.map((chart) => {
                  const cd = chartDataMap[chart.id]
                  return (
                    <div key={chart.id} className="h-80">
                      <ChartCard
                        chart={chart}
                        data={cd?.data ?? null}
                        columns={cd?.columns ?? []}
                        loading={cd?.loading ?? true}
                        error={cd?.error ?? null}
                        executionTimeMs={cd?.executionTimeMs}
                        onRefresh={handleRefreshOne}
                        onDelete={(id) => deleteMutation.mutate(id)}
                        onExpand={setExpandedChartId}
                        onAskAi={(id) => void handleAskAiChart(id)}
                        isAskingAi={askingAiChartId === chart.id}
                        canAskAi={canAskAi}
                      />
                    </div>
                  )
                })}
              </div>
            )}
          </section>

          {/* Reports section */}
          <section className="space-y-4">
            <h2 className="text-lg font-semibold text-zinc-200">Reports</h2>
            <div className="h-190 min-h-130 rounded-2xl border border-white/10 overflow-hidden bg-[#0a0a0c]">
              <ReportsTab connectionId={selectedId} hidePromptInput />
            </div>
          </section>
        </div>
      </div>

      {/* Expanded chart dialog */}
      {expandedChart && (
        <ExpandedChartDialog
          open={!!expandedChartId}
          onOpenChange={(open) => { if (!open) setExpandedChartId(null) }}
          chart={expandedChart}
          data={expandedData?.data ?? null}
          columns={expandedData?.columns ?? []}
          executionTimeMs={expandedData?.executionTimeMs}
        />
      )}

    </div>
  )
}

export default function AnalyticsPage() {
  return (
    <AnalyticsErrorBoundary>
      <AnalyticsPageInner />
    </AnalyticsErrorBoundary>
  )
}
