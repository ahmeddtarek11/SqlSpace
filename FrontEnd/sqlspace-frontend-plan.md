# SqlSpace Frontend — Full Implementation Plan

## Project Overview

**SqlSpace** is a natural-language-to-SQL workspace that lets users connect databases, type prompts in plain English, and get query results instantly. The frontend is a React + shadcn/ui application with a futuristic dark/light theme, a schema explorer panel, a query editor, dashboard analytics, and access control management.

---

## 1. Tech Stack

| Layer | Choice | Notes |
|-------|--------|-------|
| Framework | **React 18 + Vite** | Fast HMR, ESM-native |
| Routing | **React Router v6** | Nested layouts, auth guards |
| State | **Zustand** | Lightweight global state for auth, connections, theme |
| Server State | **TanStack Query (React Query v5)** | Caching, optimistic updates, infinite scroll for history |
| UI Library | **shadcn/ui** | Radix primitives + Tailwind — fully customizable |
| Styling | **Tailwind CSS v3** + CSS variables | Dark/light via `class` strategy |
| Code Editor | **@monaco-editor/react** | SQL syntax highlighting, autocomplete |
| Charts | **Recharts** | Dashboard analytics charts |
| Icons | **Lucide React** | Consistent icon set (ships with shadcn) |
| HTTP Client | **Axios** | Interceptors for JWT refresh |
| Fonts | **Geist Mono** + **Geist Sans** (Vercel) | Futuristic, clean, modern |
| Animations | **Framer Motion** | Page transitions, panel reveals, micro-interactions |

---

## 2. Design System — "NeonGrid" Aesthetic

### 2.1 Theme Tokens (CSS Variables)

```css
/* ─── DARK THEME (default) ─── */
:root {
  --background:        hsl(222, 47%, 6%);    /* #0B0F1A deep void */
  --background-secondary: hsl(222, 40%, 9%); /* #111827 panels */
  --foreground:        hsl(210, 40%, 96%);   /* #F1F5F9 text */
  --card:              hsl(222, 35%, 10%);   /* #141B2D cards */
  --card-foreground:   hsl(210, 40%, 96%);
  --border:            hsl(222, 20%, 18%);   /* #252D3D subtle borders */
  --primary:           hsl(195, 95%, 50%);   /* #0ACDFF electric cyan */
  --primary-foreground: hsl(222, 47%, 6%);
  --accent:            hsl(265, 90%, 65%);   /* #8B5CF6 vivid purple */
  --accent-foreground: hsl(0, 0%, 100%);
  --destructive:       hsl(0, 85%, 60%);     /* #EF4444 */
  --success:           hsl(142, 76%, 50%);   /* #22C55E */
  --warning:           hsl(38, 92%, 55%);    /* #F59E0B */
  --muted:             hsl(222, 15%, 25%);
  --muted-foreground:  hsl(222, 10%, 55%);
  --glow-primary:      0 0 20px hsla(195,95%,50%,0.3);
  --glow-accent:       0 0 20px hsla(265,90%,65%,0.25);
  --radius:            0.5rem;
}

/* ─── LIGHT THEME ─── */
.light {
  --background:        hsl(210, 20%, 98%);   /* #F8FAFC */
  --background-secondary: hsl(0, 0%, 100%);
  --foreground:        hsl(222, 47%, 11%);   /* #1E293B */
  --card:              hsl(0, 0%, 100%);
  --border:            hsl(214, 32%, 91%);   /* #E2E8F0 */
  --primary:           hsl(200, 98%, 40%);   /* #0284C7 */
  --accent:            hsl(265, 80%, 55%);   /* #7C3AED */
  --glow-primary:      0 0 15px hsla(200,98%,40%,0.15);
}
```

### 2.2 Futuristic Design Principles

1. **Glassmorphism panels** — `backdrop-blur-xl bg-card/60 border border-border/50` on sidebars and cards
2. **Subtle grid background** — a faint dot-grid or line-grid SVG pattern on the main background (dark mode only)
3. **Glow accents** — primary-colored box-shadow on focused inputs, active nav items, and CTAs
4. **Monospace data** — All SQL, UUIDs, timestamps render in `Geist Mono`
5. **Animated borders** — Gradient border animation on the query input area (conic-gradient keyframe)
6. **Staggered reveals** — Framer Motion `staggerChildren` on table rows, sidebar items, and cards
7. **Status indicators** — Pulsing dot animations for live connections, query running states

