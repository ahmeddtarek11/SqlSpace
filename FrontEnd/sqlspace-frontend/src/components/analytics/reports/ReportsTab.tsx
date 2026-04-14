import { useEffect } from 'react'
import { FileText, AlertCircle } from 'lucide-react'
import { toast } from 'sonner'
import { cn } from '@/lib/utils'
import { exportReportAsPdf } from '@/lib/report-export'
import { useReportsStore, selectConnectionReports } from '@/stores/reports-store'
import { ReportPromptInput } from './ReportPromptInput'
import { ReportView } from './ReportView'
import { ReportToolbar } from './ReportToolbar'
import type { CreateReportRequest } from '@/types'

interface ReportsTabProps {
  connectionId: string
  hidePromptInput?: boolean
}

export function ReportsTab({ connectionId, hidePromptInput = false }: ReportsTabProps) {
  const store = useReportsStore((s) => selectConnectionReports(connectionId)(s))
  const {
    loadList,
    generateDraft,
    clearDraft,
    saveReport,
    openReport,
    clearActiveReport,
    refreshReport,
    deleteReport,
  } = useReportsStore()

  useEffect(() => {
    void loadList(connectionId)
  }, [connectionId, loadList])

  const activeContent = store.draft ?? store.activeReport
  const isDraft = !!store.draft && !store.activeReport

  const handleExportPdf = () => {
    if (!store.activeReport) return
    try {
      exportReportAsPdf(store.activeReport)
      toast.success('Report PDF downloaded')
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to download report PDF')
    }
  }

  const handleSave = async () => {
    if (!store.draft) return
    const request: CreateReportRequest = {
      title: store.draft.title,
      originalPrompt: store.draft.originalPrompt,
      summary: store.draft.summary,
      sections: store.draft.sections.map((s) => ({
        sortOrder: s.sortOrder,
        heading: s.heading,
        narrativeText: s.narrativeText,
        chartType: s.chartType,
        chartConfigJson: s.chartConfigJson,
        sqlQuery: s.sqlQuery,
        resultsJson: s.resultsJson,
        rowsReturned: s.rowsReturned,
        executionTimeMs: s.executionTimeMs,
        executionSuccess: s.executionSuccess,
        executionErrorMessage: s.executionErrorMessage,
      })),
    }
    await saveReport(connectionId, request)
  }

  return (
    <div className="flex h-full min-h-0 overflow-hidden">
      {/* Sidebar — saved reports list */}
      <div className="w-64 shrink-0 border-r border-white/10 flex flex-col overflow-hidden bg-[#0d0d0f]">
        <div className="px-4 py-3 border-b border-white/10 shrink-0">
          <p className="text-[10px] font-medium text-zinc-500 uppercase tracking-wider">Saved Reports</p>
        </div>

        <div className="flex-1 overflow-y-auto py-2">
          {store.isLoadingList && store.list.length === 0 && (
            <div className="space-y-2 px-3 py-2 animate-pulse">
              {Array.from({ length: 3 }).map((_, i) => (
                <div key={i} className="h-10 bg-white/5 rounded-lg" />
              ))}
            </div>
          )}

          {!store.isLoadingList && store.list.length === 0 && (
            <div className="px-4 py-6 text-center">
              <FileText className="w-6 h-6 text-zinc-700 mx-auto mb-2" />
              <p className="text-xs text-zinc-600">No saved reports yet</p>
            </div>
          )}

          {store.list.map((header) => {
            const isActive = store.activeReport?.reportId === header.reportId
            return (
              <button
                key={header.reportId}
                type="button"
                onClick={() => void openReport(connectionId, header.reportId)}
                className={cn(
                  'w-full text-left px-3 py-2.5 mx-1 rounded-lg transition-colors',
                  isActive
                    ? 'bg-sky-500/10 border border-sky-500/20'
                    : 'hover:bg-white/5 border border-transparent'
                )}
              >
                <p className={cn('text-xs font-medium truncate', isActive ? 'text-sky-300' : 'text-zinc-300')}>
                  {header.title}
                </p>
                <p className="text-[10px] text-zinc-600 mt-0.5">
                  {header.sectionCount} section{header.sectionCount !== 1 ? 's' : ''} ·{' '}
                  {new Date(header.updatedAtUtc).toLocaleDateString(undefined, { month: 'short', day: 'numeric' })}
                </p>
              </button>
            )
          })}
        </div>
      </div>

      {/* Main content area */}
      <div className="flex-1 min-w-0 flex flex-col overflow-hidden">
        {/* Error banner */}
        {store.error && (
          <div className="shrink-0 border-b border-red-500/20 bg-red-500/5 px-4 py-2">
            <div className="flex items-center gap-2 text-xs text-red-300">
              <AlertCircle className="w-3.5 h-3.5 shrink-0" />
              <span className="truncate">{store.error}</span>
            </div>
          </div>
        )}

        {/* Toolbar — shown only when content is visible */}
        {activeContent && (
          <ReportToolbar
            isDraft={isDraft}
            isSaving={store.isSaving}
            isRefreshing={store.isRefreshing}
            onNewReport={() => {
              clearDraft(connectionId)
              clearActiveReport(connectionId)
            }}
            onSave={isDraft ? handleSave : undefined}
            onRefresh={
              !isDraft && store.activeReport
                ? () => void refreshReport(connectionId, store.activeReport!.reportId)
                : undefined
            }
            onDelete={
              !isDraft && store.activeReport
                ? () => void deleteReport(connectionId, store.activeReport!.reportId)
                : undefined
            }
            onExportPdf={!isDraft && store.activeReport ? handleExportPdf : undefined}
          />
        )}

        {/* Generating skeleton */}
        {store.isGenerating && (
          <div className="flex-1 overflow-y-auto px-6 py-8">
            <div className="max-w-4xl mx-auto space-y-6 animate-pulse">
              <div className="h-7 bg-white/5 rounded w-1/2" />
              <div className="h-4 bg-white/5 rounded w-3/4" />
              {Array.from({ length: 3 }).map((_, i) => (
                <div key={i} className="space-y-3">
                  <div className="h-5 bg-white/5 rounded w-1/3" />
                  <div className="h-4 bg-white/5 rounded w-full" />
                  <div className="h-4 bg-white/5 rounded w-5/6" />
                  <div className="h-48 bg-white/5 rounded-xl" />
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Report content (draft or saved) */}
        {!store.isGenerating && activeContent && (
          <ReportView report={activeContent} />
        )}

        {/* Empty state — prompt input */}
        {!store.isGenerating && !activeContent && !hidePromptInput && (
          <ReportPromptInput
            onGenerate={(prompt) => void generateDraft(connectionId, prompt)}
            isGenerating={store.isGenerating}
          />
        )}

        {!store.isGenerating && !activeContent && hidePromptInput && (
          <div className="flex-1 flex items-center justify-center px-8 text-center">
            <div>
              <FileText className="w-6 h-6 text-zinc-700 mx-auto mb-2" />
              <p className="text-sm text-zinc-400">No active report</p>
              <p className="text-xs text-zinc-600 mt-1">Use the shared prompt bar above in report mode to generate one.</p>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
