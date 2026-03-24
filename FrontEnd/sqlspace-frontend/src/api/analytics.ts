import { apiClient } from './client'
import type {
  ApiResponse,
  ChartSuggestion,
  SavedChartDto,
  SaveChartRequest,
  ChartDataResult,
} from '@/types'

export const analyticsApi = {
  suggestCharts: async (
    connectionId: string,
    userPrompt?: string,
    maxSuggestions = 5,
  ): Promise<ChartSuggestion[]> => {
    const { data } = await apiClient.post<ApiResponse<ChartSuggestion[]>>(
      `/api/connections/${connectionId}/analytics/suggest`,
      { userPrompt, maxSuggestions },
    )
    return data.data ?? []
  },

  getCharts: async (connectionId: string): Promise<SavedChartDto[]> => {
    const { data } = await apiClient.get<ApiResponse<SavedChartDto[]>>(
      `/api/connections/${connectionId}/analytics/charts`,
    )
    return data.data ?? []
  },

  saveChart: async (
    connectionId: string,
    payload: SaveChartRequest,
  ): Promise<SavedChartDto> => {
    const { data } = await apiClient.post<ApiResponse<SavedChartDto>>(
      `/api/connections/${connectionId}/analytics/charts`,
      payload,
    )
    return data.data
  },

  updateChart: async (
    connectionId: string,
    chartId: string,
    payload: Partial<SaveChartRequest>,
  ): Promise<SavedChartDto> => {
    const { data } = await apiClient.put<ApiResponse<SavedChartDto>>(
      `/api/connections/${connectionId}/analytics/charts/${chartId}`,
      payload,
    )
    return data.data
  },

  deleteChart: async (connectionId: string, chartId: string): Promise<void> => {
    await apiClient.delete(
      `/api/connections/${connectionId}/analytics/charts/${chartId}`,
    )
  },

  executeChart: async (
    connectionId: string,
    chartId: string,
  ): Promise<ChartDataResult> => {
    const { data } = await apiClient.post<ApiResponse<ChartDataResult>>(
      `/api/connections/${connectionId}/analytics/charts/${chartId}/execute`,
    )
    return data.data
  },

  refreshAllCharts: async (
    connectionId: string,
  ): Promise<ChartDataResult[]> => {
    const { data } = await apiClient.post<ApiResponse<ChartDataResult[]>>(
      `/api/connections/${connectionId}/analytics/charts/refresh`,
    )
    return data.data ?? []
  },

  updateLayout: async (
    connectionId: string,
    layouts: { chartId: string; gridX: number; gridY: number; gridW: number; gridH: number }[],
  ): Promise<void> => {
    await apiClient.put(
      `/api/connections/${connectionId}/analytics/charts/layout`,
      layouts,
    )
  },
}
