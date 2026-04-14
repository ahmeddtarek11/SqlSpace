import {
  Save,
  RefreshCw,
  Trash2,
  Plus,
  Loader2,
  Download,
} from 'lucide-react'

interface ReportToolbarProps {
  isDraft: boolean
  isSaving: boolean
  isRefreshing: boolean
  onSave?: () => void
  onRefresh?: () => void
  onDelete?: () => void
  onExportPdf?: () => void
  onNewReport: () => void
}

export function ReportToolbar({
  isDraft,
  isSaving,
  isRefreshing,
  onSave,
  onRefresh,
  onDelete,
  onExportPdf,
  onNewReport,
}: ReportToolbarProps) {
  return (
    <div className="flex items-center gap-2 px-4 py-2 border-b border-white/10 bg-[#0d0d0f] shrink-0">
      <button
        type="button"
        onClick={onNewReport}
        className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-medium text-zinc-400 hover:text-zinc-200 hover:bg-white/5 transition-colors"
      >
        <Plus className="w-3.5 h-3.5" />
        New report
      </button>

      <div className="ml-auto flex items-center gap-2">
        {!isDraft && onExportPdf && (
          <button
            type="button"
            onClick={onExportPdf}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg border border-white/10 hover:bg-white/5 text-zinc-300 text-xs font-medium transition-colors"
          >
            <Download className="w-3.5 h-3.5" />
            Download PDF
          </button>
        )}

        {isDraft && onSave && (
          <button
            type="button"
            onClick={onSave}
            disabled={isSaving}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-sky-500 hover:bg-sky-400 disabled:opacity-40 text-white text-xs font-medium transition-colors"
          >
            {isSaving ? <Loader2 className="w-3.5 h-3.5 animate-spin" /> : <Save className="w-3.5 h-3.5" />}
            Save report
          </button>
        )}

        {!isDraft && onRefresh && (
          <button
            type="button"
            onClick={onRefresh}
            disabled={isRefreshing}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg border border-white/10 hover:bg-white/5 disabled:opacity-40 text-zinc-300 text-xs font-medium transition-colors"
          >
            {isRefreshing ? <Loader2 className="w-3.5 h-3.5 animate-spin" /> : <RefreshCw className="w-3.5 h-3.5" />}
            Refresh
          </button>
        )}

        {!isDraft && onDelete && (
          <button
            type="button"
            onClick={onDelete}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg border border-red-500/20 hover:bg-red-500/5 text-red-400 text-xs font-medium transition-colors"
          >
            <Trash2 className="w-3.5 h-3.5" />
            Delete
          </button>
        )}
      </div>
    </div>
  )
}
