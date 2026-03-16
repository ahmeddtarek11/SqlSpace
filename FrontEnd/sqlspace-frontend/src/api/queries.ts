import { apiClient } from './client'
import type {
  ApiResponse,
  QueryExecutionResult,
  PaginatedQueryHistory,
  SavedQueryDto,
  CreateSavedQueryRequest,
  QueryResult,
  QueryStatistics,
} from '@/types'

export interface ExecutePayload {
  connectionId: string
  userPrompt: string
}

/** Parse the resultsJson string from backend into a QueryResult */
function parseResults(raw: QueryExecutionResult): QueryResult {
  let rows: Record<string, unknown>[] = []
  let columns: string[] = []

  if (raw.resultsJson) {
    try {
      const parsed = JSON.parse(raw.resultsJson)
      // Backend returns { columns, rows, isTruncated, maxRows }
      if (Array.isArray(parsed)) {
        // Legacy format: direct array of rows
        rows = parsed
      } else if (parsed.rows && Array.isArray(parsed.rows)) {
        // Backend returns rows as value arrays: [[v1, v2], [v1, v2]]
        // Convert to objects keyed by column name
        columns = parsed.columns || []
        rows = (parsed.rows as unknown[][]).map((rowArr) => {
          const obj: Record<string, unknown> = {}
          columns.forEach((col, i) => { obj[col] = rowArr[i] })
          return obj
        })
      }
    } catch {
      rows = []
      columns = []
    }
  }

  if (columns.length === 0 && rows.length > 0) {
    columns = Object.keys(rows[0])
  }

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

  renameSaved: async (id: string, name: string): Promise<SavedQueryDto> => {
    const { data } = await apiClient.patch<ApiResponse<SavedQueryDto>>(`/api/saved-queries/${id}`, { name })
    if (!data.success) throw new Error(data.message ?? 'Failed to rename query')
    return data.data
  },

  executeSaved: async (id: string): Promise<{ sql: string; explanation: string; result: QueryResult; queryHistoryId: string }> => {
    const { data } = await apiClient.post<ApiResponse<QueryExecutionResult>>(`/api/saved-queries/${id}/execute`)
    if (!data.success || !data.data.success) {
      throw new Error(data.data?.errorMessage ?? data.message ?? 'Execution failed')
    }
    const raw = data.data
    return { sql: raw.generatedSql ?? '', explanation: raw.llmExplanation ?? '', result: parseResults(raw), queryHistoryId: raw.queryHistoryId }
  },

  rerun: async (queryId: string): Promise<{ sql: string; explanation: string; result: QueryResult; queryHistoryId: string }> => {
    const { data } = await apiClient.post<ApiResponse<QueryExecutionResult>>(`/api/queries/${queryId}/rerun`)
    if (!data.success || !data.data.success) {
      throw new Error(data.data?.errorMessage ?? data.message ?? 'Rerun failed')
    }
    const raw = data.data
    return { sql: raw.generatedSql ?? '', explanation: raw.llmExplanation ?? '', result: parseResults(raw), queryHistoryId: raw.queryHistoryId }
  },

  searchHistory: async (params: {
    searchTerm: string
    connectionId?: string
    pageNumber?: number
    pageSize?: number
  }): Promise<PaginatedQueryHistory> => {
    const { data } = await apiClient.get<ApiResponse<PaginatedQueryHistory>>('/api/queries/history/search', {
      params: { searchTerm: params.searchTerm, connectionId: params.connectionId, pageNumber: params.pageNumber ?? 1, pageSize: params.pageSize ?? 50 },
    })
    return data.data
  },

  historyStats: async (params?: { connectionId?: string; dateFrom?: string; dateTo?: string }): Promise<QueryStatistics> => {
    const { data } = await apiClient.get<ApiResponse<QueryStatistics>>('/api/queries/history/stats', { params })
    return data.data
  },
}
