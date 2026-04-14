import { apiClient } from './client'
import type {
  ApiResponse,
  ChatMessage,
  KnowledgeDocument,
  RagIngestResult,
  RagQueryResult,
} from '@/types'

export const knowledgeBaseApi = {
  listDocuments: async (connectionId: string): Promise<KnowledgeDocument[]> => {
    const { data } = await apiClient.get<ApiResponse<KnowledgeDocument[]>>(
      `/api/connections/${connectionId}/knowledge/documents`
    )
    return data.data ?? []
  },

  uploadDocument: async (
    connectionId: string,
    file: File,
    allowedRoles: string[]
  ): Promise<RagIngestResult> => {
    const form = new FormData()
    form.append('file', file)
    allowedRoles.forEach((role) => form.append('allowedRoles', role))

    const { data } = await apiClient.post<ApiResponse<RagIngestResult>>(
      `/api/connections/${connectionId}/knowledge/documents`,
      form,
      { headers: { 'Content-Type': 'multipart/form-data' } }
    )
    if (!data.success) throw new Error(data.errors?.[0]?.message ?? 'Upload failed')
    return data.data
  },

  deleteDocument: async (connectionId: string, documentId: string): Promise<void> => {
    const { data } = await apiClient.delete<ApiResponse<boolean>>(
      `/api/connections/${connectionId}/knowledge/documents/${documentId}`
    )
    if (!data.success) throw new Error(data.errors?.[0]?.message ?? 'Delete failed')
  },

  ask: async (
    connectionId: string,
    query: string,
    topK = 5
  ): Promise<RagQueryResult> => {
    const { data } = await apiClient.post<ApiResponse<RagQueryResult>>(
      `/api/connections/${connectionId}/knowledge/ask`,
      { query, topK }
    )
    if (!data.success) throw new Error(data.errors?.[0]?.message ?? 'Query failed')
    return data.data
  },

  getChatHistory: async (connectionId: string, take = 100): Promise<ChatMessage[]> => {
    const { data } = await apiClient.get<ApiResponse<ChatMessage[]>>(
      `/api/connections/${connectionId}/knowledge/chat`,
      { params: { take } }
    )
    return data.data ?? []
  },
}
