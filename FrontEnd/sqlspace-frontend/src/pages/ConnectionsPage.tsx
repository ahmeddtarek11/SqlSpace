import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { motion } from 'framer-motion'
import { toast } from 'sonner'
import { Plus, Trash2, Wifi, WifiOff, Database, RefreshCw, KeyRound, UserCheck, ShieldCheck } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import { TooltipProvider } from '@/components/ui/tooltip'
import { connectionsApi } from '@/api/connections'
import { useConnectionStore } from '@/stores/connection-store'
import { formatDate } from '@/lib/utils'
import type { Connection } from '@/types'

const PROVIDER_COLORS: Record<string, string> = {
  PostgreSql: 'bg-blue-600/20 text-blue-300 border-blue-500/30',
  MySql: 'bg-orange-600/20 text-orange-300 border-orange-500/30',
  SqlServer: 'bg-red-600/20 text-red-300 border-red-500/30',
}

// ── UpdatePasswordModal ────────────────────────────────────────────────────────
function UpdatePasswordModal({ conn, onClose }: { conn: Connection; onClose: () => void }) {
  const qc = useQueryClient()
  const [password, setPassword] = useState('')
  const [confirm, setConfirm] = useState('')

  const mutation = useMutation({
    mutationFn: () => connectionsApi.updatePassword(conn.connectionId, password),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['connections'] })
      toast.success('Password updated')
      onClose()
    },
    onError: (e: Error) => toast.error(e.message),
  })

  const canSubmit = password.length >= 1 && password === confirm

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60">
      <div className="bg-(--bg-surface) border border-(--border-default) rounded-xl p-6 w-full max-w-sm shadow-2xl">
        <h2 className="text-base font-semibold text-(--text-primary) mb-1">Update Password</h2>
        <p className="text-xs text-(--text-muted) mb-5">
          Change the database password for <span className="text-(--text-secondary) font-medium">{conn.connectionName}</span>.
        </p>

        <div className="flex flex-col gap-3">
          <div>
            <label className="block text-xs text-(--text-muted) mb-1">New Password</label>
            <input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className="w-full px-3 py-2 text-sm bg-(--bg-elevated) border border-(--border-default) rounded-lg text-(--text-primary) placeholder:(--text-muted) focus:outline-none focus:border-violet-500"
              placeholder="••••••••"
            />
          </div>
          <div>
            <label className="block text-xs text-(--text-muted) mb-1">Confirm Password</label>
            <input
              type="password"
              value={confirm}
              onChange={(e) => setConfirm(e.target.value)}
              className="w-full px-3 py-2 text-sm bg-(--bg-elevated) border border-(--border-default) rounded-lg text-(--text-primary) placeholder:(--text-muted) focus:outline-none focus:border-violet-500"
              placeholder="••••••••"
            />
            {confirm && password !== confirm && (
              <p className="text-xs text-red-400 mt-1">Passwords do not match</p>
            )}
          </div>
        </div>

        <div className="flex gap-2 mt-6">
          <Button variant="outline" size="sm" className="flex-1 border-(--border-default) text-(--text-secondary)" onClick={onClose}>
            Cancel
          </Button>
          <Button
            size="sm"
            className="flex-1 bg-violet-600 hover:bg-violet-500 text-white disabled:opacity-50"
            disabled={!canSubmit || mutation.isPending}
            onClick={() => mutation.mutate()}
          >
            {mutation.isPending ? 'Saving…' : 'Update Password'}
          </Button>
        </div>
      </div>
    </div>
  )
}

