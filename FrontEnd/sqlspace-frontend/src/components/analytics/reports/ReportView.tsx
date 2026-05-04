import { AlertCircle, BarChart3, Clock } from 'lucide-react'
import { ChartRenderer } from '@/components/analytics/ChartRenderer'
import type { ReportDraftDto, ReportDto, ReportSectionDto, ChartConfig, ChartType } from '@/types'
import { formatMs } from '@/lib/utils'

interface ReportViewProps {
  report: ReportDraftDto | ReportDto
}

const WEB_TABLE_PREVIEW_ROWS = 75

function extractNarrativeFromJson(raw: string, depth = 0): string | null {
  if (depth > 2) return null

  try {
    const parsed: unknown = JSON.parse(raw)

    if (typeof parsed === 'string') {
      return extractNarrativeFromJson(parsed, depth + 1)
    }

    if (typeof parsed === 'object' && parsed !== null) {
      const narrative = (parsed as { narrative?: unknown }).narrative
      if (typeof narrative === 'string' && narrative.trim()) {
        return narrative.trim()
      }
    }
  } catch {
    return null
  }

  return null
}

function normalizeNarrativeText(raw: string, heading: string): string {
  const trimmed = raw.trim()
  if (!trimmed) return ''

  const fullJsonNarrative = extractNarrativeFromJson(trimmed)
  if (fullJsonNarrative) return fullJsonNarrative

  const firstBrace = trimmed.indexOf('{')
  const lastBrace = trimmed.lastIndexOf('}')
  if (firstBrace >= 0 && lastBrace > firstBrace) {
    const jsonPart = trimmed.slice(firstBrace, lastBrace + 1)
    const jsonNarrative = extractNarrativeFromJson(jsonPart)
    if (jsonNarrative) {
      const prefix = trimmed.slice(0, firstBrace).trim()
      const normalizedPrefix = prefix.toLowerCase()
      const normalizedHeading = heading.trim().toLowerCase()
      if (!prefix || normalizedPrefix === normalizedHeading) {
        return jsonNarrative
      }
      return `${prefix}\n\n${jsonNarrative}`
    }
  }

  return trimmed.replace(/^narrative\s*:\s*/i, '').trim()
}

function parseResultsJson(json: string | null): Record<string, unknown>[] {
  if (!json) return []
  try {
    const parsed = JSON.parse(json)
    if (Array.isArray(parsed)) return parsed
    if (parsed.columns && parsed.rows) {
      const cols: string[] = parsed.columns
      return (parsed.rows as unknown[][]).map((row) => {
        const obj: Record<string, unknown> = {}
        cols.forEach((col, i) => { obj[col] = row[i] })
        return obj
      })
    }
    return []
  } catch {
    return []
  }
}

function safeParseConfig(json: string | null | undefined): ChartConfig {
  if (!json) return {}
  try { return JSON.parse(json) as ChartConfig } catch { return {} }
}

function formatCellValue(value: unknown): string {
  if (value === null || value === undefined) return '—'
  if (typeof value === 'string') return value
  if (typeof value === 'number' || typeof value === 'boolean') return String(value)
  try {
    return JSON.stringify(value)
  } catch {
    return String(value)
  }
}

function formatReportTimestamp(timestamp: string | null | undefined): string {
  if (!timestamp) return ''
  const parsed = new Date(timestamp)
  if (Number.isNaN(parsed.getTime())) return ''

  return parsed.toLocaleString(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short',
  })
}

export function ReportView({ report }: ReportViewProps) {
  const title = report.title
  const summary = report.summary
  const generatedAtUtc = 'generatedAtUtc' in report ? report.generatedAtUtc : report.createdAtUtc
  const generatedLabel = formatReportTimestamp(generatedAtUtc)
  const updatedLabel = 'updatedAtUtc' in report ? formatReportTimestamp(report.updatedAtUtc) : ''
  const showUpdatedLabel =
    !!updatedLabel &&
    'updatedAtUtc' in report &&
    report.updatedAtUtc !== report.createdAtUtc

  return (
    <div className="h-full overflow-y-auto" data-report-view-root="true">
      <div className="max-w-4xl mx-auto px-6 py-8 space-y-8">
        {/* Report header */}
        <div>
          <h1 className="text-2xl font-bold text-white tracking-tight">{title}</h1>
          {generatedLabel && (
            <div className="mt-2 flex flex-wrap gap-x-4 gap-y-1 text-xs text-zinc-500">
              <span>Generated: {generatedLabel}</span>
              {showUpdatedLabel && <span>Updated: {updatedLabel}</span>}
            </div>
          )}
          {summary && (
            <p className="mt-2 text-zinc-400 text-sm leading-relaxed">{summary}</p>
          )}
        </div>

        {/* Sections */}
        {report.sections.map((section) => (
          <ReportSectionBlock key={section.sectionId} section={section} />
        ))}
      </div>
    </div>
  )
}

