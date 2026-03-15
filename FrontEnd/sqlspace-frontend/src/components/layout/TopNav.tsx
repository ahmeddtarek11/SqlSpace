import { Link, useLocation, useNavigate } from 'react-router-dom'
import { motion } from 'framer-motion'
import {
  Database,
  LayoutDashboard,
  History,
  Bookmark,
  Settings,
  LogOut,
  Moon,
  Sun,
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
import { useThemeStore } from '@/stores/theme-store'
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
  const { theme, toggleTheme } = useThemeStore()

  const handleLogout = () => {
    logout()
    navigate('/login', { replace: true })
  }

  return (
    <header className="h-14 flex items-center gap-2 px-4 border-b border-(--border-default) bg-(--bg-surface) shrink-0">
      {/* Logo */}
      <Link to="/workspace" className="flex items-center gap-2 mr-4">
        <div className="w-7 h-7 rounded-lg bg-violet-600/20 border border-violet-500/40 flex items-center justify-center">
          <Database className="w-3.5 h-3.5 text-violet-400" />
        </div>
        <span className="font-semibold text-sm text-white hidden sm:block">SqlSpace</span>
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
                'relative flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-sm transition-colors',
                active
                  ? 'text-white bg-(--bg-elevated)'
                  : 'text-(--text-muted) hover:text-(--text-secondary) hover:bg-(--bg-elevated)'
              )}
            >
              <Icon className="w-3.5 h-3.5" />
              <span className="hidden md:block">{label}</span>
              {active && (
                <motion.div
                  layoutId="nav-indicator"
                  className="absolute inset-0 rounded-lg border border-violet-500/30 bg-violet-500/5"
                  transition={{ type: 'spring', bounce: 0.2, duration: 0.4 }}
                />
              )}
            </Link>
          )
        })}
      </nav>

      {/* Right actions */}
      <div className="flex items-center gap-2">
        <Button
          variant="ghost"
          size="icon"
          onClick={toggleTheme}
          className="w-8 h-8 text-(--text-muted) hover:text-white"
        >
          {theme === 'dark' ? <Sun className="w-4 h-4" /> : <Moon className="w-4 h-4" />}
        </Button>

        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" className="h-8 px-2 gap-2">
              <Avatar className="w-6 h-6">
                <AvatarFallback className="bg-violet-600/30 text-violet-300 text-xs">
                  {user?.username?.[0]?.toUpperCase() ?? 'U'}
                </AvatarFallback>
              </Avatar>
              <span className="text-sm text-(--text-secondary) hidden sm:block">{user?.username}</span>
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end" className="w-48 bg-(--bg-elevated) border-(--border-default)">
            <DropdownMenuItem
              onClick={handleLogout}
              className="text-red-400 focus:text-red-400 flex items-center gap-2 cursor-pointer"
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
