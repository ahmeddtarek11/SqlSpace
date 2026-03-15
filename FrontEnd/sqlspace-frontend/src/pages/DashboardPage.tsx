import { useQuery } from '@tanstack/react-query'
import { motion } from 'framer-motion'
import {
  AreaChart, Area, XAxis, YAxis, Tooltip as RechartsTooltip,
  ResponsiveContainer, BarChart, Bar, CartesianGrid,
} from 'recharts'
import { Database, Zap, Clock, CheckCircle2, XCircle } from 'lucide-react'
import { Skeleton } from '@/components/ui/skeleton'
import { insightsApi } from '@/api/insights'
import { useConnectionStore } from '@/stores/connection-store'
import { formatMs, formatNumber } from '@/lib/utils'

function StatCard({ label, value, icon, color, delay = 0 }: {
  label: string; value: string | number; icon: React.ReactNode; color: string; delay?: number
}) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 16 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ delay, duration: 0.4 }}
      className="bg-(--bg-surface) border border-(--border-default) rounded-xl p-5 flex items-center gap-4"
    >
      <div className={`w-10 h-10 rounded-lg flex items-center justify-center ${color}`}>{icon}</div>
      <div>
        <p className="text-2xl font-semibold text-white">{value}</p>
        <p className="text-xs text-(--text-muted) mt-0.5">{label}</p>
      </div>
    </motion.div>
  )
}

export default function DashboardPage() {
  const { activeConnectionId, connections } = useConnectionStore()
  const activeConn = connections.find((c) => c.connectionId === activeConnectionId)

  const { data: insight, isLoading } = useQuery({
    queryKey: ['insights', activeConnectionId],
    queryFn: () => insightsApi.getForConnection(activeConnectionId!),
    enabled: !!activeConnectionId,
  })

  if (!activeConnectionId) {
    return (
      <div className="flex flex-col items-center justify-center h-full text-center gap-3">
        <Database className="w-12 h-12 text-(--text-muted)" />
        <p className="text-(--text-muted) text-sm">Select a connection in the Workspace to view insights</p>
      </div>
    )
  }

  if (isLoading) {
    return (
      <div className="p-6 space-y-6">
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
          {[1, 2, 3, 4].map((i) => <Skeleton key={i} className="h-24 rounded-xl bg-(--bg-surface)" />)}
        </div>
        <Skeleton className="h-64 rounded-xl bg-(--bg-surface)" />
      </div>
    )
  }

  if (!insight) {
    return <div className="flex items-center justify-center h-full text-(--text-muted)">No data available</div>
  }

  const volumeData = (insight.volume ?? []).map((b) => ({ date: b.bucket, count: b.count }))
  const topTablesData = (insight.topTables ?? []).map((t) => ({ table: t.tableName, count: t.queryCount }))
  const s = insight.summary

  return (
    <div className="h-full overflow-y-auto p-6 space-y-6">
      {activeConn && (
        <p className="text-sm text-(--text-muted)">
          Insights for <span className="text-violet-300 font-medium">{activeConn.connectionName}</span>
        </p>
      )}

      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        <StatCard label="Total Queries" value={formatNumber(s.totalQueries)} icon={<Zap className="w-5 h-5 text-violet-300" />} color="bg-violet-600/20" delay={0} />
        <StatCard label="Successful" value={formatNumber(s.successfulQueries)} icon={<CheckCircle2 className="w-5 h-5 text-green-300" />} color="bg-green-600/20" delay={0.05} />
        <StatCard label="Failed" value={formatNumber(s.failedQueries)} icon={<XCircle className="w-5 h-5 text-red-300" />} color="bg-red-600/20" delay={0.1} />
        <StatCard label="Avg. Execution" value={formatMs(s.averageExecutionTimeMs)} icon={<Clock className="w-5 h-5 text-amber-300" />} color="bg-amber-600/20" delay={0.15} />
      </div>

      {volumeData.length > 0 && (
        <motion.div initial={{ opacity: 0, y: 16 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.2 }} className="bg-(--bg-surface) border border-(--border-default) rounded-xl p-5">
          <p className="text-sm font-medium text-white mb-4">Query Volume</p>
          <ResponsiveContainer width="100%" height={200}>
            <AreaChart data={volumeData}>
              <defs>
                <linearGradient id="violet-grad" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="5%" stopColor="#7c3aed" stopOpacity={0.3} />
                  <stop offset="95%" stopColor="#7c3aed" stopOpacity={0} />
                </linearGradient>
              </defs>
              <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.05)" />
              <XAxis dataKey="date" tick={{ fill: '#5a5a7a', fontSize: 11 }} axisLine={false} tickLine={false} />
              <YAxis tick={{ fill: '#5a5a7a', fontSize: 11 }} axisLine={false} tickLine={false} />
              <RechartsTooltip contentStyle={{ background: '#16162a', border: '1px solid rgba(255,255,255,0.1)', borderRadius: 8, fontSize: 12 }} labelStyle={{ color: '#9898b8' }} itemStyle={{ color: '#a78bfa' }} />
              <Area type="monotone" dataKey="count" stroke="#7c3aed" strokeWidth={2} fill="url(#violet-grad)" />
            </AreaChart>
          </ResponsiveContainer>
        </motion.div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
        {topTablesData.length > 0 && (
          <motion.div initial={{ opacity: 0, y: 16 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.25 }} className="bg-(--bg-surface) border border-(--border-default) rounded-xl p-5">
            <p className="text-sm font-medium text-white mb-4">Top Tables</p>
            <ResponsiveContainer width="100%" height={180}>
              <BarChart data={topTablesData} layout="vertical">
                <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.05)" horizontal={false} />
                <XAxis type="number" tick={{ fill: '#5a5a7a', fontSize: 11 }} axisLine={false} tickLine={false} />
                <YAxis dataKey="table" type="category" tick={{ fill: '#9898b8', fontSize: 11 }} axisLine={false} tickLine={false} width={80} />
                <RechartsTooltip contentStyle={{ background: '#16162a', border: '1px solid rgba(255,255,255,0.1)', borderRadius: 8, fontSize: 12 }} itemStyle={{ color: '#22d3ee' }} />
                <Bar dataKey="count" fill="#06b6d4" radius={[0, 4, 4, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </motion.div>
        )}

        <motion.div initial={{ opacity: 0, y: 16 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.3 }} className="bg-(--bg-surface) border border-(--border-default) rounded-xl p-5">
          <p className="text-sm font-medium text-white mb-4">Summary</p>
          <div className="space-y-3">
            {[
              { label: 'Total queries', value: formatNumber(s.totalQueries) },
              { label: 'Successful', value: formatNumber(s.successfulQueries) },
              { label: 'Failed', value: formatNumber(s.failedQueries) },
              { label: 'Avg. time', value: formatMs(s.averageExecutionTimeMs) },
              { label: 'Total rows returned', value: formatNumber(s.totalRowsReturned) },
            ].map(({ label, value }) => (
              <div key={label} className="flex items-center justify-between text-sm">
                <span className="text-(--text-muted)">{label}</span>
                <span className="text-white font-medium">{value}</span>
              </div>
            ))}
          </div>
        </motion.div>
      </div>
    </div>
  )
}
