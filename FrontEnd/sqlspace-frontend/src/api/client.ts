import axios, { type AxiosError, type InternalAxiosRequestConfig } from 'axios'
import { useAuthStore } from '@/stores/auth-store'

const BASE_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5131'

export const apiClient = axios.create({
  baseURL: BASE_URL,
  headers: { 'Content-Type': 'application/json' },
  timeout: 30_000,
})

// Attach JWT access token to every request
apiClient.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const tokens = useAuthStore.getState().tokens
  if (tokens?.accessToken) {
    config.headers.Authorization = `Bearer ${tokens.accessToken}`
  }
  return config
})

// On 401 clear auth; for all errors extract a meaningful message from the response body
apiClient.interceptors.response.use(
  (response) => response,
  (error: AxiosError<{ message?: string | null; errors?: { code: string; message: string }[] | null }>) => {
    if (error.response?.status === 401) {
      useAuthStore.getState().logout()
      window.location.href = '/login'
    }

    const body = error.response?.data
    const message =
      body?.errors?.[0]?.message ||
      body?.message ||
      error.message ||
      'An unexpected error occurred'

    return Promise.reject(new Error(message))
  }
)
