import {
  BarChart, Bar,
  LineChart, Line,
  AreaChart, Area,
  PieChart, Pie, Cell,
  ScatterChart, Scatter,
  XAxis, YAxis, CartesianGrid,
  Tooltip as RechartsTooltip,
  ResponsiveContainer, Legend,
} from 'recharts'
import type { ChartType, ChartConfig } from '@/types'

const DEFAULT_COLORS = [
  '#0ea5e9', '#f43f5e', '#22c55e', '#f59e0b', '#8b5cf6',
  '#06b6d4', '#ec4899', '#84cc16', '#f97316', '#6366f1',
]

const TT_STYLE = {
  backgroundColor: '#18181b',
  border: '1px solid rgba(255,255,255,0.10)',
  borderRadius: '8px',
  color: '#fff',
  fontSize: 12,
}

interface ChartRendererProps {
  chartType: ChartType
  config: ChartConfig
  data: Record<string, unknown>[]
}

/**
 * SQL engines often lowercase column aliases while the AI-generated chart config
 * may use PascalCase. Build a case-insensitive map so Recharts dataKey always
 * points to an actual key present in the data objects.
 */
function resolveKey(key: string | undefined, actualKeys: string[]): string {
  if (!key) return ''
  const lower = key.toLowerCase()
  return actualKeys.find((k) => k.toLowerCase() === lower) ?? key
}

/**
 * When chartConfigJson is empty ("{}"), auto-detect axes from the data:
 * first string-ish column → xAxis, remaining numeric columns → yAxis.
 */
function autoDetectConfig(data: Record<string, unknown>[], config: ChartConfig): ChartConfig {
  if (config.xAxis || (config.yAxis && config.yAxis.length > 0)) return config
  if (data.length === 0) return config

  const keys = Object.keys(data[0])
  const sample = data[0]
  const stringKeys: string[] = []
  const numericKeys: string[] = []

  for (const k of keys) {
    if (typeof sample[k] === 'number') numericKeys.push(k)
    else stringKeys.push(k)
  }

  return {
    ...config,
    xAxis: stringKeys[0] ?? keys[0],
    yAxis: numericKeys.length > 0 ? numericKeys : keys.slice(1),
  }
}

