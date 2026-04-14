import { BarChart3, Table2, Lightbulb, Clock, Rows3 } from 'lucide-react'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from '@/components/ui/dialog'
import { ChartRenderer } from './ChartRenderer'
import type { SavedChartDto, ChartType, ChartConfig } from '@/types'

interface ExpandedChartDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  chart: SavedChartDto
  data: Record<string, unknown>[] | null
  columns: string[]
  executionTimeMs?: number
}

export function ExpandedChartDialog({
  open,
  onOpenChange,
  chart,
  data,
  columns,
  executionTimeMs,
}: ExpandedChartDialogProps) {
  let config: ChartConfig = {}
  try {
    config = JSON.parse(chart.chartConfigJson)
  } catch {
    /* use defaults */
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        className="max-w-[92vw]! w-[92vw]! h-[88vh]! max-h-[88vh]! overflow-hidden flex flex-col rounded-2xl! bg-[#0c0c0e] border-white/10 p-0"
      >
        {/* Header */}
        <DialogHeader className="px-6 pt-5 pb-0 shrink-0">
          <div className="flex items-start justify-between gap-4">
            <div className="min-w-0 flex-1">
              <DialogTitle className="text-lg font-semibold text-white truncate">
                {chart.title}
              </DialogTitle>
              {chart.description && (
                <DialogDescription className="text-sm text-zinc-500 mt-1 line-clamp-2">
                  {chart.description}
                </DialogDescription>
              )}
            </div>
            <div className="flex items-center gap-2 shrink-0 text-xs text-zinc-600">
              {executionTimeMs != null && (
                <span className="flex items-center gap-1">
                  <Clock className="w-3 h-3" />
                  {executionTimeMs}ms
                </span>
              )}
              {data && (
                <span className="flex items-center gap-1">
                  <Rows3 className="w-3 h-3" />
                  {data.length} rows
                </span>
              )}
              <span className="px-1.5 py-0.5 rounded bg-sky-500/10 text-sky-400 text-[10px] font-medium">
                {chart.chartType}
              </span>
            </div>
          </div>
        </DialogHeader>

        {/* Scrollable unified content: Insight → Chart → Data */}
        <div className="flex-1 min-h-0 overflow-y-auto px-6 pt-4 pb-6 space-y-5">
          {/* Insight */}
          <section className="rounded-xl border border-white/10 bg-[#111113] p-5">
            <div className="flex items-center gap-2 mb-3">
              <Lightbulb className="w-4 h-4 text-amber-400" />
              <h3 className="text-sm font-semibold text-white">Insight</h3>
            </div>
            {chart.insight ? (
              <p className="text-sm text-zinc-300 leading-relaxed whitespace-pre-wrap">
                {chart.insight}
              </p>
            ) : (
              <p className="text-sm text-zinc-600 italic">
                No insight available for this chart.
              </p>
            )}
          </section>

          {/* Chart */}
          <section className="rounded-xl border border-white/10 bg-[#111113] p-5">
            <div className="flex items-center gap-2 mb-3">
              <BarChart3 className="w-4 h-4 text-sky-400" />
              <h3 className="text-sm font-semibold text-white">Chart</h3>
            </div>
            <div className="h-96">
              <ChartRenderer
                chartType={chart.chartType as ChartType}
                config={config}
                data={data ?? []}
              />
            </div>
          </section>

          {/* Data */}
          <section className="rounded-xl border border-white/10 bg-[#111113] p-5">
            <div className="flex items-center gap-2 mb-3">
              <Table2 className="w-4 h-4 text-zinc-300" />
              <h3 className="text-sm font-semibold text-white">Data</h3>
            </div>

            <div className="max-h-80 overflow-auto rounded-xl border border-white/10">
              {data && data.length > 0 ? (
                <table className="w-full text-sm">
                  <thead className="sticky top-0 bg-[#111113] z-10">
                    <tr>
                      <th className="px-3 py-2.5 text-left text-xs font-medium text-zinc-500 border-b border-white/10 w-10">
                        #
                      </th>
                      {columns.map((col) => (
                        <th
                          key={col}
                          className="px-3 py-2.5 text-left text-xs font-medium text-zinc-400 border-b border-white/10"
                        >
                          {col}
                        </th>
                      ))}
                    </tr>
                  </thead>
                  <tbody>
                    {data.map((row, rowIdx) => (
                      <tr
                        key={rowIdx}
                        className={rowIdx % 2 === 0 ? 'bg-transparent' : 'bg-white/2'}
                      >
                        <td className="px-3 py-2 text-zinc-600 tabular-nums text-xs">
                          {rowIdx + 1}
                        </td>
                        {columns.map((col) => {
                          const val = row[col]
                          const isNum = typeof val === 'number'
                          return (
                            <td
                              key={col}
                              className={`px-3 py-2 text-zinc-300 ${isNum ? 'tabular-nums font-mono text-right' : ''}`}
                            >
                              {val == null ? <span className="text-zinc-700">null</span> : String(val)}
                            </td>
                          )
                        })}
                      </tr>
                    ))}
                  </tbody>
                </table>
              ) : (
                <div className="h-40 flex items-center justify-center text-zinc-600 text-sm">
                  No data available.
                </div>
              )}
            </div>
          </section>
        </div>
      </DialogContent>
    </Dialog>
  )
}
