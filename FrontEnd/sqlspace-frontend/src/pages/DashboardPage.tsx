import { useState, useCallback } from 'react'
import { useQuery } from '@tanstack/react-query'
import { GridLayout, useContainerWidth } from 'react-grid-layout'
import type { LayoutItem } from 'react-grid-layout'
import 'react-grid-layout/css/styles.css'
import 'react-resizable/css/styles.css'
import {
  AreaChart, Area, XAxis, YAxis, Tooltip as RechartsTooltip,
  ResponsiveContainer, BarChart, Bar, CartesianGrid,
  PieChart, Pie, Cell, Legend, LineChart, Line,
} from 'recharts'
import {
  Database, Zap, Clock, CheckCircle2, XCircle,
  ChevronDown, AlertCircle, TrendingUp, Table2, Users, Rows3,
  Activity, GripVertical, RotateCcw,
} from 'lucide-react'
import { Skeleton } from '@/components/ui/skeleton'
import { insightsApi } from '@/api/insights'
import { connectionsApi } from '@/api/connections'
import { useConnectionStore } from '@/stores/connection-store'
import { formatMs, formatNumber } from '@/lib/utils'
import type { Connection, ConnectionInsights } from '@/types'

// ── Grid constants & layout persistence ───────────────────────────────────

const COLS       = 12
const ROW_H      = 52
const STORAGE_KEY = 'sqlspace-dashboard-layout'

const DEFAULT_LAYOUT: LayoutItem[] = [
  { i: 'stat-0', x: 0,  y: 0,  w: 2, h: 2, minW: 2, minH: 2 },
  { i: 'stat-1', x: 2,  y: 0,  w: 2, h: 2, minW: 2, minH: 2 },
  { i: 'stat-2', x: 4,  y: 0,  w: 2, h: 2, minW: 2, minH: 2 },
  { i: 'stat-3', x: 6,  y: 0,  w: 2, h: 2, minW: 2, minH: 2 },
  { i: 'stat-4', x: 8,  y: 0,  w: 2, h: 2, minW: 2, minH: 2 },
  { i: 'stat-5', x: 10, y: 0,  w: 2, h: 2, minW: 2, minH: 2 },
  { i: 'volume',       x: 0,  y: 2,  w: 12, h: 7, minW: 4, minH: 4 },
  { i: 'success-rate', x: 0,  y: 9,  w: 6,  h: 6, minW: 3, minH: 4 },
  { i: 'failed-trend', x: 6,  y: 9,  w: 6,  h: 6, minW: 3, minH: 4 },
  { i: 'outcome',      x: 0,  y: 15, w: 6,  h: 6, minW: 3, minH: 4 },
  { i: 'daily',        x: 6,  y: 15, w: 6,  h: 6, minW: 3, minH: 4 },
  { i: 'top-tables',   x: 0,  y: 21, w: 6,  h: 6, minW: 3, minH: 4 },
  { i: 'top-users',    x: 6,  y: 21, w: 6,  h: 6, minW: 3, minH: 4 },
]

function loadLayout(): LayoutItem[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (raw) {
      const saved = JSON.parse(raw) as LayoutItem[]
      const ids = new Set(saved.map((l) => l.i))
      if (DEFAULT_LAYOUT.every((d) => ids.has(d.i))) return saved
    }
  } catch {}
  return DEFAULT_LAYOUT
}

// ── Helpers ────────────────────────────────────────────────────────────────

function fmtDate(iso: string) {
  return new Date(iso).toLocaleDateString(undefined, { month: 'short', day: 'numeric' })
}

const TT = {
  contentStyle: {
    background: 'hsl(var(--card))',
    border: '1px solid hsl(var(--border))',
    borderRadius: 8,
    fontSize: 12,
  },
}

const AXIS_STROKE = 'hsl(var(--muted-foreground))'
const AX = { axisLine: true as const, tickLine: false as const }
const XTICK = { fontSize: 10, fill: 'hsl(var(--muted-foreground))' }
const GRID_STROKE = { strokeDasharray: '3 3', stroke: 'hsl(var(--border))' }

// chart palette
const C_GREEN  = 'hsl(142, 71%, 45%)'
const C_RED    = 'hsl(0, 72%, 51%)'
const C_CYAN   = 'hsl(174, 72%, 46%)'
const C_VIOLET = 'hsl(262, 83%, 58%)'

// ── Stat card ──────────────────────────────────────────────────────────────

type StatAccent = 'violet' | 'green' | 'red' | 'cyan' | 'amber' | 'pink'

