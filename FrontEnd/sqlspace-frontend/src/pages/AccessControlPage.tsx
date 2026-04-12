import { useState, useMemo } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import {
  Shield,
  Plus,
  Trash2,
  Pencil,
  ChevronDown,
  CheckSquare,
  Square,
  UserCheck,
  Unlock,
  Lock,
  Loader2,
} from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { connectionsApi } from '@/api/connections'
import { accessApi } from '@/api/insights'
import { formatDate, cn } from '@/lib/utils'
import type { UserAccessSummary, TableRestrictionInput, SchemaTable } from '@/types'

// ── Table Picker ──────────────────────────────────────────────

interface TablePickerProps {
  allTables: SchemaTable[]
  allowedTables: Set<string>
  onChange: (next: Set<string>) => void
}

function tableKey(t: SchemaTable) {
  return `${t.schema}.${t.name}`
}

function TablePicker({ allTables, allowedTables, onChange }: TablePickerProps) {
  const grouped = useMemo(() => {
    return allTables.reduce<Record<string, SchemaTable[]>>((acc, t) => {
      const key = t.schema || 'default'
      if (!acc[key]) acc[key] = []
      acc[key].push(t)
      return acc
    }, {})
  }, [allTables])

  const toggleTable = (t: SchemaTable) => {
    const key = tableKey(t)
    const next = new Set(allowedTables)
    if (next.has(key)) next.delete(key)
    else next.add(key)
    onChange(next)
  }

  const toggleAll = (select: boolean) => {
    if (select) onChange(new Set(allTables.map(tableKey)))
    else onChange(new Set())
  }

  const allSelected = allTables.length > 0 && allowedTables.size === allTables.length
  const noneSelected = allowedTables.size === 0

  return (
    <div className="space-y-2">
      <div className="flex items-center gap-2">
        <button
          type="button"
          onClick={() => toggleAll(!allSelected)}
          className="text-xs text-sky-400 hover:text-sky-300 underline-offset-2 hover:underline transition-colors"
        >
          {allSelected ? 'Deselect all' : 'Select all'}
        </button>
        <span className="text-xs text-zinc-600">
          {allowedTables.size} / {allTables.length} tables allowed
        </span>
      </div>

      <div className="max-h-56 overflow-y-auto rounded-xl border border-white/10 divide-y divide-white/5">
        {allTables.length === 0 ? (
          <p className="text-xs text-zinc-600 text-center py-4">No schema loaded</p>
        ) : (
          Object.entries(grouped).map(([schemaName, tables]) => (
            <div key={schemaName}>
              <div className="px-3 py-1.5 bg-[#18181b] sticky top-0">
                <span className="text-[10px] font-medium text-zinc-500 uppercase tracking-wider">
                  {schemaName}
                </span>
              </div>
              {tables.map((t) => {
                const key = tableKey(t)
                const checked = allowedTables.has(key)
                return (
                  <button
                    key={key}
                    type="button"
                    onClick={() => toggleTable(t)}
                    className={cn(
                      'w-full flex items-center gap-2.5 px-3 py-2 text-left transition-colors',
                      checked
                        ? 'bg-[#111113] hover:bg-white/5'
                        : 'bg-red-500/5 hover:bg-red-500/10'
                    )}
                  >
                    {checked ? (
                      <CheckSquare className="w-3.5 h-3.5 text-sky-400 shrink-0" />
                    ) : (
                      <Square className="w-3.5 h-3.5 text-zinc-600 shrink-0" />
                    )}
                    <span className={cn('text-xs font-mono', checked ? 'text-zinc-200' : 'text-zinc-600 line-through')}>
                      {t.name}
                    </span>
                    {!checked && (
                      <span className="ml-auto text-[10px] text-red-400">blocked</span>
                    )}
                  </button>
                )
              })}
            </div>
          ))
        )}
      </div>

      {noneSelected && (
        <p className="text-xs text-amber-400">
          Warning: no tables allowed — user will have no access.
        </p>
      )}
    </div>
  )
}

// ── Grant Dialog ──────────────────────────────────────────────

interface GrantDialogProps {
  connectionId: string
  allTables: SchemaTable[]
  onClose: () => void
}

