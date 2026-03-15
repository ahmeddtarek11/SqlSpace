import { create } from 'zustand'
import type { QueryResult } from '@/types'

interface WorkspaceState {
  prompt: string
  generatedSQL: string
  explanation: string
  result: QueryResult | null
  isExecuting: boolean
  error: string | null
  setPrompt: (prompt: string) => void
  setGeneratedSQL: (sql: string) => void
  setExplanation: (explanation: string) => void
  setResult: (result: QueryResult | null) => void
  setExecuting: (executing: boolean) => void
  setError: (error: string | null) => void
  reset: () => void
}

export const useWorkspaceStore = create<WorkspaceState>()((set) => ({
  prompt: '',
  generatedSQL: '',
  explanation: '',
  result: null,
  isExecuting: false,
  error: null,

  setPrompt: (prompt) => set({ prompt }),
  setGeneratedSQL: (generatedSQL) => set({ generatedSQL }),
  setExplanation: (explanation) => set({ explanation }),
  setResult: (result) => set({ result }),
  setExecuting: (isExecuting) => set({ isExecuting }),
  setError: (error) => set({ error }),
  reset: () =>
    set({ prompt: '', generatedSQL: '', explanation: '', result: null, error: null }),
}))
