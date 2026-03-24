import { useMemo } from 'react'
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  BarElement,
  LineElement,
  PointElement,
  ArcElement,
  Filler,
  Tooltip,
  Legend,
  RadialLinearScale,
  type ChartData,
  type ChartOptions,
} from 'chart.js'
import { Bar, Line, Pie, Doughnut, Scatter, Radar, PolarArea } from 'react-chartjs-2'
import { TreemapController, TreemapElement } from 'chartjs-chart-treemap'
import type { ChartType, ChartConfig } from '@/types'

ChartJS.register(
  CategoryScale,
  LinearScale,
  BarElement,
  LineElement,
  PointElement,
  ArcElement,
  Filler,
  Tooltip,
  Legend,
  RadialLinearScale,
  TreemapController,
  TreemapElement,
)

const DEFAULT_COLORS = [
  '#0ea5e9', '#f43f5e', '#22c55e', '#f59e0b', '#8b5cf6',
  '#06b6d4', '#ec4899', '#84cc16', '#f97316', '#6366f1',
]

function resolveKey(key: string | undefined, actualKeys: string[]): string {
  if (!key) return ''
  const lower = key.toLowerCase()
  return actualKeys.find((k) => k.toLowerCase() === lower) ?? key
}

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

const DARK_GRID = 'rgba(255,255,255,0.06)'
const DARK_TICK = '#71717a'

function baseCartesianOptions(indexAxis: 'x' | 'y' = 'x'): ChartOptions<'bar' | 'line'> {
  return {
    responsive: true,
    maintainAspectRatio: false,
    indexAxis,
    plugins: {
      legend: { display: true, labels: { color: '#a1a1aa', font: { size: 11 }, boxWidth: 10 } },
      tooltip: {
        backgroundColor: '#18181b',
        titleColor: '#fff',
        bodyColor: '#d4d4d8',
        borderColor: 'rgba(255,255,255,0.1)',
        borderWidth: 1,
        cornerRadius: 8,
        padding: 8,
      },
    },
    scales: {
      x: {
        grid: { color: indexAxis === 'x' ? 'transparent' : DARK_GRID },
        ticks: { color: DARK_TICK, font: { size: 11 }, maxRotation: 45 },
        border: { display: false },
      },
      y: {
        grid: { color: indexAxis === 'y' ? 'transparent' : DARK_GRID },
        ticks: { color: DARK_TICK, font: { size: 11 } },
        border: { display: false },
      },
    },
  }
}

function basePolarOptions(): ChartOptions<'pie' | 'doughnut' | 'radar' | 'polarArea'> {
  return {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { display: true, position: 'bottom', labels: { color: '#a1a1aa', font: { size: 11 }, boxWidth: 10, padding: 12 } },
      tooltip: {
        backgroundColor: '#18181b',
        titleColor: '#fff',
        bodyColor: '#d4d4d8',
        borderColor: 'rgba(255,255,255,0.1)',
        borderWidth: 1,
        cornerRadius: 8,
        padding: 8,
      },
    },
  }
}

interface ChartRendererProps {
  chartType: ChartType
  config: ChartConfig
  data: Record<string, unknown>[]
}

