import { apiClient } from './client'
import type {
  ApiResponse,
  QueryExecutionResult,
  PaginatedQueryHistory,
  SavedQueryDto,
  CreateSavedQueryRequest,
  QueryResult,
} from '@/types'

export interface ExecutePayload {
  connectionId: string
  userPrompt: string
}

/** Parse the resultsJson string from backend into a QueryResult */
function parseResults(raw: QueryExecutionResult): QueryResult {
  let rows: Record<string, unknown>[] = []
  if (raw.resultsJson) {
    try {
      rows = JSON.parse(raw.resultsJson) as Record<string, unknown>[]
    } catch {
      rows = []
    }
  }
  const columns = rows.length > 0 ? Object.keys(rows[0]) : []
  return {
    columns,
    rows,
    row_count: raw.rowsReturned ?? rows.length,
    execution_time_ms: raw.executionTimeMs,
  }
}

export const queriesApi = {
  execute: async (
    payload: ExecutePayload
  ): Promise<{ sql: string; explanation: string; result: QueryResult; queryHistoryId: string }> => {
    const { data } = await apiClient.post<ApiResponse<QueryExecutionResult>>('/api/queries/execute', {
      connectionId: payload.connectionId,
      userPrompt: payload.userPrompt,
    })
    if (!data.success || !data.data.success) {
      throw new Error(data.data?.errorMessage ?? data.message ?? 'Query failed')
    }
    const raw = data.data
    return {
      sql: raw.generatedSql ?? '',
      explanation: raw.llmExplanation ?? '',
      result: parseResults(raw),
      queryHistoryId: raw.queryHistoryId,
    }
  },

  history: async (params?: {
    pageNumber?: number
    pageSize?: number
  }): Promise<PaginatedQueryHistory> => {
    const { data } = await apiClient.get<ApiResponse<PaginatedQueryHistory>>('/api/queries/history', {
      params: { pageNumber: params?.pageNumber ?? 1, pageSize: params?.pageSize ?? 50 },
    })
    return data.data
  },

  savedQueries: async (): Promise<SavedQueryDto[]> => {
    const { data } = await apiClient.get<ApiResponse<SavedQueryDto[]>>('/api/saved-queries')
    return data.data ?? []
  },

  saveQuery: async (payload: CreateSavedQueryRequest): Promise<SavedQueryDto> => {
    const { data } = await apiClient.post<ApiResponse<SavedQueryDto>>('/api/saved-queries', payload)
    if (!data.success) throw new Error(data.message ?? 'Failed to save query')
    return data.data
  },

  deleteSaved: async (id: string): Promise<void> => {
    await apiClient.delete(`/api/saved-queries/${id}`)
  },
}
