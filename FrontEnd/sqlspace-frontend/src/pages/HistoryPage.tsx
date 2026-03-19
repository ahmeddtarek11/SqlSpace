import { useEffect, useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { Search, Filter, ChevronRight, ChevronLeft } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { queriesApi } from '@/api/queries'
export default function HistoryPage() {
  const pageSize = 15
  const [search, setSearch] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState('')
  const [selectedConnectionId, setSelectedConnectionId] = useState('all')
  const [pageNumber, setPageNumber] = useState(1)

  useEffect(() => {
    const t = setTimeout(() => setDebouncedSearch(search), 300)
    return () => clearTimeout(t)
  }, [search])

  const isSearching = debouncedSearch.trim().length > 0

  const { data: connections = [] } = useQuery({
    queryKey: ['connections-list-for-history'],
    queryFn: () => import('@/api/connections').then((m) => m.connectionsApi.list()),
  })

  const selectedConnectionName = useMemo(
    () => connections.find((c) => c.connectionId === selectedConnectionId)?.connectionName ?? null,
    [connections, selectedConnectionId]
  )

  useEffect(() => {
    setPageNumber(1)
  }, [debouncedSearch, selectedConnectionId])

  const { data, isLoading } = useQuery({
    queryKey: ['history-page', pageNumber, debouncedSearch, selectedConnectionId],
    queryFn: async () => {
      if (isSearching) {
        return queriesApi.searchHistory({
          searchTerm: debouncedSearch,
          connectionId: selectedConnectionId !== 'all' ? selectedConnectionId : undefined,
          pageNumber,
          pageSize,
        })
      }

      if (selectedConnectionId === 'all') {
        return queriesApi.history({ pageNumber, pageSize })
      }

      const allItems = await queriesApi.history({ pageNumber: 1, pageSize: 500 })
      const filtered = allItems.items.filter((item) => item.connectionName === selectedConnectionName)
      const start = (pageNumber - 1) * pageSize
      const paged = filtered.slice(start, start + pageSize)

      return {
        ...allItems,
        items: paged,
        totalCount: filtered.length,
        pageNumber,
        pageSize,
        totalPages: Math.max(1, Math.ceil(filtered.length / pageSize)),
        hasPreviousPage: pageNumber > 1,
        hasNextPage: start + pageSize < filtered.length,
      }
    },
  })

  const items = data?.items ?? []
  const totalCount = data?.totalCount ?? 0
  const hasPreviousPage = data?.hasPreviousPage ?? false
  const hasNextPage = data?.hasNextPage ?? false
  const startEntry = totalCount === 0 ? 0 : (pageNumber - 1) * pageSize + 1
  const endEntry = Math.min(pageNumber * pageSize, totalCount)

  return (
    <div className="px-6 py-8 w-full h-full min-h-0 flex flex-col">
      <div className="mb-8">
        <h1 className="text-2xl font-bold text-white tracking-tight">Query History</h1>
        <p className="text-zinc-400 mt-1">Audit log of all queries executed across your connections.</p>
      </div>

      <div className="flex flex-col sm:flex-row gap-4 mb-6">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-zinc-500" />
          <input
            type="text"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search prompts or SQL..."
            className="w-full bg-[#111113] border border-white/10 rounded-xl py-2.5 pl-10 pr-4 text-white placeholder:text-zinc-600 focus:outline-none focus:border-sky-500 transition-all shadow-sm"
          />
        </div>
        <div className="w-full sm:w-64 relative">
          <Filter className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-zinc-500" />
          <select
            className="w-full bg-[#111113] border border-white/10 rounded-xl py-2.5 pl-9 pr-4 text-white focus:outline-none focus:border-sky-500 appearance-none shadow-sm"
            value={selectedConnectionId}
            onChange={(e) => setSelectedConnectionId(e.target.value)}
          >
            <option value="all">All Connections</option>
            {connections.map((connection) => (
              <option key={connection.connectionId} value={connection.connectionId}>
                {connection.connectionName}
              </option>
            ))}
          </select>
        </div>
      </div>

      <div className="bg-[#111113] border border-white/10 rounded-2xl overflow-hidden shadow-xl flex-1 min-h-160 flex flex-col">
        <div className="overflow-x-auto overflow-y-auto flex-1 min-h-0">
          <table className="w-full text-left text-sm">
            <thead className="bg-[#18181b] border-b border-white/10 text-zinc-400 uppercase text-xs font-semibold tracking-wider">
              <tr>
                <th className="px-6 py-4 w-1/3">Query Prompt</th>
                <th className="px-6 py-4">Connection</th>
                <th className="px-6 py-4">Status</th>
                <th className="px-6 py-4">Rows</th>
                <th className="px-6 py-4">Time</th>
                <th className="px-6 py-4 text-right">Date</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-white/5">
              {isLoading ? (
                Array.from({ length: pageSize }).map((_, idx) => (
                  <tr key={idx}>
                    <td className="px-6 py-4 text-zinc-500" colSpan={6}>Loading...</td>
                  </tr>
                ))
              ) : items.length === 0 ? (
                <tr>
                  <td className="px-6 py-8 text-zinc-500 text-center" colSpan={6}>No query history found.</td>
                </tr>
              ) : (
                items.map((item) => (
                  <tr key={item.queryId} className="hover:bg-white/5 transition-colors group cursor-pointer">
                    <td className="px-6 py-4">
                      <Link to={`/history/${item.queryId}`} className="block">
                        <div className="font-medium text-white mb-1 truncate max-w-md" title={item.userPrompt ?? ''}>
                          {item.userPrompt ?? '(No prompt)'}
                        </div>
                        <div className="font-mono text-xs text-zinc-500 truncate max-w-md">
                          {item.generatedSql ? `${item.generatedSql.substring(0, 60)}...` : 'No SQL generated'}
                        </div>
                      </Link>
                    </td>
                    <td className="px-6 py-4">
                      <Link to={`/history/${item.queryId}`} className="block">
                        <span className="text-zinc-300 font-medium">{item.connectionName ?? 'Unknown Connection'}</span>
                      </Link>
                    </td>
                    <td className="px-6 py-4">
                      <Link to={`/history/${item.queryId}`} className="block">
                        {item.status === 'Success' ? (
                          <Badge variant="outline" className="bg-green-500/10 text-green-400 border-green-500/20">Success</Badge>
                        ) : (
                          <Badge variant="outline" className="bg-red-500/10 text-red-400 border-red-500/20">Failed</Badge>
                        )}
                      </Link>
                    </td>
                    <td className="px-6 py-4 text-zinc-400 font-mono text-xs">
                      <Link to={`/history/${item.queryId}`} className="block">{item.rowsReturned ?? 0}</Link>
                    </td>
                    <td className="px-6 py-4 text-zinc-400 font-mono text-xs">
                      <Link to={`/history/${item.queryId}`} className="block">{item.executionTimeMs ?? 0}ms</Link>
                    </td>
                    <td className="px-6 py-4 text-right text-zinc-400">
                      <Link to={`/history/${item.queryId}`} className="flex items-center justify-end gap-3">
                        {new Intl.DateTimeFormat('en-US', {
                          month: 'short',
                          day: '2-digit',
                          hour: '2-digit',
                          minute: '2-digit',
                          hour12: false,
                        }).format(new Date(item.executedAt))}
                        <ChevronRight className="w-4 h-4 text-zinc-600 group-hover:text-sky-400 transition-colors" />
                      </Link>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        <div className="px-6 py-4 border-t border-white/10 bg-[#18181b] flex items-center justify-between">
          <span className="text-sm text-zinc-500">
            Showing {startEntry} to {endEntry} of {totalCount.toLocaleString()} entries
          </span>
          <div className="flex gap-2">
            <button
              className="px-3 py-1.5 rounded-lg border border-white/10 text-zinc-300 hover:text-white hover:bg-white/5 transition-colors flex items-center gap-1 text-sm disabled:text-zinc-500 disabled:cursor-not-allowed disabled:bg-black/20"
              disabled={!hasPreviousPage}
              onClick={() => setPageNumber((current) => Math.max(1, current - 1))}
            >
              <ChevronLeft className="w-4 h-4" /> Prev
            </button>
            <button
              className="px-3 py-1.5 rounded-lg border border-white/10 text-zinc-300 hover:text-white hover:bg-white/5 transition-colors flex items-center gap-1 text-sm disabled:text-zinc-500 disabled:cursor-not-allowed disabled:bg-black/20"
              disabled={!hasNextPage}
              onClick={() => setPageNumber((current) => current + 1)}
            >
              Next <ChevronRight className="w-4 h-4" />
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}