### 2.3 Typography Scale

| Role | Font | Size | Weight |
|------|------|------|--------|
| Display / Logo | Geist Sans | 24px | 700 |
| Page Heading | Geist Sans | 20px | 600 |
| Section Heading | Geist Sans | 14px | 600 uppercase tracking-wider |
| Body | Geist Sans | 14px | 400 |
| Code / Data | Geist Mono | 13px | 400 |
| Caption / Meta | Geist Sans | 12px | 400 muted-foreground |

---

## 3. Application Architecture

### 3.1 Folder Structure

```
src/
├── api/                    # Axios instance + endpoint functions
│   ├── client.ts           # Axios instance, interceptors, JWT refresh
│   ├── auth.ts             # register, login, refresh, logout
│   ├── connections.ts      # CRUD + test + health + transfer
│   ├── queries.ts          # execute, rerun, history, search, stats
│   ├── saved-queries.ts    # CRUD + execute
│   ├── schema.ts           # getFilteredSchema, refreshSchema
│   ├── access-control.ts   # grant, revoke, update, list, check
│   └── insights.ts         # getInsights, getAdminInsights
│
├── stores/                 # Zustand stores
│   ├── auth-store.ts       # user, tokens, login/logout actions
│   ├── connection-store.ts # activeConnection, connectionsList
│   └── theme-store.ts      # dark/light toggle, persist to localStorage
│
├── hooks/                  # Custom React hooks (TanStack Query wrappers)
│   ├── use-auth.ts
│   ├── use-connections.ts
│   ├── use-queries.ts
│   ├── use-schema.ts
│   ├── use-insights.ts
│   └── use-access-control.ts
│
├── components/
│   ├── ui/                 # shadcn/ui primitives (button, input, dialog, etc.)
│   ├── layout/
│   │   ├── app-shell.tsx         # Main layout with header + sidebars
│   │   ├── top-nav.tsx           # Top navigation bar
│   │   ├── connection-sidebar.tsx # Left sidebar: connections list
│   │   └── schema-panel.tsx      # Right sidebar: schema tree + history
│   ├── auth/
│   │   ├── login-form.tsx
│   │   ├── register-form.tsx
│   │   └── auth-guard.tsx
│   ├── connections/
│   │   ├── connection-card.tsx
│   │   ├── connection-form.tsx      # Create/edit connection dialog
│   │   ├── connection-test-badge.tsx
│   │   └── transfer-dialog.tsx
│   ├── query/
│   │   ├── nl-prompt-input.tsx      # Natural language textarea with animated border
│   │   ├── sql-preview.tsx          # Monaco editor showing generated SQL
│   │   ├── results-table.tsx        # Virtualized data table with sticky headers
│   │   ├── results-toolbar.tsx      # Export, visualize, row count, timing
│   │   └── query-explanation.tsx    # LLM explanation card
│   ├── history/
│   │   ├── history-list.tsx         # Paginated query history
│   │   ├── history-search.tsx       # Search bar for history
│   │   └── history-item.tsx         # Single history entry card
│   ├── saved/
│   │   ├── saved-queries-list.tsx
│   │   └── save-query-dialog.tsx
│   ├── dashboard/
│   │   ├── stats-cards.tsx          # Total Queries, Avg Time, Success Rate, Failed
│   │   ├── volume-chart.tsx         # Area chart: queries over time
│   │   ├── top-tables-chart.tsx     # Bar chart: most queried tables
│   │   ├── top-connections.tsx      # Bar chart: queries per connection
│   │   ├── top-users.tsx            # Table: most active users (admin)
│   │   └── insight-chart-card.tsx   # Dynamic chart card from InsightChartCard
│   ├── access-control/
│   │   ├── users-table.tsx          # Users with access to a connection
│   │   ├── grant-access-dialog.tsx  # Grant access form
│   │   └── restrictions-editor.tsx  # Edit table restrictions
│   └── shared/
│       ├── loading-skeleton.tsx
│       ├── status-badge.tsx         # Success/Failed/Pending badges
│       ├── empty-state.tsx
│       ├── connection-health-dot.tsx
│       └── animated-counter.tsx     # Number count-up animation for stats
│
├── pages/
│   ├── landing.tsx          # Public landing page
│   ├── login.tsx
│   ├── register.tsx
│   ├── workspace.tsx        # Main query workspace (NL → SQL → Results)
│   ├── dashboard.tsx        # Analytics / Insights
│   ├── history.tsx          # Full query history page
│   ├── saved-queries.tsx    # Saved queries management
│   ├── connections.tsx      # Connections management (settings page)
│   └── access-control.tsx   # Access control management (per connection)
│
├── lib/
│   ├── utils.ts             # cn(), formatDate, formatDuration
│   ├── schema-parser.ts     # Parse schema string into tree structure
│   └── constants.ts         # API base URL, DB provider icons, status colors
│
├── App.tsx
├── main.tsx
└── index.css                # Tailwind directives + CSS variables + grid background
```

