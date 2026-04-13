import { create } from 'zustand'
import { knowledgeBaseApi } from '@/api/knowledge-base'
import type { ChatMessage } from '@/types'

interface ConnectionChatState {
  messages: ChatMessage[]
  isLoading: boolean
  isSending: boolean
  error: string | null
}

interface RagChatState {
  byConnection: Record<string, ConnectionChatState>
  loadHistory: (connectionId: string) => Promise<void>
  sendMessage: (connectionId: string, query: string) => Promise<void>
  reset: (connectionId: string) => void
}

const emptyState: ConnectionChatState = {
  messages: [],
  isLoading: false,
  isSending: false,
  error: null,
}

const roleOrder: Record<ChatMessage['role'], number> = {
  user: 0,
  assistant: 1,
}

function sortChatMessages(messages: ChatMessage[]): ChatMessage[] {
  return [...messages].sort((a, b) => {
    const aTs = Date.parse(a.createdAt)
    const bTs = Date.parse(b.createdAt)

    if (!Number.isNaN(aTs) && !Number.isNaN(bTs) && aTs !== bTs) {
      return aTs - bTs
    }

    const roleDelta = roleOrder[a.role] - roleOrder[b.role]
    if (roleDelta !== 0) return roleDelta

    return a.messageId.localeCompare(b.messageId)
  })
}

export const useRagChatStore = create<RagChatState>()((set, get) => ({
  byConnection: {},

  loadHistory: async (connectionId) => {
    set((s) => ({
      byConnection: {
        ...s.byConnection,
        [connectionId]: {
          ...(s.byConnection[connectionId] ?? emptyState),
          isLoading: true,
          error: null,
        },
      },
    }))

    try {
      const messages = sortChatMessages(await knowledgeBaseApi.getChatHistory(connectionId))
      set((s) => ({
        byConnection: {
          ...s.byConnection,
          [connectionId]: {
            ...(s.byConnection[connectionId] ?? emptyState),
            messages,
            isLoading: false,
            error: null,
          },
        },
      }))
    } catch (err) {
      set((s) => ({
        byConnection: {
          ...s.byConnection,
          [connectionId]: {
            ...(s.byConnection[connectionId] ?? emptyState),
            isLoading: false,
            error: err instanceof Error ? err.message : 'Failed to load chat history',
          },
        },
      }))
    }
  },

  sendMessage: async (connectionId, query) => {
    const trimmed = query.trim()
    if (!trimmed) return

    const optimisticUser: ChatMessage = {
      messageId: `optimistic-${Date.now()}`,
      role: 'user',
      content: trimmed,
      createdAt: new Date().toISOString(),
    }

    set((s) => {
      const current = s.byConnection[connectionId] ?? emptyState
      return {
        byConnection: {
          ...s.byConnection,
          [connectionId]: {
            ...current,
            messages: sortChatMessages([...current.messages, optimisticUser]),
            isSending: true,
            error: null,
          },
        },
      }
    })

    try {
      await knowledgeBaseApi.ask(connectionId, trimmed)
      // re-fetch to get server-truth ids, ordering, and sources
      const messages = sortChatMessages(await knowledgeBaseApi.getChatHistory(connectionId))
      set((s) => ({
        byConnection: {
          ...s.byConnection,
          [connectionId]: {
            ...(s.byConnection[connectionId] ?? emptyState),
            messages,
            isSending: false,
            error: null,
          },
        },
      }))
    } catch (err) {
      // keep the optimistic user message; surface the error
      set((s) => ({
        byConnection: {
          ...s.byConnection,
          [connectionId]: {
            ...(s.byConnection[connectionId] ?? emptyState),
            isSending: false,
            error: err instanceof Error ? err.message : 'Failed to send message',
          },
        },
      }))
      // best-effort reconcile in the background
      void get().loadHistory(connectionId)
    }
  },

  reset: (connectionId) =>
    set((s) => ({
      byConnection: { ...s.byConnection, [connectionId]: { ...emptyState } },
    })),
}))

export const selectConnectionChat = (connectionId: string) => (s: RagChatState) =>
  s.byConnection[connectionId] ?? emptyState