const ACCENT: Record<StatAccent, { bar: string; icon: string }> = {
  violet: { bar: 'bg-violet-500', icon: 'bg-violet-500/10 text-violet-300 ring-violet-500/20' },
  green:  { bar: 'bg-green-500',  icon: 'bg-green-500/10  text-green-300  ring-green-500/20'  },
  red:    { bar: 'bg-red-500',    icon: 'bg-red-500/10    text-red-300    ring-red-500/20'    },
  cyan:   { bar: 'bg-cyan-500',   icon: 'bg-cyan-500/10   text-cyan-300   ring-cyan-500/20'   },
  amber:  { bar: 'bg-amber-500',  icon: 'bg-amber-500/10  text-amber-300  ring-amber-500/20'  },
  pink:   { bar: 'bg-pink-500',   icon: 'bg-pink-500/10   text-pink-300   ring-pink-500/20'   },
}

function StatCard({ label, value, icon, accent }: {
  label: string
  value: string | number
  icon: React.ReactNode
  accent: StatAccent
}) {
  const a = ACCENT[accent]
  return (
    <div className="drag-handle cursor-grab active:cursor-grabbing h-full relative bg-(--bg-surface) border border-(--border-default) rounded-xl p-3 overflow-hidden hover:border-(--border-strong) transition-colors flex items-center gap-3 select-none">
      <div className={`absolute inset-x-0 top-0 h-0.5 ${a.bar} opacity-70`} />
      <div className={`w-8 h-8 rounded-lg flex items-center justify-center shrink-0 ring-1 ${a.icon}`}>
        {icon}
      </div>
      <div className="min-w-0 flex-1">
        <p className="text-[10px] text-(--text-muted) uppercase tracking-wide truncate">{label}</p>
        <p className="text-xl font-semibold text-(--text-primary) leading-tight">{value}</p>
      </div>
    </div>
  )
}

// ── Chart card ─────────────────────────────────────────────────────────────

function ChartCard({ title, sub, badge, children }: {
  title: string
  sub?: string
  badge?: string
  children: React.ReactNode
}) {
  return (
    <div className="h-full flex flex-col bg-(--bg-surface) border border-(--border-default) rounded-xl overflow-hidden">
      <div className="drag-handle cursor-grab active:cursor-grabbing px-4 py-2 border-b border-(--border-subtle) flex items-center justify-between gap-3 shrink-0 select-none">
        <div className="flex items-center gap-2 min-w-0">
          <GripVertical className="w-3 h-3 text-(--text-muted) opacity-50 shrink-0" />
          <div className="min-w-0">
            <p className="text-xs font-medium text-(--text-primary) truncate">{title}</p>
            {sub && <p className="text-[10px] text-(--text-muted) truncate">{sub}</p>}
          </div>
        </div>
        {badge && (
          <span className="shrink-0 text-[9px] font-medium px-1.5 py-0.5 rounded-full bg-(--bg-elevated) text-(--text-secondary) border border-(--border-subtle) uppercase tracking-wide">
            {badge}
          </span>
        )}
      </div>
      <div className="flex-1 min-h-0 p-3">{children}</div>
    </div>
  )
}

function EmptyChart() {
  return (
    <div className="w-full h-full flex flex-col items-center justify-center gap-2 text-(--text-muted)">
      <Activity className="w-5 h-5 opacity-30" />
      <span className="text-xs">No data yet</span>
    </div>
  )
}

// ── Connection picker ──────────────────────────────────────────────────────

function ConnectionPicker({ connections, selected, onChange }: {
  connections: Connection[]
  selected: string
  onChange: (id: string) => void
}) {
  const [open, setOpen] = useState(false)
  const conn = connections.find((c) => c.connectionId === selected)

  return (
    <div className="relative">
      <button
        onClick={() => setOpen((v) => !v)}
        className="flex items-center gap-2 px-3 py-1.5 bg-(--bg-elevated) border border-(--border-default) rounded-lg text-sm text-(--text-primary) hover:border-violet-500/50 transition-colors"
      >
        <Database className="w-3.5 h-3.5 text-violet-400" />
        <span>{conn?.connectionName ?? 'Select connection'}</span>
        <ChevronDown className="w-3.5 h-3.5 text-(--text-muted)" />
      </button>
      {open && (
        <>
          <div className="fixed inset-0 z-10" onClick={() => setOpen(false)} />
          <div className="absolute right-0 top-full mt-1 z-20 min-w-48 bg-(--bg-elevated) border border-(--border-default) rounded-xl shadow-xl overflow-hidden">
            {connections.map((c) => (
              <button
                key={c.connectionId}
                onClick={() => { onChange(c.connectionId); setOpen(false) }}
                className={`w-full flex items-center gap-2 px-3 py-2.5 text-sm text-left hover:bg-(--bg-surface) transition-colors ${c.connectionId === selected ? 'text-violet-300' : 'text-(--text-primary)'}`}
              >
                <span className={`w-1.5 h-1.5 rounded-full shrink-0 ${c.isHealthy ? 'bg-green-400' : 'bg-red-400'}`} />
                {c.connectionName}
              </button>
            ))}
          </div>
        </>
      )}
    </div>
  )
}