### 3.2 Routing Map

```
/                         → Landing page (public)
/login                    → Login page
/register                 → Register page
/app                      → Protected layout (AppShell) — redirects to /app/workspace
  /app/workspace          → Query workspace (default)
  /app/dashboard          → Analytics dashboard
  /app/history            → Full query history
  /app/saved              → Saved queries
  /app/connections        → Connections management
  /app/connections/:id/access → Access control for a connection
  /app/settings           → User settings (theme, profile)
```

---

## 4. Screens — Detailed Specifications

### 4.1 Landing Page (`/`)

**Purpose:** Convert visitors into sign-ups. Hero with animated SQL-to-results demo.

**Layout:**
- Full-screen hero with animated dot-grid background
- Logo + tagline: "Talk to your database. In plain English."
- Animated mock of typing a natural language prompt → SQL appears → results table fades in
- Feature cards (3 columns): "Natural Language Queries", "Multi-DB Support", "Team Access Control"
- Pricing/CTA section
- Footer

**No API calls** — purely static/animated.

**Design notes:**
- Dark background by default with gradient mesh (cyan → purple)
- Floating glassmorphism cards
- Animated typing effect using Framer Motion
- Grid-line SVG pattern overlay at 5% opacity

---

### 4.2 Auth Pages (`/login`, `/register`)

**Endpoints:**
- `POST /api/auth/register` → `{ email, username, password, firstName, lastName }`
- `POST /api/auth/login` → `{ email, password }` → returns `{ accessToken, refreshToken, expiresAt, userId }`
- `POST /api/auth/refresh` → `{ refreshToken }` → new token pair
- `POST /api/auth/logout` → `{ userId }`

**Layout:**
- Split screen: left = branding/illustration, right = form
- Glassmorphism card for the form
- Input validation with zod + react-hook-form
- Loading spinner on submit button
- Error toast on failure

**State management:**
- On login success: store tokens in Zustand (`auth-store`) + `localStorage`
- Axios interceptor reads `accessToken` from store for `Authorization: Bearer` header
- On 401: attempt silent refresh with `refreshToken`; if fails, redirect to `/login`

---

### 4.3 Workspace Page (`/app/workspace`) — MAIN SCREEN

This is the core of the app. Three-panel layout:

```
┌──────────────────────────────────────────────────────────────┐
│  TOP NAV: Logo | Workspace·Dashboard·History·Saved | Search │ Profile │
├────────┬─────────────────────────────────┬───────────────────┤
│  LEFT  │         MAIN PANEL              │   RIGHT PANEL     │
│SIDEBAR │                                 │                   │
│        │  ┌─────────────────────────┐    │  SCHEMA EXPLORER  │
│Connex. │  │  NL Prompt Input        │    │  (tree view)      │
│  List  │  │  [textarea with glow]   │    │                   │
│        │  │  [Generate SQL] button  │    │  ───────────────  │
│  ─── ──│  └─────────────────────────┘    │                   │
│        │                                 │  RECENT ACTIVITY  │
│Storage │  ┌─ SQL Preview (Monaco) ──┐    │  (last 5 queries) │
│ Usage  │  │  SELECT * FROM ...      │    │                   │
│        │  └─────────────────────────┘    │  ───────────────  │
│        │                                 │                   │
│        │  ┌─ Results Table ─────────┐    │  COMPUTE STATUS   │
│        │  │  header row             │    │                   │
│        │  │  data rows...           │    │                   │
│        │  │  (virtualized)          │    │                   │
│        │  └─────────────────────────┘    │                   │
├────────┴─────────────────────────────────┴───────────────────┤
│  STATUS BAR: Connected to __ | Replica | PostgreSQL 15.4     │
└──────────────────────────────────────────────────────────────┘
```

