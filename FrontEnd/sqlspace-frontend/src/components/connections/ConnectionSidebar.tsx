import { useEffect } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { motion, AnimatePresence } from 'framer-motion'
import { Plus, ChevronRight, Wifi, WifiOff, Loader2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip'
import { Skeleton } from '@/components/ui/skeleton'
import { connectionsApi } from '@/api/connections'
import { useConnectionStore } from '@/stores/connection-store'
import { cn } from '@/lib/utils'
import type { Connection } from '@/types'

const PROVIDER_COLORS: Record<string, string> = {
  PostgreSql: 'bg-blue-500',
  MySql: 'bg-orange-500',
  SqlServer: 'bg-red-500',
}

function ConnectionItem({
  conn,
  active,
  onClick,
}: {
  conn: Connection
  active: boolean
  onClick: () => void
}) {
  return (
    <motion.button
      layout
      initial={{ opacity: 0, x: -8 }}
      animate={{ opacity: 1, x: 0 }}
      onClick={onClick}
      className={cn(
        'w-full flex items-center gap-2.5 px-3 py-2 rounded-lg text-left transition-colors group',
        active
          ? 'bg-sky-500/10 border border-sky-500/30'
          : 'hover:bg-white/5 border border-transparent'
      )}
    >
      <span className={cn('w-2 h-2 rounded-full shrink-0', PROVIDER_COLORS[conn.databaseProvider] ?? 'bg-zinc-500')} />

      <div className="flex-1 min-w-0">
        <p className={cn('text-sm truncate', active ? 'text-sky-300' : 'text-zinc-200')}>{conn.connectionName}</p>
        <p className="text-xs text-zinc-600">{conn.databaseProvider}</p>
      </div>

      <Tooltip>
        <TooltipTrigger asChild>
          <span>
            {conn.isHealthy ? (
              <Wifi className="w-3.5 h-3.5 text-green-400 opacity-0 group-hover:opacity-100 transition-opacity" />
            ) : (
              <WifiOff className="w-3.5 h-3.5 text-red-400" />
            )}
          </span>
        </TooltipTrigger>
        <TooltipContent>{conn.isHealthy ? 'Healthy' : 'Unhealthy'}</TooltipContent>
      </Tooltip>

      {active && <ChevronRight className="w-3 h-3 text-sky-400 shrink-0" />}
    </motion.button>
  )
}

export function ConnectionSidebar() {
  const navigate = useNavigate()
  const { connections, activeConnectionId, setConnections, setActiveConnection } = useConnectionStore()

  const { data, isLoading } = useQuery({
    queryKey: ['connections'],
    queryFn: connectionsApi.list,
  })

  useEffect(() => {
    if (data) {
      setConnections(data)
      if (!activeConnectionId && data.length > 0) {
        setActiveConnection(data[0].connectionId)
      }
    }
  }, [data, activeConnectionId, setConnections, setActiveConnection])

  return (
    <>
      <aside className="flex flex-col h-full w-full bg-[#111113]">
        <div className="flex items-center justify-between px-3 py-3 border-b border-white/10">
          <span className="text-xs font-medium text-zinc-500 uppercase tracking-wider">Connections</span>
          <Tooltip>
            <TooltipTrigger asChild>
              <Button
                variant="ghost"
                size="icon"
                className="w-6 h-6 text-zinc-500 hover:text-sky-400 hover:bg-sky-500/10"
                onClick={() => navigate('/connections/new')}
              >
                <Plus className="w-3.5 h-3.5" />
              </Button>
            </TooltipTrigger>
            <TooltipContent>New connection</TooltipContent>
          </Tooltip>
        </div>

        <div className="flex-1 min-h-0 overflow-y-auto px-2 py-2">
          {isLoading ? (
            <div className="space-y-2">
              {[1, 2, 3].map((i) => (
                <Skeleton key={i} className="h-10 w-full rounded-lg bg-white/5" />
              ))}
            </div>
          ) : connections.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-8 text-center">
              <Wifi className="w-8 h-8 text-zinc-600 mb-2" />
              <p className="text-xs text-zinc-600">No connections yet</p>
              <Button variant="link" size="sm" className="text-sky-400 text-xs mt-1 h-auto p-0" onClick={() => navigate('/connections/new')}>
                Add one
              </Button>
            </div>
          ) : (
            <AnimatePresence>
              {connections.map((conn) => (
                <ConnectionItem
                  key={conn.connectionId}
                  conn={conn}
                  active={conn.connectionId === activeConnectionId}
                  onClick={() => setActiveConnection(conn.connectionId)}
                />
              ))}
            </AnimatePresence>
          )}
        </div>

        <div className="px-3 py-2 border-t border-white/10">
          {isLoading ? (
            <div className="flex items-center gap-1.5 text-xs text-zinc-600">
              <Loader2 className="w-3 h-3 animate-spin" />
              <span>Loading...</span>
            </div>
          ) : (
            <p className="text-xs text-zinc-600">
              {connections.length} connection{connections.length !== 1 ? 's' : ''}
            </p>
          )}
        </div>
      </aside>
    </>
  )
}
