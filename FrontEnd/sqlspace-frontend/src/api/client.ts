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

// On 401, clear auth and redirect to login
apiClient.interceptors.response.use(
  (response) => response,
  (error: AxiosError) => {
    if (error.response?.status === 401) {
      useAuthStore.getState().logout()
      window.location.href = '/login'
    }
    return Promise.reject(error)
  }
)
