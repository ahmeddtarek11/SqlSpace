import { Link, useLocation, useNavigate } from 'react-router-dom'
import { motion } from 'framer-motion'
import {
  Database,
  LayoutDashboard,
  History,
  Bookmark,
  Settings,
  LogOut,
  Shield,
} from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { useAuthStore } from '@/stores/auth-store'
import { cn } from '@/lib/utils'

const NAV_ITEMS = [
  { to: '/workspace', label: 'Workspace', icon: Database },
  { to: '/dashboard', label: 'Dashboard', icon: LayoutDashboard },
  { to: '/history', label: 'History', icon: History },
  { to: '/saved', label: 'Saved', icon: Bookmark },
  { to: '/connections', label: 'Connections', icon: Settings },
  { to: '/access', label: 'Access Control', icon: Shield },
]

export function TopNav() {
  const location = useLocation()
  const navigate = useNavigate()
  const { user, logout } = useAuthStore()

  const handleLogout = () => {
    logout()
    navigate('/login', { replace: true })
  }

  return (
    <header
      className="h-14 flex items-center gap-2 px-4 shrink-0"
      style={{
        background: 'var(--bg-surface)',
        borderBottom: '1px solid var(--border-subtle)',
      }}
    >
      {/* Logo */}
      <Link to="/workspace" className="flex items-center gap-2 mr-4">
        <div
          className="w-7 h-7 flex items-center justify-center"
          style={{
            borderRadius: 'var(--radius-md)',
            background: 'var(--accent)',
            boxShadow: '0 0 12px var(--accent-glow)',
          }}
        >
          <Database className="w-3.5 h-3.5 text-white" />
        </div>
        <span className="font-semibold text-[14px] hidden sm:block" style={{ color: 'var(--text-primary)' }}>
          SqlSpace
        </span>
      </Link>

      {/* Nav tabs */}
      <nav className="flex items-center gap-1 flex-1">
        {NAV_ITEMS.map(({ to, label, icon: Icon }) => {
          const active = location.pathname === to
          return (
            <Link
              key={to}
              to={to}
              className={cn(
                'relative flex items-center gap-1.5 px-3 py-1.5 text-[13px] font-medium transition-colors',
              )}
              style={{
                borderRadius: 'var(--radius-md)',
                color: active ? 'var(--text-primary)' : 'var(--text-tertiary)',
                background: active ? 'var(--accent-subtle)' : 'transparent',
              }}
              onMouseEnter={(e) => {
                if (!active) {
                  e.currentTarget.style.background = 'var(--bg-hover)'
                  e.currentTarget.style.color = 'var(--text-primary)'
                }
              }}
              onMouseLeave={(e) => {
                if (!active) {
                  e.currentTarget.style.background = 'transparent'
                  e.currentTarget.style.color = 'var(--text-tertiary)'
                }
              }}
            >
              <Icon className="w-3.5 h-3.5" strokeWidth={1.75} />
              <span className="hidden md:block">{label}</span>
              {active && (
                <motion.div
                  layoutId="nav-indicator"
                  className="absolute inset-0"
                  style={{
                    borderRadius: 'var(--radius-md)',
                    border: '1px solid rgba(77, 104, 235, 0.2)',
                    background: 'var(--accent-subtle)',
                  }}
                  transition={{ type: 'spring', bounce: 0.2, duration: 0.4 }}
                />
              )}
            </Link>
          )
        })}
      </nav>

      {/* Right actions */}
      <div className="flex items-center gap-2">
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button
              variant="ghost"
              className="h-8 px-2 gap-2"
              style={{ color: 'var(--text-secondary)' }}
            >
              <Avatar className="w-6 h-6">
                <AvatarFallback
                  className="text-[11px] font-bold"
                  style={{
                    background: 'var(--accent-subtle)',
                    color: 'var(--accent)',
                    border: '1px solid rgba(77, 104, 235, 0.2)',
                  }}
                >
                  {user?.username?.[0]?.toUpperCase() ?? 'U'}
                </AvatarFallback>
              </Avatar>
              <span className="text-[13px] hidden sm:block" style={{ color: 'var(--text-secondary)' }}>
                {user?.username}
              </span>
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent
            align="end"
            className="w-48"
            style={{
              background: 'var(--bg-elevated)',
              border: '1px solid var(--border-default)',
            }}
          >
            <DropdownMenuItem
              onClick={handleLogout}
              className="flex items-center gap-2 cursor-pointer"
              style={{ color: 'var(--danger)' }}
            >
              <LogOut className="w-4 h-4" />
              Sign out
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
    </header>
  )
}
