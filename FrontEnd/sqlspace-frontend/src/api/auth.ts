import { apiClient } from './client'
import type { ApiResponse, AuthTokensResult, User } from '@/types'

export interface LoginPayload {
  email: string
  password: string
}

export interface RegisterPayload {
  email: string
  username: string
  password: string
  firstName?: string
  lastName?: string
}

/** Decode JWT payload (base64) without verifying signature */
function decodeJWT(token: string): Record<string, unknown> {
  try {
    const payload = token.split('.')[1]
    const decoded = atob(payload.replace(/-/g, '+').replace(/_/g, '/'))
    return JSON.parse(decoded) as Record<string, unknown>
  } catch {
    return {}
  }
}

/** Build a User object from JWT claims */
function userFromToken(token: string): User {
  const claims = decodeJWT(token)
  return {
    id: String(claims['sub'] ?? ''),
    username: String(claims['name'] ?? claims['email'] ?? ''),
    email: String(claims['email'] ?? ''),
    role: 'user', // role is determined via connection-level isAdmin, not JWT claims
  }
}

export const authApi = {
  login: async (payload: LoginPayload): Promise<{ tokens: AuthTokensResult; user: User }> => {
    const { data } = await apiClient.post<ApiResponse<AuthTokensResult>>('/api/auth/login', payload)
    if (!data.success) throw new Error(data.message ?? 'Login failed')
    const tokens = data.data
    const user = userFromToken(tokens.accessToken)
    return { tokens, user }
  },

  register: async (payload: RegisterPayload): Promise<void> => {
    const { data } = await apiClient.post<ApiResponse<{ userId: string }>>('/api/auth/register', payload)
    if (!data.success) throw new Error(data.message ?? 'Registration failed')
  },

  logout: async (refreshToken: string): Promise<void> => {
    await apiClient.post('/api/auth/logout', { refreshToken }).catch(() => {})
  },
}
