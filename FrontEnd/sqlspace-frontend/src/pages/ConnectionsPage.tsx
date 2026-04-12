import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { motion } from 'framer-motion'
import { Database, Plus, Search, ShieldAlert } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import { connectionsApi } from '@/api/connections'
import { cn } from '@/lib/utils'

export default function ConnectionsPage() {
  const [search, setSearch] = useState('')

  const { data: connections = [], isLoading } = useQuery({
    queryKey: ['connections'],
    queryFn: connectionsApi.list,
  })

  const filtered = connections.filter(
    (c) =>
      c.connectionName.toLowerCase().includes(search.toLowerCase()) ||
      (c.host ?? '').toLowerCase().includes(search.toLowerCase()) ||
      (c.databaseName ?? '').toLowerCase().includes(search.toLowerCase()),
  )

  return (
    <div className="h-full overflow-y-auto">
      <div className="p-8 max-w-6xl mx-auto">
        {/* Header */}
        <div className="flex items-center justify-between mb-8">
          <div>
            <h1 className="text-2xl font-bold text-white tracking-tight">Database Connections</h1>
            <p className="text-zinc-400 mt-1">Manage your connected databases and access controls.</p>
          </div>
          <Link
            to="/connections/new"
            className="bg-sky-600 hover:bg-sky-500 text-white px-4 py-2.5 rounded-lg text-sm font-semibold flex items-center gap-2 transition-all shadow-[0_0_15px_rgba(14,165,233,0.3)] hover:shadow-[0_0_25px_rgba(14,165,233,0.5)]"
          >
            <Plus className="w-4 h-4" /> Add Connection
          </Link>
        </div>

        {/* Search */}
        <div className="relative mb-6">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-zinc-500" />
          <input
            type="text"
            placeholder="Search connections..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="w-full bg-[#111113] border border-white/10 rounded-xl py-3 pl-10 pr-4 text-white placeholder:text-zinc-600 focus:outline-none focus:border-sky-500 focus:ring-1 focus:ring-sky-500 transition-all shadow-sm"
          />
        </div>

        {/* Grid */}
        {isLoading ? (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {[1, 2, 3].map((i) => (
              <Skeleton key={i} className="h-48 rounded-2xl bg-white/5" />
            ))}
          </div>
        ) : filtered.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-20 text-center">
            <div className="w-14 h-14 rounded-2xl bg-sky-500/10 border border-sky-500/20 flex items-center justify-center mb-4">
              <Database className="w-7 h-7 text-sky-400" />
            </div>
            <p className="text-zinc-400 text-sm">
              {search ? 'No connections match your search' : 'No connections yet'}
            </p>
            {!search && (
              <Link
                to="/connections/new"
                className="mt-3 text-sm text-sky-400 hover:text-sky-300 font-medium transition-colors"
              >
                Add your first connection →
              </Link>
            )}
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {filtered.map((conn) => (
              <motion.div
                key={conn.connectionId}
                initial={{ opacity: 0, y: 12 }}
                animate={{ opacity: 1, y: 0 }}
                className="bg-[#111113] border border-white/10 rounded-2xl p-5 hover:border-white/20 transition-all group flex flex-col h-full"
              >
                <div className="flex items-start gap-3 mb-4">
                  <div className="w-10 h-10 rounded-lg bg-white/5 border border-white/10 flex items-center justify-center shrink-0">
                    <Database className="w-5 h-5 text-zinc-300" />
                  </div>
                  <div className="min-w-0">
                    <h3 className="font-semibold text-white leading-tight truncate">{conn.connectionName}</h3>
                    <div className="flex items-center gap-2 mt-1">
                      <span className="text-xs text-zinc-500">{conn.databaseProvider}</span>
                      <span className="w-1 h-1 rounded-full bg-zinc-600 shrink-0" />
                      <div className="flex items-center gap-1.5">
                        <span
                          className={cn(
                            'w-2 h-2 rounded-full shrink-0',
                            conn.isHealthy
                              ? 'bg-green-500 shadow-[0_0_8px_rgba(34,197,94,0.6)]'
                              : 'bg-red-500 shadow-[0_0_8px_rgba(239,68,68,0.6)]',
                          )}
                        />
                        <span className="text-xs text-zinc-400">
                          {conn.isHealthy ? 'Healthy' : 'Unhealthy'}
                        </span>
                      </div>
                    </div>
                  </div>
                </div>

                <p className="text-sm text-zinc-400 line-clamp-2 mb-6 flex-1">
                  {conn.connectionSummary ??
                    (conn.host
                      ? `${conn.host}:${conn.port} / ${conn.databaseName ?? ''}`
                      : (conn.databaseName ?? '—'))}
                </p>

                <div className="flex items-center justify-between pt-4 border-t border-white/5">
                  <Badge variant="outline" className="bg-white/5 text-zinc-300 border-white/10 font-medium">
                    {conn.isAdmin && <ShieldAlert className="w-3 h-3 mr-1 text-amber-400" />}
                    {conn.isAdmin ? 'Admin' : 'Viewer'}
                  </Badge>
                  <Link
                    to={`/connections/${conn.connectionId}`}
                    className="text-sm font-medium text-sky-400 hover:text-sky-300 transition-colors"
                  >
                    View details
                  </Link>
                </div>
              </motion.div>
            ))}
          </div>
        )}
      </div>
    </div>
  )
}
