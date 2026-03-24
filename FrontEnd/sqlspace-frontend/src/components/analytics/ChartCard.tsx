import { useState } from 'react'
import { RefreshCw, Trash2, AlertCircle, Clock } from 'lucide-react'
import { Skeleton } from '@/components/ui/skeleton'
import { ChartRenderer } from './ChartRenderer'
import type { ChartType, ChartConfig, SavedChartDto } from '@/types'

interface ChartCardProps {
  chart: SavedChartDto
  data: Record<string, unknown>[] | null
  columns: string[]
  loading: boolean
  error: string | null
  executionTimeMs?: number
  onRefresh: (chartId: string) => void
  onDelete: (chartId: string) => void
}

export function ChartCard({
  chart, data, loading, error, executionTimeMs,
  onRefresh, onDelete,
}: ChartCardProps) {
  const [confirmDelete, setConfirmDelete] = useState(false)

  let config: ChartConfig = {}
  try {
    config = JSON.parse(chart.chartConfigJson)
  } catch {
    /* use defaults */
  }

  return (
    <div className="bg-[#111113] border border-white/10 rounded-2xl shadow-sm hover:border-white/20 transition-colors flex flex-col h-full">
      {/* Header */}
      <div className="flex items-start justify-between gap-2 px-5 pt-4 pb-2 shrink-0">
        <div className="min-w-0 flex-1">
          <h4 className="text-sm font-semibold text-white truncate">{chart.title}</h4>
          {chart.description && (
            <p className="text-xs text-zinc-500 mt-0.5 line-clamp-2">{chart.description}</p>
          )}
        </div>
        <div className="flex items-center gap-1 shrink-0">
          {executionTimeMs != null && (
            <span className="text-[10px] text-zinc-600 flex items-center gap-0.5 mr-1">
              <Clock className="w-3 h-3" />
              {executionTimeMs}ms
            </span>
          )}
          <button
            onClick={() => onRefresh(chart.id)}
            className="p-1.5 rounded-md text-zinc-500 hover:text-sky-400 hover:bg-sky-500/10 transition-colors"
            title="Refresh"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${loading ? 'animate-spin' : ''}`} />
          </button>
          {confirmDelete ? (
            <button
              onClick={() => { onDelete(chart.id); setConfirmDelete(false) }}
              className="px-2 py-1 rounded-md text-xs text-red-400 bg-red-500/10 hover:bg-red-500/20 transition-colors"
            >
              Confirm
            </button>
          ) : (
            <button
              onClick={() => setConfirmDelete(true)}
              onBlur={() => setTimeout(() => setConfirmDelete(false), 200)}
              className="p-1.5 rounded-md text-zinc-500 hover:text-red-400 hover:bg-red-500/10 transition-colors"
              title="Delete"
            >
              <Trash2 className="w-3.5 h-3.5" />
            </button>
          )}
        </div>
      </div>

      {/* Chart body */}
      <div className="flex-1 min-h-0 px-4 pb-4">
        {loading ? (
          <Skeleton className="w-full h-full rounded-xl bg-white/5" />
        ) : error ? (
          <div className="h-full flex items-center justify-center">
            <div className="flex items-center gap-2 text-sm text-red-400">
              <AlertCircle className="w-4 h-4 shrink-0" />
              <span className="line-clamp-2">{error}</span>
            </div>
          </div>
        ) : (
          <ChartRenderer
            chartType={chart.chartType as ChartType}
            config={config}
            data={data ?? []}
          />
        )}
      </div>
    </div>
  )
}
