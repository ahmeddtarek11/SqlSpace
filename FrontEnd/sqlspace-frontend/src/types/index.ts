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
export type DBProvider =
  | 'SqlServer'
  | 'PostgreSql'
  | 'MySql'
  | 'MariaDb'
  | 'CockroachDb'
  | 'Supabase'
  | 'PlanetScale'
  | 'Redshift'
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
  status: 'Success' | 'Failed' | 'InsufficientPermissions' | 'ValidationFailed' | 'LlmError' | 'ExecutionFailed' | 'Timeout'
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
  status: 'Success' | 'Failed' | 'InsufficientPermissions' | 'ValidationFailed' | 'LlmError' | 'ExecutionFailed' | 'Timeout'
  rowsReturned: number | null
  executionTimeMs: number | null
  executedAt: string
  connectionName: string | null
}

export interface QueryHistoryDetailDto {
  queryId: string
  userId: string
  userEmail: string
  connectionId: string
  connectionName: string
  userPrompt: string
  generatedSql: string
  llmResponse: string | null
  status: 'Success' | 'Failed' | 'InsufficientPermissions' | 'ValidationFailed' | 'LlmError' | 'ExecutionFailed' | 'Timeout'
  errorMessage: string | null
  resultsJson: string | null
  rowsReturned: number | null
  executionTimeMs: number | null
  executedAt: string
  wasAdminAtExecution: boolean
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
  date: string
  total: number
  successful: number
  failed: number
}

export interface TableQueryCount {
  tableName: string
  queryCount: number
}

export interface QueryStatistics {
  totalQueries: number
  successfulQueries: number
  failedQueries: number
  averageExecutionTimeMs: number
  totalRowsReturned: number
  mostQueriedTables: TableQueryCount[]
  firstQueryDate: string | null
  lastQueryDate: string | null
}

export interface UserQueryCount {
  userId: string
  userEmail: string
  userName: string
  queryCount: number
}

export interface ConnectionQueryCount {
  connectionId: string
  connectionName: string
  queryCount: number
}

export interface ConnectionInsights {
  summary: InsightsSummary
  volume: InsightVolumeBucket[] | null
  topTables: TableQueryCount[] | null
  topUsers: UserQueryCount[] | null
  topConnections: ConnectionQueryCount[] | null
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

// ── Knowledge Base ────────────────────────────────────────────
export interface KnowledgeDocument {
  documentId: string
  connectionId: string
  uploadedByUserId: string
  fileName: string
  sourceType: string
  status: 'Pending' | 'Processing' | 'Indexed' | 'Failed'
  pythonFileId: string | null
  chunksCreated: number
  errorMessage: string | null
  createdAt: string
  processedAt: string | null
}

export interface RagIngestResult {
  fileId: string
  fileName: string
  chunksCreated: number
  status: string
}

export interface RagQuerySource {
  fileId: string
  fileName: string
  chunkId: string
  relevanceScore: number
  excerpt: string
}

export interface RagQueryResult {
  answer: string
  sources: RagQuerySource[]
  tokensUsed: number
}

export interface ChatMessageSource {
  fileId: string
  fileName: string
  chunkId: string
  relevanceScore: number
  excerpt: string
}

export interface ChatMessage {
  messageId: string
  role: 'user' | 'assistant'
  content: string
  sources?: ChatMessageSource[]
  tokensUsed?: number
  errorMessage?: string
  createdAt: string
}

// ── Analytics Charts ─────────────────────────────────────────────────────────

export type ChartType =
  // Bar family
  | 'bar' | 'horizontal_bar' | 'stacked_bar' | 'grouped_bar' | 'floating_bar'
  // Line family
  | 'line' | 'area' | 'stepped_line' | 'multi_axis_line'
  // Circular family
  | 'pie' | 'doughnut' | 'polar_area'
  // Radial
  | 'radar'
  // Point-based
  | 'scatter' | 'bubble'
  // Mixed
  | 'composed'
  // Plugin-based
  | 'treemap' | 'funnel'

export interface ChartConfig {
  xAxis?: string
  yAxis?: string[] | string
  colors?: string[]
  stacked?: boolean
  labelKey?: string
  valueKey?: string
  innerRadius?: string | number
  outerRadius?: string | number
  dataKeys?: string[]
  barKeys?: string[]
  lineKeys?: string[]
  sizeKey?: string
  minKey?: string
  maxKey?: string
}

export interface ChartSuggestion {
  title: string
  description: string
  sql: string
  chartType: ChartType
  chartConfigJson: string
  insight?: string
}

export interface SavedChartDto {
  id: string
  connectionId: string
  connectionName: string
  title: string
  description: string | null
  sqlQuery: string
  originalPrompt: string | null
  chartType: ChartType
  chartConfigJson: string
  insight?: string | null
  gridX: number
  gridY: number
  gridW: number
  gridH: number
  sortOrder: number
  createdAtUtc: string
  updatedAtUtc: string
}

export interface SaveChartRequest {
  title: string
  description?: string
  sqlQuery: string
  originalPrompt?: string
  chartType: ChartType
  chartConfigJson: string
  insight?: string
  gridX?: number
  gridY?: number
  gridW?: number
  gridH?: number
}

export interface ChartDataResult {
  chartId: string
  success: boolean
  resultsJson: string | null
  rowsReturned: number
  executionTimeMs: number
  errorMessage: string | null
}
