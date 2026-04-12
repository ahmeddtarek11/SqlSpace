# SqlSpace Frontend — Task Tracker

## Phase 1 — Foundation

- [x] Scaffold Vite + React + TypeScript project
- [x] Install all dependencies (tailwindcss, shadcn/ui, react-router-dom, zustand, @tanstack/react-query, axios, framer-motion, lucide-react, @monaco-editor/react, recharts, @tanstack/react-table, @tanstack/react-virtual)
- [x] Configure Tailwind CSS with NeonGrid theme tokens (CSS variables, dark/light themes)
- [x] Add Geist fonts (@fontsource/geist-sans, @fontsource/geist-mono)
- [x] Set up routing skeleton (App.tsx with React Router v6)
- [x] Create Axios API client with JWT interceptors (src/api/client.ts)
- [x] Create Zustand stores: auth-store, connection-store, theme-store, workspace-store
- [x] Init shadcn/ui and install all required components

## Phase 2 — Auth Flow

- [x] Build LoginForm page (/login)
- [x] Build RegisterForm page (/register)
- [x] Build AuthGuard wrapper
- [x] Wire up auth API functions + store

## Phase 3 — App Shell + Connections

- [x] Build AppShell layout (header + main outlet + toaster)
- [x] Build TopNav (navigation tabs, theme toggle, profile menu)
- [x] Build ConnectionSidebar (connections list, health dots, active selection)
- [x] Build ConnectionForm dialog (multi-step: provider → fields → test → create)

## Phase 4 — Core Workspace

- [x] Build NLPromptInput with animated gradient border
- [x] Build SQLPreview (Monaco editor, SQL syntax highlighting, explain toggle)
- [x] Build ResultsTable with @tanstack/react-table + virtualization
- [x] Build ResultsToolbar (export CSV/JSON, visualize button)
- [x] Wire up POST /api/queries/execute flow
- [x] Build SchemaPanel (fetch + parse + render tree)

## Phase 5 — Dashboard

- [x] Build StatsCards with animated counters
- [x] Build VolumeChart (Recharts area chart)
- [x] Build TopTablesChart bar chart
- [x] Build Summary panel
- [x] Wire up insights API

## Phase 6 — History + Saved Queries

- [x] Build HistoryList with search
- [x] Build HistoryItem with expand/collapse detail
- [x] Build SavedQueriesList with delete + run-in-workspace
- [x] Wire up saved queries API

## Phase 7 — Access Control + Connections Management

- [x] Build full ConnectionsPage (card grid)
- [x] Build AccessControlPage (grants table, admin-only)
- [x] Build GrantAccessDialog

## Phase 8 — Landing Page + Polish

- [x] Build landing page with hero, features, animated prompt preview
- [x] Page-level Framer Motion animations on all pages
- [x] Loading skeletons for all async content
- [x] Error display in workspace
- [x] Tailwind v4 canonical CSS-var syntax throughout (bg-(--var) pattern)
- [x] Zero TypeScript errors (tsc --noEmit clean)

---

## Progress Legend

- [ ] = Pending
- [~] = In Progress
- [x] = Completed
