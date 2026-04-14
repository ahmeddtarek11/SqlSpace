import { useState, useRef, useCallback, useEffect, useMemo, type ComponentType } from 'react'
import { useQuery } from '@tanstack/react-query'
import { toast } from 'sonner'
import { TooltipProvider } from '@/components/ui/tooltip'
import { NLPromptInput } from '@/components/workspace/NLPromptInput'
import { SQLPreview } from '@/components/workspace/SQLPreview'
import { ResultsTable } from '@/components/workspace/ResultsTable'
import { SchemaPanel } from '@/components/workspace/SchemaPanel'
import { RagChatPanel } from '@/components/workspace/RagChatPanel'
import { ChartRenderer } from '@/components/analytics/ChartRenderer'
import { connectionsApi } from '@/api/connections'
import { queriesApi } from '@/api/queries'
import { useWorkspaceStore } from '@/stores/workspace-store'
import { useConnectionStore } from '@/stores/connection-store'
import { AlertCircle, Table2, Code2, Sparkles, BarChart3, CheckCircle2, Clock, GripHorizontal, Wifi, WifiOff, ChevronDown, Check, MessageSquareText, MessageSquare } from 'lucide-react'
import { cn } from '@/lib/utils'
import { formatMs } from '@/lib/utils'
import type { QueryResult, ChartConfig, ChartType } from '@/types'

const MIN_TOP = 120
const MAX_TOP = 380
const DEFAULT_TOP = 180
const MAX_VISUALIZE_PROFILE_ROWS = 1500
const MAX_VISUALIZE_POINTS = 60
const DETAIL_LIST_MIN_ROWS = 40
const DETAIL_LIST_HIGH_CARDINALITY_RATIO = 0.75
const DETAIL_LIST_MIN_HIGH_CARD_TEXT_COLUMNS = 2
const DETAIL_LIST_MIN_HIGH_CARD_SHARE = 0.6

const IDENTIFIER_COLUMN_RE = /(^(id|pk)$|(^|_)(id|pk)$|customerid|orderid|productid|invoiceid|zip|postal|postcode|phone|ssn|account(number)?|code$)/i
const METRIC_COLUMN_RE = /(amount|total|sum|revenue|sales|price|cost|qty|quantity|count|avg|mean|score|rate|balance|value)/i
const DATE_COLUMN_RE = /(date|time|month|year|day|created|updated|timestamp|at)$/i
const CATEGORY_COLUMN_RE = /(name|type|category|group|segment|status|city|state|country|region|department|brand)/i

type ColumnProfile = {
  key: string
  nonNull: number
  uniqueCount: number
  uniqueRatio: number
  numericRatio: number
  dateRatio: number
  isNumeric: boolean
  isDateLike: boolean
  isLikelyIdentifier: boolean
}

type PreparedVisualization = {
  chartType: ChartType
  config: ChartConfig
  data: Record<string, unknown>[]
  note: string
  warning?: string
}

function toFiniteNumber(value: unknown): number | null {
  if (typeof value === 'number') return Number.isFinite(value) ? value : null
  if (typeof value === 'string') {
    const normalized = value.replace(/,/g, '').trim()
    if (!normalized) return null
    const parsed = Number(normalized)
    return Number.isFinite(parsed) ? parsed : null
  }
  return null
}

function toTimestamp(value: unknown): number | null {
  if (value instanceof Date) {
    const ts = value.getTime()
    return Number.isFinite(ts) ? ts : null
  }
  if (typeof value !== 'string') return null
  const trimmed = value.trim()
  if (!trimmed) return null
  const ts = Date.parse(trimmed)
  return Number.isFinite(ts) ? ts : null
}

function buildColumnProfiles(result: QueryResult): ColumnProfile[] {
  const sampleRows = result.rows.slice(0, MAX_VISUALIZE_PROFILE_ROWS)

  return result.columns.map((key) => {
    let nonNull = 0
    let numericMatches = 0
    let dateMatches = 0
    const uniqueValues = new Set<string>()

    for (const row of sampleRows) {
      const raw = row[key]
      if (raw == null) continue
      nonNull += 1

      const asNumber = toFiniteNumber(raw)
      if (asNumber != null) numericMatches += 1

      const asTimestamp = toTimestamp(raw)
      if (asTimestamp != null) dateMatches += 1

      uniqueValues.add(String(raw))
    }

    const uniqueCount = uniqueValues.size
    const uniqueRatio = nonNull > 0 ? uniqueCount / nonNull : 0
    const numericRatio = nonNull > 0 ? numericMatches / nonNull : 0
    const dateRatio = nonNull > 0 ? dateMatches / nonNull : 0
    const isNumeric = numericRatio >= 0.85
    const isDateLike = DATE_COLUMN_RE.test(key) || dateRatio >= 0.85
    const isLikelyIdentifier = IDENTIFIER_COLUMN_RE.test(key) || (isNumeric && uniqueRatio > 0.95 && !METRIC_COLUMN_RE.test(key))

    return {
      key,
      nonNull,
      uniqueCount,
      uniqueRatio,
      numericRatio,
      dateRatio,
      isNumeric,
      isDateLike,
      isLikelyIdentifier,
    }
  })
}

