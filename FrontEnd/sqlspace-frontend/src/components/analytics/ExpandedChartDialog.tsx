import { useState } from 'react'
import { BarChart3, Table2, Lightbulb, Clock, Code2, Rows3 } from 'lucide-react'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from '@/components/ui/dialog'
import { ChartRenderer } from './ChartRenderer'
import type { SavedChartDto, ChartType, ChartConfig } from '@/types'

type Tab = 'chart' | 'data' | 'insight'

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
  const [tab, setTab] = useState<Tab>('chart')

  let config: ChartConfig = {}
  try {
    config = JSON.parse(chart.chartConfigJson)
  } catch {
    /* use defaults */
  }

  const tabs: { id: Tab; label: string; icon: typeof BarChart3 }[] = [
    { id: 'chart', label: 'Chart', icon: BarChart3 },
    { id: 'data', label: 'Data', icon: Table2 },
    { id: 'insight', label: 'Insight', icon: Lightbulb },
  ]

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        className="!max-w-[92vw] !w-[92vw] !h-[88vh] !max-h-[88vh] overflow-hidden flex flex-col !rounded-2xl bg-[#0c0c0e] border-white/10 p-0"
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

        {/* Tab bar */}
        <div className="flex items-center gap-1 px-6 pt-4 pb-0 shrink-0">
          {tabs.map((t) => {
            const Icon = t.icon
            const isActive = tab === t.id
            return (
              <button
                key={t.id}
                onClick={() => setTab(t.id)}
                className={`flex items-center gap-1.5 px-3 py-2 rounded-lg text-sm font-medium transition-colors ${
                  isActive
                    ? 'bg-sky-500/10 text-sky-400'
                    : 'text-zinc-500 hover:text-zinc-300 hover:bg-white/5'
                }`}
              >
                <Icon className="w-4 h-4" />
                {t.label}
              </button>
            )
          })}
        </div>

        {/* Tab content */}
        <div className="flex-1 min-h-0 p-6 overflow-hidden">
          {tab === 'chart' && (
            <div className="h-full w-full">
              <ChartRenderer
                chartType={chart.chartType as ChartType}
                config={config}
                data={data ?? []}
              />
            </div>
          )}

          {tab === 'data' && (
            <div className="h-full overflow-auto rounded-xl border border-white/10">
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
                        className={rowIdx % 2 === 0 ? 'bg-transparent' : 'bg-white/[0.02]'}
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
                <div className="h-full flex items-center justify-center text-zinc-600 text-sm">
                  No data available.
                </div>
              )}
            </div>
          )}

          {tab === 'insight' && (
            <div className="h-full overflow-auto space-y-5">
              {/* AI Insight */}
              <div className="rounded-xl border border-white/10 bg-[#111113] p-5">
                <div className="flex items-center gap-2 mb-3">
                  <Lightbulb className="w-4 h-4 text-amber-400" />
                  <h3 className="text-sm font-semibold text-white">AI Insight</h3>
                </div>
                {chart.insight ? (
                  <p className="text-sm text-zinc-300 leading-relaxed whitespace-pre-wrap">
                    {chart.insight}
                  </p>
                ) : (
                  <p className="text-sm text-zinc-600 italic">
                    No AI insight available for this chart. Generate a new chart to get insights.
                  </p>
                )}
              </div>

              {/* SQL Query */}
              <div className="rounded-xl border border-white/10 bg-[#111113] p-5">
                <div className="flex items-center gap-2 mb-3">
                  <Code2 className="w-4 h-4 text-sky-400" />
                  <h3 className="text-sm font-semibold text-white">SQL Query</h3>
                </div>
                <pre className="text-xs text-zinc-400 bg-black/30 rounded-lg p-4 overflow-x-auto font-mono whitespace-pre-wrap break-all">
                  {chart.sqlQuery}
                </pre>
              </div>

              {/* Metadata */}
              <div className="rounded-xl border border-white/10 bg-[#111113] p-5">
                <h3 className="text-sm font-semibold text-white mb-3">Details</h3>
                <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm">
                  <div>
                    <span className="text-zinc-600 block text-xs">Chart Type</span>
                    <span className="text-zinc-300">{chart.chartType}</span>
                  </div>
                  <div>
                    <span className="text-zinc-600 block text-xs">Rows</span>
                    <span className="text-zinc-300">{data?.length ?? 0}</span>
                  </div>
                  <div>
                    <span className="text-zinc-600 block text-xs">Execution Time</span>
                    <span className="text-zinc-300">{executionTimeMs ?? '—'}ms</span>
                  </div>
                  <div>
                    <span className="text-zinc-600 block text-xs">Created</span>
                    <span className="text-zinc-300">
                      {new Date(chart.createdAtUtc).toLocaleDateString()}
                    </span>
                  </div>
                </div>
              </div>
            </div>
          )}
        </div>
      </DialogContent>
    </Dialog>
  )
}
