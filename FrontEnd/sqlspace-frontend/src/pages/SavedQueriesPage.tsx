import { useState, useRef, useEffect, useMemo } from 'react'
import { useQuery, useQueries, useMutation, useQueryClient } from '@tanstack/react-query'
import { motion, AnimatePresence } from 'framer-motion'
import { useNavigate } from 'react-router-dom'
import { toast } from 'sonner'
import {
  Bookmark, Trash2, Play, Search, Pencil, Check, X,
  Code2, Sparkles, Clock, Database, CheckCircle2, XCircle, ExternalLink,
} from 'lucide-react'
import { Button } from '@/components/ui/button'
import { AskAiButton } from '@/components/ui/ask-ai-button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import { queriesApi } from '@/api/queries'
import { accessApi } from '@/api/insights'
import { useWorkspaceStore } from '@/stores/workspace-store'
import { useConnectionStore } from '@/stores/connection-store'
import { formatDate, truncate, formatMs, formatSqlForDisplay, formatSqlSingleLineForDisplay } from '@/lib/utils'
import { ingestArtifactForAskAi, parseRowsFromResultsJson } from '@/lib/ask-ai'
import type { SavedQueryDto } from '@/types'

// ── Detail Modal ────────────────────────────────────────────────────────────────
function QueryDetailModal({ query, onClose }: { query: SavedQueryDto; onClose: () => void }) {
  const navigate = useNavigate()
  const qc = useQueryClient()
  const { setPrompt, setGeneratedSQL, setExplanation, setResult } = useWorkspaceStore()
  const { setActiveConnection } = useConnectionStore()
  const [activeTab, setActiveTab] = useState<'sql' | 'explain'>('sql')

  // Close on Escape
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => { if (e.key === 'Escape') onClose() }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [onClose])

  const { data: detail, isLoading: detailLoading } = useQuery({
    queryKey: ['history-detail', query.queryHistoryId],
    queryFn: () => queriesApi.historyById(query.queryHistoryId!),
    enabled: !!query.queryHistoryId,
  })

  const executeMutation = useMutation({
    mutationFn: () => queriesApi.executeSaved(query.id),
    onSuccess: (res) => {
      setPrompt(query.userPrompt ?? '')
      setGeneratedSQL(res.sql)
      setExplanation(res.explanation)
      setResult(res.result)
      setActiveConnection(query.connectionId)
      void qc.invalidateQueries({ queryKey: ['history'] })
      toast.success('Query loaded in workspace')
      navigate('/workspace')
    },
    onError: (e: Error) => toast.error(e.message),
  })

  const isSuccess = detail?.status === 'Success'

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4"
      onClick={onClose}
    >
      <motion.div
        initial={{ opacity: 0, scale: 0.97, y: 8 }}
        animate={{ opacity: 1, scale: 1, y: 0 }}
        exit={{ opacity: 0, scale: 0.97, y: 8 }}
        transition={{ duration: 0.15 }}
        className="bg-[#111113] border border-white/10 rounded-2xl w-full max-w-2xl max-h-[85vh] flex flex-col shadow-2xl"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header */}
        <div className="flex items-center justify-between px-5 py-4 border-b border-white/10 shrink-0">
          <div className="flex items-center gap-2 min-w-0">
            <Bookmark className="w-4 h-4 text-sky-400 shrink-0" />
            <h2 className="text-sm font-semibold text-white truncate">{query.name ?? 'Untitled Query'}</h2>
            {query.connectionName && (
              <Badge variant="outline" className="text-[10px] bg-white/5 text-zinc-400 border-white/10 shrink-0">
                <Database className="w-2.5 h-2.5 mr-1" />{query.connectionName}
              </Badge>
            )}
          </div>
          <button
            onClick={onClose}
            className="p-1 text-zinc-500 hover:text-zinc-200 hover:bg-white/5 rounded-md transition-colors shrink-0 ml-2"
          >
            <X className="w-4 h-4" />
          </button>
        </div>

        {/* Scrollable body */}
        <div className="flex-1 min-h-0 overflow-y-auto p-5 space-y-4">

          {/* Prompt */}
          {query.userPrompt && (
            <div>
              <p className="text-[10px] font-medium text-zinc-500 uppercase tracking-wider mb-1.5">Prompt</p>
              <p className="text-sm text-zinc-300 leading-relaxed">{query.userPrompt}</p>
            </div>
          )}

          {/* Stats — from history detail */}
          {detailLoading && (
            <div className="grid grid-cols-3 gap-3">
              {[1, 2, 3].map((i) => <Skeleton key={i} className="h-14 rounded-xl bg-white/5" />)}
            </div>
          )}
          {detail && (
            <div className="grid grid-cols-3 gap-3">
              <div className="bg-[#18181b] border border-white/5 rounded-xl px-3 py-2.5">
                <p className="text-[10px] text-zinc-500 uppercase tracking-wider mb-0.5">Status</p>
                <div className="flex items-center gap-1.5">
                  {isSuccess
                    ? <CheckCircle2 className="w-3.5 h-3.5 text-green-400" />
                    : <XCircle className="w-3.5 h-3.5 text-red-400" />}
                  <span className={`text-xs font-medium ${isSuccess ? 'text-green-400' : 'text-red-400'}`}>
                    {detail.status}
                  </span>
                </div>
              </div>
              <div className="bg-[#18181b] border border-white/5 rounded-xl px-3 py-2.5">
                <p className="text-[10px] text-zinc-500 uppercase tracking-wider mb-0.5">Rows</p>
                <p className="text-xs font-medium text-zinc-200 font-mono">{detail.rowsReturned?.toLocaleString() ?? '—'}</p>
              </div>
              <div className="bg-[#18181b] border border-white/5 rounded-xl px-3 py-2.5">
                <p className="text-[10px] text-zinc-500 uppercase tracking-wider mb-0.5">Time</p>
                <div className="flex items-center gap-1">
                  <Clock className="w-3 h-3 text-zinc-500" />
                  <p className="text-xs font-medium text-zinc-200 font-mono">
                    {detail.executionTimeMs != null ? formatMs(detail.executionTimeMs) : '—'}
                  </p>
                </div>
              </div>
            </div>
          )}

          {/* SQL / Explanation tabs */}
          <div className="bg-[#0d0d0f] border border-white/5 rounded-xl overflow-hidden">
            <div className="flex border-b border-white/5 px-1 pt-1">
              <button
                onClick={() => setActiveTab('sql')}
                className={`flex items-center gap-1.5 px-3 py-2 text-xs font-medium rounded-t-lg border-b-2 transition-colors ${
                  activeTab === 'sql'
                    ? 'border-sky-500 text-sky-400 bg-sky-500/5'
                    : 'border-transparent text-zinc-500 hover:text-zinc-300'
                }`}
              >
                <Code2 className="w-3.5 h-3.5" /> SQL
              </button>
              {(detail?.llmResponse || query.queryHistoryId) && (
                <button
                  onClick={() => setActiveTab('explain')}
                  className={`flex items-center gap-1.5 px-3 py-2 text-xs font-medium rounded-t-lg border-b-2 transition-colors ${
                    activeTab === 'explain'
                      ? 'border-sky-500 text-sky-400 bg-sky-500/5'
                      : 'border-transparent text-zinc-500 hover:text-zinc-300'
                  }`}
                >
                  <Sparkles className="w-3.5 h-3.5" /> Explanation
                </button>
              )}
            </div>
            <div className="p-4 max-h-64 overflow-y-auto">
              {activeTab === 'sql' && (
                <pre className="font-mono text-xs text-cyan-600 leading-relaxed whitespace-pre-wrap break-all">
                  {query.generatedSql ? formatSqlForDisplay(query.generatedSql) : 'No SQL available'}
                </pre>
              )}
              {activeTab === 'explain' && (
                detailLoading
                  ? <p className="text-xs text-zinc-500 italic">Loading explanation…</p>
                  : <p className="text-xs text-zinc-300 leading-relaxed">
                      {detail?.llmResponse ?? 'No explanation available.'}
                    </p>
              )}
            </div>
          </div>

          {/* Dates */}
          <div className="flex items-center gap-4 text-[10px] text-zinc-600">
            <span>Saved {formatDate(query.createdAtUtc)}</span>
            {query.updatedAtUtc !== query.createdAtUtc && (
              <span>Updated {formatDate(query.updatedAtUtc)}</span>
            )}
          </div>
        </div>

        {/* Footer */}
        <div className="flex items-center justify-between gap-2 px-5 py-4 border-t border-white/10 shrink-0">
          <Button
            variant="outline"
            size="sm"
            className="border-white/10 text-zinc-400 hover:text-white hover:bg-white/5 gap-1.5"
            onClick={onClose}
          >
            Close
          </Button>
          <Button
            size="sm"
            className="bg-sky-600 hover:bg-sky-500 text-white shadow-lg shadow-sky-500/25 gap-1.5 active:scale-[0.98] transition-all disabled:opacity-50"
            disabled={executeMutation.isPending}
            onClick={() => executeMutation.mutate()}
          >
            <Play className="w-3.5 h-3.5" />
            {executeMutation.isPending ? 'Running…' : 'Run in Workspace'}
          </Button>
        </div>
      </motion.div>
    </div>
  )
}

