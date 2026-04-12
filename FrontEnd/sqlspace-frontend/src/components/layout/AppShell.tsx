import { Outlet, Link, useLocation, useNavigate } from 'react-router-dom'
import { Toaster } from '@/components/ui/sonner'
import { useAuthStore } from '@/stores/auth-store'
import { useThemeStore } from '@/stores/theme-store'
import { cn } from '@/lib/utils'
import {
  Database,
  LayoutDashboard,
  BarChart3,
  History,
  Bookmark,
  LogOut,
  Moon,
  Sun,
  Shield,
  Settings,
} from 'lucide-react'

const NAV_ITEMS = [
  { to: '/workspace',   label: 'Workspace',     icon: Database },
  { to: '/dashboard',   label: 'Dashboard',     icon: LayoutDashboard },
  { to: '/analytics',   label: 'Analytics',     icon: BarChart3 },
  { to: '/connections', label: 'Connections',   icon: Settings },
  { to: '/history',     label: 'History',       icon: History },
  { to: '/saved',       label: 'Saved Queries', icon: Bookmark },
  { to: '/access',      label: 'Access Control',icon: Shield },
]

export function AppShell() {
  const location = useLocation()
  const navigate = useNavigate()
  const { user, logout } = useAuthStore()
  const { theme, toggleTheme } = useThemeStore()

  const handleLogout = () => {
    logout()
    navigate('/login', { replace: true })
  }

  const isActive = (to: string) => {
    if (to === '/workspace') return location.pathname === '/workspace'
    return location.pathname === to || location.pathname.startsWith(to + '/')
  }

  return (
    <div className="flex h-screen w-full bg-[#080809]">
      {/* ── LEFT SIDEBAR ─────────────────────────────────────────────────── */}
      <aside className="w-64 shrink-0 flex flex-col bg-[#111113] border-r border-white/10 z-20">

        {/* Logo Header */}
        <div className="h-16 flex items-center gap-3 px-6 border-b border-white/10 shrink-0">
          <Link to="/workspace" className="flex items-center gap-2.5">
            <div className="w-8 h-8 rounded-lg bg-sky-500 flex items-center justify-center shadow-[0_0_12px_rgba(14,165,233,0.5)] shrink-0">
              <Database className="w-4 h-4 text-white" />
            </div>
            <span className="font-semibold text-sm text-white">SqlSpace</span>
          </Link>
        </div>

        {/* Navigation */}
        <nav className="flex-1 min-h-0 overflow-y-auto px-3 py-4 space-y-1">
          {NAV_ITEMS.map(({ to, label, icon: Icon }) => {
            const active = isActive(to)
            return (
              <Link
                key={to}
                to={to}
                className={cn(
                  'flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors',
                  active
                    ? 'bg-sky-500/10 text-sky-400'
                    : 'text-zinc-400 hover:text-zinc-100 hover:bg-white/5'
                )}
              >
                <Icon className="w-4 h-4 shrink-0" />
                <span className="flex-1">{label}</span>
                {active && (
                  <div className="w-1 h-4 rounded-full bg-sky-500 shrink-0" />
                )}
              </Link>
            )
          })}
        </nav>

        {/* User Footer */}
        <div className="border-t border-white/10 px-3 py-3 shrink-0 space-y-2">
          <div className="flex items-center gap-2 px-1">
            <div className="w-7 h-7 rounded-full bg-sky-500/20 border border-sky-500/30 flex items-center justify-center text-xs font-medium text-sky-300 shrink-0">
              {user?.username?.[0]?.toUpperCase() ?? 'U'}
            </div>
            <div className="flex-1 min-w-0">
              <p className="text-xs font-medium text-zinc-200 truncate">{user?.username}</p>
              <p className="text-[10px] text-zinc-500 truncate">{user?.email}</p>
            </div>
          </div>
          <div className="flex items-center gap-1">
            <button
              onClick={toggleTheme}
              className="flex-1 flex items-center justify-center gap-1.5 px-2 py-1.5 text-xs text-zinc-400 hover:text-zinc-200 hover:bg-white/5 rounded-md transition-colors"
            >
              {theme === 'dark' ? <Sun className="w-3.5 h-3.5" /> : <Moon className="w-3.5 h-3.5" />}
              <span>{theme === 'dark' ? 'Light' : 'Dark'}</span>
            </button>
            <button
              onClick={handleLogout}
              className="flex-1 flex items-center justify-center gap-1.5 px-2 py-1.5 text-xs text-zinc-400 hover:text-red-400 hover:bg-red-500/10 rounded-md transition-colors"
            >
              <LogOut className="w-3.5 h-3.5" />
              <span>Sign out</span>
            </button>
          </div>
        </div>
      </aside>

      {/* ── MAIN CONTENT ─────────────────────────────────────────────────── */}
      <main className="flex-1 flex flex-col min-w-0 overflow-hidden relative">
        {/* Decorative glow */}
        <div className="absolute top-0 left-1/2 -translate-x-1/2 w-200 h-100 bg-sky-500/5 blur-[100px] rounded-full pointer-events-none z-0" />
        <div className="flex-1 min-h-0 overflow-hidden relative z-10">
          <Outlet />
        </div>
      </main>

      <Toaster richColors position="top-right" />
    </div>
  )
}
