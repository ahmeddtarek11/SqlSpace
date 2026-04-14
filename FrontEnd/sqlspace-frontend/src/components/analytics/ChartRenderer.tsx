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

const PALETTE_VIVID = [
  '#f97316', '#0ea5e9', '#22c55e', '#f43f5e', '#8b5cf6',
  '#eab308', '#14b8a6', '#ec4899', '#84cc16', '#6366f1',
]

const PALETTE_COMMERCE = [
  '#ea580c', '#f59e0b', '#84cc16', '#06b6d4', '#3b82f6',
  '#8b5cf6', '#ec4899', '#ef4444', '#10b981', '#f43f5e',
]

const PALETTE_COOL = [
  '#0ea5e9', '#14b8a6', '#22c55e', '#6366f1', '#8b5cf6',
  '#06b6d4', '#3b82f6', '#10b981', '#0d9488', '#4f46e5',
]

const PALETTE_NATURE = [
  '#22c55e', '#84cc16', '#16a34a', '#06b6d4', '#f59e0b',
  '#65a30d', '#15803d', '#0891b2', '#eab308', '#0ea5e9',
]

const PALETTE_SUNSET = [
  '#f97316', '#f43f5e', '#a855f7', '#6366f1', '#0ea5e9',
  '#eab308', '#ef4444', '#ec4899', '#8b5cf6', '#14b8a6',
]

const PALETTE_BY_TYPE: Partial<Record<ChartType, string[]>> = {
  bar: PALETTE_COMMERCE,
  horizontal_bar: PALETTE_COMMERCE,
  grouped_bar: PALETTE_VIVID,
  stacked_bar: PALETTE_NATURE,
  floating_bar: PALETTE_COOL,
  line: PALETTE_COOL,
  area: PALETTE_SUNSET,
  stepped_line: PALETTE_COOL,
  multi_axis_line: PALETTE_VIVID,
  pie: PALETTE_VIVID,
  doughnut: PALETTE_VIVID,
  polar_area: PALETTE_SUNSET,
  radar: PALETTE_NATURE,
  scatter: PALETTE_SUNSET,
  bubble: PALETTE_SUNSET,
  composed: PALETTE_VIVID,
  treemap: PALETTE_NATURE,
  funnel: PALETTE_COMMERCE,
}


function normalizeColorBase(color: string): string {
  const c = color.trim().toLowerCase()
  if (!c.startsWith('#')) return c

  if (c.length === 4) {
    return `#${c[1]}${c[1]}${c[2]}${c[2]}${c[3]}${c[3]}`
  }

  if (c.length === 5) {
    return `#${c[1]}${c[1]}${c[2]}${c[2]}${c[3]}${c[3]}`
  }

  if (c.length === 9) {
    return c.slice(0, 7)
  }

  return c
}


function withAlpha(color: string, alphaHex: string): string {
  const base = normalizeColorBase(color)
  if (!base.startsWith('#') || base.length !== 7) return color
  return `${base}${alphaHex}`
}


function seedIndex(seedText: string, modulo: number): number {
  if (modulo <= 0) return 0
  let hash = 0
  for (let i = 0; i < seedText.length; i += 1) {
    hash = (hash * 31 + seedText.charCodeAt(i)) >>> 0
  }
  return hash % modulo
}


function rotatePalette(palette: string[], offset: number): string[] {
  if (palette.length === 0) return []
  const normalizedOffset = ((offset % palette.length) + palette.length) % palette.length
  return [...palette.slice(normalizedOffset), ...palette.slice(0, normalizedOffset)]
}


function repeatPalette(palette: string[], count: number): string[] {
  if (count <= 0) return []
  const source = palette.length > 0 ? palette : PALETTE_VIVID
  return Array.from({ length: count }, (_, i) => source[i % source.length])
}


