import { useState, useRef, useCallback, useEffect, type ComponentType } from 'react'
import { useQuery } from '@tanstack/react-query'
import { toast } from 'sonner'
import { TooltipProvider } from '@/components/ui/tooltip'
import { NLPromptInput } from '@/components/workspace/NLPromptInput'
import { SQLPreview } from '@/components/workspace/SQLPreview'
import { ResultsTable } from '@/components/workspace/ResultsTable'
import { SchemaPanel } from '@/components/workspace/SchemaPanel'
import { RagChatPanel } from '@/components/workspace/RagChatPanel'
import { connectionsApi } from '@/api/connections'
import { queriesApi } from '@/api/queries'
import { useWorkspaceStore } from '@/stores/workspace-store'
import { useConnectionStore } from '@/stores/connection-store'
import { AlertCircle, Table2, Code2, Sparkles, BarChart3, CheckCircle2, Clock, GripHorizontal, Wifi, WifiOff, ChevronDown, Check, MessageSquareText, MessageSquare } from 'lucide-react'
import { cn } from '@/lib/utils'
import { formatMs } from '@/lib/utils'

const MIN_TOP = 120
const MAX_TOP = 380
const DEFAULT_TOP = 180

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
                      className="absolute right-0 top-full mt-2 w-[280px] overflow-hidden z-50"
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
                    <ResultsTable result={result} />
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
                    <ResultsTable result={result} />
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
              <div className="h-full overflow-y-auto p-6">
                {result ? (
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
                    <p className="text-[13px] mb-2" style={{ color: 'var(--text-secondary)' }}>
                      Visualization view is ready for chart components.
                    </p>
                    <p className="text-[12px]" style={{ color: 'var(--text-tertiary)', fontFamily: 'var(--font-mono)' }}>
                      Current result set: {result.row_count.toLocaleString()} rows, {result.columns.length} columns.
                    </p>
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
                      <BarChart3 className="w-6 h-6" style={{ color: 'var(--text-muted)' }} />
                    </div>
                    <p className="text-[13px]" style={{ color: 'var(--text-tertiary)' }}>
                      Run a query first to visualize results
                    </p>
                  </div>
                )}
              </div>
            )}
          </div>
        </div>

        {/* ── RIGHT: schema panel ────────────────────────────────────── */}
        <SchemaPanel />
      </div>
    </TooltipProvider>
  )
}
