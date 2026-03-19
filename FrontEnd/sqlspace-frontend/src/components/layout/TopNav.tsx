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
    <header className="h-14 flex items-center gap-2 px-4 border-b border-white/10 bg-[#111113] shrink-0">
      {/* Logo */}
      <Link to="/workspace" className="flex items-center gap-2 mr-4">
        <div className="w-7 h-7 rounded-lg bg-sky-500 flex items-center justify-center shadow-[0_0_12px_rgba(14,165,233,0.5)]">
          <Database className="w-3.5 h-3.5 text-white" />
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
                  ? 'text-sky-400 bg-sky-500/10'
                  : 'text-zinc-400 hover:text-zinc-100 hover:bg-white/5'
              )}
            >
              <Icon className="w-3.5 h-3.5" />
              <span className="hidden md:block">{label}</span>
              {active && (
                <motion.div
                  layoutId="nav-indicator"
                  className="absolute inset-0 rounded-lg border border-sky-500/30 bg-sky-500/5"
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
          className="w-8 h-8 text-zinc-400 hover:text-zinc-100 hover:bg-white/5"
        >
          {theme === 'dark' ? <Sun className="w-4 h-4" /> : <Moon className="w-4 h-4" />}
        </Button>

        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" className="h-8 px-2 gap-2 hover:bg-white/5">
              <Avatar className="w-6 h-6">
                <AvatarFallback className="bg-sky-500/20 text-sky-300 text-xs border border-sky-500/30">
                  {user?.username?.[0]?.toUpperCase() ?? 'U'}
                </AvatarFallback>
              </Avatar>
              <span className="text-sm text-zinc-400 hidden sm:block">{user?.username}</span>
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end" className="w-48 bg-[#18181b] border-white/10">
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
