import { toast } from 'sonner'
import { accessApi } from '@/api/insights'
import { knowledgeBaseApi } from '@/api/knowledge-base'
import { useConnectionStore } from '@/stores/connection-store'
import { useRagChatUiStore } from '@/stores/rag-chat-ui-store'

const MAX_ROWS_PER_BLOCK = 40
const MAX_STRING_LENGTH = 500
const MAX_ARTIFACT_TEXT_LENGTH = 180000

export type AskAiSourceType = 'query-history' | 'saved-query' | 'quick-insight' | 'report'

export interface AskAiArtifactSection {
  heading: string
  narrative?: string | null
  sql?: string | null
  rows?: Record<string, unknown>[]
  chartType?: string | null
  executionSuccess?: boolean | null
  executionError?: string | null
}

export interface AskAiArtifact {
  source: AskAiSourceType
  connectionId: string
  title: string
  prompt?: string | null
  sql?: string | null
  explanation?: string | null
  insight?: string | null
  rows?: Record<string, unknown>[]
  sections?: AskAiArtifactSection[]
  metadata?: Record<string, unknown>
}

function sanitizeFileToken(input: string): string {
  const safe = input
    .toLowerCase()
    .replace(/[^a-z0-9_-]+/g, '-')
    .replace(/-+/g, '-')
    .replace(/^-|-$/g, '')
  return safe || 'artifact'
}

function truncateString(value: string, max = MAX_STRING_LENGTH): string {
  if (value.length <= max) return value
  return `${value.slice(0, max)}...`
}

function normalizeValue(value: unknown): unknown {
  if (typeof value === 'string') return truncateString(value)
  if (Array.isArray(value)) return value.map((item) => normalizeValue(item))
  if (value && typeof value === 'object') {
    const output: Record<string, unknown> = {}
    for (const [key, item] of Object.entries(value)) {
      output[key] = normalizeValue(item)
    }
    return output
  }
  return value
}

function toJsonBlock(value: unknown): string {
  try {
    return JSON.stringify(value, null, 2)
  } catch {
    return String(value)
  }
}

function sampleRows(rows: Record<string, unknown>[] | undefined, maxRows = MAX_ROWS_PER_BLOCK): Record<string, unknown>[] {
  if (!rows || rows.length === 0) return []
  return rows.slice(0, maxRows).map((row) => normalizeValue(row) as Record<string, unknown>)
}

function appendTextSection(lines: string[], heading: string, value: string | null | undefined): void {
  if (!value || !value.trim()) return
  lines.push('')
  lines.push(`## ${heading}`)
  lines.push(value.trim())
}

function buildSectionsBlock(lines: string[], sections: AskAiArtifactSection[]): void {
  if (sections.length === 0) return

  lines.push('')
  lines.push('## Report Sections')

  sections.forEach((section, index) => {
    lines.push('')
    lines.push(`### Section ${index + 1}: ${section.heading}`)

    if (section.chartType) {
      lines.push(`Chart type: ${section.chartType}`)
    }

    if (section.executionSuccess === false && section.executionError) {
      lines.push(`Execution error: ${section.executionError}`)
    }

    if (section.narrative?.trim()) {
      lines.push('')
      lines.push('Narrative:')
      lines.push(section.narrative.trim())
    }

    if (section.sql?.trim()) {
      lines.push('')
      lines.push('SQL:')
      lines.push(section.sql.trim())
    }

    const sectionRows = sampleRows(section.rows)
    if (sectionRows.length > 0) {
      lines.push('')
      lines.push(`Sample rows (${sectionRows.length}${section.rows && section.rows.length > sectionRows.length ? ` of ${section.rows.length}` : ''}):`)
      lines.push(toJsonBlock(sectionRows))
    }
  })
}