function buildChartColors(
  chartType: ChartType,
  configuredColors: string[] | undefined,
  count: number,
  seedText: string,
): string[] {
  const sanitizedConfigured = (configuredColors ?? [])
    .map((c) => c.trim())
    .filter((c) => c.length > 0)

  const configuredUniqueBases = new Set(sanitizedConfigured.map((c) => normalizeColorBase(c)))
  const hasMeaningfulConfiguredPalette = sanitizedConfigured.length >= 2 && configuredUniqueBases.size >= 2

  if (hasMeaningfulConfiguredPalette) {
    return repeatPalette(sanitizedConfigured, count)
  }

  const typePalette = PALETTE_BY_TYPE[chartType] ?? PALETTE_VIVID
  const rotated = rotatePalette(typePalette, seedIndex(`${chartType}|${seedText}`, typePalette.length))
  return repeatPalette(rotated, count)
}


const SEMANTIC_LOW = '#ef4444'
const SEMANTIC_MID = '#f59e0b'
const SEMANTIC_HIGH = '#22c55e'


function clamp01(value: number): number {
  return Math.min(1, Math.max(0, value))
}


function hexToRgb(hex: string): { r: number; g: number; b: number } | null {
  const base = normalizeColorBase(hex)
  if (!base.startsWith('#') || base.length !== 7) return null
  const r = parseInt(base.slice(1, 3), 16)
  const g = parseInt(base.slice(3, 5), 16)
  const b = parseInt(base.slice(5, 7), 16)
  if ([r, g, b].some((v) => Number.isNaN(v))) return null
  return { r, g, b }
}


function rgbToHex(r: number, g: number, b: number): string {
  const toHex = (v: number) => Math.round(Math.min(255, Math.max(0, v))).toString(16).padStart(2, '0')
  return `#${toHex(r)}${toHex(g)}${toHex(b)}`
}


function interpolateHexColor(start: string, end: string, t: number): string {
  const s = hexToRgb(start)
  const e = hexToRgb(end)
  if (!s || !e) return end
  const ratio = clamp01(t)
  return rgbToHex(
    s.r + (e.r - s.r) * ratio,
    s.g + (e.g - s.g) * ratio,
    s.b + (e.b - s.b) * ratio,
  )
}


function semanticColorForRank(rankRatio: number): string {
  const t = clamp01(rankRatio)
  if (t <= 0.5) {
    return interpolateHexColor(SEMANTIC_LOW, SEMANTIC_MID, t / 0.5)
  }
  return interpolateHexColor(SEMANTIC_MID, SEMANTIC_HIGH, (t - 0.5) / 0.5)
}


function semanticColorsForValues(values: number[], fallbackColors: string[]): string[] {
  if (values.length === 0) return []

  const normalizedValues = values.map((v) => (Number.isFinite(v) ? v : 0))
  const indexed = normalizedValues.map((value, index) => ({ value, index }))
  const uniqueValues = new Set(normalizedValues)

  if (uniqueValues.size <= 1) {
    return fallbackColors.length > 0
      ? repeatPalette(fallbackColors, values.length)
      : Array.from({ length: values.length }, () => SEMANTIC_MID)
  }

  const sorted = [...indexed].sort((a, b) => a.value - b.value)
  const result = Array.from({ length: values.length }, () => SEMANTIC_MID)
  sorted.forEach((entry, rank) => {
    const ratio = sorted.length === 1 ? 1 : rank / (sorted.length - 1)
    result[entry.index] = semanticColorForRank(ratio)
  })

  return result
}


function semanticColorsForSeries(seriesValues: number[][], fallbackColors: string[]): string[] {
  const averages = seriesValues.map((values) => {
    if (values.length === 0) return 0
    const sum = values.reduce((acc, value) => acc + (Number.isFinite(value) ? value : 0), 0)
    return sum / values.length
  })
  return semanticColorsForValues(averages, fallbackColors)
}


