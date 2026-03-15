import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { motion, AnimatePresence } from 'framer-motion'
import { Search, ChevronDown, ChevronUp, CheckCircle2, XCircle, Clock } from 'lucide-react'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import { queriesApi } from '@/api/queries'
import { formatDate, formatMs, truncate } from '@/lib/utils'
import type { QueryHistoryDto } from '@/types'

function HistoryItem({ item }: { item: QueryHistoryDto }) {
  const [expanded, setExpanded] = useState(false)
  const success = item.status === 'Completed'
  return (
    <motion.div layout className="border border-(--border-default) rounded-xl overflow-hidden bg-(--bg-surface)">
      <button
        className="w-full flex items-start gap-3 p-4 text-left hover:bg-(--bg-elevated) transition-colors"
        onClick={() => setExpanded((v) => !v)}
      >
        {success ? (
          <CheckCircle2 className="w-4 h-4 text-green-400 mt-0.5 shrink-0" />
        ) : (
          <XCircle className="w-4 h-4 text-red-400 mt-0.5 shrink-0" />
        )}
        <div className="flex-1 min-w-0">
          <p className="text-sm text-white truncate">{item.userPrompt}</p>
          <p className="text-xs text-(--text-muted) mt-0.5 font-mono truncate">{truncate(item.generatedSql ?? '', 60)}</p>
        </div>
        <div className="flex flex-col items-end gap-1 shrink-0">
          {item.connectionName && (
            <Badge variant="secondary" className="text-xs bg-(--bg-elevated) text-(--text-muted) border-(--border-default)">
              {item.connectionName}
            </Badge>
          )}
          <div className="flex items-center gap-2 text-xs text-(--text-muted)">
            {item.executionTimeMs != null && (
              <><Clock className="w-3 h-3" /><span>{formatMs(item.executionTimeMs)}</span><span>·</span></>
            )}
            <span>{item.rowsReturned ?? 0} rows</span>
          </div>
          <span className="text-xs text-(--text-muted)">{formatDate(item.executedAt)}</span>
        </div>
        {expanded ? <ChevronUp className="w-4 h-4 text-(--text-muted) mt-0.5 shrink-0" /> : <ChevronDown className="w-4 h-4 text-(--text-muted) mt-0.5 shrink-0" />}
      </button>

      <AnimatePresence>
        {expanded && (
          <motion.div initial={{ height: 0 }} animate={{ height: 'auto' }} exit={{ height: 0 }} className="overflow-hidden border-t border-(--border-default)">
            <div className="p-4 space-y-2">
              <p className="text-xs font-medium text-(--text-muted) uppercase tracking-wider">Generated SQL</p>
              <pre className="text-xs font-mono text-cyan-300 bg-(--bg-elevated) rounded-lg p-3 overflow-x-auto whitespace-pre-wrap">
                {item.generatedSql}
              </pre>
            </div>
          </motion.div>
        )}
      </AnimatePresence>
    </motion.div>
  )
}

export default function HistoryPage() {
  const [search, setSearch] = useState('')

  const { data, isLoading } = useQuery({
    queryKey: ['history'],
    queryFn: () => queriesApi.history({ pageSize: 100 }),
  })

  const items = data?.items ?? []
  const filtered = items.filter(
    (item) =>
      (item.userPrompt ?? '').toLowerCase().includes(search.toLowerCase()) ||
      (item.generatedSql ?? '').toLowerCase().includes(search.toLowerCase())
  )

  return (
    <div className="flex flex-col h-full">
      <div className="flex items-center gap-4 px-6 py-4 border-b border-(--border-default) bg-(--bg-surface) shrink-0">
        <h1 className="text-lg font-semibold text-white">Query History</h1>
        <div className="relative flex-1 max-w-sm">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-(--text-muted)" />
          <Input
            placeholder="Search queries…"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="pl-9 bg-(--bg-elevated) border-(--border-default) text-white placeholder:text-(--text-muted)"
          />
        </div>
        <span className="text-sm text-(--text-muted)">{filtered.length} results</span>
      </div>

      <div className="flex-1 min-h-0 overflow-y-auto p-6">
        {isLoading ? (
          <div className="space-y-3">
            {[1, 2, 3, 4, 5].map((i) => <Skeleton key={i} className="h-20 rounded-xl bg-(--bg-surface)" />)}
          </div>
        ) : filtered.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-16 text-center">
            <p className="text-(--text-muted) text-sm">No queries found</p>
          </div>
        ) : (
          <div className="space-y-3">
            {filtered.map((item) => <HistoryItem key={item.queryId} item={item} />)}
          </div>
        )}
      </div>
    </div>
  )
}