export function ChartRenderer({ chartType, config, data }: ChartRendererProps) {
  const resolved = useMemo(() => autoDetectConfig(data, config), [data, config])
  const actualKeys = useMemo(() => (data.length > 0 ? Object.keys(data[0]) : []), [data])

  const xKey = useMemo(() => resolveKey(resolved.xAxis, actualKeys), [resolved.xAxis, actualKeys])
  const yKeys = useMemo(
    () => (resolved.yAxis ?? []).map((k) => resolveKey(k, actualKeys)),
    [resolved.yAxis, actualKeys],
  )
  const lKey = useMemo(
    () => resolveKey(resolved.labelKey ?? resolved.xAxis, actualKeys),
    [resolved.labelKey, resolved.xAxis, actualKeys],
  )
  const vKey = useMemo(
    () => resolveKey(resolved.valueKey ?? resolved.yAxis?.[0], actualKeys),
    [resolved.valueKey, resolved.yAxis, actualKeys],
  )

  const colors = resolved.colors ?? DEFAULT_COLORS

  if (!data || data.length === 0) {
    return (
      <div className="h-full flex items-center justify-center text-zinc-600 text-sm">
        No data available.
      </div>
    )
  }

  const labels = data.map((d) => String(d[xKey] ?? d[lKey] ?? ''))

  switch (chartType) {
    case 'bar':
    case 'stacked_bar': {
      const chartData: ChartData<'bar'> = {
        labels,
        datasets: yKeys.map((key, i) => ({
          label: key,
          data: data.map((d) => Number(d[key]) || 0),
          backgroundColor: colors[i % colors.length],
          borderRadius: 4,
          borderSkipped: false as const,
        })),
      }
      const opts = baseCartesianOptions() as ChartOptions<'bar'>
      if (chartType === 'stacked_bar' || resolved.stacked) {
        opts.scales = {
          ...opts.scales,
          x: { ...opts.scales!.x, stacked: true },
          y: { ...opts.scales!.y, stacked: true },
        }
      }
      return <div className="h-full w-full"><Bar data={chartData} options={opts} /></div>
    }

    case 'horizontal_bar': {
      // For horizontal bars, ensure labels are the category (string) column
      // and data values are the numeric column — even if the AI swapped them.
      const sample = data[0]
      const catKey = typeof sample[xKey] === 'string' ? xKey : yKeys.find((k) => typeof sample[k] === 'string') ?? xKey
      const valKeys = yKeys.filter((k) => typeof sample[k] === 'number')
      const effectiveValKeys = valKeys.length > 0 ? valKeys : (typeof sample[xKey] === 'number' ? [xKey] : yKeys)

      const hLabels = data.map((d) => String(d[catKey] ?? ''))
      const chartData: ChartData<'bar'> = {
        labels: hLabels,
        datasets: effectiveValKeys.map((key, i) => ({
          label: key,
          data: data.map((d) => Number(d[key]) || 0),
          backgroundColor: colors[i % colors.length],
          borderRadius: 4,
          borderSkipped: false as const,
        })),
      }
      return <div className="h-full w-full"><Bar data={chartData} options={baseCartesianOptions('y') as ChartOptions<'bar'>} /></div>
    }

    case 'line': {
      const chartData: ChartData<'line'> = {
        labels,
        datasets: yKeys.map((key, i) => ({
          label: key,
          data: data.map((d) => Number(d[key]) || 0),
          borderColor: colors[i % colors.length],
          backgroundColor: 'transparent',
          borderWidth: 2,
          pointRadius: 2,
          tension: 0.3,
        })),
      }
      return <div className="h-full w-full"><Line data={chartData} options={baseCartesianOptions() as ChartOptions<'line'>} /></div>
    }

    case 'area': {
      const chartData: ChartData<'line'> = {
        labels,
        datasets: yKeys.map((key, i) => ({
          label: key,
          data: data.map((d) => Number(d[key]) || 0),
          borderColor: colors[i % colors.length],
          backgroundColor: colors[i % colors.length] + '20',
          borderWidth: 2,
          pointRadius: 0,
          fill: true,
          tension: 0.3,
        })),
      }
      return <div className="h-full w-full"><Line data={chartData} options={baseCartesianOptions() as ChartOptions<'line'>} /></div>
    }

    case 'pie': {
      const pieLabels = data.map((d) => String(d[lKey] ?? ''))
      const pieValues = data.map((d) => Number(d[vKey]) || 0)
      const chartData: ChartData<'pie'> = {
        labels: pieLabels,
        datasets: [{
          data: pieValues,
          backgroundColor: data.map((_, i) => colors[i % colors.length]),
          borderColor: '#111113',
          borderWidth: 2,
        }],
      }
      return <div className="h-full w-full"><Pie data={chartData} options={basePolarOptions() as ChartOptions<'pie'>} /></div>
    }

    case 'donut': {
      const dLabels = data.map((d) => String(d[lKey] ?? ''))
      const dValues = data.map((d) => Number(d[vKey]) || 0)
      const chartData: ChartData<'doughnut'> = {
        labels: dLabels,
        datasets: [{
          data: dValues,
          backgroundColor: data.map((_, i) => colors[i % colors.length]),
          borderColor: '#111113',
          borderWidth: 2,
        }],
      }
      const opts: ChartOptions<'doughnut'> = {
        ...(basePolarOptions() as ChartOptions<'doughnut'>),
        cutout: '60%',
      }
      return <div className="h-full w-full"><Doughnut data={chartData} options={opts} /></div>
    }

    case 'scatter': {
      const chartData: ChartData<'scatter'> = {
        datasets: [{
          label: `${xKey} vs ${yKeys[0] ?? ''}`,
          data: data.map((d) => ({
            x: Number(d[xKey]) || 0,
            y: Number(d[yKeys[0]]) || 0,
          })),
          backgroundColor: colors[0] + '80',
          borderColor: colors[0],
          pointRadius: 4,
        }],
      }
      const opts: ChartOptions<'scatter'> = {
        ...(baseCartesianOptions() as unknown as ChartOptions<'scatter'>),
      }
      return <div className="h-full w-full"><Scatter data={chartData} options={opts} /></div>
    }

    case 'radar': {
      const radarKeys = resolved.dataKeys?.map((k) => resolveKey(k, actualKeys)) ?? yKeys
      const radarLabels = data.map((d) => String(d[xKey] ?? ''))
      const chartData: ChartData<'radar'> = {
        labels: radarLabels,
        datasets: radarKeys.map((key, i) => ({
          label: key,
          data: data.map((d) => Number(d[key]) || 0),
          borderColor: colors[i % colors.length],
          backgroundColor: colors[i % colors.length] + '20',
          borderWidth: 2,
          pointRadius: 3,
          pointBackgroundColor: colors[i % colors.length],
        })),
      }
      const opts: ChartOptions<'radar'> = {
        ...(basePolarOptions() as ChartOptions<'radar'>),
        scales: {
          r: {
            grid: { color: DARK_GRID },
            angleLines: { color: DARK_GRID },
            pointLabels: { color: DARK_TICK, font: { size: 10 } },
            ticks: { display: false },
          },
        },
      }
      return <div className="h-full w-full"><Radar data={chartData} options={opts} /></div>
    }

    case 'radial_bar': {
      const rbLabels = data.map((d) => String(d[lKey] ?? ''))
      const rbKeys = resolved.dataKeys?.map((k) => resolveKey(k, actualKeys)) ?? yKeys
      const rbKey = rbKeys[0] ?? vKey
      const rbValues = data.map((d) => Number(d[rbKey]) || 0)
      const chartData: ChartData<'polarArea'> = {
        labels: rbLabels,
        datasets: [{
          data: rbValues,
          backgroundColor: data.map((_, i) => colors[i % colors.length] + '80'),
          borderColor: data.map((_, i) => colors[i % colors.length]),
          borderWidth: 2,
        }],
      }
      const opts: ChartOptions<'polarArea'> = {
        ...(basePolarOptions() as ChartOptions<'polarArea'>),
        scales: {
          r: {
            grid: { color: DARK_GRID },
            ticks: { display: false },
          },
        },
      }
      return <div className="h-full w-full"><PolarArea data={chartData} options={opts} /></div>
    }

    case 'composed': {
      const bKeys = (resolved.barKeys ?? []).map((k) => resolveKey(k, actualKeys))
      const lnKeys = (resolved.lineKeys ?? []).map((k) => resolveKey(k, actualKeys))
      const allKeys = bKeys.length > 0 || lnKeys.length > 0
        ? [...bKeys, ...lnKeys]
        : yKeys
      const barSet = new Set(bKeys.length > 0 ? bKeys : yKeys.slice(0, Math.ceil(yKeys.length / 2)))
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      const datasets: any[] = allKeys.map((key, i) => {
        const isBar = barSet.has(key)
        return {
          label: key,
          data: data.map((d) => Number(d[key]) || 0),
          backgroundColor: isBar ? colors[i % colors.length] : 'transparent',
          borderColor: colors[i % colors.length],
          borderWidth: isBar ? 0 : 2,
          borderRadius: isBar ? 4 : 0,
          type: isBar ? 'bar' : 'line',
          pointRadius: isBar ? 0 : 2,
          tension: 0.3,
          order: isBar ? 1 : 0,
          yAxisID: 'y',
        }
      })
      const chartData = { labels, datasets }
      return <div className="h-full w-full"><Bar data={chartData as ChartData<'bar'>} options={baseCartesianOptions() as ChartOptions<'bar'>} /></div>
    }

    case 'treemap': {
      const tmData = data.map((d, i) => ({
        label: String(d[lKey] ?? d[xKey] ?? `Item ${i}`),
        value: Number(d[vKey] ?? d[yKeys[0]]) || 0,
        color: colors[i % colors.length],
      }))
      // Render as horizontal bar — more readable than treemap for most data
      const fallbackData: ChartData<'bar'> = {
        labels: tmData.map((d) => d.label),
        datasets: [{
          label: vKey || yKeys[0] || 'Value',
          data: tmData.map((d) => d.value),
          backgroundColor: tmData.map((d) => d.color + '80'),
          borderColor: tmData.map((d) => d.color),
          borderWidth: 1,
          borderRadius: 4,
          borderSkipped: false as const,
        }],
      }
      return <div className="h-full w-full"><Bar data={fallbackData} options={baseCartesianOptions('y') as ChartOptions<'bar'>} /></div>
    }

    case 'funnel': {
      // Funnel rendered as horizontal bar sorted by value (largest at top)
      const funnelData = data
        .map((d) => ({
          label: String(d[lKey] ?? d[xKey] ?? ''),
          value: Number(d[vKey] ?? d[yKeys[0]]) || 0,
        }))
        .sort((a, b) => b.value - a.value)
      const chartData: ChartData<'bar'> = {
        labels: funnelData.map((d) => d.label),
        datasets: [{
          label: vKey || yKeys[0] || 'Value',
          data: funnelData.map((d) => d.value),
          backgroundColor: funnelData.map((_, i) => colors[i % colors.length] + '80'),
          borderColor: funnelData.map((_, i) => colors[i % colors.length]),
          borderWidth: 1,
          borderRadius: 4,
          borderSkipped: false as const,
        }],
      }
      return <div className="h-full w-full"><Bar data={chartData} options={baseCartesianOptions('y') as ChartOptions<'bar'>} /></div>
    }

    default:
      return <div className="text-zinc-500 text-sm">Unsupported chart type: {chartType}</div>
  }
}