function buildArtifactText(artifact: AskAiArtifact): string {
  const lines: string[] = []

  lines.push('SqlSpace Ask AI Context Artifact')
  lines.push(`Source: ${artifact.source}`)
  lines.push(`Title: ${artifact.title}`)
  lines.push(`ConnectionId: ${artifact.connectionId}`)

  if (artifact.metadata && Object.keys(artifact.metadata).length > 0) {
    lines.push('')
    lines.push('## Metadata')
    lines.push(toJsonBlock(normalizeValue(artifact.metadata)))
  }

  appendTextSection(lines, 'Prompt', artifact.prompt)
  appendTextSection(lines, 'SQL', artifact.sql)
  appendTextSection(lines, 'Explanation', artifact.explanation)
  appendTextSection(lines, 'Insight', artifact.insight)

  const rows = sampleRows(artifact.rows)
  if (rows.length > 0) {
    lines.push('')
    lines.push(`## Sample Rows (${rows.length}${artifact.rows && artifact.rows.length > rows.length ? ` of ${artifact.rows.length}` : ''})`)
    lines.push(toJsonBlock(rows))
  }

  if (artifact.sections && artifact.sections.length > 0) {
    buildSectionsBlock(lines, artifact.sections)
  }

  let text = lines.join('\n').trim()
  if (text.length > MAX_ARTIFACT_TEXT_LENGTH) {
    text = `${text.slice(0, MAX_ARTIFACT_TEXT_LENGTH)}\n\n[TRUNCATED]`
  }

  return text
}

function buildArtifactFile(artifact: AskAiArtifact): File {
  const timestamp = new Date().toISOString().replace(/[:.]/g, '-')
  const token = sanitizeFileToken(artifact.title)
  const fileName = `${artifact.source}-${token}-${timestamp}.txt`
  const text = buildArtifactText(artifact)
  return new File([text], fileName, { type: 'text/plain' })
}

export function parseRowsFromResultsJson(resultsJson: string | null | undefined, maxRows = MAX_ROWS_PER_BLOCK): Record<string, unknown>[] {
  if (!resultsJson) return []

  try {
    const parsed = JSON.parse(resultsJson) as unknown

    if (Array.isArray(parsed)) {
      return sampleRows(parsed.filter((item) => item && typeof item === 'object') as Record<string, unknown>[], maxRows)
    }

    if (parsed && typeof parsed === 'object') {
      const envelope = parsed as { columns?: unknown; rows?: unknown }
      if (Array.isArray(envelope.columns) && Array.isArray(envelope.rows)) {
        const columns = envelope.columns.map((value) => String(value))
        const mappedRows = (envelope.rows as unknown[])
          .filter((row) => Array.isArray(row))
          .map((row) => {
            const values = row as unknown[]
            const obj: Record<string, unknown> = {}
            columns.forEach((column, index) => {
              obj[column] = values[index]
            })
            return obj
          })

        return sampleRows(mappedRows, maxRows)
      }
    }

    return []
  } catch {
    return []
  }
}

export async function ingestArtifactForAskAi(artifact: AskAiArtifact): Promise<boolean> {
  const connectionState = useConnectionStore.getState()

  const isAdmin = await accessApi.isAdmin(artifact.connectionId)
  if (!isAdmin) {
    toast.error('Ask AI ingestion is admin-only for this connection')
    return false
  }

  try {
    const file = buildArtifactFile(artifact)
    const result = await knowledgeBaseApi.uploadDocument(artifact.connectionId, file, ['admin'])

    connectionState.setActiveConnection(artifact.connectionId)
    useRagChatUiStore.getState().openPopup()

    if (result.status === 'already_indexed') {
      toast.success(`Ask AI ready (reused context): ${result.fileName}`)
    } else {
      toast.success(`Ask AI ready: ${result.fileName}`)
    }
    return true
  } catch (error: unknown) {
    const message = error instanceof Error ? error.message : 'Failed to ingest Ask AI context'
    toast.error(message)
    return false
  }
}