function GrantDialog({ connectionId, allTables, onClose }: GrantDialogProps) {
  const qc = useQueryClient()
  const [email, setEmail] = useState('')
  const [hasFullAccess, setHasFullAccess] = useState(true)
  const [allowedTables, setAllowedTables] = useState<Set<string>>(
    () => new Set(allTables.map(tableKey))
  )

  const mutation = useMutation({
    mutationFn: () => {
      const restrictedTables: TableRestrictionInput[] = hasFullAccess
        ? []
        : allTables
            .filter((t) => !allowedTables.has(tableKey(t)))
            .map((t) => ({ table: t.name, schema: t.schema }))

      return accessApi.grant(connectionId, {
        targetUserEmail: email,
        hasFullAccess,
        restrictedTables: hasFullAccess ? [] : restrictedTables,
      })
    },
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['access-users', connectionId] })
      toast.success('Access granted')
      onClose()
    },
    onError: (err: Error) => toast.error(err.message || 'Failed to grant access'),
  })

  const canSubmit = email.includes('@') && !mutation.isPending

  return (
    <Dialog open onOpenChange={onClose}>
      <DialogContent className="sm:max-w-md bg-[#18181b] border-white/10 text-white">
        <DialogHeader>
          <DialogTitle className="text-white flex items-center gap-2">
            <UserCheck className="w-4 h-4 text-sky-400" />
            Grant Access
          </DialogTitle>
        </DialogHeader>

        <div className="space-y-4 mt-2">
          <div className="space-y-1.5">
            <Label className="text-zinc-400 text-sm">User Email</Label>
            <Input
              type="email"
              placeholder="user@example.com"
              className="bg-[#111113] border-white/10 text-white placeholder:text-zinc-600 focus:border-sky-500 focus:ring-1 focus:ring-sky-500"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
            />
          </div>

          <div className="space-y-2">
            <Label className="text-zinc-400 text-sm">Access Level</Label>
            <div className="flex items-center gap-1 p-1 rounded-lg bg-[#111113] border border-white/10 w-fit">
              <button
                type="button"
                onClick={() => setHasFullAccess(true)}
                className={cn(
                  'flex items-center gap-1.5 px-3 py-1.5 rounded-md text-sm font-medium transition-colors',
                  hasFullAccess ? 'bg-sky-600 text-white' : 'text-zinc-500 hover:text-zinc-300'
                )}
              >
                <Unlock className="w-3.5 h-3.5" />
                Full Access
              </button>
              <button
                type="button"
                onClick={() => setHasFullAccess(false)}
                className={cn(
                  'flex items-center gap-1.5 px-3 py-1.5 rounded-md text-sm font-medium transition-colors',
                  !hasFullAccess ? 'bg-sky-600 text-white' : 'text-zinc-500 hover:text-zinc-300'
                )}
              >
                <Lock className="w-3.5 h-3.5" />
                Restricted
              </button>
            </div>
          </div>

          {!hasFullAccess && (
            <div className="space-y-1.5">
              <Label className="text-zinc-400 text-sm">Allowed Tables</Label>
              <TablePicker
                allTables={allTables}
                allowedTables={allowedTables}
                onChange={setAllowedTables}
              />
            </div>
          )}

          <div className="flex gap-2 pt-1">
            <Button variant="ghost" className="flex-1 text-zinc-600 hover:text-zinc-300 hover:bg-white/5" onClick={onClose}>
              Cancel
            </Button>
            <Button
              className="flex-1 bg-sky-600 hover:bg-sky-500 shadow-lg shadow-sky-500/25 active:scale-[0.98] transition-all"
              disabled={!canSubmit}
              onClick={() => mutation.mutate()}
            >
              {mutation.isPending ? <Loader2 className="w-4 h-4 animate-spin" /> : 'Grant Access'}
            </Button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  )
}

// ── Edit Dialog ───────────────────────────────────────────────

interface EditDialogProps {
  connectionId: string
  user: UserAccessSummary
  allTables: SchemaTable[]
  onClose: () => void
}