#### Left Sidebar — Connection Selector

**Endpoints:**
- `GET /api/connections` → list all user connections (`ConnectionSummaryDto[]`)
- `POST /api/connections/{id}/health-test` → check health
- `POST /api/connections` → create new
- `POST /api/connections/test` → test before saving

**Behavior:**
- List each connection with: name, provider icon (PostgreSQL/MySQL/SQL Server), health dot (green=healthy, red=error, gray=unknown)
- Click to set as **active connection** (stored in `connection-store`)
- Active connection has highlighted bg + cyan left border
- "+" button opens `ConnectionForm` dialog
- Bottom: storage/usage widget (static or from insights)

#### Main Panel — Query Flow

**Endpoints:**
- `POST /api/queries/execute` → `{ connectionId, userPrompt }` → `QueryExecutionResult`
- `POST /api/queries/{queryId}/rerun` → re-execute previous query

**Flow:**
1. User types natural language in the `NLPromptInput` textarea
2. Clicks "Generate & Run" button (or Ctrl+Enter)
3. API call to `/api/queries/execute` with `connectionId` from active connection
4. On response:
   - Show `generatedSql` in Monaco editor (read-only, SQL syntax)
   - Show `llmExplanation` in a collapsible card below the SQL
   - Parse `resultsJson` (JSON string) and render in `ResultsTable`
   - Show metadata: `rowsReturned`, `executionTimeMs`, `status`
5. If `status !== "Success"`: show error banner with `errorMessage`
6. Toolbar actions: Export as CSV/JSON, Visualize (opens chart dialog), Save Query

**NLPromptInput design:**
- Large textarea with glassmorphism card
- Animated gradient border (conic-gradient rotating on `:focus-within`)
- Sparkle/AI icon to the left
- Bottom bar: model indicator, format SQL button, explain plan button, "Generate & Run" CTA

**ResultsTable design:**
- Sticky header row with uppercase column names
- Alternating row colors (very subtle)
- Hover highlight
- Monospace font for data cells
- Status column renders colored dots
- Tier column renders badge pills (ENTERPRISE=cyan, PRO=primary, FREE=muted)
- Virtualized with `@tanstack/react-virtual` for 1000+ rows

#### Right Sidebar — Schema Explorer

**Endpoints:**
- `GET /api/schema/connections/GetFilteredConnectionSchema?connectionId={id}` → returns schema as string
- `POST /api/schema/connections/{connectionId}/refresh` → force refresh (admin)

