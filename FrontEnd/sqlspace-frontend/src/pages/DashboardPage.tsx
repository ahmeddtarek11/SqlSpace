import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  Tooltip as RechartsTooltip,
  ResponsiveContainer,
  CartesianGrid,
  Legend,
} from 'recharts'
import {
  BarChart3,
  Activity,
  CheckCircle2,
  XCircle,
  Database,
  Clock,
  AlertCircle,
  Users,
  Server,
  Rows3,
  CalendarDays,
} from 'lucide-react'
import { Skeleton } from '@/components/ui/skeleton'
import { Badge } from '@/components/ui/badge'
import { insightsApi } from '@/api/insights'
import { connectionsApi } from '@/api/connections'
import { useConnectionStore } from '@/stores/connection-store'
import { formatMs, formatNumber } from '@/lib/utils'
import type { ConnectionInsights } from '@/types'

const RANGE_OPTIONS = [
  { key: '7',   label: 'Last 7 Days'  },
  { key: '30',  label: 'Last 30 Days' },
  { key: 'all', label: 'All Time'     },
] as const

type RangeKey = (typeof RANGE_OPTIONS)[number]['key']

const TT_STYLE = {
  backgroundColor: '#18181b',
  border: '1px solid rgba(255,255,255,0.10)',
  borderRadius: '8px',
  color: '#fff',
  fontSize: 12,
}

function formatDayLabel(iso: string): string {
  return new Date(iso).toLocaleDateString(undefined, { month: 'short', day: 'numeric' })
}

function formatDate(iso: string | null): string {
  if (!iso) return '—'
  return new Date(iso).toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' })
}

// ── Stat card ──────────────────────────────────────────────────────────────────
function StatCard({
  label, value, icon: Icon, color, bg,
}: {
  label: string
  value: string | number
  icon: React.ComponentType<{ className?: string }>
  color: string
  bg: string
}) {
  return (
    <div className="bg-[#111113] border border-white/10 rounded-2xl p-5 shadow-sm hover:border-white/20 transition-colors">
      <div className="flex items-center gap-3 mb-3">
        <div className={`w-8 h-8 rounded-lg ${bg} flex items-center justify-center shrink-0`}>
          <Icon className={`w-4 h-4 ${color}`} />
        </div>
        <span className="text-sm font-medium text-zinc-400 truncate">{label}</span>
      </div>
      <div className="text-2xl font-bold text-white tracking-tight">{value}</div>
    </div>
  )
}

// ── Progress list (tables / users / connections) ───────────────────────────────
function ProgressList({
  title, icon: Icon, items, nameKey, countKey, emptyMsg, accentClass = 'bg-sky-500',
}: {
  title: string
  icon: React.ComponentType<{ className?: string }>
  items: Record<string, unknown>[]
  nameKey: string
  countKey: string
  emptyMsg: string
  accentClass?: string
}) {
  const max = (items[0]?.[countKey] as number) ?? 1
  return (
    <div className="bg-[#111113] border border-white/10 rounded-2xl p-6 shadow-sm hover:border-white/20 transition-colors flex flex-col">
      <h3 className="text-base font-semibold text-white mb-5 flex items-center gap-2 shrink-0">
        <Icon className="w-4 h-4 text-sky-400" />
        {title}
      </h3>
      {items.length === 0 ? (
        <p className="text-sm text-zinc-600">{emptyMsg}</p>
      ) : (
        <div className="space-y-4 flex-1">
          {items.map((item, i) => {
            const name  = String(item[nameKey] ?? '—')
            const count = item[countKey] as number
            const pct   = Math.round((count / max) * 100)
            return (
              <div key={i} className="group">
                <div className="flex items-end justify-between mb-1.5">
                  <span className="text-sm font-medium text-zinc-300 font-mono truncate max-w-[70%] group-hover:text-sky-400 transition-colors">
                    {name}
                  </span>
                  <span className="text-xs text-zinc-500 shrink-0">{formatNumber(count)}</span>
                </div>
                <div className="w-full h-1.5 bg-[#18181b] rounded-full overflow-hidden">
                  <div
                    style={{ width: `${pct}%` }}
                    className={`h-full rounded-full transition-all duration-700 ${i === 0 ? accentClass : `${accentClass}/50`}`}
                  />
                </div>
              </div>
            )
          })}
        </div>
      )}
    </div>
  )
}

