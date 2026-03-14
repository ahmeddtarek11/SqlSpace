# SqlSpace Frontend — Task Tracker

## Phase 1 — Foundation

- [ ] Scaffold Vite + React + TypeScript project
- [ ] Install all dependencies (tailwindcss, shadcn/ui, react-router-dom, zustand, @tanstack/react-query, axios, framer-motion, lucide-react, @monaco-editor/react, recharts, @tanstack/react-table, @tanstack/react-virtual)
- [ ] Configure Tailwind CSS with NeonGrid theme tokens (CSS variables, dark/light themes)
- [ ] Add Geist fonts (@fontsource/geist-sans, @fontsource/geist-mono)
- [ ] Set up routing skeleton (App.tsx with React Router v6)
- [ ] Create Axios API client with JWT interceptors (src/api/client.ts)
- [ ] Create Zustand stores: auth-store, connection-store, theme-store
- [ ] Init shadcn/ui and install all required components

## Phase 2 — Auth Flow

- [ ] Build LoginForm page (/login)
- [ ] Build RegisterForm page (/register)
- [ ] Build AuthGuard wrapper
- [ ] Wire up auth API functions + store

## Phase 3 — App Shell + Connections

- [ ] Build AppShell layout (header + left sidebar + main + right sidebar + status bar)
- [ ] Build TopNav (navigation tabs, search, theme toggle, profile menu)
- [ ] Build ConnectionSidebar (connections list, health dots, active selection)
- [ ] Build ConnectionForm dialog (multi-step: provider → fields → test → create)

## Phase 4 — Core Workspace

- [ ] Build NLPromptInput with animated gradient border
- [ ] Build SQLPreview (Monaco editor, SQL syntax highlighting)
- [ ] Build QueryExplanation card
- [ ] Build ResultsTable with @tanstack/react-table + virtualization
- [ ] Build ResultsToolbar (export CSV/JSON, visualize, stats)
- [ ] Wire up POST /api/queries/execute flow
- [ ] Build SchemaPanel (fetch + parse + render tree)
- [ ] Build recent activity section in right sidebar

## Phase 5 — Dashboard

- [ ] Build StatsCards with animated counters
- [ ] Build VolumeChart (Recharts area chart)
- [ ] Build TopTablesChart + TopConnections bar charts
- [ ] Build TopUsers table (admin)
- [ ] Wire up insights API

## Phase 6 — History + Saved Queries

- [ ] Build HistoryList with pagination
- [ ] Build HistorySearch with debounce
- [ ] Build HistoryItem with expand/collapse detail
- [ ] Build SavedQueriesList with CRUD
- [ ] Build SaveQueryDialog

## Phase 7 — Access Control + Connections Management

- [ ] Build full ConnectionsPage (card grid + detail view)
- [ ] Build AccessControlPage (users table)
- [ ] Build GrantAccessDialog
- [ ] Build RestrictionsEditor

## Phase 8 — Landing Page + Polish

- [ ] Build landing page with animations
- [ ] Add page transitions (Framer Motion AnimatePresence)
- [ ] Add loading skeletons for all async content
- [ ] Add error boundaries
- [ ] Add responsive breakpoints (mobile sidebars as Sheet)
- [ ] Final visual polish: glow effects, grid background, micro-interactions

---

## Progress Legend
- [ ] = Pending
- [~] = In Progress
- [x] = Completed