// ── TransferOwnershipModal ─────────────────────────────────────────────────────
function TransferOwnershipModal({ conn, onClose }: { conn: Connection; onClose: () => void }) {
  const qc = useQueryClient()
  const [email, setEmail] = useState('')

  const mutation = useMutation({
    mutationFn: () => connectionsApi.transferOwnership(conn.connectionId, email),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['connections'] })
      toast.success('Ownership transferred')
      onClose()
    },
    onError: (e: Error) => toast.error(e.message),
  })

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60">
      <div className="bg-(--bg-surface) border border-(--border-default) rounded-xl p-6 w-full max-w-sm shadow-2xl">
        <h2 className="text-base font-semibold text-(--text-primary) mb-1">Transfer Ownership</h2>
        <p className="text-xs text-(--text-muted) mb-4">
          Transfer admin rights for <span className="text-(--text-secondary) font-medium">{conn.connectionName}</span> to another user.
        </p>

        <div className="bg-amber-500/10 border border-amber-500/30 rounded-lg px-3 py-2 mb-4">
          <p className="text-xs text-amber-400">
            You will lose admin access to this connection after the transfer.
          </p>
        </div>

        <div>
          <label className="block text-xs text-(--text-muted) mb-1">New Admin Email</label>
          <input
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            className="w-full px-3 py-2 text-sm bg-(--bg-elevated) border border-(--border-default) rounded-lg text-(--text-primary) placeholder:(--text-muted) focus:outline-none focus:border-violet-500"
            placeholder="user@example.com"
          />
        </div>

        <div className="flex gap-2 mt-6">
          <Button variant="outline" size="sm" className="flex-1 border-(--border-default) text-(--text-secondary)" onClick={onClose}>
            Cancel
          </Button>
          <Button
            size="sm"
            className="flex-1 bg-amber-600 hover:bg-amber-500 text-white disabled:opacity-50"
            disabled={!email.includes('@') || mutation.isPending}
            onClick={() => mutation.mutate()}
          >
            {mutation.isPending ? 'Transferring…' : 'Transfer'}
          </Button>
        </div>
      </div>
    </div>
  )
}

// ── ConnectionCard ─────────────────────────────────────────────────────────────
function ConnectionCard({
  conn,
  onDelete,
  onTest,
  onUpdatePassword,
  onTransfer,
}: {
  conn: Connection
  onDelete: () => void
  onTest: () => void
  onUpdatePassword: () => void
  onTransfer: () => void
}) {
  return (
    <motion.div
      layout
      initial={{ opacity: 0, y: 12 }}
      animate={{ opacity: 1, y: 0 }}
      className="bg-(--bg-surface) border border-(--border-default) rounded-xl p-5 flex flex-col gap-4"
    >
      <div className="flex items-start justify-between">
        <div className="flex items-start gap-3">
          <div className="w-9 h-9 rounded-lg bg-(--bg-elevated) border border-(--border-default) flex items-center justify-center">
            <Database className="w-4 h-4 text-(--text-secondary)" />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <p className="text-sm font-medium text-(--text-primary)">{conn.connectionName}</p>
              {conn.isAdmin && (
                <Badge className="text-[10px] px-1.5 py-0 bg-violet-600/20 text-violet-400 border-violet-500/30">
                  <ShieldCheck className="w-2.5 h-2.5 mr-0.5" />Admin
                </Badge>
              )}
            </div>
            <p className="text-xs text-(--text-muted) mt-0.5">
              {conn.host ? `${conn.host}:${conn.port}` : conn.databaseName}
            </p>
          </div>
        </div>
        <Button variant="ghost" size="icon" className="w-7 h-7 text-(--text-muted) hover:text-red-400" onClick={onDelete}>
          <Trash2 className="w-3.5 h-3.5" />
        </Button>
      </div>

      <div className="flex items-center gap-2 flex-wrap">
        <Badge className={`text-xs ${PROVIDER_COLORS[conn.databaseProvider] ?? ''}`}>{conn.databaseProvider}</Badge>
        <Badge variant="secondary" className={`text-xs ${conn.isHealthy ? 'bg-green-600/15 text-green-400 border-green-500/30' : 'bg-red-600/15 text-red-400 border-red-500/30'}`}>
          {conn.isHealthy ? <><Wifi className="w-3 h-3 mr-1" />Healthy</> : <><WifiOff className="w-3 h-3 mr-1" />Unhealthy</>}
        </Badge>
      </div>

      <div className="text-xs text-(--text-muted) border-t border-(--border-subtle) pt-3 space-y-2">
        <div className="flex items-center justify-between">
          <span>
            Created {formatDate(conn.createdAt)}
            {conn.lastSuccessfulConnection && (
              <span className="ml-2">· Last connected {formatDate(conn.lastSuccessfulConnection)}</span>
            )}
          </span>
          <Button
            variant="outline"
            size="sm"
            className="h-7 text-xs border-(--border-strong) text-(--text-secondary) hover:text-(--text-primary) gap-1.5"
            onClick={onTest}
          >
            <RefreshCw className="w-3 h-3" />
            Test
          </Button>
        </div>

        {conn.isAdmin && (
          <div className="flex items-center gap-2 pt-1">
            <Button
              variant="outline"
              size="sm"
              className="h-7 text-xs border-(--border-strong) text-(--text-secondary) hover:text-(--text-primary) gap-1.5"
              onClick={onUpdatePassword}
            >
              <KeyRound className="w-3 h-3" />
              Update Password
            </Button>
            <Button
              variant="outline"
              size="sm"
              className="h-7 text-xs border-(--border-strong) text-(--text-secondary) hover:text-amber-400 gap-1.5"
              onClick={onTransfer}
            >
              <UserCheck className="w-3 h-3" />
              Transfer Ownership
            </Button>
          </div>
        )}
      </div>
    </motion.div>
  )
}