// ── Dashboard grid ─────────────────────────────────────────────────────────

const PIE_COLORS = [C_GREEN, C_RED]

function DashboardGrid({ insight }: { insight: ConnectionInsights }) {
  const s = insight.summary
  const { width, containerRef, mounted } = useContainerWidth({ initialWidth: 1200 })
  const [layout, setLayout] = useState<LayoutItem[]>(loadLayout)

  const handleLayoutChange = useCallback((next: readonly LayoutItem[]) => {
    const arr = [...next]
    setLayout(arr)
    localStorage.setItem(STORAGE_KEY, JSON.stringify(arr))
  }, [])

  const resetLayout = useCallback(() => {
    setLayout(DEFAULT_LAYOUT)
    localStorage.removeItem(STORAGE_KEY)
  }, [])

  // ── data ──────────────────────────────────────────────────────────────
  const rawVolume = insight.volume ?? []
  const first  = rawVolume.findIndex((b) => b.total > 0)
  const last   = [...rawVolume].reverse().findIndex((b) => b.total > 0)
  const trimmed = first === -1
    ? rawVolume.slice(-14)
    : rawVolume.slice(first, rawVolume.length - (last === -1 ? 0 : last))

  const volumeData = trimmed.map((b) => ({
    date:        fmtDate(b.date),
    Successful:  b.successful,
    Failed:      b.failed,
    Total:       b.total,
    'Success %': b.total > 0 ? Math.round((b.successful / b.total) * 100) : 0,
  }))

  const hasVolume   = volumeData.some((d) => d.Total > 0)
  const failedTrend = volumeData.filter((d) => d.Failed > 0 || d.Total > 0)
  const successRate = s.totalQueries > 0
    ? Math.round((s.successfulQueries / s.totalQueries) * 100) : 0

  const pieData   = [
    { name: 'Successful', value: s.successfulQueries },
    { name: 'Failed',     value: s.failedQueries     },
  ]
  const topTables = (insight.topTables ?? []).map((t) => ({ table: t.tableName, Queries: t.queryCount }))
  const topUsers  = (insight.topUsers  ?? []).map((u) => ({
    user: u.userName || u.userEmail || u.userId.slice(0, 8), Queries: u.queryCount,
  }))

  const statCards: { label: string; value: string | number; icon: React.ReactNode; accent: StatAccent }[] = [
    { label: 'Total Queries',  value: formatNumber(s.totalQueries),       icon: <Zap          className="w-4 h-4" />, accent: 'violet' },
    { label: 'Successful',     value: formatNumber(s.successfulQueries),  icon: <CheckCircle2 className="w-4 h-4" />, accent: 'green'  },
    { label: 'Failed',         value: formatNumber(s.failedQueries),      icon: <XCircle      className="w-4 h-4" />, accent: 'red'    },
    { label: 'Success Rate',   value: `${successRate}%`,                  icon: <TrendingUp   className="w-4 h-4" />, accent: 'cyan'   },
    { label: 'Avg. Execution', value: formatMs(s.averageExecutionTimeMs), icon: <Clock        className="w-4 h-4" />, accent: 'amber'  },
    { label: 'Total Rows',     value: formatNumber(s.totalRowsReturned),  icon: <Rows3        className="w-4 h-4" />, accent: 'pink'   },
  ]

  return (
    <div>
      {/* Toolbar */}
      <div className="flex justify-end mb-2">
        <button
          onClick={resetLayout}
          className="flex items-center gap-1.5 px-2.5 py-1 text-xs text-(--text-muted) hover:text-(--text-primary) bg-(--bg-elevated) border border-(--border-subtle) rounded-lg transition-colors"
        >
          <RotateCcw className="w-3 h-3" />
          Reset layout
        </button>
      </div>

      {/* Grid container */}
      <div ref={containerRef}>
        {mounted && (
          <GridLayout
            width={width}
            layout={layout}
            onLayoutChange={handleLayoutChange}
            gridConfig={{
              cols: COLS,
              rowHeight: ROW_H,
              margin: [8, 8],
              containerPadding: [0, 0],
            }}
            dragConfig={{ enabled: true, handle: '.drag-handle' }}
            resizeConfig={{ enabled: true, handles: ['se'] }}
            autoSize
          >
            {/* ── KPI stat cards ─────────────────────────────────────── */}
            {statCards.map((card, idx) => (
              <div key={`stat-${idx}`} className="h-full">
                <StatCard {...card} />
              </div>
            ))}

            {/* ── Query Volume ──────────────────────────────────────── */}
            <div key="volume" className="h-full">
              <ChartCard title="Query Volume" sub="Successful vs Failed over time" badge="30d">
                {!hasVolume ? <EmptyChart /> : (
                  <ResponsiveContainer width="100%" height="100%">
                    <BarChart data={volumeData} margin={{ top: 4, right: 4, left: -20, bottom: 0 }}>
                      <CartesianGrid {...GRID_STROKE} />
                      <XAxis dataKey="date" tick={XTICK} stroke={AXIS_STROKE} {...AX} interval="preserveStartEnd" />
                      <YAxis tick={XTICK} stroke={AXIS_STROKE} {...AX} allowDecimals={false} />
                      <RechartsTooltip {...TT} />
                      <Bar dataKey="Successful" fill={C_GREEN} radius={[2, 2, 0, 0]} />
                      <Bar dataKey="Failed"     fill={C_RED}   radius={[2, 2, 0, 0]} />
                    </BarChart>
                  </ResponsiveContainer>
                )}
              </ChartCard>
            </div>

            {/* ── Success Rate ───────────────────────────────────────── */}
            <div key="success-rate" className="h-full">
              <ChartCard title="Success Rate Trend" sub="Daily % of successful queries">
                {!hasVolume ? <EmptyChart /> : (
                  <ResponsiveContainer width="100%" height="100%">
                    <LineChart data={volumeData} margin={{ top: 4, right: 4, left: -20, bottom: 0 }}>
                      <CartesianGrid {...GRID_STROKE} />
                      <XAxis dataKey="date" tick={XTICK} stroke={AXIS_STROKE} {...AX} interval="preserveStartEnd" />
                      <YAxis domain={[0, 100]} tickFormatter={(v) => `${v}%`} tick={XTICK} stroke={AXIS_STROKE} {...AX} />
                      <RechartsTooltip {...TT} formatter={(v: number) => [`${v}%`, 'Success Rate']} />
                      <Line type="monotone" dataKey="Success %" stroke={C_CYAN} strokeWidth={2} dot={{ fill: C_CYAN, r: 3 }} activeDot={{ r: 5 }} />
                    </LineChart>
                  </ResponsiveContainer>
                )}
              </ChartCard>
            </div>

            {/* ── Failed Trend ───────────────────────────────────────── */}
            <div key="failed-trend" className="h-full">
              <ChartCard title="Failed Queries" sub="Daily failed query count">
                {!hasVolume || failedTrend.every((d) => d.Failed === 0) ? <EmptyChart /> : (
                  <ResponsiveContainer width="100%" height="100%">
                    <AreaChart data={failedTrend} margin={{ top: 4, right: 4, left: -20, bottom: 0 }}>
                      <CartesianGrid {...GRID_STROKE} />
                      <XAxis dataKey="date" tick={XTICK} stroke={AXIS_STROKE} {...AX} interval="preserveStartEnd" />
                      <YAxis tick={XTICK} stroke={AXIS_STROKE} {...AX} allowDecimals={false} />
                      <RechartsTooltip {...TT} />
                      <Area type="monotone" dataKey="Failed" stroke={C_RED} strokeWidth={2} fill={C_RED} fillOpacity={0.15} />
                    </AreaChart>
                  </ResponsiveContainer>
                )}
              </ChartCard>
            </div>

            {/* ── Outcome Breakdown ──────────────────────────────────── */}
            <div key="outcome" className="h-full">
              <ChartCard title="Success vs Failure" sub="Overall query outcome breakdown">
                {s.totalQueries === 0 ? <EmptyChart /> : (
                  <ResponsiveContainer width="100%" height="100%">
                    <PieChart>
                      <Pie data={pieData} cx="50%" cy="50%" innerRadius="35%" outerRadius="55%" paddingAngle={2} dataKey="value" strokeWidth={0}>
                        {pieData.map((_, i) => <Cell key={i} fill={PIE_COLORS[i]} />)}
                      </Pie>
                      <RechartsTooltip {...TT} formatter={(v: number, n: string) => [formatNumber(v), n]} />
                    </PieChart>
                  </ResponsiveContainer>
                )}
              </ChartCard>
            </div>

            {/* ── Daily Activity ─────────────────────────────────────── */}
            <div key="daily" className="h-full">
              <ChartCard title="Daily Activity" sub="Total queries per day">
                {!hasVolume ? <EmptyChart /> : (
                  <ResponsiveContainer width="100%" height="100%">
                    <AreaChart data={volumeData} margin={{ top: 4, right: 4, left: -20, bottom: 0 }}>
                      <CartesianGrid {...GRID_STROKE} />
                      <XAxis dataKey="date" tick={XTICK} stroke={AXIS_STROKE} {...AX} interval="preserveStartEnd" />
                      <YAxis tick={XTICK} stroke={AXIS_STROKE} {...AX} allowDecimals={false} />
                      <RechartsTooltip {...TT} />
                      <Area type="monotone" dataKey="Total" stroke={C_CYAN} strokeWidth={2} fill={C_CYAN} fillOpacity={0.15} />
                    </AreaChart>
                  </ResponsiveContainer>
                )}
              </ChartCard>
            </div>

            {/* ── Top Tables ─────────────────────────────────────────── */}
            <div key="top-tables" className="h-full">
              <ChartCard title="Top Tables Queried" sub="Most queried tables">
                {topTables.length === 0 ? <EmptyChart /> : (
                  <ResponsiveContainer width="100%" height="100%">
                    <BarChart data={topTables} layout="vertical" margin={{ top: 0, right: 8, left: 0, bottom: 0 }}>
                      <CartesianGrid {...GRID_STROKE} horizontal={false} />
                      <XAxis type="number" tick={XTICK} stroke={AXIS_STROKE} {...AX} allowDecimals={false} />
                      <YAxis dataKey="table" type="category" tick={XTICK} stroke={AXIS_STROKE} {...AX} width={120} />
                      <RechartsTooltip {...TT} />
                      <Bar dataKey="Queries" fill={C_VIOLET} radius={[0, 4, 4, 0]} />
                    </BarChart>
                  </ResponsiveContainer>
                )}
              </ChartCard>
            </div>

            {/* ── Top Users ──────────────────────────────────────────── */}
            <div key="top-users" className="h-full">
              <ChartCard title="Top Users" sub="Users with the most queries">
                {topUsers.length === 0 ? (
                  <div className="w-full h-full flex flex-col items-center justify-center gap-2 text-(--text-muted)">
                    <Users className="w-6 h-6 opacity-30" />
                    <span className="text-xs">Admin view required</span>
                  </div>
                ) : (
                  <ResponsiveContainer width="100%" height="100%">
                    <BarChart data={topUsers} margin={{ top: 4, right: 4, left: -20, bottom: 0 }}>
                      <CartesianGrid {...GRID_STROKE} />
                      <XAxis dataKey="user" tick={XTICK} stroke={AXIS_STROKE} {...AX} />
                      <YAxis tick={XTICK} stroke={AXIS_STROKE} {...AX} allowDecimals={false} />
                      <RechartsTooltip {...TT} />
                      <Bar dataKey="Queries" fill={C_CYAN} radius={[4, 4, 0, 0]} />
                    </BarChart>
                  </ResponsiveContainer>
                )}
              </ChartCard>
            </div>
          </GridLayout>
        )}
      </div>

      {/* Footer */}
      {(s.firstQueryDate || s.lastQueryDate) && (
        <div className="flex items-center gap-5 px-1 pt-3 mt-1 border-t border-(--border-subtle)">
          {s.firstQueryDate && (
            <span className="flex items-center gap-1.5 text-xs text-(--text-muted)">
              <Table2 className="w-3 h-3" />
              First query: <span className="text-(--text-secondary) ml-0.5">{new Date(s.firstQueryDate).toLocaleDateString()}</span>
            </span>
          )}
          {s.lastQueryDate && (
            <span className="flex items-center gap-1.5 text-xs text-(--text-muted)">
              <Table2 className="w-3 h-3" />
              Last query: <span className="text-(--text-secondary) ml-0.5">{new Date(s.lastQueryDate).toLocaleDateString()}</span>
            </span>
          )}
        </div>
      )}
    </div>
  )
}