function EditDialog({ connectionId, user, allTables, onClose }: EditDialogProps) {
  const qc = useQueryClient()
  const [hasFullAccess, setHasFullAccess] = useState(user.hasFullAccess)

  const [allowedTables, setAllowedTables] = useState<Set<string>>(() => {
    if (user.hasFullAccess) return new Set(allTables.map(tableKey))
    return new Set(
      allTables
        .filter((t) => !user.restrictedTables.includes(t.name))
        .map(tableKey)
    )
  })

  const mutation = useMutation({
    mutationFn: () => {
      const restrictedTables: TableRestrictionInput[] = hasFullAccess
        ? []
        : allTables
            .filter((t) => !allowedTables.has(tableKey(t)))
            .map((t) => ({ table: t.name, schema: t.schema }))

      return accessApi.updateRestrictions(connectionId, user.userId, {
        hasFullAccess,
        restrictedTables,
      })
    },
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['access-users', connectionId] })
      toast.success('Access updated')
      onClose()
    },
    onError: (err: Error) => toast.error(err.message || 'Failed to update access'),
  })

  return (
    <Dialog open onOpenChange={onClose}>
      <DialogContent className="sm:max-w-md bg-[#18181b] border-white/10 text-white">
        <DialogHeader>
          <DialogTitle className="text-white flex items-center gap-2">
            <Pencil className="w-4 h-4 text-sky-400" />
            Edit Access
          </DialogTitle>
        </DialogHeader>

        <div className="space-y-4 mt-2">
          <div className="rounded-xl border border-white/10 bg-[#111113] px-4 py-3">
            <p className="text-sm text-zinc-200">{user.userEmail}</p>
            {user.userName && (
              <p className="text-xs text-zinc-600 mt-0.5">{user.userName}</p>
            )}
          </div>

          <div className="space-y-2">
            <Label className="text-zinc-400 text-sm">Access Level</Label>
            <div className="flex items-center gap-1 p-1 rounded-lg bg-[#111113] border border-white/10 w-fit">
              <button
                type="button"
                onClick={() => {
                  setHasFullAccess(true)
                  setAllowedTables(new Set(allTables.map(tableKey)))
                }}
                className={cn(
                  'flex items-center gap-1.5 px-3 py-1.5 rounded-md text-sm font-medium transition-colors',
                  hasFullAccess ? 'bg-sky-600 text-white' : 'text-zinc-500 hover:text-zinc-300'
                )}
              >
                <Unlock className="w-3.5 h-3.5" />
                Full Access
              </button>
              <button
                type="button"
                onClick={() => setHasFullAccess(false)}
                className={cn(
                  'flex items-center gap-1.5 px-3 py-1.5 rounded-md text-sm font-medium transition-colors',
                  !hasFullAccess ? 'bg-sky-600 text-white' : 'text-zinc-500 hover:text-zinc-300'
                )}
              >
                <Lock className="w-3.5 h-3.5" />
                Restricted
              </button>
            </div>
          </div>

          {!hasFullAccess && (
            <div className="space-y-1.5">
              <Label className="text-zinc-400 text-sm">Allowed Tables</Label>
              <TablePicker
                allTables={allTables}
                allowedTables={allowedTables}
                onChange={setAllowedTables}
              />
            </div>
          )}

          <div className="flex gap-2 pt-1">
            <Button variant="ghost" className="flex-1 text-zinc-600 hover:text-zinc-300 hover:bg-white/5" onClick={onClose}>
              Cancel
            </Button>
            <Button
              className="flex-1 bg-sky-600 hover:bg-sky-500 shadow-lg shadow-sky-500/25 active:scale-[0.98] transition-all"
              disabled={mutation.isPending}
              onClick={() => mutation.mutate()}
            >
              {mutation.isPending ? <Loader2 className="w-4 h-4 animate-spin" /> : 'Save Changes'}
            </Button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  )
}

// ── User Row ──────────────────────────────────────────────────

interface UserRowProps {
  user: UserAccessSummary
  totalTables: number
  onEdit: () => void
  onRevoke: () => void
  isRevoking: boolean
}

