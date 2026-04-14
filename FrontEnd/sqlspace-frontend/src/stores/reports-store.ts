import { create } from 'zustand'
import { reportsApi } from '@/api/reports'
import type { ReportDraftDto, ReportDto, ReportHeaderDto, CreateReportRequest } from '@/types'

interface ConnectionReportsState {
  list: ReportHeaderDto[]
  draft: ReportDraftDto | null
  hasSessionDraft: boolean
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
  hydrateDraft: (connectionId: string) => void
  restoreSessionDraft: (connectionId: string) => void
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
  hasSessionDraft: false,
  activeReport: null,
  isGenerating: false,
  isSaving: false,
  isRefreshing: false,
  isLoadingList: false,
  error: null,
}

const REPORT_DRAFT_SESSION_PREFIX = 'sqlspace:report-draft:'

function hasSessionStorage(): boolean {
  return typeof window !== 'undefined' && typeof window.sessionStorage !== 'undefined'
}

function draftSessionKey(connectionId: string): string {
  return `${REPORT_DRAFT_SESSION_PREFIX}${connectionId}`
}

function isReportDraftDto(value: unknown): value is ReportDraftDto {
  if (!value || typeof value !== 'object') return false
  const draft = value as Partial<ReportDraftDto>
  return typeof draft.title === 'string' && typeof draft.originalPrompt === 'string' && Array.isArray(draft.sections)
}

function getSessionDraft(connectionId: string): ReportDraftDto | null {
  if (!hasSessionStorage()) return null
  try {
    const raw = window.sessionStorage.getItem(draftSessionKey(connectionId))
    if (!raw) return null
    const parsed: unknown = JSON.parse(raw)
    if (!isReportDraftDto(parsed)) {
      window.sessionStorage.removeItem(draftSessionKey(connectionId))
      return null
    }
    return parsed
  } catch {
    return null
  }
}

function hasSessionDraft(connectionId: string): boolean {
  if (!hasSessionStorage()) return false
  try {
    return !!window.sessionStorage.getItem(draftSessionKey(connectionId))
  } catch {
    return false
  }
}

function setSessionDraft(connectionId: string, draft: ReportDraftDto): void {
  if (!hasSessionStorage()) return
  try {
    window.sessionStorage.setItem(draftSessionKey(connectionId), JSON.stringify(draft))
  } catch {
    // Ignore storage quota and privacy mode errors.
  }
}

function clearSessionDraft(connectionId: string): void {
  if (!hasSessionStorage()) return
  try {
    window.sessionStorage.removeItem(draftSessionKey(connectionId))
  } catch {
    // Ignore storage quota and privacy mode errors.
  }
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
    set((s) =>
      patch(s, connectionId, {
        isLoadingList: true,
        error: null,
        hasSessionDraft: hasSessionDraft(connectionId),
      })
    )
    try {
      const list = await reportsApi.list(connectionId)
      set((s) =>
        patch(s, connectionId, {
          list,
          isLoadingList: false,
          hasSessionDraft: hasSessionDraft(connectionId),
        })
      )
    } catch (err) {
      set((s) =>
        patch(s, connectionId, {
          isLoadingList: false,
          hasSessionDraft: hasSessionDraft(connectionId),
          error: err instanceof Error ? err.message : 'Failed to load reports',
        })
      )
    }
  },

  generateDraft: async (connectionId, prompt) => {
    set((s) =>
      patch(s, connectionId, {
        isGenerating: true,
        error: null,
        draft: null,
        hasSessionDraft: hasSessionDraft(connectionId),
      })
    )
    try {
      const draft = await reportsApi.draft(connectionId, prompt)
      setSessionDraft(connectionId, draft)
      set((s) => patch(s, connectionId, { draft, isGenerating: false, hasSessionDraft: true }))
    } catch (err) {
      set((s) =>
        patch(s, connectionId, {
          isGenerating: false,
          hasSessionDraft: hasSessionDraft(connectionId),
          error: err instanceof Error ? err.message : 'Failed to generate report',
        })
      )
    }
  },

  hydrateDraft: (connectionId) => {
    const current = get().byConnection[connectionId] ?? emptyConnectionState
    const sessionDraft = getSessionDraft(connectionId)
    if (!sessionDraft) {
      set((s) => patch(s, connectionId, { hasSessionDraft: false }))
      return
    }

    if (current.draft || current.activeReport) {
      set((s) => patch(s, connectionId, { hasSessionDraft: true }))
      return
    }

    set((s) =>
      patch(s, connectionId, {
        draft: sessionDraft,
        hasSessionDraft: true,
        error: null,
      })
    )
  },

  restoreSessionDraft: (connectionId) => {
    const sessionDraft = getSessionDraft(connectionId)
    if (!sessionDraft) {
      set((s) => patch(s, connectionId, { hasSessionDraft: false }))
      return
    }

    set((s) =>
      patch(s, connectionId, {
        draft: sessionDraft,
        activeReport: null,
        hasSessionDraft: true,
        error: null,
      })
    )
  },

  clearDraft: (connectionId) => {
    set((s) =>
      patch(s, connectionId, {
        draft: null,
        error: null,
        hasSessionDraft: hasSessionDraft(connectionId),
      })
    )
  },

  saveReport: async (connectionId, request) => {
    set((s) => patch(s, connectionId, { isSaving: true, error: null }))
    try {
      const saved = await reportsApi.save(connectionId, request)
      // Reload list so the new entry appears in the sidebar
      const list = await reportsApi.list(connectionId)
      clearSessionDraft(connectionId)
      set((s) =>
        patch(s, connectionId, {
          isSaving: false,
          draft: null,
          hasSessionDraft: false,
          activeReport: saved,
          list,
        })
      )
      return saved
    } catch (err) {
      set((s) =>
        patch(s, connectionId, {
          isSaving: false,
          hasSessionDraft: hasSessionDraft(connectionId),
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
      set((s) =>
        patch(s, connectionId, {
          activeReport: report,
          draft: null,
          hasSessionDraft: hasSessionDraft(connectionId),
        })
      )
    } catch (err) {
      set((s) =>
        patch(s, connectionId, {
          hasSessionDraft: hasSessionDraft(connectionId),
          error: err instanceof Error ? err.message : 'Failed to load report',
        })
      )
    }
  },

  clearActiveReport: (connectionId) => {
    set((s) => patch(s, connectionId, { activeReport: null, hasSessionDraft: hasSessionDraft(connectionId) }))
  },

  refreshReport: async (connectionId, reportId, regenerateNarrative = true) => {
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
          hasSessionDraft: hasSessionDraft(connectionId),
        })
      })
    } catch (err) {
      set((s) =>
        patch(s, connectionId, {
          hasSessionDraft: hasSessionDraft(connectionId),
          error: err instanceof Error ? err.message : 'Failed to delete report',
        })
      )
    }
  },
}))

export const selectConnectionReports = (connectionId: string) => (s: ReportsState) =>
  s.byConnection[connectionId] ?? emptyConnectionState