// ── Card ───────────────────────────────────────────────────────────────────────
function SavedCard({
  query,
  onOpen,
  onDelete,
  onAskAi,
  isAskingAi,
  canAskAi,
}: {
  query: SavedQueryDto
  onOpen: () => void
  onDelete: () => void
  onAskAi: () => void
  isAskingAi: boolean
  canAskAi: boolean
}) {
  const navigate = useNavigate()
  const { setPrompt, setGeneratedSQL, setExplanation, setResult } = useWorkspaceStore()
  const { setActiveConnection } = useConnectionStore()
  const [editing, setEditing] = useState(false)
  const [nameInput, setNameInput] = useState(query.name ?? '')
  const inputRef = useRef<HTMLInputElement>(null)
  const qc = useQueryClient()

  const renameMutation = useMutation({
    mutationFn: (name: string) => queriesApi.renameSaved(query.id, name),
    onSuccess: (updated) => {
      void qc.invalidateQueries({ queryKey: ['saved-queries'] })
      toast.success('Query renamed')
      setEditing(false)
      setNameInput(updated.name ?? '')
    },
    onError: (e: Error) => toast.error(e.message),
  })

  const executeMutation = useMutation({
    mutationFn: () => queriesApi.executeSaved(query.id),
    onSuccess: (res) => {
      setPrompt(query.userPrompt ?? '')
      setGeneratedSQL(res.sql)
      setExplanation(res.explanation)
      setResult(res.result)
      setActiveConnection(query.connectionId)
      void qc.invalidateQueries({ queryKey: ['history'] })
      toast.success('Query executed')
      navigate('/workspace')
    },
    onError: (e: Error) => toast.error(e.message),
  })

  const commitRename = () => {
    const trimmed = nameInput.trim()
    if (!trimmed || trimmed === query.name) {
      setEditing(false)
      setNameInput(query.name ?? '')
      return
    }
    renameMutation.mutate(trimmed)
  }

  const startEditing = () => {
    setNameInput(query.name ?? '')
    setEditing(true)
    setTimeout(() => inputRef.current?.focus(), 0)
  }

  return (
    <motion.div
      layout
      initial={{ opacity: 0, y: 12 }}
      animate={{ opacity: 1, y: 0 }}
      exit={{ opacity: 0, scale: 0.95 }}
      className="bg-[#111113] border border-white/10 rounded-xl p-5 flex flex-col gap-3 hover:border-white/20 transition-all cursor-pointer"
      onClick={onOpen}
    >
      <div className="flex items-start justify-between gap-2">
        <div className="flex items-start gap-2 min-w-0 flex-1">
          <Bookmark className="w-4 h-4 text-sky-400 mt-0.5 shrink-0" />
          <div className="min-w-0 flex-1">
            {editing ? (
              <div className="flex items-center gap-1" onClick={(e) => e.stopPropagation()}>
                <input
                  ref={inputRef}
                  type="text"
                  value={nameInput}
                  onChange={(e) => setNameInput(e.target.value)}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter') commitRename()
                    if (e.key === 'Escape') { setEditing(false); setNameInput(query.name ?? '') }
                  }}
                  className="flex-1 min-w-0 px-2 py-0.5 text-sm bg-[#18181b] border border-sky-500 rounded-lg text-white focus:outline-none"
                />
                <button onClick={commitRename} disabled={renameMutation.isPending} className="p-0.5 text-green-400 hover:text-green-300 transition-colors">
                  <Check className="w-3.5 h-3.5" />
                </button>
                <button onClick={() => { setEditing(false); setNameInput(query.name ?? '') }} className="p-0.5 text-zinc-600 hover:text-zinc-300 transition-colors">
                  <X className="w-3.5 h-3.5" />
                </button>
              </div>
            ) : (
              <div className="flex items-center gap-1 group">
                <p className="text-sm font-medium text-zinc-200 truncate">{query.name}</p>
                <button
                  onClick={(e) => { e.stopPropagation(); startEditing() }}
                  className="opacity-0 group-hover:opacity-100 transition-opacity p-0.5 text-zinc-600 hover:text-zinc-300"
                >
                  <Pencil className="w-3 h-3" />
                </button>
              </div>
            )}
            {query.connectionName && <p className="text-xs text-zinc-600 mt-0.5">{query.connectionName}</p>}
          </div>
        </div>
        <div className="flex gap-1 shrink-0" onClick={(e) => e.stopPropagation()}>
          <Button
            variant="ghost"
            size="icon"
            className="w-7 h-7 text-sky-400 hover:text-sky-300 hover:bg-sky-500/10"
            disabled={executeMutation.isPending}
            onClick={() => executeMutation.mutate()}
            title="Run in Workspace"
          >
            <Play className="w-3.5 h-3.5" />
          </Button>
          {canAskAi && (
            <AskAiButton
              size="icon"
              onClick={onAskAi}
              loading={isAskingAi}
              ariaLabel="Ask AI about this saved query"
              className="w-8 h-8"
            />
          )}
          <Button
            variant="ghost"
            size="icon"
            className="w-7 h-7 text-zinc-500 hover:text-sky-400 hover:bg-sky-500/10"
            onClick={onOpen}
            title="View details"
          >
            <ExternalLink className="w-3.5 h-3.5" />
          </Button>
          <Button variant="ghost" size="icon" className="w-7 h-7 text-zinc-600 hover:text-red-400 hover:bg-red-500/10" onClick={onDelete} title="Delete">
            <Trash2 className="w-3.5 h-3.5" />
          </Button>
        </div>
      </div>

      <p className="text-xs text-zinc-600 italic">{truncate(query.userPrompt ?? '', 80)}</p>
      <pre className="text-xs font-mono text-cyan-600 bg-[#0d0d0f] border border-white/5 rounded-xl px-3 py-2 truncate">
        {truncate(formatSqlSingleLineForDisplay(query.generatedSql ?? ''), 100)}
      </pre>

      <div className="flex items-center justify-between">
        <span className="text-xs text-zinc-600">{formatDate(query.updatedAtUtc)}</span>
        {query.connectionName && (
          <Badge variant="secondary" className="text-xs bg-white/5 text-zinc-400 border-white/10">
            {query.connectionName}
          </Badge>
        )}
      </div>
    </motion.div>
  )
}

