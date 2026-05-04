import { useEffect, useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useSearchParams } from 'react-router-dom'
import { ClipboardList, ChevronDown, ChevronLeft, ChevronRight } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import { connectionsApi } from '@/api/connections'
import { accessApi } from '@/api/insights'
import { formatDate } from '@/lib/utils'
import type { AuditLogDto, AccessAuditLogAction } from '@/types'

const ACTION_LABELS: Record<AccessAuditLogAction, { label: string; className: string }> = {
  Granted_Access: {
    label: 'Access Granted',
    className: 'bg-green-500/10 text-green-400 border-green-500/20',
  },
  Revoked_Access: {
    label: 'Access Revoked',
    className: 'bg-red-500/10 text-red-400 border-red-500/20',
  },
  PermissionsUpdated: {
    label: 'Permissions Updated',
    className: 'bg-amber-500/10 text-amber-400 border-amber-500/20',
  },
  OwnershipTransferred: {
    label: 'Ownership Transferred',
    className: 'bg-sky-500/10 text-sky-400 border-sky-500/20',
  },
}

function safeParseDetails(details?: string | null): unknown | null {
  if (!details) return null
  try {
    return JSON.parse(details)
  } catch {
    return null
  }
}

function normalizeTables(value: unknown): string[] {
  if (!Array.isArray(value)) return []
  return value
    .filter((item) => typeof item === 'string' && item.trim().length > 0)
    .map((item) => (item as string).trim())
}

function formatAccessSummary(hasFullAccess?: boolean, restrictedTables?: unknown) {
  if (hasFullAccess) return 'Full access'
  const tables = normalizeTables(restrictedTables)
  if (tables.length === 0) return 'Restricted access'
  const preview = tables.slice(0, 3).join(', ')
  const suffix = tables.length > 3 ? ` +${tables.length - 3} more` : ''
  return `Restricted access (${tables.length}): ${preview}${suffix}`
}

function renderDetails(log: AuditLogDto) {
  const rawDetails = log.details?.trim() ?? ''
  const parsed = safeParseDetails(rawDetails)

  if (rawDetails && !parsed) {
    return <span className="text-xs text-zinc-400 whitespace-pre-wrap">{rawDetails}</span>
  }

  switch (log.action) {
    case 'Granted_Access': {
      if (parsed && typeof parsed === 'object' && parsed !== null) {
        const details = parsed as { hasFullAccess?: boolean; restrictedTables?: unknown }
        return (
          <span className="text-xs text-zinc-300">
            {formatAccessSummary(details.hasFullAccess, details.restrictedTables)}
          </span>
        )
      }
      return <span className="text-xs text-zinc-400">Access granted.</span>
    }
    case 'PermissionsUpdated': {
      if (parsed && typeof parsed === 'object' && parsed !== null) {
        const details = parsed as {
          previous?: { hasFullAccess?: boolean; restrictedTables?: unknown }
          current?: { hasFullAccess?: boolean; restrictedTables?: unknown }
        }
        return (
          <div className="space-y-1 text-xs text-zinc-400">
            <div>
              <span className="text-zinc-500">Previous:</span>{' '}
              {formatAccessSummary(details.previous?.hasFullAccess, details.previous?.restrictedTables)}
            </div>
            <div>
              <span className="text-zinc-500">Current:</span>{' '}
              {formatAccessSummary(details.current?.hasFullAccess, details.current?.restrictedTables)}
            </div>
          </div>
        )
      }
      return <span className="text-xs text-zinc-400">Permissions updated.</span>
    }
    case 'Revoked_Access':
      return <span className="text-xs text-zinc-400">Access revoked.</span>
    case 'OwnershipTransferred':
      return (
        <span className="text-xs text-zinc-400">
          Ownership moved from {log.actorUserName || log.actorUserEmail || 'previous owner'} to{' '}
          {log.targetUserName || log.targetUserEmail || 'new owner'}.
        </span>
      )
    default:
      return <span className="text-xs text-zinc-400">No details.</span>
  }
}