function metricSpreadScore(rows: Record<string, unknown>[], key: string): number {
  let min = Number.POSITIVE_INFINITY
  let max = Number.NEGATIVE_INFINITY
  for (const row of rows) {
    const value = toFiniteNumber(row[key])
    if (value == null) continue
    if (value < min) min = value
    if (value > max) max = value
  }
  if (!Number.isFinite(min) || !Number.isFinite(max)) return 0
  return Math.abs(max - min)
}

function pickMeasureColumn(profiles: ColumnProfile[], rows: Record<string, unknown>[]): string | null {
  const candidates = profiles.filter((p) => p.isNumeric && !p.isLikelyIdentifier)
  if (candidates.length === 0) return null

  const ranked = [...candidates].sort((a, b) => {
    const aNameScore = METRIC_COLUMN_RE.test(a.key) ? 1 : 0
    const bNameScore = METRIC_COLUMN_RE.test(b.key) ? 1 : 0
    if (aNameScore !== bNameScore) return bNameScore - aNameScore

    const spreadA = metricSpreadScore(rows, a.key)
    const spreadB = metricSpreadScore(rows, b.key)
    return spreadB - spreadA
  })

  return ranked[0]?.key ?? null
}

function pickCategoryColumn(profiles: ColumnProfile[], rowCount: number, excluded: string[] = []): string | null {
  const excludedSet = new Set(excluded)
  const maxUniqueCount = Math.min(MAX_VISUALIZE_POINTS, Math.max(8, Math.floor(rowCount * 0.5)))

  const candidates = profiles.filter(
    (p) =>
      !p.isNumeric &&
      !p.isDateLike &&
      !p.isLikelyIdentifier &&
      !excludedSet.has(p.key) &&
      p.uniqueCount >= 2 &&
      p.uniqueCount <= maxUniqueCount &&
      p.uniqueRatio <= 0.85,
  )

  if (candidates.length === 0) return null

  const ranked = [...candidates].sort((a, b) => {
    const aNameScore = CATEGORY_COLUMN_RE.test(a.key) ? 1 : 0
    const bNameScore = CATEGORY_COLUMN_RE.test(b.key) ? 1 : 0
    if (aNameScore !== bNameScore) return bNameScore - aNameScore

    if (a.uniqueCount !== b.uniqueCount) return a.uniqueCount - b.uniqueCount
    return a.key.localeCompare(b.key)
  })

  return ranked[0]?.key ?? null
}

function isLikelyDetailResultSet(
  result: QueryResult,
  profiles: ColumnProfile[],
  hasStrongMetricSignal: boolean,
): boolean {
  if (hasStrongMetricSignal || result.rows.length < DETAIL_LIST_MIN_ROWS || result.columns.length < 2) {
    return false
  }

  const textColumns = profiles.filter((p) => !p.isNumeric && !p.isDateLike)
  if (textColumns.length === 0) return false

  const highCardinalityText = textColumns.filter(
    (p) =>
      p.uniqueRatio >= DETAIL_LIST_HIGH_CARDINALITY_RATIO &&
      p.nonNull >= Math.min(result.rows.length, 20),
  )

  const highCardinalityShare = highCardinalityText.length / textColumns.length
  const hasIdentifier = profiles.some((p) => p.isLikelyIdentifier)

  return (
    highCardinalityText.length >= DETAIL_LIST_MIN_HIGH_CARD_TEXT_COLUMNS &&
    highCardinalityShare >= DETAIL_LIST_MIN_HIGH_CARD_SHARE &&
    (hasIdentifier || result.rows.length >= 100)
  )
}

function pickDateColumn(profiles: ColumnProfile[], excluded: string[] = []): string | null {
  const excludedSet = new Set(excluded)
  const candidates = profiles.filter((p) => p.isDateLike && !excludedSet.has(p.key))
  if (candidates.length === 0) return null
  const prioritized = [...candidates].sort((a, b) => b.dateRatio - a.dateRatio)
  return prioritized[0]?.key ?? null
}