// ── Page ───────────────────────────────────────────────────────────────────────
export default function SavedQueriesPage() {
  const [search, setSearch] = useState('')
  const [selectedQuery, setSelectedQuery] = useState<SavedQueryDto | null>(null)
  const [askingAiSavedQueryId, setAskingAiSavedQueryId] = useState<string | null>(null)
  const qc = useQueryClient()

  const { data = [], isLoading } = useQuery({
    queryKey: ['saved-queries'],
    queryFn: queriesApi.savedQueries,
  })

  const deleteMutation = useMutation({
    mutationFn: queriesApi.deleteSaved,
    onSuccess: () => { void qc.invalidateQueries({ queryKey: ['saved-queries'] }); toast.success('Query removed') },
    onError: () => toast.error('Failed to delete query'),
  })

  const filtered = data.filter(
    (q) =>
      (q.name ?? '').toLowerCase().includes(search.toLowerCase()) ||
      (q.userPrompt ?? '').toLowerCase().includes(search.toLowerCase())
  )

  const visibleConnectionIds = useMemo(() => {
    return [...new Set(filtered.map((query) => query.connectionId))]
  }, [filtered])

  const adminQueryResults = useQueries({
    queries: visibleConnectionIds.map((connectionId) => ({
      queryKey: ['connection-is-admin', connectionId],
      queryFn: () => accessApi.isAdmin(connectionId),
      staleTime: 60_000,
      retry: false,
    })),
  })

  const adminByConnectionId = useMemo(() => {
    const map = new Map<string, boolean>()
    visibleConnectionIds.forEach((connectionId, index) => {
      map.set(connectionId, adminQueryResults[index]?.data === true)
    })
    return map
  }, [visibleConnectionIds, adminQueryResults])

  const handleAskAi = async (query: SavedQueryDto) => {
    if (askingAiSavedQueryId) return

    setAskingAiSavedQueryId(query.id)
    try {
      const detail = query.queryHistoryId
        ? await queriesApi.historyById(query.queryHistoryId)
        : null

      await ingestArtifactForAskAi({
        source: 'saved-query',
        connectionId: detail?.connectionId ?? query.connectionId,
        title: query.name?.trim() || query.userPrompt?.trim() || `Saved query ${query.id}`,
        prompt: query.userPrompt ?? detail?.userPrompt,
        sql: query.generatedSql ?? detail?.generatedSql,
        explanation: detail?.llmResponse,
        rows: parseRowsFromResultsJson(detail?.resultsJson),
        metadata: {
          savedQueryId: query.id,
          queryHistoryId: query.queryHistoryId,
          connectionName: query.connectionName,
          status: detail?.status,
          rowsReturned: detail?.rowsReturned,
        },
      })
    } finally {
      setAskingAiSavedQueryId(null)
    }
  }

  return (
    <div className="flex flex-col h-full">
      <div className="flex items-center gap-4 px-6 py-4 border-b border-white/10 bg-[#111113] shrink-0">
        <h1 className="text-lg font-semibold text-white">Saved Queries</h1>
        <div className="relative flex-1 max-w-sm">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-zinc-600" />
          <Input
            placeholder="Search saved queries…"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="pl-9 bg-[#18181b] border-white/10 text-white placeholder:text-zinc-600 focus:border-sky-500 focus:ring-1 focus:ring-sky-500 transition-all"
          />
        </div>
        <span className="text-sm text-zinc-600">{filtered.length} saved</span>
      </div>

      <div className="flex-1 min-h-0 overflow-y-auto p-6 bg-[#080809]">
        {isLoading ? (
          <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
            {[1, 2, 3].map((i) => <Skeleton key={i} className="h-48 rounded-xl bg-white/5" />)}
          </div>
        ) : filtered.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-16">
            <Bookmark className="w-10 h-10 text-zinc-600 mb-3" />
            <p className="text-zinc-600 text-sm">No saved queries yet</p>
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
            {filtered.map((q) => (
              <SavedCard
                key={q.id}
                query={q}
                onOpen={() => setSelectedQuery(q)}
                onDelete={() => deleteMutation.mutate(q.id)}
                onAskAi={() => void handleAskAi(q)}
                isAskingAi={askingAiSavedQueryId === q.id}
                canAskAi={adminByConnectionId.get(q.connectionId) === true}
              />
            ))}
          </div>
        )}
      </div>

      <AnimatePresence>
        {selectedQuery && (
          <QueryDetailModal
            query={selectedQuery}
            onClose={() => setSelectedQuery(null)}
          />
        )}
      </AnimatePresence>
    </div>
  )
}
