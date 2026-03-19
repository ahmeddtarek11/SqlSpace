import { useState, useRef, useCallback, useEffect, type ComponentType } from 'react'
import { useQuery } from '@tanstack/react-query'
import { toast } from 'sonner'
import { TooltipProvider } from '@/components/ui/tooltip'
import { NLPromptInput } from '@/components/workspace/NLPromptInput'
import { SQLPreview } from '@/components/workspace/SQLPreview'
import { ResultsTable } from '@/components/workspace/ResultsTable'
import { SchemaPanel } from '@/components/workspace/SchemaPanel'
import { connectionsApi } from '@/api/connections'
import { queriesApi } from '@/api/queries'
import { useWorkspaceStore } from '@/stores/workspace-store'
import { useConnectionStore } from '@/stores/connection-store'
import { AlertCircle, Table2, Code2, Sparkles, BarChart3, CheckCircle2, Clock, GripHorizontal, Wifi, WifiOff, ChevronDown, Check } from 'lucide-react'
import { cn } from '@/lib/utils'
import { formatMs } from '@/lib/utils'

const MIN_TOP = 120
const MAX_TOP = 380
const DEFAULT_TOP = 170

type WorkspaceTab = 'results' | 'sql' | 'explanation' | 'visualize'

const TABS: { id: WorkspaceTab; label: string; icon: ComponentType<{ className?: string }> }[] = [
  { id: 'results',     label: 'Results',     icon: Table2 },
  { id: 'sql',         label: 'SQL',         icon: Code2 },
  { id: 'explanation', label: 'Explanation', icon: Sparkles },
  { id: 'visualize',   label: 'Visualize',   icon: BarChart3 },
]

