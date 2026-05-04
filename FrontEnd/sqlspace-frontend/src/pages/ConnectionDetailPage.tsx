import { useState } from 'react'
import { Link, useParams, useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import {
  ArrowLeft,
  TerminalSquare,
  Shield,
  History,
  Activity,
  RefreshCw,
  KeyRound,
  Trash2,
  Database,
  Settings,
  ClipboardList,
  Clock,
  Users,
} from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { connectionsApi } from '@/api/connections'
import { accessApi } from '@/api/insights'
import { formatDate } from '@/lib/utils'
import { cn } from '@/lib/utils'

export default function ConnectionDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const qc = useQueryClient()

  const [showPasswordModal, setShowPasswordModal] = useState(false)
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')

  const { data: conn, isLoading } = useQuery({
    queryKey: ['connections', id],
    queryFn: () => connectionsApi.get(id!),
    enabled: !!id,
  })

  const { data: accessUsers = [], isLoading: usersLoading } = useQuery({
    queryKey: ['connection-users', id],
    queryFn: () => accessApi.list(id!),
    enabled: !!id && !!conn?.isAdmin,
  })

  const healthTestMutation = useMutation({
    mutationFn: () => connectionsApi.healthTest(id!),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['connections'] })
      void qc.invalidateQueries({ queryKey: ['connections', id] })
      toast.success('Connection is healthy')
    },
    onError: () => {
      void qc.invalidateQueries({ queryKey: ['connections'] })
      void qc.invalidateQueries({ queryKey: ['connections', id] })
      toast.error('Health test failed')
    },
  })

  const deleteMutation = useMutation({
    mutationFn: () => connectionsApi.delete(id!),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['connections'] })
      toast.success('Connection deleted')
      navigate('/connections')
    },
    onError: () => toast.error('Failed to delete connection'),
  })

  const updatePasswordMutation = useMutation({
    mutationFn: () => connectionsApi.updatePassword(id!, newPassword),
    onSuccess: () => {
      toast.success('Password updated')
      setShowPasswordModal(false)
      setNewPassword('')
      setConfirmPassword('')
    },
    onError: (e: Error) => toast.error(e.message),
  })

  if (isLoading) {
    return (
      <div className="h-full overflow-y-auto">
        <div className="p-8 max-w-5xl mx-auto space-y-6">
          <Skeleton className="h-5 w-36 bg-white/5" />
          <Skeleton className="h-28 w-full rounded-2xl bg-white/5" />
          <div className="grid grid-cols-3 gap-4">
            {[1, 2, 3].map((i) => (
              <Skeleton key={i} className="h-20 rounded-xl bg-white/5" />
            ))}
          </div>
        </div>
      </div>
    )
  }

  if (!conn) {
    return (
      <div className="h-full overflow-y-auto">
        <div className="p-8 max-w-5xl mx-auto">
          <Link
            to="/connections"
            className="inline-flex items-center gap-2 text-sm text-zinc-400 hover:text-white mb-6 transition-colors"
          >
            <ArrowLeft className="w-4 h-4" /> Back to Connections
          </Link>
          <p className="text-zinc-500">Connection not found.</p>
        </div>
      </div>
    )
  }

  return (
    <div className="h-full overflow-y-auto">
      <div className="p-8 max-w-5xl mx-auto">

        {/* Back */}
        <Link
          to="/connections"
          className="inline-flex items-center gap-2 text-sm font-medium text-zinc-400 hover:text-white mb-6 transition-colors"
        >
          <ArrowLeft className="w-4 h-4" /> Back to Connections
        </Link>

        {/* Header */}
        <div className="flex flex-col md:flex-row md:items-start justify-between gap-6 mb-8">
          <div>
            <div className="flex items-center gap-3 mb-2">
              <div className="w-12 h-12 rounded-xl bg-white/5 border border-white/10 flex items-center justify-center shrink-0">
                <Database className="w-6 h-6 text-sky-400" />
              </div>
              <div>
                <h1 className="text-3xl font-bold text-white tracking-tight">{conn.connectionName}</h1>
                <div className="flex items-center gap-3 mt-1">
                  <Badge variant="outline" className="bg-white/5 border-white/10 font-mono text-xs text-zinc-300">
                    {conn.databaseProvider}
                  </Badge>
                  <div className="flex items-center gap-1.5">
                    <span
                      className={cn(
                        'w-2 h-2 rounded-full',
                        conn.isHealthy
                          ? 'bg-green-500 shadow-[0_0_8px_rgba(34,197,94,0.6)]'
                          : 'bg-red-500 shadow-[0_0_8px_rgba(239,68,68,0.6)]',
                      )}
                    />
                    <span className="text-sm font-medium text-zinc-300">
                      {conn.isHealthy ? 'Healthy' : 'Unhealthy'}
                    </span>
                  </div>
                </div>
              </div>
            </div>
            {conn.connectionSummary && (
              <p className="text-zinc-400 mt-4 max-w-2xl">{conn.connectionSummary}</p>
            )}
          </div>

          <Button
            onClick={() => navigate('/workspace')}
            className="bg-sky-600 hover:bg-sky-500 text-white px-5 py-2.5 rounded-xl font-semibold flex items-center gap-2 transition-all shadow-lg shadow-sky-500/25 shrink-0"
          >
            <TerminalSquare className="w-4 h-4" /> Open Workspace
          </Button>
        </div>

        {/* Quick Links */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
          <Link
            to="/workspace"
            className="bg-[#111113] border border-white/5 hover:border-white/20 hover:bg-[#18181b] p-4 rounded-xl flex items-center gap-4 transition-all group"
          >
            <div className="w-10 h-10 rounded-lg bg-sky-500/10 text-sky-400 flex items-center justify-center group-hover:bg-sky-500 group-hover:text-white transition-colors shrink-0">
              <TerminalSquare className="w-5 h-5" />
            </div>
            <div>
              <div className="font-semibold text-white">Query Data</div>
              <div className="text-xs text-zinc-500">Ask AI or write SQL</div>
            </div>
          </Link>

          <Link
            to="/history"
            className="bg-[#111113] border border-white/5 hover:border-white/20 hover:bg-[#18181b] p-4 rounded-xl flex items-center gap-4 transition-all group"
          >
            <div className="w-10 h-10 rounded-lg bg-cyan-500/10 text-cyan-400 flex items-center justify-center group-hover:bg-cyan-500 group-hover:text-white transition-colors shrink-0">
              <History className="w-5 h-5" />
            </div>
            <div>
              <div className="font-semibold text-white">View History</div>
              <div className="text-xs text-zinc-500">Past executions</div>
            </div>
          </Link>

          <Link
            to="/access"
            className="bg-[#111113] border border-white/5 hover:border-white/20 hover:bg-[#18181b] p-4 rounded-xl flex items-center gap-4 transition-all group"
          >
            <div className="w-10 h-10 rounded-lg bg-emerald-500/10 text-emerald-400 flex items-center justify-center group-hover:bg-emerald-500 group-hover:text-white transition-colors shrink-0">
              <Shield className="w-5 h-5" />
            </div>
            <div>
              <div className="font-semibold text-white">Access Control</div>
              <div className="text-xs text-zinc-500">Manage permissions</div>
            </div>
          </Link>

          {conn.isAdmin && (
            <Link
              to={`/audit-logs?connectionId=${conn.connectionId}`}
              className="bg-[#111113] border border-white/5 hover:border-white/20 hover:bg-[#18181b] p-4 rounded-xl flex items-center gap-4 transition-all group"
            >
              <div className="w-10 h-10 rounded-lg bg-amber-500/10 text-amber-400 flex items-center justify-center group-hover:bg-amber-500 group-hover:text-white transition-colors shrink-0">
                <ClipboardList className="w-5 h-5" />
              </div>
              <div>
                <div className="font-semibold text-white">Audit Logs</div>
                <div className="text-xs text-zinc-500">Review access events</div>
              </div>
            </Link>
          )}
        </div>

        {/* Main Grid */}
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">

          {/* Left: Config + Activity */}
          <div className="lg:col-span-2 space-y-6">

            {/* Configuration */}
            <div className="bg-[#111113] border border-white/10 rounded-2xl p-6">
              <h3 className="text-lg font-semibold text-white mb-6 flex items-center gap-2">
                <Settings className="w-5 h-5 text-zinc-400" /> Configuration
              </h3>

              {!conn.usesRawConnectionString ? (
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-y-6 gap-x-8">
                  <div>
                    <div className="text-xs font-medium text-zinc-500 mb-1 uppercase tracking-wider">Host</div>
                    <div className="text-zinc-200 font-mono text-sm bg-[#18181b] border border-white/5 px-3 py-2 rounded-lg">
                      {conn.host ?? '—'}
                    </div>
                  </div>
                  <div>
                    <div className="text-xs font-medium text-zinc-500 mb-1 uppercase tracking-wider">Port</div>
                    <div className="text-zinc-200 font-mono text-sm bg-[#18181b] border border-white/5 px-3 py-2 rounded-lg">
                      {conn.port ?? '—'}
                    </div>
                  </div>
                  <div>
                    <div className="text-xs font-medium text-zinc-500 mb-1 uppercase tracking-wider">Database Name</div>
                    <div className="text-zinc-200 font-mono text-sm bg-[#18181b] border border-white/5 px-3 py-2 rounded-lg">
                      {conn.databaseName ?? '—'}
                    </div>
                  </div>
                  <div>
                    <div className="text-xs font-medium text-zinc-500 mb-1 uppercase tracking-wider">Username</div>
                    <div className="text-zinc-200 font-mono text-sm bg-[#18181b] border border-white/5 px-3 py-2 rounded-lg">
                      {conn.username ?? '—'}
                    </div>
                  </div>
                </div>
              ) : (
                <div>
                  <div className="text-xs font-medium text-zinc-500 mb-1 uppercase tracking-wider">Connection String</div>
                  <div className="text-zinc-400 font-mono text-sm bg-[#18181b] border border-white/5 px-3 py-2 rounded-lg italic">
                    Hidden for security
                  </div>
                </div>
              )}

              <div className="mt-8 pt-6 border-t border-white/5 flex flex-wrap gap-8">
                <div className="flex items-center gap-3">
                  <div className="text-sm text-zinc-400">SSL Encryption</div>
                  {conn.useSSL ? (
                    <Badge variant="outline" className="bg-green-500/10 text-green-400 border-green-500/20 font-mono">
                      Enabled
                    </Badge>
                  ) : (
                    <Badge variant="outline" className="bg-zinc-800 text-zinc-400 border-white/10 font-mono">
                      Disabled
                    </Badge>
                  )}
                </div>
                <div className="flex items-center gap-3">
                  <div className="text-sm text-zinc-400">Your Role</div>
                  <Badge variant="outline" className="bg-sky-500/10 text-sky-400 border-sky-500/20 font-mono">
                    {conn.isAdmin ? 'Admin' : 'Viewer'}
                  </Badge>
                </div>
              </div>
            </div>

            {/* Activity */}
            <div className="bg-[#111113] border border-white/10 rounded-2xl p-6">
              <h3 className="text-lg font-semibold text-white mb-6 flex items-center gap-2">
                <Clock className="w-5 h-5 text-zinc-400" /> Activity
              </h3>
              <div className="flex items-center justify-between py-3 border-b border-white/5">
                <span className="text-sm text-zinc-400">Created</span>
                <span className="text-sm font-medium text-white">{formatDate(conn.createdAt)}</span>
              </div>
              <div className="flex items-center justify-between py-3">
                <span className="text-sm text-zinc-400">Last Connected</span>
                <span className="text-sm font-medium text-white">
                  {conn.lastSuccessfulConnection ? formatDate(conn.lastSuccessfulConnection) : '—'}
                </span>
              </div>
              {conn.lastConnectionError && (
                <div className="mt-4 p-3 bg-red-500/10 border border-red-500/20 rounded-xl">
                  <p className="text-xs text-red-400">{conn.lastConnectionError}</p>
                </div>
              )}
            </div>

            {/* Users with Access — admin only */}
            {conn.isAdmin && (
              <div className="bg-[#111113] border border-white/10 rounded-2xl p-6">
                <h3 className="text-lg font-semibold text-white mb-6 flex items-center gap-2">
                  <Users className="w-5 h-5 text-zinc-400" /> Users with Access
                  {!usersLoading && (
                    <span className="ml-auto text-xs font-normal text-zinc-500">
                      {accessUsers.length} {accessUsers.length === 1 ? 'user' : 'users'}
                    </span>
                  )}
                </h3>

                {usersLoading ? (
                  <div className="space-y-3">
                    {[1, 2, 3].map((i) => (
                      <Skeleton key={i} className="h-14 rounded-xl bg-white/5" />
                    ))}
                  </div>
                ) : accessUsers.length === 0 ? (
                  <p className="text-sm text-zinc-500 text-center py-4">No users have been granted access yet.</p>
                ) : (
                  <div className="space-y-3">
                    {accessUsers.map((u) => (
                      <div
                        key={u.accessId}
                        className="flex flex-col gap-2 p-4 rounded-xl bg-[#18181b] border border-white/5"
                      >
                        <div className="flex items-center gap-3">
                          {/* Avatar */}
                          <div className="w-8 h-8 rounded-full bg-sky-500/20 border border-sky-500/30 flex items-center justify-center text-xs font-semibold text-sky-300 shrink-0">
                            {(u.userName?.[0] ?? u.userEmail[0]).toUpperCase()}
                          </div>

                          {/* Name + email */}
                          <div className="flex-1 min-w-0">
                            <p className="text-sm font-medium text-zinc-200 truncate">{u.userName || u.userEmail}</p>
                            <p className="text-xs text-zinc-500 truncate">{u.userEmail}</p>
                          </div>

                          {/* Access badge */}
                          {u.hasFullAccess ? (
                            <Badge variant="outline" className="bg-green-500/10 text-green-400 border-green-500/20 text-xs shrink-0">
                              Full Access
                            </Badge>
                          ) : (
                            <Badge variant="outline" className="bg-amber-500/10 text-amber-400 border-amber-500/20 text-xs shrink-0">
                              Restricted
                            </Badge>
                          )}
                        </div>

                        {/* Restricted tables */}
                        {!u.hasFullAccess && u.restrictedTables.length > 0 && (
                          <div className="pl-11">
                            <p className="text-[10px] font-medium text-zinc-500 uppercase tracking-wider mb-1.5">
                              Cannot access
                            </p>
                            <div className="flex flex-wrap gap-1.5">
                              {u.restrictedTables.map((table) => (
                                <span
                                  key={table}
                                  className="inline-flex items-center px-2 py-0.5 rounded-md bg-red-500/10 border border-red-500/20 text-red-300 text-xs font-mono"
                                >
                                  {table}
                                </span>
                              ))}
                            </div>
                          </div>
                        )}

                        {/* Granted info */}
                        <div className="pl-11 text-[10px] text-zinc-600">
                          Granted {formatDate(u.grantedAt)}
                          {u.grantedByUserEmail && ` by ${u.grantedByUserEmail}`}
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            )}
          </div>

          {/* Right: Management Actions */}
          <div className="space-y-4">
            <h3 className="text-sm font-semibold text-zinc-500 uppercase tracking-wider px-2">
              Management Actions
            </h3>

            <button
              onClick={() => healthTestMutation.mutate()}
              disabled={healthTestMutation.isPending}
              className="w-full flex items-center gap-3 p-4 rounded-xl bg-[#111113] border border-white/10 hover:bg-[#18181b] hover:border-white/20 transition-all disabled:opacity-50 text-left"
            >
              <Activity className="w-5 h-5 text-blue-400 shrink-0" />
              <div>
                <div className="text-sm font-medium text-zinc-200">Run Health Test</div>
                <div className="text-xs text-zinc-500 mt-0.5">Ping DB to verify connection</div>
              </div>
            </button>

            <button
              onClick={() => toast.info('Schema refresh triggered')}
              className="w-full flex items-center gap-3 p-4 rounded-xl bg-[#111113] border border-white/10 hover:bg-[#18181b] hover:border-white/20 transition-all text-left"
            >
              <RefreshCw className="w-5 h-5 text-sky-400 shrink-0" />
              <div>
                <div className="text-sm font-medium text-zinc-200">Refresh Schema</div>
                <div className="text-xs text-zinc-500 mt-0.5">Update AI schema knowledge</div>
              </div>
            </button>

            {conn.isAdmin && (
              <button
                onClick={() => setShowPasswordModal(true)}
                className="w-full flex items-center gap-3 p-4 rounded-xl bg-[#111113] border border-white/10 hover:bg-[#18181b] hover:border-white/20 transition-all text-left"
              >
                <KeyRound className="w-5 h-5 text-amber-400 shrink-0" />
                <div>
                  <div className="text-sm font-medium text-zinc-200">Update Credentials</div>
                  <div className="text-xs text-zinc-500 mt-0.5">Change password or username</div>
                </div>
              </button>
            )}

            <div className="pt-6 mt-2 border-t border-white/5">
              <button
                onClick={() => {
                  if (window.confirm('Are you sure you want to delete this connection? This cannot be undone.')) {
                    deleteMutation.mutate()
                  }
                }}
                disabled={deleteMutation.isPending}
                className="w-full flex items-center justify-center gap-2 p-3 rounded-xl bg-red-500/10 border border-red-500/20 text-red-400 hover:bg-red-500/20 transition-all font-medium text-sm disabled:opacity-50"
              >
                <Trash2 className="w-4 h-4" /> Delete Connection
              </button>
            </div>
          </div>
        </div>
      </div>

      {/* Update Credentials Modal */}
      {showPasswordModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60">
          <div className="bg-[#111113] border border-white/10 rounded-2xl p-6 w-full max-w-sm shadow-2xl">
            <h2 className="text-base font-semibold text-white mb-1">Update Credentials</h2>
            <p className="text-xs text-zinc-500 mb-5">
              Change the database password for{' '}
              <span className="text-zinc-300 font-medium">{conn.connectionName}</span>.
            </p>
            <div className="flex flex-col gap-3">
              <div>
                <label className="block text-xs text-zinc-500 mb-1">New Password</label>
                <input
                  type="password"
                  value={newPassword}
                  onChange={(e) => setNewPassword(e.target.value)}
                  className="w-full px-3 py-2 text-sm bg-[#18181b] border border-white/10 rounded-xl text-white placeholder:text-zinc-600 focus:outline-none focus:border-sky-500 focus:ring-1 focus:ring-sky-500 transition-all"
                  placeholder="••••••••"
                />
              </div>
              <div>
                <label className="block text-xs text-zinc-500 mb-1">Confirm Password</label>
                <input
                  type="password"
                  value={confirmPassword}
                  onChange={(e) => setConfirmPassword(e.target.value)}
                  className="w-full px-3 py-2 text-sm bg-[#18181b] border border-white/10 rounded-xl text-white placeholder:text-zinc-600 focus:outline-none focus:border-sky-500 focus:ring-1 focus:ring-sky-500 transition-all"
                  placeholder="••••••••"
                />
                {confirmPassword && newPassword !== confirmPassword && (
                  <p className="text-xs text-red-400 mt-1">Passwords do not match</p>
                )}
              </div>
            </div>
            <div className="flex gap-2 mt-6">
              <Button
                variant="outline"
                size="sm"
                className="flex-1 border-white/10 text-zinc-400 hover:text-white hover:bg-white/5"
                onClick={() => {
                  setShowPasswordModal(false)
                  setNewPassword('')
                  setConfirmPassword('')
                }}
              >
                Cancel
              </Button>
              <Button
                size="sm"
                className="flex-1 bg-sky-600 hover:bg-sky-500 text-white shadow-lg shadow-sky-500/25 active:scale-[0.98] transition-all disabled:opacity-50"
                disabled={
                  newPassword.length < 1 ||
                  newPassword !== confirmPassword ||
                  updatePasswordMutation.isPending
                }
                onClick={() => updatePasswordMutation.mutate()}
              >
                {updatePasswordMutation.isPending ? 'Saving…' : 'Update Password'}
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
