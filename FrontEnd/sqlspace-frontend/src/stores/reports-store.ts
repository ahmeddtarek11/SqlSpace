import { create } from 'zustand'
import { reportsApi } from '@/api/reports'
import type { ReportDraftDto, ReportDto, ReportHeaderDto, CreateReportRequest } from '@/types'

interface ConnectionReportsState {
  list: ReportHeaderDto[]
  draft: ReportDraftDto | null
  activeReport: ReportDto | null
  isGenerating: boolean
  isSaving: boolean
  isRefreshing: boolean
  isLoadingList: boolean
  error: string | null
}

interface ReportsState {
  byConnection: Record<string, ConnectionReportsState>
  loadList: (connectionId: string) => Promise<void>
  generateDraft: (connectionId: string, prompt: string) => Promise<void>
  clearDraft: (connectionId: string) => void
  saveReport: (connectionId: string, request: CreateReportRequest) => Promise<ReportDto | null>
  openReport: (connectionId: string, reportId: string) => Promise<void>
  clearActiveReport: (connectionId: string) => void
  refreshReport: (connectionId: string, reportId: string, regenerateNarrative?: boolean) => Promise<void>
  deleteReport: (connectionId: string, reportId: string) => Promise<void>
}

const emptyConnectionState: ConnectionReportsState = {
  list: [],
  draft: null,
  activeReport: null,
  isGenerating: false,
  isSaving: false,
  isRefreshing: false,
  isLoadingList: false,
  error: null,
}

function patch(
  state: ReportsState,
  connectionId: string,
  update: Partial<ConnectionReportsState>
): Pick<ReportsState, 'byConnection'> {
  return {
    byConnection: {
      ...state.byConnection,
      [connectionId]: {
        ...(state.byConnection[connectionId] ?? emptyConnectionState),
        ...update,
      },
    },
  }
}

export const useReportsStore = create<ReportsState>()((set, get) => ({
  byConnection: {},

  loadList: async (connectionId) => {
    set((s) => patch(s, connectionId, { isLoadingList: true, error: null }))
    try {
      const list = await reportsApi.list(connectionId)
      set((s) => patch(s, connectionId, { list, isLoadingList: false }))
    } catch (err) {
      set((s) =>
        patch(s, connectionId, {
          isLoadingList: false,
          error: err instanceof Error ? err.message : 'Failed to load reports',
        })
      )
    }
  },

  generateDraft: async (connectionId, prompt) => {
    set((s) => patch(s, connectionId, { isGenerating: true, error: null, draft: null }))
    try {
      const draft = await reportsApi.draft(connectionId, prompt)
      set((s) => patch(s, connectionId, { draft, isGenerating: false }))
    } catch (err) {
      set((s) =>
        patch(s, connectionId, {
          isGenerating: false,
          error: err instanceof Error ? err.message : 'Failed to generate report',
        })
      )
    }
  },

  clearDraft: (connectionId) => {
    set((s) => patch(s, connectionId, { draft: null, error: null }))
  },

  saveReport: async (connectionId, request) => {
    set((s) => patch(s, connectionId, { isSaving: true, error: null }))
    try {
      const saved = await reportsApi.save(connectionId, request)
      // Reload list so the new entry appears in the sidebar
      const list = await reportsApi.list(connectionId)
      set((s) =>
        patch(s, connectionId, {
          isSaving: false,
          draft: null,
          activeReport: saved,
          list,
        })
      )
      return saved
    } catch (err) {
      set((s) =>
        patch(s, connectionId, {
          isSaving: false,
          error: err instanceof Error ? err.message : 'Failed to save report',
        })
      )
      return null
    }
  },

  openReport: async (connectionId, reportId) => {
    set((s) => patch(s, connectionId, { error: null }))
    try {
      const report = await reportsApi.get(connectionId, reportId)
      set((s) => patch(s, connectionId, { activeReport: report, draft: null }))
    } catch (err) {
      set((s) =>
        patch(s, connectionId, {
          error: err instanceof Error ? err.message : 'Failed to load report',
        })
      )
    }
  },

  clearActiveReport: (connectionId) => {
    set((s) => patch(s, connectionId, { activeReport: null }))
  },

  refreshReport: async (connectionId, reportId, regenerateNarrative = false) => {
    set((s) => patch(s, connectionId, { isRefreshing: true, error: null }))
    try {
      const report = await reportsApi.refresh(connectionId, reportId, regenerateNarrative)
      set((s) => patch(s, connectionId, { isRefreshing: false, activeReport: report }))
    } catch (err) {
      set((s) =>
        patch(s, connectionId, {
          isRefreshing: false,
          error: err instanceof Error ? err.message : 'Failed to refresh report',
        })
      )
    }
  },

  deleteReport: async (connectionId, reportId) => {
    try {
      await reportsApi.remove(connectionId, reportId)
      const list = await reportsApi.list(connectionId)
      set((s) => {
        const current = s.byConnection[connectionId] ?? emptyConnectionState
        const wasActive = current.activeReport?.reportId === reportId
        return patch(s, connectionId, {
          list,
          activeReport: wasActive ? null : current.activeReport,
        })
      })
    } catch (err) {
      set((s) =>
        patch(s, connectionId, {
          error: err instanceof Error ? err.message : 'Failed to delete report',
        })
      )
    }
  },
}))

export const selectConnectionReports = (connectionId: string) => (s: ReportsState) =>
  s.byConnection[connectionId] ?? emptyConnectionState
