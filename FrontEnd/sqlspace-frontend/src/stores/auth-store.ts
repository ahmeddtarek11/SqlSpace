import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import type { User, AuthTokensResult } from '@/types'

interface AuthState {
  user: User | null
  tokens: AuthTokensResult | null
  isAuthenticated: boolean
  setAuth: (user: User, tokens: AuthTokensResult) => void
  logout: () => void
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      user: null,
      tokens: null,
      isAuthenticated: false,

      setAuth: (user, tokens) =>
        set({ user, tokens, isAuthenticated: true }),

      logout: () =>
        set({ user: null, tokens: null, isAuthenticated: false }),
    }),
    {
      name: 'sqlspace-auth',
      partialize: (state) => ({
        user: state.user,
        tokens: state.tokens,
        isAuthenticated: state.isAuthenticated,
      }),
    }
  )
)