export default function WorkspacePage() {
  const [activeTab, setActiveTab] = useState<WorkspaceTab>('results')
  const [topHeight, setTopHeight] = useState(DEFAULT_TOP)
  const [showContextPicker, setShowContextPicker] = useState(false)
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

  return (
    <TooltipProvider>
      <div className="flex h-full overflow-hidden">

        {/* ── CENTER: prompt → tabs → content ────────────────────────────── */}
        <div className="flex-1 flex flex-col overflow-hidden bg-[#080809]">

          {/* Active context moved from sidebar */}
          <div className="px-4 pt-3 pb-2 border-b border-white/10 bg-[#0d0d0f] shrink-0 relative">
            <p className="text-[10px] font-medium text-zinc-500 uppercase tracking-wider mb-2">
              Active Context
            </p>
            {activeConn ? (
              <>
                <button
                  type="button"
                  className="w-full flex items-center gap-2 px-2.5 py-2 bg-white/5 rounded-lg border border-white/10 hover:bg-white/10 transition-colors"
                  onClick={() => setShowContextPicker((v) => !v)}
                >
                  <span
                    className={cn(
                      'w-1.5 h-1.5 rounded-full shrink-0',
                      activeConn.isHealthy
                        ? 'bg-green-500 shadow-[0_0_6px_rgba(34,197,94,0.5)]'
                        : 'bg-red-500'
                    )}
                  />
                  <span className="text-xs font-medium text-zinc-200 flex-1 truncate text-left">
                    {activeConn.connectionName}
                  </span>
                  {activeConn.isHealthy ? (
                    <Wifi className="w-3 h-3 text-green-400 shrink-0" />
                  ) : (
                    <WifiOff className="w-3 h-3 text-red-400 shrink-0" />
                  )}
                  <ChevronDown className={cn('w-3.5 h-3.5 text-zinc-500 transition-transform', showContextPicker && 'rotate-180')} />
                </button>

                {showContextPicker && (
                  <div className="mt-2 rounded-lg border border-white/10 bg-[#111113] overflow-hidden">
                    {connections.map((connection) => {
                      const selected = connection.connectionId === activeConnectionId
                      return (
                        <button
                          key={connection.connectionId}
                          type="button"
                          className={cn(
                            'w-full flex items-center gap-2 px-3 py-2 text-left text-xs border-b border-white/5 last:border-b-0 transition-colors',
                            selected ? 'bg-sky-500/10 text-sky-300' : 'text-zinc-300 hover:bg-white/5'
                          )}
                          onClick={() => {
                            setActiveConnection(connection.connectionId)
                            setShowContextPicker(false)
                          }}
                        >
                          <span className={cn('w-1.5 h-1.5 rounded-full shrink-0', connection.isHealthy ? 'bg-green-500' : 'bg-red-500')} />
                          <span className="flex-1 truncate">{connection.connectionName}</span>
                          {selected && <Check className="w-3.5 h-3.5 text-sky-400" />}
                        </button>
                      )
                    })}
                  </div>
                )}
              </>
            ) : (
              <div className="flex items-center gap-2 px-2.5 py-2 bg-white/5 rounded-lg border border-white/10">
                <span className="w-1.5 h-1.5 rounded-full bg-zinc-600 shrink-0" />
                <span className="text-xs text-zinc-600">No connection selected</span>
              </div>
            )}
          </div>

          {/* Prompt input — height controlled by drag handle */}
          <div
            style={{ height: topHeight }}
            className="shrink-0 border-b border-white/10 bg-[#0d0d0f] flex flex-col justify-end overflow-hidden"
          >
            <div className="px-4 pb-4">
              <NLPromptInput onSubmit={handleSubmit} />
            </div>
          </div>

          {/* Tab bar */}
          <div className="h-12 border-b border-white/10 px-4 flex items-center justify-between bg-[#111113] shrink-0">
            <div className="flex items-center h-full">
              {TABS.map(({ id, label, icon: Icon }) => (
                <button
                  key={id}
                  onClick={() => setActiveTab(id)}
                  className={`h-full px-4 text-sm font-medium border-b-2 flex items-center gap-2 transition-colors ${
                    activeTab === id
                      ? 'border-sky-500 text-sky-400'
                      : 'border-transparent text-zinc-400 hover:text-zinc-200 hover:bg-white/5'
                  }`}
                >
                  <Icon className="w-3.5 h-3.5" />
                  {label}
                </button>
              ))}
            </div>

            {/* Status badge */}
            {result && (
              <div className="flex items-center gap-3 text-xs text-zinc-500 bg-[#18181b] border border-white/5 px-3 py-1.5 rounded-lg">
                <span className="flex items-center gap-1.5">
                  <CheckCircle2 className="w-3 h-3 text-green-400" />
                  <span className="text-green-400 font-medium">{result.row_count.toLocaleString()} rows</span>
                </span>
                <span className="text-zinc-700">·</span>
                <span className="flex items-center gap-1">
                  <Clock className="w-3 h-3" />
                  {formatMs(result.execution_time_ms)}
                </span>
              </div>
            )}
          </div>

          {/* Drag handle — resize the prompt area vs content area */}
          <div
            onMouseDown={onDragStart}
            className="h-2.5 shrink-0 flex items-center justify-center cursor-ns-resize bg-[#0d0d0f] hover:bg-sky-500/10 transition-colors group"
          >
            <GripHorizontal className="w-4 h-4 text-zinc-700 group-hover:text-sky-400 transition-colors" />
          </div>

          {/* Tab content */}
          <div className="flex-1 min-h-0 overflow-hidden">

            {/* Results */}
            {activeTab === 'results' && (
              <div className="h-full min-h-0 overflow-hidden p-4 flex flex-col">
                {error ? (
                  <div className="flex items-start gap-2 rounded-xl border border-red-500/30 bg-red-500/10 px-4 py-3">
                    <AlertCircle className="w-4 h-4 text-red-400 mt-0.5 shrink-0" />
                    <p className="text-sm text-red-300">{error}</p>
                  </div>
                ) : result ? (
                  <ResultsTable result={result} />
                ) : (
                  <div className="flex flex-col items-center justify-center h-full text-center select-none">
                    <div className="w-16 h-16 rounded-2xl bg-sky-500/10 border border-sky-500/20 flex items-center justify-center mb-4">
                      <span className="text-2xl">✦</span>
                    </div>
                    <p className="text-zinc-600 text-sm max-w-xs">
                      Ask a question in plain English and SqlSpace will generate and run the SQL for you.
                    </p>
                  </div>
                )}
              </div>
            )}

            {/* SQL */}
            {activeTab === 'sql' && (
              <div className="h-full min-h-0 overflow-hidden p-4 flex flex-col gap-3">
                {generatedSQL ? (
                  <div className="shrink-0">
                    <SQLPreview sql={generatedSQL} readOnly />
                  </div>
                ) : (
                  <div className="shrink-0 rounded-xl border border-white/10 bg-[#111113] p-6 text-center">
                    <div className="w-12 h-12 rounded-xl bg-[#18181b] border border-white/5 flex items-center justify-center mx-auto mb-3">
                      <Code2 className="w-6 h-6 text-zinc-600" />
                    </div>
                    <p className="text-zinc-600 text-sm">Generated SQL will appear here</p>
                  </div>
                )}

                <div className="flex-1 min-h-0 overflow-hidden">
                  {error ? (
                    <div className="flex items-start gap-2 rounded-xl border border-red-500/30 bg-red-500/10 px-4 py-3">
                      <AlertCircle className="w-4 h-4 text-red-400 mt-0.5 shrink-0" />
                      <p className="text-sm text-red-300">{error}</p>
                    </div>
                  ) : result ? (
                    <ResultsTable result={result} />
                  ) : (
                    <div className="flex flex-col items-center justify-center h-full text-center select-none">
                      <div className="w-16 h-16 rounded-2xl bg-sky-500/10 border border-sky-500/20 flex items-center justify-center mb-4">
                        <span className="text-2xl">✦</span>
                      </div>
                      <p className="text-zinc-600 text-sm max-w-xs">
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
                    <p className="text-xs font-medium text-zinc-500 uppercase tracking-wider mb-3">AI Explanation</p>
                    <div className="bg-[#111113] border border-white/10 rounded-xl p-6 mb-4">
                      <p className="text-sm text-zinc-300 leading-relaxed">{explanation}</p>
                    </div>
                    <div className="p-4 bg-sky-500/10 border border-sky-500/20 rounded-xl flex items-start gap-3">
                      <Sparkles className="w-4 h-4 text-sky-400 shrink-0 mt-0.5" />
                      <p className="text-xs text-sky-300 leading-relaxed">
                        This explanation was generated by AI. Always verify SQL results before using them in production.
                      </p>
                    </div>
                  </div>
                ) : (
                  <div className="flex flex-col items-center justify-center h-full text-center select-none">
                    <div className="w-16 h-16 rounded-2xl bg-[#111113] border border-white/5 flex items-center justify-center mb-4">
                      <Sparkles className="w-7 h-7 text-zinc-600" />
                    </div>
                    <p className="text-zinc-600 text-sm">AI explanation will appear here after running a query</p>
                  </div>
                )}
              </div>
            )}

            {/* Visualize */}
            {activeTab === 'visualize' && (
              <div className="h-full overflow-y-auto p-6">
                {result ? (
                  <div className="bg-[#111113] border border-white/10 rounded-xl p-6">
                    <p className="text-xs font-medium text-zinc-500 uppercase tracking-wider mb-3">Visualization</p>
                    <p className="text-sm text-zinc-300 mb-2">
                      Visualization view is ready for chart components.
                    </p>
                    <p className="text-xs text-zinc-500">
                      Current result set: {result.row_count.toLocaleString()} rows, {result.columns.length} columns.
                    </p>
                  </div>
                ) : (
                  <div className="flex flex-col items-center justify-center h-full text-center select-none">
                    <div className="w-16 h-16 rounded-2xl bg-[#111113] border border-white/5 flex items-center justify-center mb-4">
                      <BarChart3 className="w-7 h-7 text-zinc-600" />
                    </div>
                    <p className="text-zinc-600 text-sm">Run a query first to visualize results</p>
                  </div>
                )}
              </div>
            )}
          </div>
        </div>

        {/* ── RIGHT: schema panel ─────────────────────────────────────────── */}
        <SchemaPanel />
      </div>
    </TooltipProvider>
  )
}
