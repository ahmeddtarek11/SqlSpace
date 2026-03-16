import { useState, useRef } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { motion } from 'framer-motion'
import { useNavigate } from 'react-router-dom'
import { toast } from 'sonner'
import { Bookmark, Trash2, Play, Search, Pencil, Check, X } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import { queriesApi } from '@/api/queries'
import { useWorkspaceStore } from '@/stores/workspace-store'
import { useConnectionStore } from '@/stores/connection-store'
import { formatDate, truncate } from '@/lib/utils'
import type { SavedQueryDto } from '@/types'

// ── Card ───────────────────────────────────────────────────────────────────────
function SavedCard({
  query,
  onDelete,
  onRename,
}: {
  query: SavedQueryDto
  onDelete: () => void
  onRename: (name: string) => void
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
      className="bg-(--bg-surface) border border-(--border-default) rounded-xl p-4 flex flex-col gap-3"
    >
      <div className="flex items-start justify-between gap-2">
        <div className="flex items-start gap-2 min-w-0 flex-1">
          <Bookmark className="w-4 h-4 text-violet-400 mt-0.5 shrink-0" />
          <div className="min-w-0 flex-1">
            {editing ? (
              <div className="flex items-center gap-1">
                <input
                  ref={inputRef}
                  type="text"
                  value={nameInput}
                  onChange={(e) => setNameInput(e.target.value)}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter') commitRename()
                    if (e.key === 'Escape') { setEditing(false); setNameInput(query.name ?? '') }
                  }}
                  className="flex-1 min-w-0 px-2 py-0.5 text-sm bg-(--bg-elevated) border border-violet-500 rounded text-(--text-primary) focus:outline-none"
                />
                <button
                  onClick={commitRename}
                  disabled={renameMutation.isPending}
                  className="p-0.5 text-green-400 hover:text-green-300"
                >
                  <Check className="w-3.5 h-3.5" />
                </button>
                <button
                  onClick={() => { setEditing(false); setNameInput(query.name ?? '') }}
                  className="p-0.5 text-(--text-muted) hover:text-(--text-primary)"
                >
                  <X className="w-3.5 h-3.5" />
                </button>
              </div>
            ) : (
              <div className="flex items-center gap-1 group">
                <p className="text-sm font-medium text-(--text-primary) truncate">{query.name}</p>
                <button
                  onClick={startEditing}
                  className="opacity-0 group-hover:opacity-100 transition-opacity p-0.5 text-(--text-muted) hover:text-(--text-primary)"
                >
                  <Pencil className="w-3 h-3" />
                </button>
              </div>
            )}
            {query.connectionName && <p className="text-xs text-(--text-muted) mt-0.5">{query.connectionName}</p>}
          </div>
        </div>
        <div className="flex gap-1 shrink-0">
          <Button
            variant="ghost"
            size="icon"
            className="w-7 h-7 text-violet-400 hover:text-violet-300 hover:bg-violet-600/10"
            disabled={executeMutation.isPending}
            onClick={() => executeMutation.mutate()}
            title="Execute"
          >
            <Play className="w-3.5 h-3.5" />
          </Button>
          <Button variant="ghost" size="icon" className="w-7 h-7 text-(--text-muted) hover:text-red-400" onClick={onDelete} title="Delete">
            <Trash2 className="w-3.5 h-3.5" />
          </Button>
        </div>
      </div>

      <p className="text-xs text-(--text-muted) italic">{truncate(query.userPrompt ?? '', 80)}</p>
      <pre className="text-xs font-mono text-cyan-300 bg-(--bg-elevated) rounded-lg px-3 py-2 truncate">
        {truncate(query.generatedSql ?? '', 100)}
      </pre>

      <div className="flex items-center justify-between">
        <span className="text-xs text-(--text-muted)">{formatDate(query.updatedAtUtc)}</span>
        {query.connectionName && (
          <Badge variant="secondary" className="text-xs bg-(--bg-elevated) text-(--text-muted) border-(--border-default)">
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

  return (
    <div className="flex flex-col h-full">
      <div className="flex items-center gap-4 px-6 py-4 border-b border-(--border-default) bg-(--bg-surface) shrink-0">
        <h1 className="text-lg font-semibold text-(--text-primary)">Saved Queries</h1>
        <div className="relative flex-1 max-w-sm">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-(--text-muted)" />
          <Input
            placeholder="Search saved queries…"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="pl-9 bg-(--bg-elevated) border-(--border-default) text-(--text-primary) placeholder:text-(--text-muted)"
          />
        </div>
        <span className="text-sm text-(--text-muted)">{filtered.length} saved</span>
      </div>

      <div className="flex-1 min-h-0 overflow-y-auto p-6">
        {isLoading ? (
          <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
            {[1, 2, 3].map((i) => <Skeleton key={i} className="h-48 rounded-xl bg-(--bg-surface)" />)}
          </div>
        ) : filtered.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-16">
            <Bookmark className="w-10 h-10 text-(--text-muted) mb-3" />
            <p className="text-(--text-muted) text-sm">No saved queries yet</p>
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
            {filtered.map((q) => (
              <SavedCard
                key={q.id}
                query={q}
                onDelete={() => deleteMutation.mutate(q.id)}
                onRename={(name) => queriesApi.renameSaved(q.id, name)}
              />
            ))}
          </div>
        )}
      </div>
    </div>
  )
}