export default function AuditLogsPage() {
  const pageSize = 20
  const [searchParams, setSearchParams] = useSearchParams()
  const [selectedConnectionId, setSelectedConnectionId] = useState<string>('')
  const [pageNumber, setPageNumber] = useState(1)

  const { data: connections = [], isLoading: connectionsLoading } = useQuery({
    queryKey: ['connections'],
    queryFn: connectionsApi.list,
  })

  const adminConnections = useMemo(
    () => connections.filter((connection) => connection.isAdmin),
    [connections]
  )

  useEffect(() => {
    if (adminConnections.length === 0) return

    const paramId = searchParams.get('connectionId') ?? ''
    const paramMatch = adminConnections.find((c) => c.connectionId === paramId)
    const nextId = paramMatch?.connectionId ?? adminConnections[0]?.connectionId ?? ''

    if (!nextId) return
    if (selectedConnectionId && selectedConnectionId === nextId) return

    setSelectedConnectionId(nextId)
    if (paramId !== nextId) {
      setSearchParams({ connectionId: nextId })
    }
  }, [adminConnections, searchParams, selectedConnectionId, setSearchParams])

  useEffect(() => {
    setPageNumber(1)
  }, [selectedConnectionId])

  const { data: auditLogs, isLoading: logsLoading } = useQuery({
    queryKey: ['audit-logs', selectedConnectionId, pageNumber],
    queryFn: () => accessApi.getAuditLogs(selectedConnectionId, { pageNumber, pageSize }),
    enabled: !!selectedConnectionId,
  })

  const items = auditLogs?.items ?? []
  const totalCount = auditLogs?.totalCount ?? 0
  const totalPages = auditLogs?.totalPages ?? 1
  const hasPreviousPage = pageNumber > 1
  const hasNextPage = pageNumber < totalPages
  const startEntry = totalCount === 0 ? 0 : (pageNumber - 1) * pageSize + 1
  const endEntry = Math.min(pageNumber * pageSize, totalCount)
  const lastEventAt = items[0]?.performedAt

  const selectedConnection = connections.find((c) => c.connectionId === selectedConnectionId)

  return (
    <div className="flex flex-col h-full">
      <div className="flex items-center gap-4 px-6 py-4 border-b border-white/10 bg-[#111113] shrink-0 flex-wrap">
        <div className="flex items-center gap-2">
          <ClipboardList className="w-5 h-5 text-sky-400" />
          <h1 className="text-lg font-semibold text-white">Audit Logs</h1>
        </div>

        <div className="flex items-center gap-2 ml-2">
          <span className="text-xs text-zinc-600">Connection:</span>
          {connectionsLoading ? (
            <Skeleton className="h-8 w-40 rounded-lg bg-white/5" />
          ) : adminConnections.length === 0 ? (
            <span className="text-xs text-zinc-600">No admin connections</span>
          ) : (
            <div className="relative">
              <select
                value={selectedConnectionId}
                onChange={(e) => {
                  setSelectedConnectionId(e.target.value)
                  setSearchParams({ connectionId: e.target.value })
                }}
                className="appearance-none pl-3 pr-8 py-1.5 rounded-lg border border-white/10 bg-[#18181b] text-zinc-200 text-sm cursor-pointer focus:outline-none focus:border-sky-500 transition-colors"
              >
                {adminConnections.map((c) => (
                  <option key={c.connectionId} value={c.connectionId}>
                    {c.connectionName}
                  </option>
                ))}
              </select>
              <ChevronDown className="absolute right-2 top-1/2 -translate-y-1/2 w-3.5 h-3.5 text-zinc-600 pointer-events-none" />
            </div>
          )}
        </div>
      </div>

      <div className="flex-1 min-h-0 overflow-y-auto bg-[#080809]">
        {!selectedConnectionId ? (
          <div className="flex flex-col items-center justify-center h-full text-center gap-2">
            <ClipboardList className="w-10 h-10 text-zinc-600" />
            <p className="text-sm text-zinc-300 font-medium">No admin connections</p>
            <p className="text-xs text-zinc-600">
              You need to be the owner of a connection to view its audit logs.
            </p>
          </div>
        ) : (
          <div className="p-6 space-y-4">
            <div className="flex items-center gap-3 flex-wrap">
              {selectedConnection && (
                <div className="flex items-center gap-2 px-3 py-2 rounded-lg bg-[#111113] border border-white/10">
                  <div className="w-2 h-2 rounded-full bg-sky-500 shadow-[0_0_6px_rgba(14,165,233,0.5)]" />
                  <span className="text-sm text-zinc-300">{selectedConnection.connectionName}</span>
                </div>
              )}
              <div className="px-3 py-2 rounded-lg bg-[#111113] border border-white/10">
                <span className="text-sm text-zinc-500">
                  <span className="text-zinc-200 font-medium">{totalCount}</span> total log entries
                </span>
              </div>
              <div className="px-3 py-2 rounded-lg bg-[#111113] border border-white/10">
                <span className="text-sm text-zinc-500">
                  Last event:{' '}
                  <span className="text-zinc-200 font-medium">
                    {lastEventAt ? formatDate(lastEventAt) : 'n/a'}
                  </span>
                </span>
              </div>
            </div>

            <div className="bg-[#111113] border border-white/10 rounded-2xl overflow-hidden shadow-xl">
              <div className="overflow-x-auto">
                <table className="w-full text-left text-sm">
                  <thead className="bg-[#18181b] border-b border-white/10 text-zinc-400 uppercase text-xs font-semibold tracking-wider">
                    <tr>
                      <th className="px-6 py-4">Actor</th>
                      <th className="px-6 py-4">Target</th>
                      <th className="px-6 py-4">Action</th>
                      <th className="px-6 py-4">Timestamp</th>
                      <th className="px-6 py-4">Details</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-white/5">
                    {logsLoading ? (
                      Array.from({ length: pageSize }).map((_, idx) => (
                        <tr key={idx}>
                          <td className="px-6 py-4 text-zinc-500" colSpan={5}>
                            Loading...
                          </td>
                        </tr>
                      ))
                    ) : items.length === 0 ? (
                      <tr>
                        <td className="px-6 py-8 text-zinc-500 text-center" colSpan={5}>
                          No audit logs found for this connection.
                        </td>
                      </tr>
                    ) : (
                      items.map((log) => {
                        const actorLabel = log.actorUserName || log.actorUserEmail || 'Unknown'
                        const targetLabel = log.targetUserName || log.targetUserEmail || 'Unknown'
                        const meta = ACTION_LABELS[log.action]

                        return (
                          <tr key={log.auditLogId} className="hover:bg-white/5 transition-colors">
                            <td className="px-6 py-4">
                              <div className="text-sm text-zinc-200 truncate max-w-xs">{actorLabel}</div>
                              <div className="text-xs text-zinc-600 truncate max-w-xs">{log.actorUserEmail}</div>
                            </td>
                            <td className="px-6 py-4">
                              <div className="text-sm text-zinc-200 truncate max-w-xs">{targetLabel}</div>
                              <div className="text-xs text-zinc-600 truncate max-w-xs">{log.targetUserEmail}</div>
                            </td>
                            <td className="px-6 py-4">
                              <Badge variant="outline" className={meta?.className ?? ''}>
                                {meta?.label ?? log.action}
                              </Badge>
                            </td>
                            <td className="px-6 py-4 text-zinc-300 text-xs whitespace-nowrap">
                              {formatDate(log.performedAt)}
                            </td>
                            <td className="px-6 py-4 max-w-md">{renderDetails(log)}</td>
                          </tr>
                        )
                      })
                    )}
                  </tbody>
                </table>
              </div>

              <div className="px-6 py-4 border-t border-white/10 bg-[#18181b] flex items-center justify-between">
                <span className="text-sm text-zinc-500">
                  Showing {startEntry} to {endEntry} of {totalCount.toLocaleString()} entries
                </span>
                <div className="flex gap-2">
                  <button
                    className="px-3 py-1.5 rounded-lg border border-white/10 text-zinc-300 hover:text-white hover:bg-white/5 transition-colors flex items-center gap-1 text-sm disabled:text-zinc-500 disabled:cursor-not-allowed disabled:bg-black/20"
                    disabled={!hasPreviousPage}
                    onClick={() => setPageNumber((current) => Math.max(1, current - 1))}
                  >
                    <ChevronLeft className="w-4 h-4" /> Prev
                  </button>
                  <button
                    className="px-3 py-1.5 rounded-lg border border-white/10 text-zinc-300 hover:text-white hover:bg-white/5 transition-colors flex items-center gap-1 text-sm disabled:text-zinc-500 disabled:cursor-not-allowed disabled:bg-black/20"
                    disabled={!hasNextPage}
                    onClick={() => setPageNumber((current) => current + 1)}
                  >
                    Next <ChevronRight className="w-4 h-4" />
                  </button>
                </div>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
