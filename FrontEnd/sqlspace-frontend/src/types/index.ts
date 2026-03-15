// ── Shared API wrapper ────────────────────────────────────────
export interface ApiResponse<T> {
  success: boolean
  statusCode: number
  message: string | null
  data: T
  errors: { code: string; message: string }[] | null
  traceId: string | null
  timestampUtc: string
}

// ── Auth ─────────────────────────────────────────────────────
export interface User {
  id: string
  username: string
  email: string
  role: 'admin' | 'user'
}

export interface AuthTokensResult {
  accessToken: string
  refreshToken: string
  expiresAt: string
  userId: string
}

// ── Connections ───────────────────────────────────────────────
export type DBProvider = 'SqlServer' | 'PostgreSql' | 'MySql'
export type ConnectionInputMode = 'IndividualFields' | 'RawConnectionString'

export interface Connection {
  connectionId: string
  connectionName: string
  databaseProvider: DBProvider
  host: string | null
  port: number | null
  databaseName: string | null
  username: string | null
  useSSL: boolean
  usesRawConnectionString: boolean
  isHealthy: boolean
  lastSuccessfulConnection: string | null
  lastConnectionError: string | null
  createdAt: string
  isAdmin: boolean
  connectionSummary: string | null
}

export interface CreateConnectionRequest {
  connectionName: string
  databaseProvider: DBProvider
  inputMode: ConnectionInputMode
  host?: string
  port?: number
  databaseName?: string
  username?: string
  password?: string
  useSSL?: boolean
  additionalParameters?: string
  rawConnectionString?: string
}

export interface TestConnectionRequest {
  databaseProvider: DBProvider
  inputMode: ConnectionInputMode
  host?: string
  port?: number
  databaseName?: string
  username?: string
  password?: string
  useSSL?: boolean
  rawConnectionString?: string
}

// ── Query ─────────────────────────────────────────────────────
export interface ExecutePromptRequest {
  connectionId: string
  userPrompt: string
}

export interface QueryExecutionResult {
  success: boolean
  queryHistoryId: string
  generatedSql: string | null
  llmExplanation: string | null
  resultsJson: string | null       // JSON-encoded rows
  rowsReturned: number | null
  executionTimeMs: number
  status: 'Completed' | 'Failed' | 'Pending'
  errorMessage: string | null
}

export interface QueryResult {
  columns: string[]
  rows: Record<string, unknown>[]
  row_count: number
  execution_time_ms: number
}

export interface QueryHistoryDto {
  queryId: string
  userPrompt: string | null
  generatedSql: string | null
  status: 'Completed' | 'Failed' | 'Pending'
  rowsReturned: number | null
  executionTimeMs: number | null
  executedAt: string
  connectionName: string | null
}

export interface PaginatedQueryHistory {
  items: QueryHistoryDto[]
  totalCount: number
  pageNumber: number
  pageSize: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

export interface SavedQueryDto {
  id: string
  name: string | null
  userPrompt: string | null
  generatedSql: string | null
  connectionId: string
  connectionName: string | null
  queryHistoryId: string | null
  createdAtUtc: string
  updatedAtUtc: string
}

export interface CreateSavedQueryRequest {
  name: string
  queryHistoryId: string
}

// ── Schema (backend returns a JSON-encoded string) ────────────
export interface SchemaColumn {
  name: string
  dataType: string
  isPrimaryKey: boolean
  isNullable: boolean
  maxLength: number | null
  foreignKeyName: string | null
  referencedTableName: string | null
  referencedColumnName: string | null
}

export interface SchemaTable {
  schema: string
  name: string
  type: string
  columns: SchemaColumn[]
}

export interface ParsedSchema {
  database: string
  capturedAt: string
  tables: SchemaTable[]
}

// ── Insights ─────────────────────────────────────────────────
export interface InsightsSummary {
  totalQueries: number
  successfulQueries: number
  failedQueries: number
  averageExecutionTimeMs: number
  totalRowsReturned: number
  firstQueryDate: string | null
  lastQueryDate: string | null
}

export interface InsightVolumeBucket {
  bucket: string
  count: number
}

export interface TableQueryCount {
  tableName: string
  queryCount: number
}

export interface ConnectionInsights {
  summary: InsightsSummary
  volume: InsightVolumeBucket[] | null
  topTables: TableQueryCount[] | null
}

// ── Access Control ────────────────────────────────────────────
export interface UserAccessSummary {
  accessId: string
  userId: string
  userEmail: string
  userName: string
  hasFullAccess: boolean
  /** Table names the user CANNOT access (empty when hasFullAccess is true) */
  restrictedTables: string[]
  grantedAt: string
  grantedByUserEmail: string
}

export interface TableRestrictionInput {
  table: string
  schema: string
}

export interface GrantAccessRequest {
  targetUserEmail: string
  hasFullAccess: boolean
  restrictedTables?: TableRestrictionInput[]
}

export interface UpdateAccessRestrictionsRequest {
  hasFullAccess: boolean
  restrictedTables?: TableRestrictionInput[]
}