// ── Page ───────────────────────────────────────────────────────────────────────
export default function ConnectionsPage() {
  const navigate = useNavigate()
  const qc = useQueryClient()
  const { removeConnection } = useConnectionStore()

  const [updatePasswordConn, setUpdatePasswordConn] = useState<Connection | null>(null)
  const [transferConn, setTransferConn] = useState<Connection | null>(null)

  const { data: connections = [], isLoading } = useQuery({
    queryKey: ['connections'],
    queryFn: connectionsApi.list,
  })

  const deleteMutation = useMutation({
    mutationFn: connectionsApi.delete,
    onSuccess: (_, id) => {
      removeConnection(id)
      void qc.invalidateQueries({ queryKey: ['connections'] })
      toast.success('Connection removed')
    },
    onError: () => toast.error('Failed to remove connection'),
  })

  const testMutation = useMutation({
    mutationFn: connectionsApi.healthTest,
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['connections'] })
      toast.success('Connection is healthy')
    },
    onError: () => {
      void qc.invalidateQueries({ queryKey: ['connections'] })
      toast.error('Connection is unhealthy')
    },
  })

  return (
    <TooltipProvider>
      <div className="flex flex-col h-full">
        <div className="flex items-center justify-between px-6 py-4 border-b border-(--border-default) bg-(--bg-surface) shrink-0">
          <h1 className="text-lg font-semibold text-(--text-primary)">Connections</h1>
          <Button size="sm" className="bg-violet-600 hover:bg-violet-500 text-white" onClick={() => navigate('/connections/new')}>
            <Plus className="w-4 h-4 mr-1" />New Connection
          </Button>
        </div>

        <div className="flex-1 min-h-0 overflow-y-auto p-6">
          {isLoading ? (
            <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
              {[1, 2, 3].map((i) => <Skeleton key={i} className="h-48 rounded-xl bg-(--bg-surface)" />)}
            </div>
          ) : connections.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-16">
              <Database className="w-12 h-12 text-(--text-muted) mb-4" />
              <p className="text-(--text-muted) text-sm mb-3">No connections yet</p>
              <Button size="sm" className="bg-violet-600 hover:bg-violet-500 text-white" onClick={() => navigate('/connections/new')}>
                <Plus className="w-4 h-4 mr-1" />Add your first connection
              </Button>
            </div>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
              {connections.map((conn) => (
                <ConnectionCard
                  key={conn.connectionId}
                  conn={conn}
                  onDelete={() => deleteMutation.mutate(conn.connectionId)}
                  onTest={() => testMutation.mutate(conn.connectionId)}
                  onUpdatePassword={() => setUpdatePasswordConn(conn)}
                  onTransfer={() => setTransferConn(conn)}
                />
              ))}
            </div>
          )}
        </div>
      </div>

      {updatePasswordConn && (
        <UpdatePasswordModal conn={updatePasswordConn} onClose={() => setUpdatePasswordConn(null)} />
      )}
      {transferConn && (
        <TransferOwnershipModal conn={transferConn} onClose={() => setTransferConn(null)} />
      )}
    </TooltipProvider>
  )
}