function prepareWorkspaceVisualization(result: QueryResult): PreparedVisualization | null {
  if (result.rows.length === 0 || result.columns.length === 0) return null

  const profiles = buildColumnProfiles(result)
  const measure = pickMeasureColumn(profiles, result.rows)
  const hasStrongMetricSignal = Boolean(measure && METRIC_COLUMN_RE.test(measure))
  const detailLikeResult = isLikelyDetailResultSet(result, profiles, hasStrongMetricSignal)

  if (measure) {
    const dateDimension = pickDateColumn(profiles, [measure])

    if (dateDimension) {
      const timeSeries = result.rows
        .map((row) => ({
          label: String(row[dateDimension] ?? ''),
          ts: toTimestamp(row[dateDimension]),
          value: toFiniteNumber(row[measure]) ?? 0,
        }))
        .filter((point) => point.ts != null)
        .sort((a, b) => (a.ts ?? 0) - (b.ts ?? 0))
        .slice(-MAX_VISUALIZE_POINTS)
        .map((point) => ({ label: point.label, value: point.value }))

      if (timeSeries.length > 1) {
        return {
          chartType: 'line',
          config: { xAxis: 'label', yAxis: 'value' },
          data: timeSeries,
          note: `Time series using ${measure} over ${dateDimension}`,
          warning: result.rows.length > timeSeries.length
            ? `Showing latest ${timeSeries.length.toLocaleString()} points for readability.`
            : undefined,
        }
      }
    }

    const categoryDimension = pickCategoryColumn(profiles, result.rows.length, [measure])

    if (categoryDimension) {
      const grouped = new Map<string, number>()
      for (const row of result.rows) {
        const label = String(row[categoryDimension] ?? 'Unknown')
        const value = toFiniteNumber(row[measure]) ?? 0
        grouped.set(label, (grouped.get(label) ?? 0) + value)
      }

      const aggregated = [...grouped.entries()]
        .map(([label, value]) => ({ label, value }))
        .sort((a, b) => b.value - a.value)

      const limited = aggregated.slice(0, MAX_VISUALIZE_POINTS)
      return {
        chartType: limited.length > 18 ? 'horizontal_bar' : 'bar',
        config: { xAxis: 'label', yAxis: 'value' },
        data: limited,
        note: `Aggregated ${measure} by ${categoryDimension}`,
        warning: aggregated.length > limited.length
          ? `Showing top ${limited.length.toLocaleString()} categories out of ${aggregated.length.toLocaleString()}.`
          : undefined,
      }
    }

    if (detailLikeResult) {
      return null
    }

    const fallbackSeries = result.rows
      .map((row, index) => ({ label: `Row ${index + 1}`, value: toFiniteNumber(row[measure]) ?? 0 }))
      .slice(0, MAX_VISUALIZE_POINTS)

    return {
      chartType: 'line',
      config: { xAxis: 'label', yAxis: 'value' },
      data: fallbackSeries,
      note: `Trend of ${measure} across returned rows`,
      warning: result.rows.length > fallbackSeries.length
        ? `Showing first ${fallbackSeries.length.toLocaleString()} rows for readability.`
        : undefined,
    }
  }

  if (detailLikeResult) return null

  const categoryOnly = pickCategoryColumn(profiles, result.rows.length)
  if (!categoryOnly) return null

  const counts = new Map<string, number>()
  for (const row of result.rows) {
    const label = String(row[categoryOnly] ?? 'Unknown')
    counts.set(label, (counts.get(label) ?? 0) + 1)
  }

  const distribution = [...counts.entries()]
    .map(([label, value]) => ({ label, value }))
    .sort((a, b) => b.value - a.value)

  const limited = distribution.slice(0, MAX_VISUALIZE_POINTS)

  return {
    chartType: 'horizontal_bar',
    config: { xAxis: 'label', yAxis: 'value' },
    data: limited,
    note: `Row count distribution by ${categoryOnly}`,
    warning: distribution.length > limited.length
      ? `Showing top ${limited.length.toLocaleString()} categories out of ${distribution.length.toLocaleString()}.`
      : 'No numeric metric detected, so this chart uses row counts.',
  }
}

type WorkspaceTab = 'results' | 'sql' | 'explanation' | 'visualize' | 'chat'

