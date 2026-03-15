import { apiClient } from './client'
import type {
  ApiResponse,
  Connection,
  CreateConnectionRequest,
  TestConnectionRequest,
  ParsedSchema,
} from '@/types'

export const connectionsApi = {
  list: async (): Promise<Connection[]> => {
    const { data } = await apiClient.get<ApiResponse<Connection[]>>('/api/connections')
    return data.data ?? []
  },

  create: async (payload: CreateConnectionRequest): Promise<Connection> => {
    const { data } = await apiClient.post<ApiResponse<Connection>>('/api/connections', payload)
    if (!data.success) throw new Error(data.message ?? 'Failed to create connection')
    return data.data
  },

  get: async (id: string): Promise<Connection> => {
    const { data } = await apiClient.get<ApiResponse<Connection>>(`/api/connections/${id}`)
    return data.data
  },

  delete: async (id: string): Promise<void> => {
    await apiClient.delete(`/api/connections/${id}`)
  },

  test: async (payload: TestConnectionRequest): Promise<{ success: boolean; message: string }> => {
    const { data } = await apiClient.post<ApiResponse<{ isSuccessful: boolean; message: string }>>('/api/connections/test', payload)
    return {
      success: data.data?.isSuccessful ?? data.success,
      message: data.data?.message ?? data.message ?? '',
    }
  },

  healthTest: async (id: string): Promise<void> => {
    await apiClient.post(`/api/connections/${id}/health-test`)
  },

  /** Returns parsed schema from backend (backend returns a JSON-encoded string) */
  schema: async (connectionId: string): Promise<ParsedSchema> => {
    const { data } = await apiClient.get<ApiResponse<string>>(
      '/api/schema/connections/GetFilteredConnectionSchema',
      { params: { connectionId } }
    )
    const raw = data.data ?? '{}'
    return JSON.parse(raw) as ParsedSchema
  },
}
