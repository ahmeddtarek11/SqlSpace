import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { useThemeStore } from '@/stores/theme-store'
import { AuthGuard } from '@/components/auth/AuthGuard'
import { AppShell } from '@/components/layout/AppShell'

// Pages
import LoginPage from '@/pages/LoginPage'
import RegisterPage from '@/pages/RegisterPage'
import WorkspacePage from '@/pages/WorkspacePage'
import DashboardPage from '@/pages/DashboardPage'
import HistoryPage from '@/pages/HistoryPage'
import SavedQueriesPage from '@/pages/SavedQueriesPage'
import ConnectionsPage from '@/pages/ConnectionsPage'
import NewConnectionPage from '@/pages/NewConnectionPage'
import AccessControlPage from '@/pages/AccessControlPage'
import LandingPage from '@/pages/LandingPage'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      staleTime: 30_000,
    },
  },
})

export default function App() {
  const { theme } = useThemeStore()

  return (
    <div className={theme}>
      <QueryClientProvider client={queryClient}>
        <BrowserRouter>
          <Routes>
            {/* Public */}
            <Route path="/" element={<LandingPage />} />
            <Route path="/login" element={<LoginPage />} />
            <Route path="/register" element={<RegisterPage />} />

            {/* Protected */}
            <Route element={<AuthGuard />}>
              <Route element={<AppShell />}>
                <Route path="/workspace" element={<WorkspacePage />} />
                <Route path="/dashboard" element={<DashboardPage />} />
                <Route path="/history" element={<HistoryPage />} />
                <Route path="/saved" element={<SavedQueriesPage />} />
                <Route path="/connections" element={<ConnectionsPage />} />
                <Route path="/connections/new" element={<NewConnectionPage />} />
                <Route path="/access" element={<AccessControlPage />} />
              </Route>
            </Route>

            <Route path="*" element={<Navigate to="/workspace" replace />} />
          </Routes>
        </BrowserRouter>
      </QueryClientProvider>
    </div>
  )
}