function trendColor(values: number[], fallbackColor: string): string {
  if (values.length < 2) return fallbackColor
  const first = values[0]
  const last = values[values.length - 1]
  if (!Number.isFinite(first) || !Number.isFinite(last)) return fallbackColor
  if (last > first) return SEMANTIC_HIGH
  if (last < first) return SEMANTIC_LOW
  return SEMANTIC_MID
}

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

  const paletteSeed = `${safeXKey}|${safeLabelKey}|${safeValueKey}|${actualKeys.join('|')}`
  const colorsFor = (count: number, seedSuffix = '') =>
    buildChartColors(chartType, resolved.colors, count, `${paletteSeed}|${seedSuffix}`)

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
      const metricKeys = fallbackYKeys.length > 0 ? fallbackYKeys : yKeys
      const fallbackPalette = colorsFor(metricKeys.length > 1 ? metricKeys.length : data.length, metricKeys.join('|'))
      const seriesValueArrays = metricKeys.map((key) => data.map((d) => Number(d[key]) || 0))
      const seriesColors = metricKeys.length > 1
        ? semanticColorsForSeries(seriesValueArrays, fallbackPalette)
        : []
      const chartData: ChartData<'bar'> = {
        labels,
        datasets: metricKeys.map((key, i) => {
          const values = seriesValueArrays[i]
          const perPointSemanticColors = metricKeys.length === 1
            ? semanticColorsForValues(values, fallbackPalette)
            : []

          return {
            label: key,
            data: values,
            backgroundColor: metricKeys.length === 1
              ? perPointSemanticColors
              : seriesColors[i % seriesColors.length],
            borderColor: metricKeys.length === 1
              ? perPointSemanticColors
              : seriesColors[i % seriesColors.length],
            borderRadius: 4,
            borderSkipped: false as const,
          }
        }),
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
      const fallbackPalette = colorsFor(effectiveValKeys.length > 1 ? effectiveValKeys.length : data.length, effectiveValKeys.join('|'))
      const seriesValueArrays = effectiveValKeys.map((key) => data.map((d) => Number(d[key]) || 0))
      const seriesColors = effectiveValKeys.length > 1
        ? semanticColorsForSeries(seriesValueArrays, fallbackPalette)
        : []

      const hLabels = data.map((d) => String(d[catKey] ?? ''))
      const chartData: ChartData<'bar'> = {
        labels: hLabels,
        datasets: effectiveValKeys.map((key, i) => {
          const values = seriesValueArrays[i]
          const perPointSemanticColors = effectiveValKeys.length === 1
            ? semanticColorsForValues(values, fallbackPalette)
            : []

          return {
            label: key,
            data: values,
            backgroundColor: effectiveValKeys.length === 1
              ? perPointSemanticColors
              : seriesColors[i % seriesColors.length],
            borderColor: effectiveValKeys.length === 1
              ? perPointSemanticColors
              : seriesColors[i % seriesColors.length],
            borderRadius: 4,
            borderSkipped: false as const,
          }
        }),
      }
      return <div className="h-full w-full"><Bar data={chartData} options={baseCartesianOptions('y', compact) as ChartOptions<'bar'>} /></div>
    }

    case 'grouped_bar': {
      const metricKeys = fallbackYKeys.length > 0 ? fallbackYKeys : yKeys
      const fallbackPalette = colorsFor(metricKeys.length > 1 ? metricKeys.length : data.length, metricKeys.join('|'))
      const seriesValueArrays = metricKeys.map((key) => data.map((d) => Number(d[key]) || 0))
      const seriesColors = metricKeys.length > 1
        ? semanticColorsForSeries(seriesValueArrays, fallbackPalette)
        : []
      const chartData: ChartData<'bar'> = {
        labels,
        datasets: metricKeys.map((key, i) => {
          const values = seriesValueArrays[i]
          const perPointSemanticColors = metricKeys.length === 1
            ? semanticColorsForValues(values, fallbackPalette)
            : []

          return {
            label: key,
            data: values,
            backgroundColor: metricKeys.length === 1
              ? perPointSemanticColors
              : seriesColors[i % seriesColors.length],
            borderColor: metricKeys.length === 1
              ? perPointSemanticColors
              : seriesColors[i % seriesColors.length],
            borderRadius: 4,
            borderSkipped: false as const,
          }
        }),
      }
      return <div className="h-full w-full"><Bar data={chartData} options={baseCartesianOptions('x', compact) as ChartOptions<'bar'>} /></div>
    }

    case 'floating_bar': {
      const minK = resolveKey(resolved.minKey, actualKeys) || yKeys[0]
      const maxK = resolveKey(resolved.maxKey, actualKeys) || yKeys[1] || yKeys[0]
      const midpointValues = data.map((d) => {
        const minValue = Number(d[minK]) || 0
        const maxValue = Number(d[maxK]) || 0
        return (minValue + maxValue) / 2
      })
      const semanticRangeColors = semanticColorsForValues(
        midpointValues,
        colorsFor(data.length, `${minK}|${maxK}`),
      )
      const chartData: ChartData<'bar'> = {
        labels,
        datasets: [{
          label: `${minK} – ${maxK}`,
          data: data.map((d) => [Number(d[minK]) || 0, Number(d[maxK]) || 0] as [number, number]),
          backgroundColor: semanticRangeColors.map((color) => withAlpha(color, '80')),
          borderColor: semanticRangeColors,
          borderWidth: 1,
          borderRadius: 4,
          borderSkipped: false as const,
        }],
      }
      return <div className="h-full w-full"><Bar data={chartData} options={baseCartesianOptions('x', compact) as ChartOptions<'bar'>} /></div>
    }

    case 'line': {
      const metricKeys = fallbackYKeys.length > 0 ? fallbackYKeys : yKeys
      const seriesValueArrays = metricKeys.map((key) => data.map((d) => Number(d[key]) || 0))
      const semanticSeries = metricKeys.length > 1
        ? semanticColorsForSeries(seriesValueArrays, colorsFor(metricKeys.length, metricKeys.join('|')))
        : []
      const chartData: ChartData<'line'> = {
        labels,
        datasets: metricKeys.map((key, i) => {
          const values = seriesValueArrays[i]
          const semanticPointColors = semanticColorsForValues(values, colorsFor(values.length, key))
          const semanticLineColor = metricKeys.length === 1
            ? trendColor(values, SEMANTIC_MID)
            : semanticSeries[i % semanticSeries.length]

          return {
            label: key,
            data: values,
            borderColor: semanticLineColor,
            backgroundColor: 'transparent',
            borderWidth: 2,
            pointRadius: 2,
            pointBackgroundColor: semanticPointColors,
            pointBorderColor: semanticPointColors,
            tension: 0.3,
          }
        }),
      }
      return <div className="h-full w-full"><Line data={chartData} options={baseCartesianOptions('x', compact) as ChartOptions<'line'>} /></div>
    }

    case 'area': {
      const metricKeys = fallbackYKeys.length > 0 ? fallbackYKeys : yKeys
      const seriesValueArrays = metricKeys.map((key) => data.map((d) => Number(d[key]) || 0))
      const semanticSeries = metricKeys.length > 1
        ? semanticColorsForSeries(seriesValueArrays, colorsFor(metricKeys.length, metricKeys.join('|')))
        : []
      const chartData: ChartData<'line'> = {
        labels,
        datasets: metricKeys.map((key, i) => {
          const values = seriesValueArrays[i]
          const semanticAreaColor = metricKeys.length === 1
            ? trendColor(values, SEMANTIC_MID)
            : semanticSeries[i % semanticSeries.length]

          return {
            label: key,
            data: values,
            borderColor: semanticAreaColor,
            backgroundColor: withAlpha(semanticAreaColor, '33'),
            borderWidth: 2,
            pointRadius: 0,
            fill: true,
            tension: 0.3,
          }
        }),
      }
      return <div className="h-full w-full"><Line data={chartData} options={baseCartesianOptions('x', compact) as ChartOptions<'line'>} /></div>
    }

    case 'stepped_line': {
      const metricKeys = fallbackYKeys.length > 0 ? fallbackYKeys : yKeys
      const seriesValueArrays = metricKeys.map((key) => data.map((d) => Number(d[key]) || 0))
      const semanticSeries = metricKeys.length > 1
        ? semanticColorsForSeries(seriesValueArrays, colorsFor(metricKeys.length, metricKeys.join('|')))
        : []
      const chartData: ChartData<'line'> = {
        labels,
        datasets: metricKeys.map((key, i) => {
          const values = seriesValueArrays[i]
          const semanticPointColors = semanticColorsForValues(values, colorsFor(values.length, key))
          const semanticLineColor = metricKeys.length === 1
            ? trendColor(values, SEMANTIC_MID)
            : semanticSeries[i % semanticSeries.length]

          return {
            label: key,
            data: values,
            borderColor: semanticLineColor,
            backgroundColor: 'transparent',
            borderWidth: 2,
            pointRadius: 3,
            pointBackgroundColor: semanticPointColors,
            pointBorderColor: semanticPointColors,
            stepped: true as const,
          }
        }),
      }
      return <div className="h-full w-full"><Line data={chartData} options={baseCartesianOptions('x', compact) as ChartOptions<'line'>} /></div>
    }

    case 'multi_axis_line': {
      const multiKeys = fallbackYKeys.length > 0 ? fallbackYKeys : yKeys
      const multiSeriesValues = multiKeys.map((key) => data.map((d) => Number(d[key]) || 0))
      const semanticSeries = semanticColorsForSeries(
        multiSeriesValues,
        colorsFor(multiKeys.length, multiKeys.join('|')),
      )
      const chartData: ChartData<'line'> = {
        labels,
        datasets: multiKeys.map((key, i) => ({
          label: key,
          data: multiSeriesValues[i],
          borderColor: semanticSeries[i % semanticSeries.length],
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
              ticks: { color: semanticSeries[0], font: { size: 11 } },
              border: { display: false },
            },
            y1: {
              type: 'linear',
              position: 'right',
              grid: { drawOnChartArea: false },
              ticks: { color: semanticSeries[1] ?? DARK_TICK, font: { size: 11 } },
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
      const semanticPieColors = semanticColorsForValues(
        pieValues,
        colorsFor(data.length, pieLabels.join('|')),
      )
      const chartData: ChartData<'pie'> = {
        labels: pieLabels,
        datasets: [{
          data: pieValues,
          backgroundColor: semanticPieColors,
          borderColor: '#111113',
          borderWidth: 2,
        }],
      }
      return <div className="h-full w-full"><Pie data={chartData} options={basePolarOptions(compact) as ChartOptions<'pie'>} /></div>
    }

    case 'doughnut': {
      const dLabels = data.map((d) => String(d[safeLabelKey] ?? ''))
      const dValues = data.map((d) => Number(d[safeValueKey]) || 0)
      const semanticDoughnutColors = semanticColorsForValues(
        dValues,
        colorsFor(data.length, dLabels.join('|')),
      )
      const chartData: ChartData<'doughnut'> = {
        labels: dLabels,
        datasets: [{
          data: dValues,
          backgroundColor: semanticDoughnutColors,
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
      const scatterValues = data.map((d) => Number(d[yKeys[0]]) || 0)
      const scatterPalette = semanticColorsForValues(
        scatterValues,
        colorsFor(data.length, `${xKey}|${yKeys[0] ?? ''}`),
      )
      const chartData: ChartData<'scatter'> = {
        datasets: [{
          label: `${xKey} vs ${yKeys[0] ?? ''}`,
          data: data.map((d) => ({
            x: Number(d[xKey]) || 0,
            y: Number(d[yKeys[0]]) || 0,
          })),
          backgroundColor: scatterPalette.map((color) => withAlpha(color, '80')),
          borderColor: scatterPalette,
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
      const bubbleValues = data.map((d) => Number(d[sKey]) || 0)
      const bubblePalette = semanticColorsForValues(
        bubbleValues,
        colorsFor(data.length, `${xKey}|${yKeys[0] ?? ''}|${sKey}`),
      )
      const chartData: ChartData<'bubble'> = {
        datasets: [{
          label: `${xKey} vs ${yKeys[0] ?? ''}`,
          data: data.map((d) => ({
            x: Number(d[xKey]) || 0,
            y: Number(d[yKeys[0]]) || 0,
            r: Math.min(Math.max(Number(d[sKey]) || 3, 2), 30),
          })),
          backgroundColor: bubblePalette.map((color) => withAlpha(color, '66')),
          borderColor: bubblePalette,
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
      const radarSeriesValues = radarKeys.map((key) => data.map((d) => Number(d[key]) || 0))
      const radarPalette = semanticColorsForSeries(
        radarSeriesValues,
        colorsFor(radarKeys.length, radarKeys.join('|')),
      )
      const radarLabels = data.map((d) => String(d[safeXKey] ?? ''))
      const chartData: ChartData<'radar'> = {
        labels: radarLabels,
        datasets: radarKeys.map((key, i) => ({
          label: key,
          data: radarSeriesValues[i],
          borderColor: radarPalette[i % radarPalette.length],
          backgroundColor: withAlpha(radarPalette[i % radarPalette.length], '26'),
          borderWidth: 2,
          pointRadius: 3,
          pointBackgroundColor: radarPalette[i % radarPalette.length],
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
      const paPalette = semanticColorsForValues(
        paValues,
        colorsFor(data.length, `${paKey}|${paLabels.join('|')}`),
      )
      const chartData: ChartData<'polarArea'> = {
        labels: paLabels,
        datasets: [{
          data: paValues,
          backgroundColor: data.map((_, i) => withAlpha(paPalette[i % paPalette.length], '80')),
          borderColor: data.map((_, i) => paPalette[i % paPalette.length]),
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
      const composedSeriesValues = allKeys.map((key) => data.map((d) => Number(d[key]) || 0))
      const composedPalette = semanticColorsForSeries(
        composedSeriesValues,
        colorsFor(allKeys.length, allKeys.join('|')),
      )
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      const datasets: any[] = allKeys.map((key, i) => {
        const isBar = barSet.has(key)
        return {
          label: key,
          data: composedSeriesValues[i],
          backgroundColor: isBar ? composedPalette[i % composedPalette.length] : 'transparent',
          borderColor: composedPalette[i % composedPalette.length],
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
      const treemapValues = data.map((d) => Number(d[safeValueKey] ?? d[fallbackYKeys[0]]) || 0)
      const tmPalette = semanticColorsForValues(
        treemapValues,
        colorsFor(data.length, `${safeLabelKey}|${safeValueKey}`),
      )
      const tmData = data.map((d, i) => ({
        label: String(d[safeLabelKey] ?? d[safeXKey] ?? `Item ${i}`),
        value: treemapValues[i],
        color: tmPalette[i % tmPalette.length],
      }))
      // Render as horizontal bar — more readable than treemap for most data
      const fallbackData: ChartData<'bar'> = {
        labels: tmData.map((d) => d.label),
        datasets: [{
          label: safeValueKey || fallbackYKeys[0] || 'Value',
          data: tmData.map((d) => d.value),
          backgroundColor: tmData.map((d) => withAlpha(d.color, '80')),
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
      const funnelPalette = semanticColorsForValues(
        funnelData.map((d) => d.value),
        colorsFor(funnelData.length, `${safeLabelKey}|${safeValueKey}`),
      )
      const chartData: ChartData<'bar'> = {
        labels: funnelData.map((d) => d.label),
        datasets: [{
          label: safeValueKey || fallbackYKeys[0] || 'Value',
          data: funnelData.map((d) => d.value),
          backgroundColor: funnelData.map((_, i) => withAlpha(funnelPalette[i % funnelPalette.length], '80')),
          borderColor: funnelData.map((_, i) => funnelPalette[i % funnelPalette.length]),
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
