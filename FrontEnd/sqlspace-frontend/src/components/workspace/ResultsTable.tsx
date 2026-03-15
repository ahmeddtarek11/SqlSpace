import { useState, useMemo } from 'react'
import {
  useReactTable,
  getCoreRowModel,
  getSortedRowModel,
  getPaginationRowModel,
  flexRender,
  type SortingState,
  type ColumnDef,
} from '@tanstack/react-table'
import { useVirtualizer } from '@tanstack/react-virtual'
import { useRef } from 'react'
import { ChevronUp, ChevronDown, Download, BarChart2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { formatMs, formatNumber } from '@/lib/utils'
import type { QueryResult } from '@/types'

interface Props {
  result: QueryResult
}

function downloadCSV(columns: string[], rows: Record<string, unknown>[]) {
  const header = columns.join(',')
  const body = rows.map((r) => columns.map((c) => JSON.stringify(r[c] ?? '')).join(','))
  const blob = new Blob([[header, ...body].join('\n')], { type: 'text/csv' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = 'results.csv'
  a.click()
  URL.revokeObjectURL(url)
}

function downloadJSON(rows: Record<string, unknown>[]) {
  const blob = new Blob([JSON.stringify(rows, null, 2)], { type: 'application/json' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = 'results.json'
  a.click()
  URL.revokeObjectURL(url)
}

export function ResultsTable({ result }: Props) {
  const [sorting, setSorting] = useState<SortingState>([])
  const parentRef = useRef<HTMLDivElement>(null)

  const columns = useMemo<ColumnDef<Record<string, unknown>>[]>(
    () =>
      result.columns.map((col) => ({
        accessorKey: col,
        header: col,
        cell: (info) => {
          const val = info.getValue()
          if (val === null || val === undefined) {
            return <span className="text-(--text-muted) italic text-xs">null</span>
          }
          return <span className="text-xs font-mono">{String(val)}</span>
        },
      })),
    [result.columns]
  )

  const table = useReactTable({
    data: result.rows,
    columns,
    state: { sorting },
    onSortingChange: setSorting,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
    getPaginationRowModel: getPaginationRowModel(),
    initialState: { pagination: { pageSize: 100 } },
  })

  const { rows } = table.getRowModel()

  const virtualizer = useVirtualizer({
    count: rows.length,
    getScrollElement: () => parentRef.current,
    estimateSize: () => 36,
    overscan: 10,
  })

  return (
    <div className="flex flex-col rounded-xl border border-(--border-default) overflow-hidden bg-(--bg-surface)">
      {/* Toolbar */}
      <div className="flex items-center justify-between px-4 py-2 border-b border-(--border-default) bg-(--bg-elevated) shrink-0">
        <div className="flex items-center gap-3">
          <Badge variant="secondary" className="bg-green-500/15 text-green-400 border-green-500/30 text-xs">
            {formatNumber(result.row_count)} rows
          </Badge>
          <span className="text-xs text-(--text-muted)">{formatMs(result.execution_time_ms)}</span>
        </div>
        <div className="flex items-center gap-1">
          <Button
            variant="ghost"
            size="sm"
            className="h-7 text-xs text-(--text-muted) hover:text-(--text-secondary)"
            onClick={() => downloadCSV(result.columns, result.rows)}
          >
            <Download className="w-3.5 h-3.5 mr-1" />
            CSV
          </Button>
          <Button
            variant="ghost"
            size="sm"
            className="h-7 text-xs text-(--text-muted) hover:text-(--text-secondary)"
            onClick={() => downloadJSON(result.rows)}
          >
            <Download className="w-3.5 h-3.5 mr-1" />
            JSON
          </Button>
          <Button
            variant="ghost"
            size="sm"
            className="h-7 text-xs text-(--text-muted) hover:text-(--text-secondary)"
          >
            <BarChart2 className="w-3.5 h-3.5 mr-1" />
            Visualize
          </Button>
        </div>
      </div>

      {/* Table */}
      <div ref={parentRef} className="overflow-auto flex-1" style={{ maxHeight: '340px' }}>
        <table className="w-full text-sm border-collapse">
          <thead className="sticky top-0 z-10 bg-(--bg-elevated)">
            {table.getHeaderGroups().map((hg) => (
              <tr key={hg.id}>
                {hg.headers.map((header) => (
                  <th
                    key={header.id}
                    className="px-3 py-2 text-left text-xs font-medium text-(--text-muted) border-b border-(--border-default) cursor-pointer select-none hover:text-white whitespace-nowrap"
                    onClick={header.column.getToggleSortingHandler()}
                  >
                    <span className="flex items-center gap-1">
                      {flexRender(header.column.columnDef.header, header.getContext())}
                      {header.column.getIsSorted() === 'asc' && <ChevronUp className="w-3 h-3" />}
                      {header.column.getIsSorted() === 'desc' && <ChevronDown className="w-3 h-3" />}
                    </span>
                  </th>
                ))}
              </tr>
            ))}
          </thead>
          <tbody>
            {virtualizer.getVirtualItems().map((virtualRow) => {
              const row = rows[virtualRow.index]
              return (
                <tr
                  key={row.id}
                  data-index={virtualRow.index}
                  ref={virtualizer.measureElement}
                  className="hover:bg-(--bg-elevated) transition-colors border-b border-(--border-subtle) last:border-0"
                >
                  {row.getVisibleCells().map((cell) => (
                    <td key={cell.id} className="px-3 py-2 text-xs text-white whitespace-nowrap max-w-48 truncate">
                      {flexRender(cell.column.columnDef.cell, cell.getContext())}
                    </td>
                  ))}
                </tr>
              )
            })}
          </tbody>
        </table>

        {/* Virtualizer spacer */}
        {virtualizer.getTotalSize() > 0 && (
          <div style={{ height: `${virtualizer.getTotalSize()}px` }} />
        )}
      </div>
    </div>
  )
}