// ── Page ───────────────────────────────────────────────────────────────────

export default function DashboardPage() {
  const { activeConnectionId, setActiveConnection } = useConnectionStore()
  const [viewMode, setViewMode] = useState<'mine' | 'all'>('mine')

  const { data: connections = [], isLoading: connectionsLoading } = useQuery({
    queryKey: ['connections'],
    queryFn: connectionsApi.list,
    staleTime: 60_000,
  })

  const selectedId = activeConnectionId ?? connections[0]?.connectionId ?? ''
  const activeConn = connections.find((c) => c.connectionId === selectedId)
  const isAdmin    = activeConn?.isAdmin ?? false
  const useAdminEp = isAdmin && viewMode === 'all'

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
      <div className="p-6 space-y-4">
        <Skeleton className="h-8 w-48 rounded-lg bg-(--bg-surface)" />
        <div className="grid grid-cols-3 xl:grid-cols-6 gap-2">
          {[1,2,3,4,5,6].map((i) => <Skeleton key={i} className="h-16 rounded-xl bg-(--bg-surface)" />)}
        </div>
        <Skeleton className="h-52 rounded-xl bg-(--bg-surface)" />
        <div className="grid grid-cols-2 gap-2">
          {[1,2,3,4].map((i) => <Skeleton key={i} className="h-48 rounded-xl bg-(--bg-surface)" />)}
        </div>
      </div>
    )
  }

  if (connections.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center h-full text-center gap-3">
        <div className="w-14 h-14 rounded-2xl bg-violet-500/10 ring-1 ring-violet-500/20 flex items-center justify-center mb-1">
          <Database className="w-7 h-7 text-violet-400" />
        </div>
        <p className="text-(--text-primary) font-medium">No connections yet</p>
        <p className="text-(--text-muted) text-sm">Add a connection to start seeing insights</p>
      </div>
    )
  }

  return (
    <div className="h-full overflow-y-auto">
      <div className="p-6 space-y-4">
        {/* Header */}
        <div className="flex items-center justify-between gap-4 flex-wrap">
          <div>
            <h1 className="text-lg font-semibold text-(--text-primary)">Dashboard</h1>
            <p className="text-xs text-(--text-muted) mt-0.5">
              Insights · last 30 days
              {isAdmin && viewMode === 'all' && (
                <span className="ml-2 text-violet-400 font-medium">· All users</span>
              )}
            </p>
          </div>

          <div className="flex items-center gap-3">
            {isAdmin && (
              <div className="flex items-center bg-(--bg-elevated) border border-(--border-default) rounded-lg p-0.5">
                <button
                  onClick={() => setViewMode('mine')}
                  className={`px-3 py-1 text-xs rounded-md transition-colors ${
                    viewMode === 'mine' ? 'bg-violet-600 text-white' : 'text-(--text-muted) hover:text-(--text-primary)'
                  }`}
                >
                  My Data
                </button>
                <button
                  onClick={() => setViewMode('all')}
                  className={`px-3 py-1 text-xs rounded-md transition-colors ${
                    viewMode === 'all' ? 'bg-violet-600 text-white' : 'text-(--text-muted) hover:text-(--text-primary)'
                  }`}
                >
                  All Users
                </button>
              </div>
            )}

            <ConnectionPicker
              connections={connections}
              selected={selectedId}
              onChange={(id) => { setActiveConnection(id); setViewMode('mine') }}
            />
          </div>
        </div>

        {insightLoading ? (
          <div className="space-y-3">
            <div className="grid grid-cols-3 xl:grid-cols-6 gap-2">
              {[1,2,3,4,5,6].map((i) => <Skeleton key={i} className="h-16 rounded-xl bg-(--bg-surface)" />)}
            </div>
            {[1,2,3].map((i) => <Skeleton key={i} className="h-48 rounded-xl bg-(--bg-surface)" />)}
          </div>
        ) : isError ? (
          <div className="flex items-center gap-3 rounded-xl border border-amber-500/20 bg-amber-500/10 px-4 py-3">
            <AlertCircle className="w-4 h-4 text-amber-400 shrink-0" />
            <p className="text-sm text-amber-300">Failed to load insights for this connection.</p>
          </div>
        ) : insight ? (
          <DashboardGrid insight={insight} />
        ) : null}
      </div>
    </div>
  )
}