function ExplanationBanner({ explanation }: { explanation: string }) {
  return (
    <div
      className="shrink-0 mb-4 p-4"
      style={{
        background: 'var(--accent-subtle)',
        borderRadius: 'var(--radius-lg)',
        border: '1px solid rgba(77, 104, 235, 0.15)',
      }}
    >
      <div className="flex items-start gap-3">
        <MessageSquareText className="w-4 h-4 mt-0.5 shrink-0" style={{ color: 'var(--accent)' }} />
        <div>
          <p className="text-[11px] font-semibold uppercase tracking-wider mb-1.5" style={{ color: 'var(--accent)' }}>
            AI Analysis
          </p>
          <p className="text-[13px] leading-relaxed" style={{ color: 'var(--text-secondary)' }}>
            {explanation}
          </p>
        </div>
      </div>
    </div>
  )
}

function WorkspaceVisualizationPanel({ result }: { result: QueryResult }) {
  const prepared = useMemo(() => prepareWorkspaceVisualization(result), [result])
  const hasRows = result.rows.length > 0
  const hasColumns = result.columns.length > 0

  if (!hasRows || !hasColumns) {
    return (
      <div className="h-full overflow-y-auto p-6">
        <div
          className="p-6"
          style={{
            borderRadius: 'var(--radius-lg)',
            background: 'var(--bg-surface)',
            border: '1px solid var(--border-subtle)',
          }}
        >
          <p className="text-[11px] font-semibold uppercase tracking-wider mb-3" style={{ color: 'var(--text-tertiary)' }}>
            Visualization
          </p>
          <p className="text-[13px]" style={{ color: 'var(--text-secondary)' }}>
            Query returned no rows to visualize.
          </p>
        </div>
      </div>
    )
  }

  if (!prepared || prepared.data.length === 0) {
    return (
      <div className="h-full overflow-y-auto p-6">
        <div
          className="p-6"
          style={{
            borderRadius: 'var(--radius-lg)',
            background: 'var(--bg-surface)',
            border: '1px solid var(--border-subtle)',
          }}
        >
          <p className="text-[11px] font-semibold uppercase tracking-wider mb-3" style={{ color: 'var(--text-tertiary)' }}>
            Visualization
          </p>
          <p className="text-[13px]" style={{ color: 'var(--text-secondary)' }}>
            This result looks like a detailed record list, so a table is more appropriate than a chart. For visualization, use an aggregate query (for example: COUNT, SUM, AVG with GROUP BY).
          </p>
        </div>
      </div>
    )
  }

  return (
    <div className="h-full overflow-y-auto p-6">
      <div className="space-y-4">
        <div
          className="p-4"
          style={{
            borderRadius: 'var(--radius-lg)',
            background: 'var(--bg-surface)',
            border: '1px solid var(--border-subtle)',
          }}
        >
          <p className="text-[11px] font-semibold uppercase tracking-wider mb-2" style={{ color: 'var(--text-tertiary)' }}>
            Visualization
          </p>
          <p className="text-[13px]" style={{ color: 'var(--text-secondary)' }}>
            {prepared.note}
          </p>
          <p className="text-[12px] mt-2" style={{ color: 'var(--text-tertiary)', fontFamily: 'var(--font-mono)' }}>
            {result.row_count.toLocaleString()} rows · {result.columns.length} columns · {formatMs(result.execution_time_ms)}
          </p>
          {prepared.warning && (
            <p className="text-[11px] mt-2" style={{ color: 'var(--text-muted)' }}>
              {prepared.warning}
            </p>
          )}
        </div>

        <div
          className="p-4"
          style={{
            borderRadius: 'var(--radius-lg)',
            background: 'var(--bg-surface)',
            border: '1px solid var(--border-subtle)',
          }}
        >
          <div className="h-105">
            <ChartRenderer
              chartType={prepared.chartType}
              config={prepared.config}
              data={prepared.data}
            />
          </div>
        </div>
      </div>
    </div>
  )
}

const TABS: { id: WorkspaceTab; label: string; icon: ComponentType<{ className?: string }> }[] = [
  { id: 'results',     label: 'Results',        icon: Table2 },
  { id: 'sql',         label: 'SQL',            icon: Code2 },
  { id: 'explanation', label: 'Explanation',    icon: Sparkles },
  { id: 'visualize',   label: 'Visualize',      icon: BarChart3 },
  { id: 'chat',        label: 'Knowledge Base', icon: MessageSquare },
]