// ── Main content ───────────────────────────────────────────────────────────────
function DashboardContent({
  insight, range, isAdmin, viewMode,
}: {
  insight: ConnectionInsights
  range: RangeKey
  isAdmin: boolean
  viewMode: 'mine' | 'all'
}) {
  const s = insight.summary

  const baseVolume = insight.volume ?? []
  const filteredVolume =
    range === 'all' ? baseVolume : baseVolume.slice(-(range === '7' ? 7 : 30))

  const chartData = filteredVolume.map((item) => ({
    name:       formatDayLabel(item.date),
    Successful: item.successful,
    Failed:     item.failed,
    Total:      item.total,
  }))

  const topTables      = insight.topTables      ?? []
  const topUsers       = insight.topUsers       ?? []
  const topConnections = insight.topConnections ?? []

  const successRate = s.totalQueries > 0
    ? Math.round((s.successfulQueries / s.totalQueries) * 100)
    : 0

  const statCards = [
    { label: 'Total Queries',      value: formatNumber(s.totalQueries),        icon: Activity,      color: 'text-sky-400', bg: 'bg-sky-500/10' },
    { label: 'Successful',         value: formatNumber(s.successfulQueries),   icon: CheckCircle2,  color: 'text-green-400',  bg: 'bg-green-500/10'  },
    { label: 'Failed',             value: formatNumber(s.failedQueries),       icon: XCircle,       color: 'text-red-400',    bg: 'bg-red-500/10'    },
    { label: 'Success Rate',       value: `${successRate}%`,                   icon: BarChart3,     color: 'text-cyan-400',   bg: 'bg-cyan-500/10'   },
    { label: 'Avg. Execution',     value: formatMs(s.averageExecutionTimeMs),  icon: Clock,         color: 'text-amber-400',  bg: 'bg-amber-500/10'  },
    { label: 'Total Rows',         value: formatNumber(s.totalRowsReturned),   icon: Rows3,         color: 'text-pink-400',   bg: 'bg-pink-500/10'   },
  ]

  const showTopConnections = isAdmin && viewMode === 'all' && topConnections.length > 0

  return (
    <div className="space-y-8">

      {/* ── 6 Stat cards ─────────────────────────────────────────────────── */}
      <div className="grid grid-cols-2 sm:grid-cols-3 xl:grid-cols-6 gap-4">
        {statCards.map((s) => (
          <StatCard key={s.label} {...s} />
        ))}
      </div>

      {/* ── Activity range row ────────────────────────────────────────────── */}
      {(s.firstQueryDate || s.lastQueryDate) && (
        <div className="flex flex-wrap items-center gap-4 px-1">
          {s.firstQueryDate && (
            <div className="flex items-center gap-2 text-xs text-zinc-500">
              <CalendarDays className="w-3.5 h-3.5 shrink-0" />
              <span>First query:</span>
              <Badge variant="secondary" className="text-xs bg-white/5 border-white/10 text-zinc-300 font-mono">
                {formatDate(s.firstQueryDate)}
              </Badge>
            </div>
          )}
          {s.lastQueryDate && (
            <div className="flex items-center gap-2 text-xs text-zinc-500">
              <CalendarDays className="w-3.5 h-3.5 shrink-0" />
              <span>Last query:</span>
              <Badge variant="secondary" className="text-xs bg-white/5 border-white/10 text-zinc-300 font-mono">
                {formatDate(s.lastQueryDate)}
              </Badge>
            </div>
          )}
        </div>
      )}

      {/* ── Queries over time (success + failed breakdown) ────────────────── */}
      <div className="bg-[#111113] border border-white/10 rounded-2xl p-6 shadow-sm hover:border-white/20 transition-colors">
        <h3 className="text-base font-semibold text-white mb-6 flex items-center gap-2">
          <BarChart3 className="w-4 h-4 text-sky-400" />
          Queries Over Time
          <span className="text-xs text-zinc-600 font-normal ml-1">successful vs failed</span>
        </h3>
        <div className="h-72">
          {chartData.length === 0 || chartData.every((d) => d.Total === 0) ? (
            <div className="h-full flex items-center justify-center text-zinc-600 text-sm">
              No data for this period.
            </div>
          ) : (
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={chartData} margin={{ top: 4, right: 4, left: -20, bottom: 0 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.05)" vertical={false} />
                <XAxis
                  dataKey="name"
                  axisLine={false}
                  tickLine={false}
                  tick={{ fill: '#52525b', fontSize: 11 }}
                  dy={8}
                />
                <YAxis
                  axisLine={false}
                  tickLine={false}
                  tick={{ fill: '#52525b', fontSize: 11 }}
                  allowDecimals={false}
                />
                <RechartsTooltip
                  cursor={{ fill: 'rgba(255,255,255,0.02)' }}
                  contentStyle={TT_STYLE}
                />
                <Legend
                  iconType="circle"
                  iconSize={8}
                  wrapperStyle={{ fontSize: 12, color: '#a1a1aa', paddingTop: 12 }}
                />
                <Bar dataKey="Successful" stackId="a" fill="#22c55e" radius={[0, 0, 0, 0]} />
                <Bar dataKey="Failed"     stackId="a" fill="#ef4444" radius={[4, 4, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          )}
        </div>
      </div>

      {/* ── Top tables + Top users ─────────────────────────────────────────── */}
      <div className={`grid grid-cols-1 gap-6 ${showTopConnections ? 'lg:grid-cols-3' : 'lg:grid-cols-2'}`}>
        <ProgressList
          title="Most Queried Tables"
          icon={Database}
          items={topTables as unknown as Record<string, unknown>[]}
          nameKey="tableName"
          countKey="queryCount"
          emptyMsg="No table usage data yet."
          accentClass="bg-sky-500"
        />

        <ProgressList
          title="Top Users"
          icon={Users}
          items={topUsers.map((u) => ({
            displayName: u.userName || u.userEmail || u.userId.slice(0, 8),
            queryCount:  u.queryCount,
          }))}
          nameKey="displayName"
          countKey="queryCount"
          emptyMsg={isAdmin ? 'No user data yet.' : 'Admin view required to see user breakdown.'}
          accentClass="bg-cyan-500"
        />

        {showTopConnections && (
          <ProgressList
            title="Top Connections"
            icon={Server}
            items={topConnections as unknown as Record<string, unknown>[]}
            nameKey="connectionName"
            countKey="queryCount"
            emptyMsg="No connection data yet."
            accentClass="bg-amber-500"
          />
        )}
      </div>

      {/* ── Summary footer ────────────────────────────────────────────────── */}
      <div className="bg-[#111113] border border-white/10 rounded-2xl p-6 shadow-sm">
        <h3 className="text-base font-semibold text-white mb-4 flex items-center gap-2">
          <Activity className="w-4 h-4 text-sky-400" />
          Summary
        </h3>
        <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 gap-x-8 gap-y-3 text-sm">
          {[
            { label: 'Total Queries',      value: formatNumber(s.totalQueries),       color: 'text-zinc-200' },
            { label: 'Successful',         value: formatNumber(s.successfulQueries),  color: 'text-green-400' },
            { label: 'Failed',             value: formatNumber(s.failedQueries),      color: 'text-red-400' },
            { label: 'Total Rows Returned',value: formatNumber(s.totalRowsReturned),  color: 'text-zinc-200' },
            { label: 'Avg. Execution',     value: formatMs(s.averageExecutionTimeMs), color: 'text-amber-400' },
          ].map(({ label, value, color }) => (
            <div key={label}>
              <p className="text-xs text-zinc-600 mb-0.5">{label}</p>
              <p className={`font-mono font-medium ${color}`}>{value}</p>
            </div>
          ))}
        </div>
      </div>

    </div>
  )
}

// ── Page ───────────────────────────────────────────────────────────────────────
export default function DashboardPage() {
  const { activeConnectionId, setActiveConnection } = useConnectionStore()
  const [range, setRange] = useState<RangeKey>('7')
  const [viewMode, setViewMode] = useState<'mine' | 'all'>('mine')

  const { data: connections = [], isLoading: connectionsLoading } = useQuery({
    queryKey: ['connections'],
    queryFn: connectionsApi.list,
    staleTime: 60_000,
  })

  const selectedId  = activeConnectionId ?? connections[0]?.connectionId ?? ''
  const activeConn  = connections.find((c) => c.connectionId === selectedId)
  const isAdmin     = activeConn?.isAdmin ?? false
  const useAdminEp  = isAdmin && viewMode === 'all'

  const { data: insight, isLoading: insightLoading, isError } = useQuery({
    queryKey: ['insights', selectedId, useAdminEp],
    queryFn: () =>
      useAdminEp
        ? insightsApi.getForConnectionAdmin(selectedId)
        : insightsApi.getForConnection(selectedId),
    enabled: !!selectedId,
    retry: false,
  })

  if (connectionsLoading) {
    return (
      <div className="p-8 max-w-7xl mx-auto space-y-6">
        <Skeleton className="h-10 w-72 rounded-xl bg-white/5" />
        <div className="grid grid-cols-2 sm:grid-cols-3 xl:grid-cols-6 gap-4">
          {[1,2,3,4,5,6].map((i) => <Skeleton key={i} className="h-28 rounded-2xl bg-white/5" />)}
        </div>
        <Skeleton className="h-72 rounded-2xl bg-white/5" />
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          <Skeleton className="h-64 rounded-2xl bg-white/5" />
          <Skeleton className="h-64 rounded-2xl bg-white/5" />
        </div>
      </div>
    )
  }

  if (connections.length === 0) {
    return (
      <div className="h-full flex items-center justify-center p-8">
        <div className="text-center">
          <div className="w-14 h-14 rounded-2xl bg-sky-500/10 border border-sky-500/20 flex items-center justify-center mx-auto mb-4">
            <Database className="w-7 h-7 text-sky-400" />
          </div>
          <p className="text-zinc-200 font-medium">No connections yet</p>
          <p className="text-zinc-500 text-sm mt-1">Add a connection to start seeing statistics.</p>
        </div>
      </div>
    )
  }

  return (
    <div className="h-full overflow-y-auto">
      <div className="p-8 max-w-7xl mx-auto">

        {/* Header */}
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-8">
          <div>
            <h1 className="text-2xl font-bold text-white tracking-tight">Dashboard</h1>
            <p className="text-zinc-400 mt-1 text-sm">
              Query insights and usage patterns
              {isAdmin && viewMode === 'all' && (
                <span className="ml-2 text-sky-400 font-medium">· All users</span>
              )}
            </p>
          </div>

          <div className="flex items-center gap-2 flex-wrap">
            {isAdmin && (
              <div className="flex items-center bg-[#111113] border border-white/10 rounded-lg p-0.5">
                <button
                  onClick={() => setViewMode('mine')}
                  className={`px-3 py-1 text-xs rounded-md transition-colors ${
                    viewMode === 'mine' ? 'bg-sky-600 text-white' : 'text-zinc-400 hover:text-zinc-200'
                  }`}
                >
                  My Data
                </button>
                <button
                  onClick={() => setViewMode('all')}
                  className={`px-3 py-1 text-xs rounded-md transition-colors ${
                    viewMode === 'all' ? 'bg-sky-600 text-white' : 'text-zinc-400 hover:text-zinc-200'
                  }`}
                >
                  All Users
                </button>
              </div>
            )}

            <select
              value={selectedId}
              onChange={(e) => { setActiveConnection(e.target.value); setViewMode('mine') }}
              className="bg-[#111113] border border-white/10 rounded-lg py-2 pl-3 pr-8 text-sm text-white focus:outline-none focus:border-sky-500 appearance-none"
            >
              {connections.map((c) => (
                <option key={c.connectionId} value={c.connectionId}>{c.connectionName}</option>
              ))}
            </select>

            <select
              value={range}
              onChange={(e) => setRange(e.target.value as RangeKey)}
              className="bg-[#111113] border border-white/10 rounded-lg py-2 pl-3 pr-8 text-sm text-white focus:outline-none focus:border-sky-500 appearance-none"
            >
              {RANGE_OPTIONS.map((o) => (
                <option key={o.key} value={o.key}>{o.label}</option>
              ))}
            </select>
          </div>
        </div>

        {/* Content */}
        {insightLoading ? (
          <div className="space-y-6">
            <div className="grid grid-cols-2 sm:grid-cols-3 xl:grid-cols-6 gap-4">
              {[1,2,3,4,5,6].map((i) => <Skeleton key={i} className="h-28 rounded-2xl bg-white/5" />)}
            </div>
            <Skeleton className="h-72 rounded-2xl bg-white/5" />
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
              <Skeleton className="h-64 rounded-2xl bg-white/5" />
              <Skeleton className="h-64 rounded-2xl bg-white/5" />
            </div>
          </div>
        ) : isError || !insight ? (
          <div className="flex items-center gap-3 rounded-xl border border-amber-500/20 bg-amber-500/10 px-4 py-3">
            <AlertCircle className="w-4 h-4 text-amber-400 shrink-0" />
            <p className="text-sm text-amber-300">Failed to load statistics for this connection.</p>
          </div>
        ) : (
          <DashboardContent
            insight={insight}
            range={range}
            isAdmin={isAdmin}
            viewMode={viewMode}
          />
        )}

      </div>
    </div>
  )
}