function ReportSectionBlock({ section }: { section: ReportSectionDto }) {
  const rows = parseResultsJson(section.resultsJson)
  const config = safeParseConfig(section.chartConfigJson)
  const narrativeText = normalizeNarrativeText(section.narrativeText ?? '', section.heading)
  const isSqlBackedSection = !!section.sqlQuery
  const hasDataRows = rows.length > 0
  const hasChart = !!section.chartType && rows.length > 0
  const hasError = section.executionSuccess === false
  const showNarrative = !!narrativeText && (!isSqlBackedSection || (!hasError && hasDataRows))
  const tableRows = rows.slice(0, WEB_TABLE_PREVIEW_ROWS)
  const tableColumns = tableRows.length > 0
    ? Array.from(new Set(tableRows.flatMap((row) => Object.keys(row))))
    : []
  const showDataTable = tableColumns.length > 0

  return (
    <section className="space-y-3" data-report-section-id={section.sectionId}>
      <h2 className="text-base font-semibold text-zinc-200 border-b border-white/10 pb-2">
        {section.heading}
      </h2>

      {/* Narrative */}
      {showNarrative && (
        <div className="text-sm text-zinc-300 leading-relaxed whitespace-pre-line">
          {narrativeText}
        </div>
      )}

      {/* Chart */}
      {hasChart && (
        <div className="rounded-xl border border-white/10 bg-[#111113] p-4" data-report-chart="true">
          <div className="h-64">
            <ChartRenderer
              chartType={section.chartType as ChartType}
              config={config}
              data={rows}
            />
          </div>
          {section.rowsReturned != null && section.executionTimeMs != null && (
            <div className="flex items-center gap-3 mt-2 text-[10px] text-zinc-600 uppercase tracking-wider">
              <span>{section.rowsReturned.toLocaleString()} rows</span>
              <span>·</span>
              <span className="flex items-center gap-1">
                <Clock className="w-2.5 h-2.5" />
                {formatMs(section.executionTimeMs)}
              </span>
            </div>
          )}

          {showDataTable && (
            <div className="mt-4 rounded-lg border border-white/10 overflow-hidden">
              <div className="max-h-72 overflow-auto">
                <table className="min-w-full text-xs">
                  <thead className="sticky top-0 z-10 bg-[#18181b] text-zinc-300">
                    <tr>
                      {tableColumns.map((column) => (
                        <th
                          key={column}
                          className="px-3 py-2 text-left font-medium border-b border-white/10 whitespace-nowrap"
                        >
                          {column}
                        </th>
                      ))}
                    </tr>
                  </thead>
                  <tbody>
                    {tableRows.map((row, rowIndex) => (
                      <tr key={rowIndex} className="odd:bg-[#121214] even:bg-[#101012]">
                        {tableColumns.map((column) => (
                          <td
                            key={column}
                            className="px-3 py-2 border-b border-white/5 text-zinc-400 align-top"
                          >
                            {formatCellValue(row[column])}
                          </td>
                        ))}
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              {rows.length > WEB_TABLE_PREVIEW_ROWS && (
                <div className="px-3 py-2 text-[10px] text-zinc-600 border-t border-white/10 bg-[#0f0f11]">
                  Showing first {WEB_TABLE_PREVIEW_ROWS.toLocaleString()} of {rows.length.toLocaleString()} rows
                </div>
              )}
            </div>
          )}
        </div>
      )}

      {/* No chart — show empty data state */}
      {!hasChart && !hasError && isSqlBackedSection && !hasDataRows && (
        <div className="rounded-lg border border-white/5 bg-white/2 px-4 py-3 flex items-center gap-2 text-xs text-zinc-500">
          <BarChart3 className="w-3.5 h-3.5" />
          Query returned no data
        </div>
      )}

      {/* Execution error */}
      {hasError && (
        <div className="rounded-lg border border-red-500/20 bg-red-500/5 px-4 py-3 flex items-start gap-2 text-xs text-red-300">
          <AlertCircle className="w-3.5 h-3.5 mt-0.5 shrink-0" />
          <span>{section.executionErrorMessage ?? 'SQL execution failed'}</span>
        </div>
      )}
    </section>
  )
}
