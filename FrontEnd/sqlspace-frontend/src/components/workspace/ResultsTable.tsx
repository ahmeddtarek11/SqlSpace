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
import { ChevronUp, ChevronDown, ChevronLeft, ChevronRight, Download, BarChart2, BookmarkPlus, Maximize2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { formatMs, formatNumber } from '@/lib/utils'
import type { QueryResult } from '@/types'

interface Props {
  result: QueryResult
  onVisualize?: () => void
  onSaveQuery?: () => void
  isSavingQuery?: boolean
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

export function ResultsTable({ result, onVisualize, onSaveQuery, isSavingQuery = false }: Props) {
  const [sorting, setSorting] = useState<SortingState>([])
  const [isExpanded, setIsExpanded] = useState(false)

  const columns = useMemo<ColumnDef<Record<string, unknown>>[]>(
    () => [
      {
        id: '__rowNumber',
        header: '#',
        cell: (info) => <span className="text-xs font-mono text-zinc-500">{info.row.index + 1}</span>,
        enableSorting: false,
      },
      ...result.columns.map<ColumnDef<Record<string, unknown>>>((col) => ({
        accessorKey: col,
        header: col,
        cell: (info) => {
          const val = info.getValue()
          if (val === null || val === undefined) {
            return <span className="text-zinc-600 italic text-xs">null</span>
          }
          return <span className="text-xs font-mono">{String(val)}</span>
        },
      })),
    ],
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
  const pageIndex = table.getState().pagination.pageIndex
  const pageCount = table.getPageCount()

  const renderTable = (fullscreen = false) => (
    <div className="overflow-auto flex-1 min-h-0">
      <table className="w-full text-sm border-collapse">
        <thead className="sticky top-0 z-10 bg-[#18181b]">
          {table.getHeaderGroups().map((hg) => (
            <tr key={hg.id}>
              {hg.headers.map((header) => (
                <th
                  key={header.id}
                  className="px-3 py-2 text-left text-xs font-medium text-zinc-400 uppercase tracking-wide border-b border-white/10 cursor-pointer select-none hover:text-zinc-200 whitespace-nowrap transition-colors"
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
        <tbody className="divide-y divide-white/5 font-mono text-zinc-300">
          {rows.length === 0 ? (
            <tr>
              <td colSpan={table.getAllColumns().length} className="px-3 py-8 text-center text-zinc-600">
                No rows found
              </td>
            </tr>
          ) : (
            rows.map((row) => {
              return (
                <tr
                  key={row.id}
                  className="hover:bg-white/5 transition-colors"
                >
                  {row.getVisibleCells().map((cell) => (
                    <td
                      key={cell.id}
                      className={fullscreen
                        ? 'px-3 py-2 text-xs text-zinc-300 align-top whitespace-normal wrap-break-word min-w-40'
                        : 'px-3 py-2 text-xs text-zinc-300 whitespace-nowrap max-w-48 truncate'}
                    >
                      {flexRender(cell.column.columnDef.cell, cell.getContext())}
                    </td>
                  ))}
                </tr>
              )
            })
          )}
        </tbody>
      </table>
    </div>
  )

  const renderPagination = () => (
    <div className="flex items-center justify-between px-4 py-2 border-t border-white/10 bg-[#18181b] shrink-0">
      <span className="text-xs text-zinc-500">
        Page {pageCount === 0 ? 0 : pageIndex + 1} of {pageCount}
      </span>
      <div className="flex items-center gap-2">
        <Button
          variant="outline"
          size="sm"
          className="h-7 px-2 text-xs border-white/10 text-zinc-300 hover:text-white hover:bg-white/5 disabled:text-zinc-600 disabled:bg-black/20"
          onClick={() => table.previousPage()}
          disabled={!table.getCanPreviousPage()}
        >
          <ChevronLeft className="w-3.5 h-3.5 mr-1" />
          Prev
        </Button>
        <Button
          variant="outline"
          size="sm"
          className="h-7 px-2 text-xs border-white/10 text-zinc-300 hover:text-white hover:bg-white/5 disabled:text-zinc-600 disabled:bg-black/20"
          onClick={() => table.nextPage()}
          disabled={!table.getCanNextPage()}
        >
          Next
          <ChevronRight className="w-3.5 h-3.5 ml-1" />
        </Button>
      </div>
    </div>
  )

  return (
    <>
      <div className="flex h-full min-h-0 flex-col rounded-xl border border-white/10 overflow-hidden bg-[#111113]">
        {/* Toolbar */}
        <div className="flex items-center justify-between px-4 py-2 border-b border-white/10 bg-[#18181b] shrink-0">
          <div className="flex items-center gap-3">
            <Badge variant="secondary" className="bg-green-500/10 text-green-400 border-green-500/20 text-xs">
              {formatNumber(result.row_count)} rows
            </Badge>
            <span className="text-xs text-zinc-500">{formatMs(result.execution_time_ms)}</span>
          </div>
          <div className="flex items-center gap-1">
            <Button
              variant="ghost"
              size="sm"
              className="h-7 text-xs text-zinc-500 hover:text-zinc-300 hover:bg-white/5"
              onClick={() => downloadCSV(result.columns, result.rows)}
            >
              <Download className="w-3.5 h-3.5 mr-1" />
              CSV
            </Button>
            <Button
              variant="ghost"
              size="sm"
              className="h-7 text-xs text-zinc-500 hover:text-zinc-300 hover:bg-white/5"
              onClick={() => downloadJSON(result.rows)}
            >
              <Download className="w-3.5 h-3.5 mr-1" />
              JSON
            </Button>
            <Button
              variant="ghost"
              size="sm"
              className="h-7 text-xs text-zinc-500 hover:text-zinc-300 hover:bg-white/5"
              onClick={() => setIsExpanded(true)}
            >
              <Maximize2 className="w-3.5 h-3.5 mr-1" />
              Expand
            </Button>
            <Button
              variant="ghost"
              size="sm"
              className="h-7 text-xs text-zinc-500 hover:text-zinc-300 hover:bg-white/5"
              onClick={onVisualize}
              disabled={!onVisualize}
            >
              <BarChart2 className="w-3.5 h-3.5 mr-1" />
              Visualize
            </Button>
            <Button
              variant="ghost"
              size="sm"
              className="h-7 text-xs text-zinc-500 hover:text-zinc-300 hover:bg-white/5"
              onClick={onSaveQuery}
              disabled={!onSaveQuery || isSavingQuery}
            >
              <BookmarkPlus className="w-3.5 h-3.5 mr-1" />
              {isSavingQuery ? 'Saving…' : 'Save'}
            </Button>
          </div>
        </div>

        {renderTable()}
        {renderPagination()}
      </div>

      <Dialog open={isExpanded} onOpenChange={setIsExpanded}>
        <DialogContent
          className="w-[96vw] max-w-[96vw] sm:max-w-[96vw] h-[92vh] max-h-[92vh] p-0 overflow-hidden flex flex-col rounded-2xl bg-[#0c0c0e] border-white/10"
        >
          <DialogHeader className="px-5 py-4 border-b border-white/10 shrink-0">
            <div className="flex items-center justify-between gap-3 pr-8">
              <DialogTitle className="text-sm font-semibold text-white">Query Results</DialogTitle>
              <span className="text-xs text-zinc-500">
                {formatNumber(result.row_count)} rows · {formatMs(result.execution_time_ms)}
              </span>
            </div>
          </DialogHeader>

          <div className="flex-1 min-h-0 flex flex-col bg-[#111113]">
            {renderTable(true)}
            {renderPagination()}
          </div>
        </DialogContent>
      </Dialog>
    </>
  )
}
