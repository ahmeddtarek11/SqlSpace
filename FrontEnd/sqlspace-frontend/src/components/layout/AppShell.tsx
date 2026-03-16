import { Outlet } from 'react-router-dom'
import { Toaster } from '@/components/ui/sonner'
import { TopNav } from './TopNav'

export function AppShell() {
  return (
    <div className="flex flex-col h-screen bg-(--bg-base)">
      <TopNav />
      <main className="flex-1 min-h-0 overflow-hidden">
        <Outlet />
      </main>
      <Toaster richColors position="top-right" />
    </div>
  )
}
