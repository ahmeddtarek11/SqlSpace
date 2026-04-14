import { Outlet, Link, useLocation, useNavigate } from 'react-router-dom'
import { Toaster } from '@/components/ui/sonner'
import { GlobalRagChatPopup } from '@/components/workspace/GlobalRagChatPopup'
import { useAuthStore } from '@/stores/auth-store'
import { cn } from '@/lib/utils'
import {
  Database,
  LayoutDashboard,
  BarChart3,
  History,
  Bookmark,
  LogOut,
  Shield,
  Settings,
  BookOpen,
  ChevronRight,
} from 'lucide-react'

const NAV_ITEMS = [
  { to: '/workspace',   label: 'Workspace',      icon: Database },
  { to: '/dashboard',   label: 'Dashboard',      icon: LayoutDashboard },
  { to: '/analytics',   label: 'Analytics',      icon: BarChart3 },
  { to: '/connections', label: 'Connections',    icon: Settings },
  { to: '/history',     label: 'History',        icon: History },
  { to: '/saved',       label: 'Saved Queries',  icon: Bookmark },
  { to: '/access',      label: 'Access Control', icon: Shield },
  { to: '/knowledge',   label: 'Knowledge Base', icon: BookOpen },
]

export function AppShell() {
  const location = useLocation()
  const navigate = useNavigate()
  const { user, logout } = useAuthStore()

  const handleLogout = () => {
    logout()
    navigate('/login', { replace: true })
  }

  const isActive = (to: string) => {
    if (to === '/workspace') return location.pathname === '/workspace'
    return location.pathname === to || location.pathname.startsWith(to + '/')
  }

  return (
    <div className="flex h-screen w-full" style={{ background: 'var(--bg-base)' }}>
      {/* ── LEFT SIDEBAR ─────────────────────────────────────────── */}
      <aside
        className="w-[260px] shrink-0 flex flex-col z-20"
        style={{
          background: 'var(--bg-surface)',
          borderRight: '1px solid var(--border-subtle)',
        }}
      >
        {/* Logo */}
        <div
          className="h-[72px] flex items-center gap-3 px-6 shrink-0"
          style={{ borderBottom: '1px solid var(--border-subtle)' }}
        >
          <Link to="/workspace" className="flex items-center gap-3 group">
            <div
              className="w-9 h-9 flex items-center justify-center shrink-0"
              style={{
                background: 'var(--accent)',
                borderRadius: 'var(--radius-md)',
              }}
            >
              <Database className="w-[18px] h-[18px] text-white" strokeWidth={2} />
            </div>
            <span
              className="text-[15px] font-bold tracking-tight"
              style={{ color: 'var(--text-primary)' }}
            >
              SqlSpace
            </span>
          </Link>
        </div>

        {/* Navigation */}
        <nav className="flex-1 min-h-0 overflow-y-auto px-3 py-5 space-y-0.5">
          {NAV_ITEMS.map(({ to, label, icon: Icon }) => {
            const active = isActive(to)
            return (
              <Link
                key={to}
                to={to}
                className={cn(
                  'group flex items-center gap-3 px-3 py-2.5 text-[13px] font-medium transition-all duration-200',
                  active
                    ? 'text-white'
                    : 'hover:text-[var(--text-primary)]'
                )}
                style={{
                  borderRadius: 'var(--radius-md)',
                  color: active ? 'var(--text-primary)' : 'var(--text-secondary)',
                  background: active ? 'var(--accent-subtle)' : 'transparent',
                }}
                onMouseEnter={(e) => {
                  if (!active) e.currentTarget.style.background = 'var(--bg-hover)'
                }}
                onMouseLeave={(e) => {
                  if (!active) e.currentTarget.style.background = 'transparent'
                }}
              >
                <Icon
                  className="w-[18px] h-[18px] shrink-0"
                  strokeWidth={1.75}
                  style={{ color: active ? 'var(--accent)' : 'var(--text-tertiary)' }}
                />
                <span className="flex-1">{label}</span>
                {active && (
                  <ChevronRight
                    className="w-3.5 h-3.5 shrink-0 opacity-50"
                    style={{ color: 'var(--accent)' }}
                  />
                )}
              </Link>
            )
          })}
        </nav>

        {/* User Footer */}
        <div
          className="px-4 py-4 shrink-0"
          style={{ borderTop: '1px solid var(--border-subtle)' }}
        >
          <div className="flex items-center gap-3 mb-3 px-1">
            <div
              className="w-8 h-8 flex items-center justify-center text-xs font-bold shrink-0"
              style={{
                borderRadius: 'var(--radius-pill)',
                background: 'var(--accent-subtle)',
                color: 'var(--accent)',
              }}
            >
              {user?.username?.[0]?.toUpperCase() ?? 'U'}
            </div>
            <div className="flex-1 min-w-0">
              <p
                className="text-[13px] font-semibold truncate"
                style={{ color: 'var(--text-primary)' }}
              >
                {user?.username}
              </p>
              <p
                className="text-[11px] truncate"
                style={{ color: 'var(--text-tertiary)', fontFamily: 'var(--font-mono)' }}
              >
                {user?.email}
              </p>
            </div>
          </div>

          <button
            onClick={handleLogout}
            className="w-full flex items-center justify-center gap-2 px-3 py-2 text-[12px] font-medium transition-all duration-200"
            style={{
              borderRadius: 'var(--radius-md)',
              color: 'var(--text-tertiary)',
              border: '1px solid var(--border-subtle)',
              background: 'transparent',
            }}
            onMouseEnter={(e) => {
              e.currentTarget.style.background = 'var(--danger-subtle)'
              e.currentTarget.style.color = 'var(--danger)'
              e.currentTarget.style.borderColor = 'rgba(248, 113, 113, 0.2)'
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.background = 'transparent'
              e.currentTarget.style.color = 'var(--text-tertiary)'
              e.currentTarget.style.borderColor = 'var(--border-subtle)'
            }}
          >
            <LogOut className="w-3.5 h-3.5" strokeWidth={2} />
            <span>Sign out</span>
          </button>
        </div>
      </aside>

      {/* ── MAIN CONTENT ──────────────────────────────────────────── */}
      <main
        className="flex-1 flex flex-col min-w-0 overflow-hidden relative"
        style={{ background: 'var(--bg-base)' }}
      >
        <div className="flex-1 min-h-0 overflow-hidden relative z-10">
          <Outlet />
        </div>
      </main>

      <GlobalRagChatPopup />
      <Toaster richColors position="top-right" />
    </div>
  )
}