function UserRow({ user, totalTables, onEdit, onRevoke, isRevoking }: UserRowProps) {
  const allowedCount = user.hasFullAccess
    ? totalTables
    : totalTables - user.restrictedTables.length

  const initials = (user.userName || user.userEmail)
    .split(' ')
    .map((w) => w[0])
    .join('')
    .slice(0, 2)
    .toUpperCase()

  return (
    <div className="flex items-center gap-4 px-4 py-3 border-b border-white/5 last:border-0 hover:bg-white/5 transition-colors group">
      {/* Avatar */}
      <div className="w-8 h-8 rounded-full bg-sky-500/10 border border-sky-500/20 flex items-center justify-center shrink-0">
        <span className="text-xs font-semibold text-sky-300">{initials}</span>
      </div>

      {/* User info */}
      <div className="flex-1 min-w-0">
        <p className="text-sm text-zinc-200 truncate">{user.userEmail}</p>
        {user.userName && (
          <p className="text-xs text-zinc-600 truncate">{user.userName}</p>
        )}
      </div>

      {/* Access level badge */}
      {user.hasFullAccess ? (
        <Badge className="bg-green-500/10 text-green-400 border-green-500/20 text-xs shrink-0">
          <Unlock className="w-3 h-3 mr-1" />
          Full Access
        </Badge>
      ) : (
        <Badge className="bg-amber-500/10 text-amber-400 border-amber-500/20 text-xs shrink-0">
          <Lock className="w-3 h-3 mr-1" />
          {allowedCount} / {totalTables} tables
        </Badge>
      )}

      {/* Granted info */}
      <div className="text-right hidden md:block shrink-0">
        <p className="text-xs text-zinc-600">{formatDate(user.grantedAt)}</p>
        {user.grantedByUserEmail && (
          <p className="text-[10px] text-zinc-600 truncate max-w-32">by {user.grantedByUserEmail}</p>
        )}
      </div>

      {/* Actions */}
      <div className="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
        <Button
          variant="ghost"
          size="icon"
          className="w-7 h-7 text-zinc-600 hover:text-sky-400 hover:bg-sky-500/10"
          onClick={onEdit}
          title="Edit access"
        >
          <Pencil className="w-3.5 h-3.5" />
        </Button>
        <Button
          variant="ghost"
          size="icon"
          className="w-7 h-7 text-zinc-600 hover:text-red-400 hover:bg-red-500/10"
          onClick={onRevoke}
          disabled={isRevoking}
          title="Revoke access"
        >
          {isRevoking ? (
            <Loader2 className="w-3.5 h-3.5 animate-spin" />
          ) : (
            <Trash2 className="w-3.5 h-3.5" />
          )}
        </Button>
      </div>
    </div>
  )
}

// ── Main Page ─────────────────────────────────────────────────