export function ChartRenderer({ chartType, config, data }: ChartRendererProps) {
  if (!data || data.length === 0) {
    return (
      <div className="h-full flex items-center justify-center text-zinc-600 text-sm">
        No data available.
      </div>
    )
  }

  const resolved = autoDetectConfig(data, config)
  const colors = resolved.colors ?? DEFAULT_COLORS

  // Resolve config keys against actual data keys (case-insensitive)
  const actualKeys = data.length > 0 ? Object.keys(data[0]) : []
  const xKey = resolveKey(resolved.xAxis, actualKeys)
  const yKeys = (resolved.yAxis ?? []).map((k) => resolveKey(k, actualKeys))
  const lKey = resolveKey(resolved.labelKey ?? resolved.xAxis, actualKeys)
  const vKey = resolveKey(resolved.valueKey ?? resolved.yAxis?.[0], actualKeys)

  switch (chartType) {
    case 'bar':
      return (
        <ResponsiveContainer width="100%" height="100%">
          <BarChart data={data} margin={{ top: 4, right: 4, left: -20, bottom: 0 }}>
            <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.05)" vertical={false} />
            <XAxis dataKey={xKey} axisLine={false} tickLine={false} tick={{ fill: '#52525b', fontSize: 11 }} dy={8} />
            <YAxis axisLine={false} tickLine={false} tick={{ fill: '#52525b', fontSize: 11 }} allowDecimals={false} />
            <RechartsTooltip cursor={{ fill: 'rgba(255,255,255,0.02)' }} contentStyle={TT_STYLE} />
            <Legend iconType="circle" iconSize={8} wrapperStyle={{ fontSize: 12, color: '#a1a1aa', paddingTop: 12 }} />
            {yKeys.map((key, i) => (
              <Bar key={key} dataKey={key} fill={colors[i % colors.length]} radius={[4, 4, 0, 0]} stackId={resolved.stacked ? 'a' : undefined} />
            ))}
          </BarChart>
        </ResponsiveContainer>
      )

    case 'line':
      return (
        <ResponsiveContainer width="100%" height="100%">
          <LineChart data={data} margin={{ top: 4, right: 4, left: -20, bottom: 0 }}>
            <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.05)" vertical={false} />
            <XAxis dataKey={xKey} axisLine={false} tickLine={false} tick={{ fill: '#52525b', fontSize: 11 }} dy={8} />
            <YAxis axisLine={false} tickLine={false} tick={{ fill: '#52525b', fontSize: 11 }} allowDecimals={false} />
            <RechartsTooltip contentStyle={TT_STYLE} />
            <Legend iconType="circle" iconSize={8} wrapperStyle={{ fontSize: 12, color: '#a1a1aa', paddingTop: 12 }} />
            {yKeys.map((key, i) => (
              <Line key={key} type="monotone" dataKey={key} stroke={colors[i % colors.length]} strokeWidth={2} dot={false} />
            ))}
          </LineChart>
        </ResponsiveContainer>
      )

    case 'area':
      return (
        <ResponsiveContainer width="100%" height="100%">
          <AreaChart data={data} margin={{ top: 4, right: 4, left: -20, bottom: 0 }}>
            <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.05)" vertical={false} />
            <XAxis dataKey={xKey} axisLine={false} tickLine={false} tick={{ fill: '#52525b', fontSize: 11 }} dy={8} />
            <YAxis axisLine={false} tickLine={false} tick={{ fill: '#52525b', fontSize: 11 }} allowDecimals={false} />
            <RechartsTooltip contentStyle={TT_STYLE} />
            <Legend iconType="circle" iconSize={8} wrapperStyle={{ fontSize: 12, color: '#a1a1aa', paddingTop: 12 }} />
            {yKeys.map((key, i) => (
              <Area key={key} type="monotone" dataKey={key} stroke={colors[i % colors.length]} fill={colors[i % colors.length]} fillOpacity={0.15} strokeWidth={2} />
            ))}
          </AreaChart>
        </ResponsiveContainer>
      )

    case 'pie': {
      return (
        <ResponsiveContainer width="100%" height="100%">
          <PieChart>
            <Pie
              data={data}
              dataKey={vKey}
              nameKey={lKey}
              cx="50%"
              cy="50%"
              outerRadius="75%"
              label={({ name, percent }: { name: string; percent: number }) => `${name} ${(percent * 100).toFixed(0)}%`}
              labelLine={false}
            >
              {data.map((_, i) => (
                <Cell key={i} fill={colors[i % colors.length]} />
              ))}
            </Pie>
            <RechartsTooltip contentStyle={TT_STYLE} />
            <Legend iconType="circle" iconSize={8} wrapperStyle={{ fontSize: 12, color: '#a1a1aa' }} />
          </PieChart>
        </ResponsiveContainer>
      )
    }

    case 'scatter':
      return (
        <ResponsiveContainer width="100%" height="100%">
          <ScatterChart margin={{ top: 4, right: 4, left: -20, bottom: 0 }}>
            <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.05)" />
            <XAxis dataKey={xKey} name={xKey} axisLine={false} tickLine={false} tick={{ fill: '#52525b', fontSize: 11 }} />
            <YAxis dataKey={yKeys[0]} name={yKeys[0]} axisLine={false} tickLine={false} tick={{ fill: '#52525b', fontSize: 11 }} />
            <RechartsTooltip contentStyle={TT_STYLE} />
            <Scatter data={data} fill={colors[0]} />
          </ScatterChart>
        </ResponsiveContainer>
      )

    default:
      return <div className="text-zinc-500 text-sm">Unsupported chart type: {chartType}</div>
  }
}
