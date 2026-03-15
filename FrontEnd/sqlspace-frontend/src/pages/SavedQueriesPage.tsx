import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { motion } from 'framer-motion'
import { useNavigate } from 'react-router-dom'
import { toast } from 'sonner'
import { Bookmark, Trash2, Play, Search } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import { queriesApi } from '@/api/queries'
import { useWorkspaceStore } from '@/stores/workspace-store'
import { useConnectionStore } from '@/stores/connection-store'
import { formatDate, truncate } from '@/lib/utils'
import type { SavedQueryDto } from '@/types'

function SavedCard({ query, onDelete }: { query: SavedQueryDto; onDelete: () => void }) {
  const navigate = useNavigate()
  const { setPrompt, setGeneratedSQL } = useWorkspaceStore()
  const { setActiveConnection } = useConnectionStore()

  const handleRun = () => {
    setPrompt(query.userPrompt ?? '')
    setGeneratedSQL(query.generatedSql ?? '')
    setActiveConnection(query.connectionId)
    navigate('/workspace')
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
        <div className="flex items-start gap-2 min-w-0">
          <Bookmark className="w-4 h-4 text-violet-400 mt-0.5 shrink-0" />
          <div className="min-w-0">
            <p className="text-sm font-medium text-white truncate">{query.name}</p>
            {query.connectionName && <p className="text-xs text-(--text-muted) mt-0.5">{query.connectionName}</p>}
          </div>
        </div>
        <div className="flex gap-1 shrink-0">
          <Button variant="ghost" size="icon" className="w-7 h-7 text-violet-400 hover:text-violet-300 hover:bg-violet-600/10" onClick={handleRun}>
            <Play className="w-3.5 h-3.5" />
          </Button>
          <Button variant="ghost" size="icon" className="w-7 h-7 text-(--text-muted) hover:text-red-400" onClick={onDelete}>
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
        <h1 className="text-lg font-semibold text-white">Saved Queries</h1>
        <div className="relative flex-1 max-w-sm">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-(--text-muted)" />
          <Input
            placeholder="Search saved queries…"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="pl-9 bg-(--bg-elevated) border-(--border-default) text-white placeholder:text-(--text-muted)"
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
              <SavedCard key={q.id} query={q} onDelete={() => deleteMutation.mutate(q.id)} />
            ))}
          </div>
        )}
      </div>
    </div>
  )
}