export default function WorkspacePage() {
  const [activeTab, setActiveTab] = useState<WorkspaceTab>('results')
  const [topHeight, setTopHeight] = useState(DEFAULT_TOP)
  const [showContextPicker, setShowContextPicker] = useState(false)
  const [latestQueryHistoryId, setLatestQueryHistoryId] = useState<string | null>(null)
  const [isSavingQuery, setIsSavingQuery] = useState(false)
  const dragStartY = useRef(0)
  const dragStartH = useRef(DEFAULT_TOP)

  const onDragStart = useCallback((e: React.MouseEvent) => {
    e.preventDefault()
    dragStartY.current = e.clientY
    dragStartH.current = topHeight
    const onMove = (mv: MouseEvent) => {
      const next = dragStartH.current + (mv.clientY - dragStartY.current)
      setTopHeight(Math.min(MAX_TOP, Math.max(MIN_TOP, next)))
    }
    const onUp = () => {
      window.removeEventListener('mousemove', onMove)
      window.removeEventListener('mouseup', onUp)
    }
    window.addEventListener('mousemove', onMove)
    window.addEventListener('mouseup', onUp)
  }, [topHeight])

  const {
    prompt,
    generatedSQL,
    explanation,
    result,
    error,
    setGeneratedSQL,
    setExplanation,
    setResult,
    setExecuting,
    setError,
  } = useWorkspaceStore()

  const activeConnectionId = useConnectionStore((s) => s.activeConnectionId)
  const setConnections = useConnectionStore((s) => s.setConnections)
  const setActiveConnection = useConnectionStore((s) => s.setActiveConnection)
  const connections = useConnectionStore((s) => s.connections)
  const activeConn = connections.find((c) => c.connectionId === activeConnectionId) ?? null

  const { data: fetchedConnections } = useQuery({
    queryKey: ['connections'],
    queryFn: connectionsApi.list,
  })

  useEffect(() => {
    if (!fetchedConnections) return
    setConnections(fetchedConnections)
    if (!activeConnectionId && fetchedConnections.length > 0) {
      setActiveConnection(fetchedConnections[0].connectionId)
    }
  }, [fetchedConnections, activeConnectionId, setConnections, setActiveConnection])

  const handleSubmit = async (prompt: string) => {
    if (!activeConnectionId) {
      toast.error('Select a connection first')
      return
    }
    setExecuting(true)
    setError(null)
    try {
      const data = await queriesApi.execute({ connectionId: activeConnectionId, userPrompt: prompt })
      setGeneratedSQL(data.sql)
      setExplanation(data.explanation)
      setResult(data.result)
      setLatestQueryHistoryId(data.queryHistoryId)
      setActiveTab('results')
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Query failed'
      setError(msg)
      setActiveTab('results')
      toast.error(msg)
    } finally {
      setExecuting(false)
    }
  }

  const buildSavedQueryName = (value: string): string => {
    const compact = value.trim().replace(/\s+/g, ' ')
    if (compact) {
      return compact.length > 80 ? `${compact.slice(0, 77)}...` : compact
    }

    const stamp = new Intl.DateTimeFormat('en-US', {
      month: 'short',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      hour12: false,
    }).format(new Date())
    return `Saved query ${stamp}`
  }

  const handleSaveQuery = async () => {
    if (!latestQueryHistoryId) {
      toast.error('Run a query first before saving')
      return
    }

    if (isSavingQuery) return

    setIsSavingQuery(true)
    try {
      await queriesApi.saveQuery({
        name: buildSavedQueryName(prompt),
        queryHistoryId: latestQueryHistoryId,
      })
      toast.success('Query saved to Saved Queries')
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Failed to save query'
      toast.error(msg)
    } finally {
      setIsSavingQuery(false)
    }
  }

  return (
    <TooltipProvider>
      <div className="flex h-full overflow-hidden">

        {/* ── CENTER: prompt → tabs → content ──────────────────────── */}
        <div className="flex-1 flex flex-col overflow-hidden" style={{ background: 'var(--bg-base)' }}>

          {/* Active context bar */}
          <div
            className="px-5 py-3 shrink-0 flex items-center justify-between"
            style={{
              background: 'var(--bg-surface)',
              borderBottom: '1px solid var(--border-subtle)',
            }}
          >
            <div className="flex items-center gap-2">
              <span className="text-[11px] font-semibold uppercase tracking-wider" style={{ color: 'var(--text-tertiary)' }}>
                Context
              </span>
            </div>

            {/* Connection selector */}
            <div className="relative">
              {activeConn ? (
                <>
                  <button
                    type="button"
                    className="flex items-center gap-2.5 px-3 py-1.5 text-[13px] font-medium transition-all"
                    style={{
                      borderRadius: 'var(--radius-md)',
                      background: 'var(--bg-elevated)',
                      border: '1px solid var(--border-default)',
                      color: 'var(--text-primary)',
                    }}
                    onClick={() => setShowContextPicker((v) => !v)}
                  >
                    <span
                      className="w-2 h-2 shrink-0"
                      style={{
                        borderRadius: 'var(--radius-pill)',
                        background: activeConn.isHealthy ? 'var(--success)' : 'var(--danger)',
                        boxShadow: activeConn.isHealthy
                          ? '0 0 6px rgba(52, 211, 153, 0.4)'
                          : '0 0 6px rgba(248, 113, 113, 0.4)',
                      }}
                    />
                    <span>{activeConn.connectionName}</span>
                    {activeConn.isHealthy ? (
                      <Wifi className="w-3 h-3" style={{ color: 'var(--success)' }} />
                    ) : (
                      <WifiOff className="w-3 h-3" style={{ color: 'var(--danger)' }} />
                    )}
                    <ChevronDown className={cn('w-3.5 h-3.5 transition-transform', showContextPicker && 'rotate-180')} style={{ color: 'var(--text-tertiary)' }} />
                  </button>

                  {showContextPicker && (
                    <div
                      className="absolute right-0 top-full mt-2 w-70 overflow-hidden z-50"
                      style={{
                        borderRadius: 'var(--radius-lg)',
                        background: 'var(--bg-elevated)',
                        border: '1px solid var(--border-default)',
                        boxShadow: '0 16px 48px rgba(0,0,0,0.4), 0 0 1px rgba(0,0,0,0.3)',
                      }}
                    >
                      <div className="p-2">
                        {connections.map((connection) => {
                          const selected = connection.connectionId === activeConnectionId
                          return (
                            <button
                              key={connection.connectionId}
                              type="button"
                              className="w-full flex items-center gap-2.5 px-3 py-2.5 text-left text-[13px] font-medium transition-colors"
                              style={{
                                borderRadius: 'var(--radius-md)',
                                color: selected ? 'var(--text-primary)' : 'var(--text-secondary)',
                                background: selected ? 'var(--accent-subtle)' : 'transparent',
                              }}
                              onClick={() => {
                                setActiveConnection(connection.connectionId)
                                setShowContextPicker(false)
                              }}
                              onMouseEnter={(e) => {
                                if (!selected) e.currentTarget.style.background = 'var(--bg-hover)'
                              }}
                              onMouseLeave={(e) => {
                                if (!selected) e.currentTarget.style.background = 'transparent'
                              }}
                            >
                              <span
                                className="w-2 h-2 shrink-0"
                                style={{
                                  borderRadius: 'var(--radius-pill)',
                                  background: connection.isHealthy ? 'var(--success)' : 'var(--danger)',
                                }}
                              />
                              <span className="flex-1 truncate">{connection.connectionName}</span>
                              {selected && <Check className="w-4 h-4" style={{ color: 'var(--accent)' }} />}
                            </button>
                          )
                        })}
                      </div>
                    </div>
                  )}
                </>
              ) : (
                <div
                  className="flex items-center gap-2 px-3 py-1.5"
                  style={{
                    borderRadius: 'var(--radius-md)',
                    background: 'var(--bg-elevated)',
                    border: '1px solid var(--border-subtle)',
                    color: 'var(--text-tertiary)',
                  }}
                >
                  <span className="w-2 h-2 shrink-0" style={{ borderRadius: 'var(--radius-pill)', background: 'var(--text-muted)' }} />
                  <span className="text-[13px]">No connection</span>
                </div>
              )}
            </div>
          </div>

          {/* Prompt input — height controlled by drag handle */}
          <div
            style={{
              height: topHeight,
              background: 'var(--bg-surface)',
              borderBottom: '1px solid var(--border-subtle)',
            }}
            className="shrink-0 flex flex-col justify-end overflow-hidden"
          >
            <div className="px-5 pb-4">
              <NLPromptInput onSubmit={handleSubmit} />
            </div>
          </div>

          {/* Drag handle */}
          <div
            onMouseDown={onDragStart}
            className="h-2 shrink-0 flex items-center justify-center cursor-ns-resize transition-colors group"
            style={{ background: 'var(--bg-base)' }}
            onMouseEnter={(e) => { e.currentTarget.style.background = 'var(--accent-subtle)' }}
            onMouseLeave={(e) => { e.currentTarget.style.background = 'var(--bg-base)' }}
          >
            <GripHorizontal className="w-4 h-4 transition-colors" style={{ color: 'var(--text-muted)' }} />
          </div>

          {/* Tab bar */}
          <div
            className="h-11 px-5 flex items-center justify-between shrink-0"
            style={{
              background: 'var(--bg-surface)',
              borderBottom: '1px solid var(--border-subtle)',
            }}
          >
            <div className="flex items-center h-full gap-1">
              {TABS.map(({ id, label, icon: Icon }) => (
                <button
                  key={id}
                  onClick={() => setActiveTab(id)}
                  className="h-full px-3 text-[12px] font-semibold flex items-center gap-2 transition-all border-b-2"
                  style={{
                    borderColor: activeTab === id ? 'var(--accent)' : 'transparent',
                    color: activeTab === id ? 'var(--text-primary)' : 'var(--text-tertiary)',
                  }}
                  onMouseEnter={(e) => {
                    if (activeTab !== id) e.currentTarget.style.color = 'var(--text-secondary)'
                  }}
                  onMouseLeave={(e) => {
                    if (activeTab !== id) e.currentTarget.style.color = 'var(--text-tertiary)'
                  }}
                >
                  <Icon className="w-3.5 h-3.5" />
                  {label}
                </button>
              ))}
            </div>

            {/* Status badge */}
            {result && (
              <div
                className="flex items-center gap-3 text-[11px] px-3 py-1.5"
                style={{
                  borderRadius: 'var(--radius-md)',
                  background: 'var(--success-subtle)',
                  color: 'var(--success)',
                  fontFamily: 'var(--font-mono)',
                  fontWeight: 500,
                }}
              >
                <span className="flex items-center gap-1.5">
                  <CheckCircle2 className="w-3 h-3" />
                  <span>{result.row_count.toLocaleString()} rows</span>
                </span>
                <span style={{ color: 'var(--border-strong)' }}>·</span>
                <span className="flex items-center gap-1" style={{ color: 'var(--text-tertiary)' }}>
                  <Clock className="w-3 h-3" />
                  {formatMs(result.execution_time_ms)}
                </span>
              </div>
            )}
          </div>

          {/* Tab content */}
          <div className="flex-1 min-h-0 overflow-hidden">

            {/* Results */}
            {activeTab === 'results' && (
              <div className="h-full min-h-0 overflow-hidden p-5 flex flex-col">
                {error ? (
                  <div
                    className="flex items-start gap-3 px-4 py-3"
                    style={{
                      borderRadius: 'var(--radius-lg)',
                      background: 'var(--danger-subtle)',
                      border: '1px solid rgba(248, 113, 113, 0.15)',
                    }}
                  >
                    <AlertCircle className="w-4 h-4 mt-0.5 shrink-0" style={{ color: 'var(--danger)' }} />
                    <p className="text-[13px]" style={{ color: 'var(--danger)' }}>{error}</p>
                  </div>
                ) : result ? (
                  <>
                    {explanation && <ExplanationBanner explanation={explanation} />}
                    <ResultsTable
                      result={result}
                      onVisualize={() => setActiveTab('visualize')}
                      onSaveQuery={latestQueryHistoryId ? () => void handleSaveQuery() : undefined}
                      isSavingQuery={isSavingQuery}
                    />
                  </>
                ) : (
                  <div className="flex flex-col items-center justify-center h-full text-center select-none">
                    <div
                      className="w-14 h-14 flex items-center justify-center mb-5"
                      style={{
                        borderRadius: 'var(--radius-xl)',
                        background: 'var(--accent-subtle)',
                      }}
                    >
                      <Sparkles className="w-6 h-6" style={{ color: 'var(--accent)' }} />
                    </div>
                    <p className="text-[13px] max-w-xs" style={{ color: 'var(--text-tertiary)' }}>
                      Ask a question in plain English and SqlSpace will generate and run the SQL for you.
                    </p>
                  </div>
                )}
              </div>
            )}

            {/* SQL */}
            {activeTab === 'sql' && (
              <div className="h-full min-h-0 overflow-hidden p-5 flex flex-col gap-4">
                {generatedSQL ? (
                  <div className="shrink-0 space-y-4">
                    <SQLPreview sql={generatedSQL} readOnly />
                    {explanation && <ExplanationBanner explanation={explanation} />}
                  </div>
                ) : (
                  <div
                    className="p-8 text-center"
                    style={{
                      borderRadius: 'var(--radius-lg)',
                      background: 'var(--bg-surface)',
                      border: '1px solid var(--border-subtle)',
                    }}
                  >
                    <div
                      className="w-12 h-12 flex items-center justify-center mx-auto mb-3"
                      style={{
                        borderRadius: 'var(--radius-lg)',
                        background: 'var(--bg-elevated)',
                      }}
                    >
                      <Code2 className="w-6 h-6" style={{ color: 'var(--text-muted)' }} />
                    </div>
                    <p className="text-[13px]" style={{ color: 'var(--text-tertiary)' }}>Generated SQL will appear here</p>
                  </div>
                )}

                <div className="flex-1 min-h-0 overflow-hidden">
                  {error ? (
                    <div
                      className="flex items-start gap-3 px-4 py-3"
                      style={{
                        borderRadius: 'var(--radius-lg)',
                        background: 'var(--danger-subtle)',
                        border: '1px solid rgba(248, 113, 113, 0.15)',
                      }}
                    >
                      <AlertCircle className="w-4 h-4 mt-0.5 shrink-0" style={{ color: 'var(--danger)' }} />
                      <p className="text-[13px]" style={{ color: 'var(--danger)' }}>{error}</p>
                    </div>
                  ) : result ? (
                    <ResultsTable
                      result={result}
                      onVisualize={() => setActiveTab('visualize')}
                      onSaveQuery={latestQueryHistoryId ? () => void handleSaveQuery() : undefined}
                      isSavingQuery={isSavingQuery}
                    />
                  ) : (
                    <div className="flex flex-col items-center justify-center h-full text-center select-none">
                      <div
                        className="w-14 h-14 flex items-center justify-center mb-5"
                        style={{
                          borderRadius: 'var(--radius-xl)',
                          background: 'var(--accent-subtle)',
                        }}
                      >
                        <Sparkles className="w-6 h-6" style={{ color: 'var(--accent)' }} />
                      </div>
                      <p className="text-[13px] max-w-xs" style={{ color: 'var(--text-tertiary)' }}>
                        Ask a question in plain English and SqlSpace will generate and run the SQL for you.
                      </p>
                    </div>
                  )}
                </div>
              </div>
            )}

            {/* Explanation */}
            {activeTab === 'explanation' && (
              <div className="h-full overflow-y-auto p-6">
                {explanation ? (
                  <div>
                    <p className="text-[11px] font-semibold uppercase tracking-wider mb-3" style={{ color: 'var(--text-tertiary)' }}>
                      AI Explanation
                    </p>
                    <div
                      className="p-6 mb-4"
                      style={{
                        borderRadius: 'var(--radius-lg)',
                        background: 'var(--bg-surface)',
                        border: '1px solid var(--border-subtle)',
                      }}
                    >
                      <p className="text-[14px] leading-relaxed" style={{ color: 'var(--text-secondary)' }}>
                        {explanation}
                      </p>
                    </div>
                    <div
                      className="p-4 flex items-start gap-3"
                      style={{
                        borderRadius: 'var(--radius-lg)',
                        background: 'var(--accent-subtle)',
                        border: '1px solid rgba(77, 104, 235, 0.15)',
                      }}
                    >
                      <Sparkles className="w-4 h-4 shrink-0 mt-0.5" style={{ color: 'var(--accent)' }} />
                      <p className="text-[12px] leading-relaxed" style={{ color: 'var(--accent-hover)' }}>
                        This explanation was generated by AI. Always verify SQL results before using them in production.
                      </p>
                    </div>
                  </div>
                ) : (
                  <div className="flex flex-col items-center justify-center h-full text-center select-none">
                    <div
                      className="w-14 h-14 flex items-center justify-center mb-5"
                      style={{
                        borderRadius: 'var(--radius-xl)',
                        background: 'var(--bg-surface)',
                        border: '1px solid var(--border-subtle)',
                      }}
                    >
                      <Sparkles className="w-6 h-6" style={{ color: 'var(--text-muted)' }} />
                    </div>
                    <p className="text-[13px]" style={{ color: 'var(--text-tertiary)' }}>
                      AI explanation will appear here after running a query
                    </p>
                  </div>
                )}
              </div>
            )}

            {/* Knowledge Base chat */}
            {activeTab === 'chat' && (
              <RagChatPanel connectionId={activeConnectionId} />
            )}

            {/* Visualize */}
            {activeTab === 'visualize' && (
              result ? (
                <WorkspaceVisualizationPanel result={result} />
              ) : (
                <div className="h-full overflow-y-auto p-6">
                  <div className="flex flex-col items-center justify-center h-full text-center select-none">
                    <div
                      className="w-14 h-14 flex items-center justify-center mb-5"
                      style={{
                        borderRadius: 'var(--radius-xl)',
                        background: 'var(--bg-surface)',
                        border: '1px solid var(--border-subtle)',
                      }}
                    >
                      <BarChart3 className="w-6 h-6" style={{ color: 'var(--text-muted)' }} />
                    </div>
                    <p className="text-[13px]" style={{ color: 'var(--text-tertiary)' }}>
                      Run a query first to visualize results
                    </p>
                  </div>
                </div>
              )
            )}
          </div>
        </div>

        {/* ── RIGHT: schema panel ────────────────────────────────────── */}
        <SchemaPanel />
      </div>
    </TooltipProvider>
  )
}
