import { apiClient } from './client'
import type {
  ApiResponse,
  ConnectionInsights,
  UserAccessSummary,
  GrantAccessRequest,
  UpdateAccessRestrictionsRequest,
} from '@/types'

export const insightsApi = {
  getForConnection: async (connectionId: string): Promise<ConnectionInsights> => {
    const { data } = await apiClient.get<ApiResponse<ConnectionInsights>>(
      `/api/connections/${connectionId}/insights`
    )
    return data.data
  },

  getForConnectionAdmin: async (connectionId: string): Promise<ConnectionInsights> => {
    const { data } = await apiClient.get<ApiResponse<ConnectionInsights>>(
      `/api/connections/${connectionId}/insights/admin`
    )
    return data.data
  },
}

export const accessApi = {
  /** Check if the current user is admin (owner) of a specific connection */
  isAdmin: async (connectionId: string): Promise<boolean> => {
    const { data } = await apiClient.get<ApiResponse<boolean>>(
      `/api/AccessControl/connections/IsAdmin`,
      { params: { ConnectionId: connectionId } }
    )
    return data.data ?? false
  },

  list: async (connectionId: string): Promise<UserAccessSummary[]> => {
    const { data } = await apiClient.get<ApiResponse<UserAccessSummary[]>>(
      `/api/AccessControl/connections/${connectionId}/users`
    )
    return data.data ?? []
  },

  grant: async (connectionId: string, payload: GrantAccessRequest): Promise<UserAccessSummary> => {
    const { data } = await apiClient.post<ApiResponse<UserAccessSummary>>(
      `/api/AccessControl/connections/${connectionId}/grants`,
      payload
    )
    if (!data.success) throw new Error(data.message ?? 'Failed to grant access')
    return data.data
  },

  updateRestrictions: async (
    connectionId: string,
    targetUserId: string,
    payload: UpdateAccessRestrictionsRequest
  ): Promise<void> => {
    const { data } = await apiClient.put<ApiResponse<object>>(
      `/api/AccessControl/connections/${connectionId}/users/${targetUserId}/restrictions`,
      payload
    )
    if (!data.success) throw new Error(data.message ?? 'Failed to update access')
  },

  revoke: async (connectionId: string, targetUserId: string): Promise<void> => {
    await apiClient.delete(
      `/api/AccessControl/connections/${connectionId}/users/${targetUserId}`
    )
  },
}
