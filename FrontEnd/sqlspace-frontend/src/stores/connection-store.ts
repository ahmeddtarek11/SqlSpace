import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import type { Connection } from '@/types'

interface ConnectionState {
  connections: Connection[]
  activeConnectionId: string | null
  setConnections: (connections: Connection[]) => void
  setActiveConnection: (id: string | null) => void
  upsertConnection: (conn: Connection) => void
  removeConnection: (id: string) => void
  activeConnection: () => Connection | null
}

export const useConnectionStore = create<ConnectionState>()(
  persist(
    (set, get) => ({
      connections: [],
      activeConnectionId: null,

      setConnections: (connections) => set({ connections }),

      setActiveConnection: (id) => set({ activeConnectionId: id }),

      upsertConnection: (conn) =>
        set((state) => {
          const exists = state.connections.find((c) => c.connectionId === conn.connectionId)
          if (exists) {
            return { connections: state.connections.map((c) => (c.connectionId === conn.connectionId ? conn : c)) }
          }
          return { connections: [...state.connections, conn] }
        }),

      removeConnection: (id) =>
        set((state) => ({
          connections: state.connections.filter((c) => c.connectionId !== id),
          activeConnectionId: state.activeConnectionId === id ? null : state.activeConnectionId,
        })),

      activeConnection: () => {
        const { connections, activeConnectionId } = get()
        return connections.find((c) => c.connectionId === activeConnectionId) ?? null
      },
    }),
    {
      name: 'sqlspace-connections',
      partialize: (state) => ({
        connections: state.connections,
        activeConnectionId: state.activeConnectionId,
      }),
    }
  )
)
