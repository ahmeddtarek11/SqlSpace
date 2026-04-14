import { apiClient } from './client'
import type {
  ApiResponse,
  ReportDraftDto,
  ReportDto,
  ReportHeaderDto,
  CreateReportRequest,
} from '@/types'

const REPORTS_LONG_OPERATION_TIMEOUT_MS = 180_000

export const reportsApi = {
  draft: async (connectionId: string, prompt: string): Promise<ReportDraftDto> => {
    const { data } = await apiClient.post<ApiResponse<ReportDraftDto>>(
      `/api/connections/${connectionId}/reports/draft`,
      { prompt },
      { timeout: REPORTS_LONG_OPERATION_TIMEOUT_MS }
    )
    if (!data.success) throw new Error(data.errors?.[0]?.message ?? 'Failed to generate report')
    return data.data
  },

  save: async (connectionId: string, request: CreateReportRequest): Promise<ReportDto> => {
    const { data } = await apiClient.post<ApiResponse<ReportDto>>(
      `/api/connections/${connectionId}/reports`,
      request
    )
    if (!data.success) throw new Error(data.errors?.[0]?.message ?? 'Failed to save report')
    return data.data
  },

  list: async (connectionId: string): Promise<ReportHeaderDto[]> => {
    const { data } = await apiClient.get<ApiResponse<ReportHeaderDto[]>>(
      `/api/connections/${connectionId}/reports`
    )
    return data.data ?? []
  },

  get: async (connectionId: string, reportId: string): Promise<ReportDto> => {
    const { data } = await apiClient.get<ApiResponse<ReportDto>>(
      `/api/connections/${connectionId}/reports/${reportId}`
    )
    if (!data.success) throw new Error(data.errors?.[0]?.message ?? 'Failed to load report')
    return data.data
  },

  refresh: async (
    connectionId: string,
    reportId: string,
    regenerateNarrative = true
  ): Promise<ReportDto> => {
    const { data } = await apiClient.post<ApiResponse<ReportDto>>(
      `/api/connections/${connectionId}/reports/${reportId}/refresh`,
      null,
      {
        params: { regenerateNarrative },
        timeout: regenerateNarrative ? REPORTS_LONG_OPERATION_TIMEOUT_MS : undefined,
      }
    )
    if (!data.success) throw new Error(data.errors?.[0]?.message ?? 'Failed to refresh report')
    return data.data
  },

  remove: async (connectionId: string, reportId: string): Promise<void> => {
    await apiClient.delete(`/api/connections/${connectionId}/reports/${reportId}`)
  },
}