**Schema Tree:**
- Parse the returned schema string into a tree: `Schema → Tables → Columns`
- Each schema (e.g., "public") is a collapsible folder
- Each table is a collapsible item with table icon
- Each column shows: icon (key for PK, link for FK, calendar for timestamps, # for numbers), name, type
- Click a table name to auto-insert it into the prompt input
- Click a column to auto-insert `table.column` into the prompt

**Recent Activity (bottom of right sidebar):**
- `GET /api/queries/history?pageSize=5` → last 5 queries
- Show truncated SQL, time ago, click to re-run

---

### 4.4 Dashboard Page (`/app/dashboard`)

**Endpoints:**
- `GET /api/connections/{connectionId}/insights` → `ConnectionInsights`
- `GET /api/connections/{connectionId}/insights/admin` → admin-level insights
- `GET /api/queries/history/stats` → `QueryStatistics`

**Layout (2x2 grid + charts below):**

**Row 1 — Stats Cards (4 cards):**
| Card | Data Source | Display |
|------|------------|---------|
| Total Queries | `summary.totalQueries` | Animated counter + lightning icon |
| Avg Execution Time | `summary.averageExecutionTimeMs` | Formatted as `XXms` + clock icon |
| Success Rate | `successful / total * 100` | Percentage + check icon |
| Failed Queries | `summary.failedQueries` | Counter + alert icon |

**Row 2 — Volume Chart:**
- `insights.volume[]` → `InsightVolumeBucket[]` with `{ date, total, successful, failed }`
- Area chart (Recharts) with stacked areas: successful (cyan), failed (red)
- Time range selector: 7d / 30d / 90d (filter client-side or re-fetch)

**Row 3 — Two columns:**
- Left: **Most Queried Tables** — `insights.topTables[]` → horizontal bar chart
- Right: **Top Connections** — `insights.topConnections[]` → horizontal bar chart

**Row 4 (admin only):**
- **Top Users** — `insights.topUsers[]` → data table with user email, query count

**Row 5 — Insight Chart Cards:**
- `insights.cards[]` → `InsightChartCard[]` → render dynamic charts based on `chartType` and `series`

**Design:**
- Each stat card is a glassmorphism card with glow on hover
- Charts use cyan/purple color scheme
- Animated number count-up on page load (Framer Motion)

---

### 4.5 History Page (`/app/history`)

**Endpoints:**
- `GET /api/queries/history?pageNumber=1&pageSize=20` → `PaginatedQueryHistory`
- `GET /api/queries/history/{queryId}` → `QueryHistoryDetailDto`
- `GET /api/queries/history/search?term=...` → search by prompt or SQL
- `POST /api/queries/{queryId}/rerun` → re-execute
- `GET /api/queries/history/connection/{connectionId}` → admin: history for connection

**Layout:**
- Top: search bar (`HistorySearch`) with debounced search
- Filter pills: All, Success, Failed, by connection dropdown
- List of `HistoryItem` cards:
  - Shows: userPrompt (truncated), generatedSql (code block), status badge, rowsReturned, executionTimeMs, executedAt (relative time), connectionName
  - Actions: Re-run, View Details, Save Query
- Click to expand: full details including `resultsJson`, `llmResponse`, `errorMessage`
- Pagination controls at bottom (use `hasNextPage`, `hasPreviousPage`, `totalPages`)

**Design:**
- Each history item is a card with a left colored border (green=success, red=failed, yellow=warning)
- Hover reveals action buttons
- Search results highlight matching text

---

### 4.6 Saved Queries Page (`/app/saved`)

**Endpoints:**
- `GET /api/saved-queries` → `SavedQueryDto[]`
- `POST /api/saved-queries` → `{ name, queryHistoryId }`
- `PATCH /api/saved-queries/{id}` → rename `{ name }`
- `DELETE /api/saved-queries/{id}` → delete
- `POST /api/saved-queries/{id}/execute` → re-execute saved query

**Layout:**
- Grid of saved query cards (or switchable to list view)
- Each card shows: name, userPrompt, generatedSql (truncated), connectionName, createdAtUtc
- Actions: Execute, Rename (inline edit), Delete (confirm dialog)
- Empty state: illustration + "Save your first query from the workspace"

---

### 4.7 Connections Management Page (`/app/connections`)

**Endpoints:**
- `GET /api/connections` → list all
- `POST /api/connections` → create
- `GET /api/connections/{id}` → details (`ConnectionDto`)
- `DELETE /api/connections/{id}` → delete
- `POST /api/connections/test` → test new connection
- `POST /api/connections/{id}/health-test` → test existing
- `PATCH /api/connections/{id}/password` → update password
- `POST /api/connections/{id}/transfer-ownership` → transfer

**Layout:**
- Card grid: one card per connection showing: name, provider badge, host, database, health status, isAdmin badge, lastSuccessfulConnection, connectionSummary
- Click card → expand to detail view with: full connection info, health test button, password update, transfer ownership, delete
- "Add Connection" opens a multi-step dialog:
  1. Select provider (PostgreSQL / MySQL / SQL Server) — visual cards
  2. Choose input mode (Individual Fields vs Raw Connection String)
  3. Fill form: host, port, database, username, password, SSL toggle, name
  4. Test connection (shows success/error + response time + server version)
  5. Confirm & create

**Connection form validation:**
- Required fields enforced per `CreateConnectionRequest` schema
- Port defaults: PostgreSQL=5432, MySQL=3306, SQL Server=1433
- Real-time test before save

---

### 4.8 Access Control Page (`/app/connections/:id/access`)

**Endpoints:**
- `GET /api/AccessControl/connections/{id}/users` → `UserAccessSummary[]`
- `POST /api/AccessControl/connections/{id}/grants` → grant access
- `PUT /api/AccessControl/connections/{id}/users/{userId}/restrictions` → update restrictions
- `DELETE /api/AccessControl/connections/{id}/users/{userId}` → revoke
- `GET /api/AccessControl/connections/{id}/accessible-tables` → table list for restriction picker

**Layout:**
- Page header: connection name + "Manage Access" title
- "Grant Access" button → opens dialog: email input, toggle full access vs restricted, table multi-select picker
- Users table: email, userName, hasFullAccess badge, restrictedTables (comma-separated or count), grantedAt, grantedByUserEmail
- Row actions: Edit restrictions (opens editor with table checkboxes), Revoke (confirm dialog)

**RestrictionsEditor:**
- Fetches accessible tables for the connection
- Checkbox list of all tables
- Toggle: "Full Access" (unchecks all restrictions) vs "Restricted" (enables table picker)

---

## 5. API Client Setup

### 5.1 Axios Instance (`src/api/client.ts`)

```typescript
import axios from 'axios';
import { useAuthStore } from '@/stores/auth-store';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000';

export const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: { 'Content-Type': 'application/json' },
});

// Request interceptor: attach JWT
apiClient.interceptors.request.use((config) => {
  const token = useAuthStore.getState().accessToken;
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

// Response interceptor: handle 401 → silent refresh
apiClient.interceptors.response.use(
  (res) => res,
  async (error) => {
    const original = error.config;
    if (error.response?.status === 401 && !original._retry) {
      original._retry = true;
      const refreshToken = useAuthStore.getState().refreshToken;
      try {
        const { data } = await axios.post(`${API_BASE_URL}/api/auth/refresh`, { refreshToken });
        useAuthStore.getState().setTokens(data.data);
        original.headers.Authorization = `Bearer ${data.data.accessToken}`;
        return apiClient(original);
      } catch {
        useAuthStore.getState().logout();
        window.location.href = '/login';
      }
    }
    return Promise.reject(error);
  }
);
```

### 5.2 API Response Wrapper

All API responses follow this pattern:
```typescript
interface ApiResponse<T> {
  success: boolean;
  statusCode: number;
  message: string | null;
  data: T;
  errors: ApiError[] | null;
  traceId: string | null;
  timestampUtc: string;
}
```

Every API function should unwrap: `return response.data.data` and throw on `!success`.

---

## 6. Key Implementation Patterns

### 6.1 Auth Guard

```tsx
// src/components/auth/auth-guard.tsx
function AuthGuard({ children }) {
  const { accessToken } = useAuthStore();
  const location = useLocation();
  if (!accessToken) return <Navigate to="/login" state={{ from: location }} replace />;
  return children;
}
```

Wrap all `/app/*` routes.

### 6.2 Active Connection Context

Many endpoints require `connectionId`. The `connection-store` holds:
```typescript
interface ConnectionStore {
  activeConnectionId: string | null;
  connections: ConnectionSummaryDto[];
  setActive: (id: string) => void;
  // Auto-select first connection on initial load
}
```

The workspace, schema, history, and insights pages all read `activeConnectionId`.

### 6.3 Schema Parser

The `GET /api/schema/connections/GetFilteredConnectionSchema` returns a **string** (likely DDL or structured text). Write a parser in `src/lib/schema-parser.ts` that converts it into:

```typescript
interface SchemaTree {
  schemas: {
    name: string;       // e.g., "public"
    tables: {
      name: string;     // e.g., "users"
      columns: {
        name: string;
        type: string;   // e.g., "uuid", "varchar", "timestamp"
        isPrimaryKey: boolean;
        isForeignKey: boolean;
      }[];
    }[];
  }[];
}
```

This drives the right-panel schema explorer tree.

### 6.4 Results Table Rendering

`resultsJson` from query execution is a JSON string. Parse it:
```typescript
const data: Record<string, any>[] = JSON.parse(result.resultsJson);
const columns = Object.keys(data[0] || {});
```

Use `@tanstack/react-table` for a powerful, headless table with sorting, resizing, and virtualization via `@tanstack/react-virtual`.

### 6.5 Theme Toggle

```typescript
// src/stores/theme-store.ts
interface ThemeStore {
  theme: 'dark' | 'light';
  toggle: () => void;
}
// On toggle: document.documentElement.classList.toggle('dark')
// Persist to localStorage
```

---

## 7. Endpoint-to-Screen Mapping (Quick Reference)

| Endpoint | Screen | Component |
|----------|--------|-----------|
| `POST /api/auth/register` | Register | `RegisterForm` |
| `POST /api/auth/login` | Login | `LoginForm` |
| `POST /api/auth/refresh` | Global | Axios interceptor |
| `POST /api/auth/logout` | TopNav | Logout button |
| `GET /api/connections` | Workspace sidebar, Connections page | `ConnectionSidebar`, `ConnectionsPage` |
| `POST /api/connections` | Connections page | `ConnectionForm` dialog |
| `GET /api/connections/{id}` | Connections page detail | `ConnectionDetailCard` |
| `DELETE /api/connections/{id}` | Connections page | Delete button |
| `POST /api/connections/test` | Connection form step 4 | `ConnectionTestBadge` |
| `PATCH /api/connections/{id}/password` | Connections detail | Password update form |
| `POST /api/connections/{id}/transfer-ownership` | Connections detail | `TransferDialog` |
| `POST /api/connections/{id}/health-test` | Sidebar, Connections page | Health dot refresh |
| `POST /api/queries/execute` | Workspace main panel | `NLPromptInput` → submit |
| `POST /api/queries/{id}/rerun` | History, Workspace | Re-run button |
| `GET /api/queries/history` | History page, Right sidebar | `HistoryList` |
| `GET /api/queries/history/{id}` | History detail | `HistoryItem` expanded |
| `GET /api/queries/history/connection/{id}` | History (admin filter) | `HistoryList` filtered |
| `GET /api/queries/history/search` | History page | `HistorySearch` |
| `GET /api/queries/history/stats` | Dashboard | `StatsCards` |
| `GET /api/saved-queries` | Saved Queries page | `SavedQueriesList` |
| `POST /api/saved-queries` | Workspace (save btn) | `SaveQueryDialog` |
| `PATCH /api/saved-queries/{id}` | Saved Queries page | Inline rename |
| `DELETE /api/saved-queries/{id}` | Saved Queries page | Delete button |
| `POST /api/saved-queries/{id}/execute` | Saved Queries page | Execute button |
| `GET /api/schema/.../GetFilteredConnectionSchema` | Workspace right panel | `SchemaPanel` |
| `POST /api/schema/connections/{id}/refresh` | Schema panel (admin) | Refresh button |
| `GET /api/connections/{id}/insights` | Dashboard | All dashboard charts |
| `GET /api/connections/{id}/insights/admin` | Dashboard (admin) | Admin-only sections |
| `POST /api/AccessControl/.../grants` | Access Control page | `GrantAccessDialog` |
| `PUT /api/AccessControl/.../restrictions` | Access Control page | `RestrictionsEditor` |
| `DELETE /api/AccessControl/.../users/{id}` | Access Control page | Revoke button |
| `GET /api/AccessControl/.../users` | Access Control page | `UsersTable` |
| `GET /api/AccessControl/.../has-access` | Workspace (guard) | Connection access check |
| `GET /api/AccessControl/.../can-access-table` | Schema panel | Filter tree |
| `GET /api/AccessControl/.../accessible-tables` | Access Control | `RestrictionsEditor` table picker |

---

## 8. Build Order (for Claude Code)

### Phase 1 — Foundation (do this first)
1. `npm create vite@latest sqlspace-frontend -- --template react-ts`
2. Install deps: `tailwindcss`, `shadcn/ui` (init), `react-router-dom`, `zustand`, `@tanstack/react-query`, `axios`, `framer-motion`, `lucide-react`
3. Set up Tailwind config with CSS variables (dark/light themes from Section 2.1)
4. Add Geist fonts via `@fontsource/geist-sans` and `@fontsource/geist-mono`
5. Set up routing skeleton (`App.tsx` with React Router)
6. Create `apiClient` with interceptors
7. Create Zustand stores: `auth-store`, `connection-store`, `theme-store`
8. Add shadcn components: `button`, `input`, `dialog`, `dropdown-menu`, `card`, `badge`, `toast`, `tooltip`, `tabs`, `table`, `select`, `switch`, `separator`, `skeleton`, `sheet`, `command`, `scroll-area`, `popover`, `avatar`, `checkbox`

### Phase 2 — Auth Flow
9. Build `LoginForm` + `RegisterForm` pages
10. Build `AuthGuard` wrapper
11. Wire up auth API functions + store

### Phase 3 — App Shell + Connections
12. Build `AppShell` layout (header + left sidebar + main + right sidebar + status bar)
13. Build `TopNav` with navigation tabs, search, theme toggle, profile menu
14. Build `ConnectionSidebar` — fetches connections, health dots, active selection
15. Build `ConnectionForm` dialog (multi-step: provider → fields → test → create)

### Phase 4 — Core Workspace
16. Build `NLPromptInput` with animated border
17. Build `SQLPreview` (Monaco editor integration)
18. Build `QueryExplanation` card
19. Build `ResultsTable` with `@tanstack/react-table`
20. Build `ResultsToolbar` (export, visualize, stats)
21. Wire up `POST /api/queries/execute` flow
22. Build `SchemaPanel` — fetch + parse + render tree
23. Build recent activity section in right sidebar

### Phase 5 — Dashboard
24. Build `StatsCards` with animated counters
25. Build `VolumeChart` (Recharts area chart)
26. Build `TopTablesChart` + `TopConnections` bar charts
27. Build `TopUsers` table (admin)
28. Wire up insights API

### Phase 6 — History + Saved Queries
29. Build `HistoryList` with pagination
30. Build `HistorySearch` with debounce
31. Build `HistoryItem` with expand/collapse detail
32. Build `SavedQueriesList` with CRUD
33. Build `SaveQueryDialog`

### Phase 7 — Access Control + Connections Management
34. Build full `ConnectionsPage` with card grid + detail view
35. Build `AccessControlPage` with users table
36. Build `GrantAccessDialog`
37. Build `RestrictionsEditor`

### Phase 8 — Landing Page + Polish
38. Build landing page with animations
39. Add page transitions (Framer Motion `AnimatePresence`)
40. Add loading skeletons for all async content
41. Add error boundaries
42. Add responsive breakpoints (collapse sidebars on mobile)
43. Final visual polish: glow effects, grid background, micro-interactions

---

## 9. Environment Variables

```env
VITE_API_BASE_URL=http://localhost:5000
```

---

## 10. Key shadcn/ui Components to Install

```bash
npx shadcn@latest init
npx shadcn@latest add button input card dialog dropdown-menu badge toast tooltip tabs table select switch separator skeleton sheet command scroll-area popover avatar checkbox alert textarea label form sonner resizable collapsible context-menu
```

---

## 11. Critical Notes for Claude Code

1. **All API responses are wrapped** in `ApiResponse<T>`. Always access `response.data.data` for the actual payload. Check `response.data.success` before consuming.

2. **`resultsJson` is a JSON string**, not an object. Always `JSON.parse()` it. Handle null/empty gracefully.

3. **Schema endpoint returns a string**, not structured JSON. You'll need to write a parser based on the actual format returned (could be DDL, could be JSON — inspect at runtime and adapt).

4. **`DbProviders` enum**: `"SqlServer"`, `"PostgreSql"`, `"MySql"` — use these for provider-specific icons and default ports.

5. **`QueryStatus` enum**: `"Success"`, `"Failed"`, `"InsufficientPermissions"`, `"ValidationFailed"`, `"LlmError"`, `"ExecutionFailed"`, `"Timeout"` — color-code appropriately.

6. **Pagination**: History uses `PaginatedQueryHistory` with `pageNumber`, `pageSize`, `totalPages`, `hasNextPage`, `hasPreviousPage`. Implement page navigation controls.

7. **Admin vs regular user**: Some endpoints are admin-only (schema refresh, connection history, admin insights). Check `isAdmin` from connection data to conditionally show these features.

8. **JWT tokens**: Store in memory (Zustand) + localStorage. The refresh flow is critical — intercept 401s globally.

9. **Theme**: Use Tailwind `dark:` classes everywhere. The theme toggle adds/removes `dark` class on `<html>`.

10. **Responsive**: Sidebars collapse to slide-out sheets on screens < 1024px. Use shadcn's `Sheet` component for mobile sidebars.
