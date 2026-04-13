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
import { Bar, Line, Pie, Doughnut, Scatter, Bubble, Radar, PolarArea } from 'react-chartjs-2'
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

function baseCartesianOptions(indexAxis: 'x' | 'y' = 'x', compact = false): ChartOptions<'bar' | 'line'> {
  if (compact) {
    return {
      responsive: true,
      maintainAspectRatio: false,
      indexAxis,
      plugins: {
        legend: { display: false },
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
        x: { display: false },
        y: { display: false },
      },
    }
  }
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

function basePolarOptions(compact = false): ChartOptions<'pie' | 'doughnut' | 'radar' | 'polarArea'> {
  return {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: compact
        ? { display: false }
        : { display: true, position: 'bottom', labels: { color: '#a1a1aa', font: { size: 11 }, boxWidth: 10, padding: 12 } },
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
  compact?: boolean
}

export function ChartRenderer({ chartType, config, data, compact = false }: ChartRendererProps) {
  const resolved = useMemo(() => autoDetectConfig(data, config), [data, config])
  const actualKeys = useMemo(() => (data.length > 0 ? Object.keys(data[0]) : []), [data])

  const xKey = useMemo(() => resolveKey(resolved.xAxis, actualKeys), [resolved.xAxis, actualKeys])
  const yKeys = useMemo(
    () => {
      const raw = resolved.yAxis
      if (!raw) return []
      const arr = Array.isArray(raw) ? raw : [raw]
      return arr.map((k) => resolveKey(String(k), actualKeys))
    },
    [resolved.yAxis, actualKeys],
  )
  const lKey = useMemo(
    () => resolveKey(resolved.labelKey ?? resolved.xAxis, actualKeys),
    [resolved.labelKey, resolved.xAxis, actualKeys],
  )
  const vKey = useMemo(
    () => {
      const primaryY = Array.isArray(resolved.yAxis) ? resolved.yAxis[0] : resolved.yAxis
      return resolveKey(resolved.valueKey ?? primaryY, actualKeys)
    },
    [resolved.valueKey, resolved.yAxis, actualKeys],
  )

  const colors = resolved.colors ?? DEFAULT_COLORS

  const isNumericColumn = (key: string): boolean => {
    if (!key) return false
    let nonNull = 0
    let numeric = 0
    for (const row of data) {
      const value = row[key]
      if (value == null) continue
      nonNull += 1
      if (typeof value === 'number') {
        numeric += 1
        continue
      }
      if (typeof value === 'string' && value.trim().length > 0 && Number.isFinite(Number(value.replace(/,/g, '')))) {
        numeric += 1
      }
    }
    return nonNull > 0 && numeric / nonNull >= 0.7
  }

  const safeYKeys = yKeys.filter((k) => isNumericColumn(k))

  const safeXKey = (!xKey || isNumericColumn(xKey))
    ? (actualKeys.find((k) => !isNumericColumn(k) && k !== safeYKeys[0]) ?? xKey)
    : xKey

  const fallbackYKeys = safeYKeys.length > 0
    ? safeYKeys
    : actualKeys.filter((k) => isNumericColumn(k) && k !== safeXKey)

  const safeLabelKey = (lKey && !isNumericColumn(lKey))
    ? lKey
    : (actualKeys.find((k) => !isNumericColumn(k) && k !== vKey) ?? safeXKey)

  const safeValueKey = (vKey && isNumericColumn(vKey))
    ? vKey
    : (fallbackYKeys[0] ?? vKey)

  if (!data || data.length === 0) {
    return (
      <div className="h-full flex items-center justify-center text-zinc-600 text-sm">
        No data available.
      </div>
    )
  }

  const labels = data.map((d) => String(d[safeXKey] ?? d[safeLabelKey] ?? ''))

  switch (chartType) {
    case 'bar':
    case 'stacked_bar': {
      const chartData: ChartData<'bar'> = {
        labels,
        datasets: (fallbackYKeys.length > 0 ? fallbackYKeys : yKeys).map((key, i) => ({
          label: key,
          data: data.map((d) => Number(d[key]) || 0),
          backgroundColor: colors[i % colors.length],
          borderRadius: 4,
          borderSkipped: false as const,
        })),
      }
      const opts = baseCartesianOptions('x', compact) as ChartOptions<'bar'>
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
      const catKey = safeXKey
      const effectiveValKeys = fallbackYKeys.length > 0 ? fallbackYKeys : yKeys

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
      return <div className="h-full w-full"><Bar data={chartData} options={baseCartesianOptions('y', compact) as ChartOptions<'bar'>} /></div>
    }

    case 'grouped_bar': {
      const chartData: ChartData<'bar'> = {
        labels,
        datasets: (fallbackYKeys.length > 0 ? fallbackYKeys : yKeys).map((key, i) => ({
          label: key,
          data: data.map((d) => Number(d[key]) || 0),
          backgroundColor: colors[i % colors.length],
          borderRadius: 4,
          borderSkipped: false as const,
        })),
      }
      return <div className="h-full w-full"><Bar data={chartData} options={baseCartesianOptions('x', compact) as ChartOptions<'bar'>} /></div>
    }

    case 'floating_bar': {
      const minK = resolveKey(resolved.minKey, actualKeys) || yKeys[0]
      const maxK = resolveKey(resolved.maxKey, actualKeys) || yKeys[1] || yKeys[0]
      const chartData: ChartData<'bar'> = {
        labels,
        datasets: [{
          label: `${minK} – ${maxK}`,
          data: data.map((d) => [Number(d[minK]) || 0, Number(d[maxK]) || 0] as [number, number]),
          backgroundColor: colors[0] + '80',
          borderColor: colors[0],
          borderWidth: 1,
          borderRadius: 4,
          borderSkipped: false as const,
        }],
      }
      return <div className="h-full w-full"><Bar data={chartData} options={baseCartesianOptions('x', compact) as ChartOptions<'bar'>} /></div>
    }

    case 'line': {
      const chartData: ChartData<'line'> = {
        labels,
        datasets: (fallbackYKeys.length > 0 ? fallbackYKeys : yKeys).map((key, i) => ({
          label: key,
          data: data.map((d) => Number(d[key]) || 0),
          borderColor: colors[i % colors.length],
          backgroundColor: 'transparent',
          borderWidth: 2,
          pointRadius: 2,
          tension: 0.3,
        })),
      }
      return <div className="h-full w-full"><Line data={chartData} options={baseCartesianOptions('x', compact) as ChartOptions<'line'>} /></div>
    }

    case 'area': {
      const chartData: ChartData<'line'> = {
        labels,
        datasets: (fallbackYKeys.length > 0 ? fallbackYKeys : yKeys).map((key, i) => ({
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
      return <div className="h-full w-full"><Line data={chartData} options={baseCartesianOptions('x', compact) as ChartOptions<'line'>} /></div>
    }

    case 'stepped_line': {
      const chartData: ChartData<'line'> = {
        labels,
        datasets: (fallbackYKeys.length > 0 ? fallbackYKeys : yKeys).map((key, i) => ({
          label: key,
          data: data.map((d) => Number(d[key]) || 0),
          borderColor: colors[i % colors.length],
          backgroundColor: 'transparent',
          borderWidth: 2,
          pointRadius: 3,
          stepped: true as const,
        })),
      }
      return <div className="h-full w-full"><Line data={chartData} options={baseCartesianOptions('x', compact) as ChartOptions<'line'>} /></div>
    }

    case 'multi_axis_line': {
      const multiKeys = fallbackYKeys.length > 0 ? fallbackYKeys : yKeys
      const chartData: ChartData<'line'> = {
        labels,
        datasets: multiKeys.map((key, i) => ({
          label: key,
          data: data.map((d) => Number(d[key]) || 0),
          borderColor: colors[i % colors.length],
          backgroundColor: 'transparent',
          borderWidth: 2,
          pointRadius: 2,
          tension: 0.3,
          yAxisID: i === 0 ? 'y' : 'y1',
        })),
      }
      const opts: ChartOptions<'line'> = {
        ...baseCartesianOptions('x', compact) as ChartOptions<'line'>,
        ...(compact ? {} : {
          scales: {
            x: {
              grid: { color: 'transparent' },
              ticks: { color: DARK_TICK, font: { size: 11 }, maxRotation: 45 },
              border: { display: false },
            },
            y: {
              type: 'linear',
              position: 'left',
              grid: { color: DARK_GRID },
              ticks: { color: colors[0], font: { size: 11 } },
              border: { display: false },
            },
            y1: {
              type: 'linear',
              position: 'right',
              grid: { drawOnChartArea: false },
              ticks: { color: colors[1] ?? DARK_TICK, font: { size: 11 } },
              border: { display: false },
            },
          },
        }),
      }
      return <div className="h-full w-full"><Line data={chartData} options={opts} /></div>
    }

    case 'pie': {
      const pieLabels = data.map((d) => String(d[safeLabelKey] ?? ''))
      const pieValues = data.map((d) => Number(d[safeValueKey]) || 0)
      const chartData: ChartData<'pie'> = {
        labels: pieLabels,
        datasets: [{
          data: pieValues,
          backgroundColor: data.map((_, i) => colors[i % colors.length]),
          borderColor: '#111113',
          borderWidth: 2,
        }],
      }
      return <div className="h-full w-full"><Pie data={chartData} options={basePolarOptions(compact) as ChartOptions<'pie'>} /></div>
    }

    case 'doughnut': {
      const dLabels = data.map((d) => String(d[safeLabelKey] ?? ''))
      const dValues = data.map((d) => Number(d[safeValueKey]) || 0)
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
        ...(basePolarOptions(compact) as ChartOptions<'doughnut'>),
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
        ...(baseCartesianOptions('x', compact) as unknown as ChartOptions<'scatter'>),
      }
      return <div className="h-full w-full"><Scatter data={chartData} options={opts} /></div>
    }

    case 'bubble': {
      const sKey = resolveKey(resolved.sizeKey, actualKeys) || yKeys[1] || yKeys[0]
      const chartData: ChartData<'bubble'> = {
        datasets: [{
          label: `${xKey} vs ${yKeys[0] ?? ''}`,
          data: data.map((d) => ({
            x: Number(d[xKey]) || 0,
            y: Number(d[yKeys[0]]) || 0,
            r: Math.min(Math.max(Number(d[sKey]) || 3, 2), 30),
          })),
          backgroundColor: colors[0] + '60',
          borderColor: colors[0],
          borderWidth: 1,
        }],
      }
      const opts: ChartOptions<'bubble'> = {
        ...(baseCartesianOptions('x', compact) as unknown as ChartOptions<'bubble'>),
      }
      return <div className="h-full w-full"><Bubble data={chartData} options={opts} /></div>
    }

    case 'radar': {
      const radarKeys = resolved.dataKeys?.map((k) => resolveKey(k, actualKeys)) ?? (fallbackYKeys.length > 0 ? fallbackYKeys : yKeys)
      const radarLabels = data.map((d) => String(d[safeXKey] ?? ''))
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
        ...(basePolarOptions(compact) as ChartOptions<'radar'>),
        ...(compact ? {} : {
          scales: {
            r: {
              grid: { color: DARK_GRID },
              angleLines: { color: DARK_GRID },
              pointLabels: { color: DARK_TICK, font: { size: 10 } },
              ticks: { display: false },
            },
          },
        }),
      }
      return <div className="h-full w-full"><Radar data={chartData} options={opts} /></div>
    }

    case 'polar_area': {
      const paLabels = data.map((d) => String(d[safeLabelKey] ?? ''))
      const paKeys = resolved.dataKeys?.map((k) => resolveKey(k, actualKeys)) ?? (fallbackYKeys.length > 0 ? fallbackYKeys : yKeys)
      const paKey = paKeys[0] ?? safeValueKey
      const paValues = data.map((d) => Number(d[paKey]) || 0)
      const chartData: ChartData<'polarArea'> = {
        labels: paLabels,
        datasets: [{
          data: paValues,
          backgroundColor: data.map((_, i) => colors[i % colors.length] + '80'),
          borderColor: data.map((_, i) => colors[i % colors.length]),
          borderWidth: 2,
        }],
      }
      const opts: ChartOptions<'polarArea'> = {
        ...(basePolarOptions(compact) as ChartOptions<'polarArea'>),
        ...(compact ? {} : {
          scales: {
            r: {
              grid: { color: DARK_GRID },
              ticks: { display: false },
            },
          },
        }),
      }
      return <div className="h-full w-full"><PolarArea data={chartData} options={opts} /></div>
    }

    case 'composed': {
      const bKeys = (resolved.barKeys ?? []).map((k) => resolveKey(k, actualKeys))
      const lnKeys = (resolved.lineKeys ?? []).map((k) => resolveKey(k, actualKeys))
      const allKeys = bKeys.length > 0 || lnKeys.length > 0
        ? [...bKeys, ...lnKeys]
        : (fallbackYKeys.length > 0 ? fallbackYKeys : yKeys)
      const defaultKeys = fallbackYKeys.length > 0 ? fallbackYKeys : yKeys
      const barSet = new Set(bKeys.length > 0 ? bKeys : defaultKeys.slice(0, Math.ceil(defaultKeys.length / 2)))
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
      return <div className="h-full w-full"><Bar data={chartData as ChartData<'bar'>} options={baseCartesianOptions('x', compact) as ChartOptions<'bar'>} /></div>
    }

    case 'treemap': {
      const tmData = data.map((d, i) => ({
        label: String(d[safeLabelKey] ?? d[safeXKey] ?? `Item ${i}`),
        value: Number(d[safeValueKey] ?? d[fallbackYKeys[0]]) || 0,
        color: colors[i % colors.length],
      }))
      // Render as horizontal bar — more readable than treemap for most data
      const fallbackData: ChartData<'bar'> = {
        labels: tmData.map((d) => d.label),
        datasets: [{
          label: safeValueKey || fallbackYKeys[0] || 'Value',
          data: tmData.map((d) => d.value),
          backgroundColor: tmData.map((d) => d.color + '80'),
          borderColor: tmData.map((d) => d.color),
          borderWidth: 1,
          borderRadius: 4,
          borderSkipped: false as const,
        }],
      }
      return <div className="h-full w-full"><Bar data={fallbackData} options={baseCartesianOptions('y', compact) as ChartOptions<'bar'>} /></div>
    }

    case 'funnel': {
      // Funnel rendered as horizontal bar sorted by value (largest at top)
      const funnelData = data
        .map((d) => ({
          label: String(d[safeLabelKey] ?? d[safeXKey] ?? ''),
          value: Number(d[safeValueKey] ?? d[fallbackYKeys[0]]) || 0,
        }))
        .sort((a, b) => b.value - a.value)
      const chartData: ChartData<'bar'> = {
        labels: funnelData.map((d) => d.label),
        datasets: [{
          label: safeValueKey || fallbackYKeys[0] || 'Value',
          data: funnelData.map((d) => d.value),
          backgroundColor: funnelData.map((_, i) => colors[i % colors.length] + '80'),
          borderColor: funnelData.map((_, i) => colors[i % colors.length]),
          borderWidth: 1,
          borderRadius: 4,
          borderSkipped: false as const,
        }],
      }
      return <div className="h-full w-full"><Bar data={chartData} options={baseCartesianOptions('y', compact) as ChartOptions<'bar'>} /></div>
    }

    default:
      return <div className="text-zinc-500 text-sm">Unsupported chart type: {chartType}</div>
  }
}