export default function AccessControlPage() {
  const qc = useQueryClient()
  const [selectedConnectionId, setSelectedConnectionId] = useState<string>('')
  const [showGrant, setShowGrant] = useState(false)
  const [editingUser, setEditingUser] = useState<UserAccessSummary | null>(null)
  const [revokingId, setRevokingId] = useState<string | null>(null)

  const { data: connections = [], isLoading: connectionsLoading } = useQuery({
    queryKey: ['connections'],
    queryFn: connectionsApi.list,
  })

  const adminConnections = connections.filter((c) => c.isAdmin)

  const activeConnectionId =
    selectedConnectionId || adminConnections[0]?.connectionId || ''

  const { data: schema } = useQuery({
    queryKey: ['schema', activeConnectionId],
    queryFn: () => connectionsApi.schema(activeConnectionId),
    enabled: !!activeConnectionId,
  })
  const allTables = schema?.tables ?? []

  const { data: users = [], isLoading: usersLoading } = useQuery({
    queryKey: ['access-users', activeConnectionId],
    queryFn: () => accessApi.list(activeConnectionId),
    enabled: !!activeConnectionId,
  })

  const revokeMutation = useMutation({
    mutationFn: ({ userId }: { userId: string }) =>
      accessApi.revoke(activeConnectionId, userId),
    onMutate: ({ userId }) => setRevokingId(userId),
    onSettled: () => setRevokingId(null),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['access-users', activeConnectionId] })
      toast.success('Access revoked')
    },
    onError: () => toast.error('Failed to revoke access'),
  })

  const selectedConn = connections.find((c) => c.connectionId === activeConnectionId)

  return (
    <div className="flex flex-col h-full">
      {/* ── Header ── */}
      <div className="flex items-center gap-4 px-6 py-4 border-b border-white/10 bg-[#111113] shrink-0 flex-wrap">
        <div className="flex items-center gap-2">
          <Shield className="w-5 h-5 text-sky-400" />
          <h1 className="text-lg font-semibold text-white">Access Control</h1>
        </div>

        {/* Connection selector */}
        <div className="flex items-center gap-2 ml-2">
          <span className="text-xs text-zinc-600">Connection:</span>
          {connectionsLoading ? (
            <Skeleton className="h-8 w-40 rounded-lg bg-white/5" />
          ) : adminConnections.length === 0 ? (
            <span className="text-xs text-zinc-600">No admin connections</span>
          ) : (
            <div className="relative">
              <select
                value={activeConnectionId}
                onChange={(e) => setSelectedConnectionId(e.target.value)}
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

        <div className="ml-auto">
          <Button
            size="sm"
            className="bg-sky-600 hover:bg-sky-500 text-white shadow-lg shadow-sky-500/25 active:scale-[0.98] transition-all"
            disabled={!activeConnectionId}
            onClick={() => setShowGrant(true)}
          >
            <Plus className="w-4 h-4 mr-1" />
            Grant Access
          </Button>
        </div>
      </div>

      {/* ── Body ── */}
      <div className="flex-1 min-h-0 overflow-y-auto bg-[#080809]">
        {!activeConnectionId ? (
          <div className="flex flex-col items-center justify-center h-full text-center gap-2">
            <Shield className="w-10 h-10 text-zinc-600" />
            <p className="text-sm text-zinc-300 font-medium">No admin connections</p>
            <p className="text-xs text-zinc-600">You need to be the owner of a connection to manage its access control</p>
          </div>
        ) : (
          <div className="p-6 space-y-4">
            {/* Stats */}
            {selectedConn && (
              <div className="flex items-center gap-3 flex-wrap">
                <div className="flex items-center gap-2 px-3 py-2 rounded-lg bg-[#111113] border border-white/10">
                  <div className="w-2 h-2 rounded-full bg-sky-500 shadow-[0_0_6px_rgba(14,165,233,0.5)]" />
                  <span className="text-sm text-zinc-300">{selectedConn.connectionName}</span>
                </div>
                <div className="px-3 py-2 rounded-lg bg-[#111113] border border-white/10">
                  <span className="text-sm text-zinc-500">
                    <span className="text-zinc-200 font-medium">{users.length}</span>{' '}
                    user{users.length !== 1 ? 's' : ''} with access
                  </span>
                </div>
                <div className="px-3 py-2 rounded-lg bg-[#111113] border border-white/10">
                  <span className="text-sm text-zinc-500">
                    <span className="text-zinc-200 font-medium">{allTables.length}</span> tables in schema
                  </span>
                </div>
              </div>
            )}

            {/* User list */}
            <div className="bg-[#111113] border border-white/10 rounded-xl overflow-hidden">
              <div className="flex items-center gap-4 px-4 py-2.5 border-b border-white/10 bg-[#18181b]">
                <span className="w-8 shrink-0" />
                <span className="flex-1 text-xs font-medium text-zinc-500 uppercase tracking-wider">User</span>
                <span className="text-xs font-medium text-zinc-500 uppercase tracking-wider w-36">Access Level</span>
                <span className="text-xs font-medium text-zinc-500 uppercase tracking-wider hidden md:block w-28">Granted</span>
                <span className="w-16 shrink-0" />
              </div>

              {usersLoading ? (
                <div className="p-4 space-y-3">
                  {[1, 2, 3].map((i) => (
                    <Skeleton key={i} className="h-14 rounded-lg bg-white/5" />
                  ))}
                </div>
              ) : users.length === 0 ? (
                <div className="flex flex-col items-center justify-center py-12 gap-2">
                  <UserCheck className="w-8 h-8 text-zinc-600" />
                  <p className="text-sm text-zinc-600">No users have been granted access yet</p>
                  <Button
                    size="sm"
                    variant="outline"
                    className="mt-2 border-white/10 text-zinc-400 hover:text-zinc-200 hover:bg-white/5"
                    onClick={() => setShowGrant(true)}
                  >
                    <Plus className="w-3.5 h-3.5 mr-1" />
                    Grant first access
                  </Button>
                </div>
              ) : (
                users.map((u) => (
                  <UserRow
                    key={u.userId}
                    user={u}
                    totalTables={allTables.length}
                    onEdit={() => setEditingUser(u)}
                    onRevoke={() => revokeMutation.mutate({ userId: u.userId })}
                    isRevoking={revokingId === u.userId}
                  />
                ))
              )}
            </div>
          </div>
        )}
      </div>

      {showGrant && activeConnectionId && (
        <GrantDialog
          connectionId={activeConnectionId}
          allTables={allTables}
          onClose={() => setShowGrant(false)}
        />
      )}

      {editingUser && activeConnectionId && (
        <EditDialog
          connectionId={activeConnectionId}
          user={editingUser}
          allTables={allTables}
          onClose={() => setEditingUser(null)}
        />
      )}
    </div>
  )
}
