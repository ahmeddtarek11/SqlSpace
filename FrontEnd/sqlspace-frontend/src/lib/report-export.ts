import { jsPDF } from 'jspdf'
import autoTable from 'jspdf-autotable'
import type { ReportDto } from '@/types'

const MAX_PREVIEW_ROWS_PER_SECTION = 200

function sanitizeFileName(raw: string): string {
  const cleaned = raw
    .trim()
    .replace(/[\\/:*?"<>|]+/g, '-')
    .replace(/\s+/g, '-')
    .replace(/-+/g, '-')
    .replace(/^-|-$/g, '')

  return cleaned || 'report'
}

function timeStampForFile(now = new Date()): string {
  const yyyy = String(now.getFullYear())
  const mm = String(now.getMonth() + 1).padStart(2, '0')
  const dd = String(now.getDate()).padStart(2, '0')
  const hh = String(now.getHours()).padStart(2, '0')
  const min = String(now.getMinutes()).padStart(2, '0')
  return `${yyyy}${mm}${dd}-${hh}${min}`
}

function normalizeValue(value: unknown): string {
  if (value === null || value === undefined) return '—'
  if (typeof value === 'string') return value
  if (typeof value === 'number' || typeof value === 'boolean') return String(value)
  try {
    return JSON.stringify(value)
  } catch {
    return String(value)
  }
}

function parseResultsJson(json: string | null): Record<string, unknown>[] {
  if (!json) return []

  try {
    const parsed = JSON.parse(json)

    if (Array.isArray(parsed)) {
      return parsed.filter((item): item is Record<string, unknown> => typeof item === 'object' && item !== null)
    }

    if (
      typeof parsed === 'object' &&
      parsed !== null &&
      'columns' in parsed &&
      'rows' in parsed &&
      Array.isArray((parsed as { columns?: unknown }).columns) &&
      Array.isArray((parsed as { rows?: unknown }).rows)
    ) {
      const cols = (parsed as { columns: string[] }).columns
      const rawRows = (parsed as { rows: unknown[] }).rows

      return rawRows
        .filter(Array.isArray)
        .map((row) => {
          const mapped: Record<string, unknown> = {}
          cols.forEach((col, i) => {
            mapped[col] = (row as unknown[])[i]
          })
          return mapped
        })
    }

    return []
  } catch {
    return []
  }
}

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

function collectRenderedChartImages(report: ReportDto, reportRoot?: HTMLElement | null): Record<string, string> {
  const root = reportRoot ?? (document.querySelector('[data-report-view-root="true"]') as HTMLElement | null)
  if (!root) return {}

  const result: Record<string, string> = {}

  for (const section of report.sections) {
    const sectionNode = root.querySelector(`[data-report-section-id="${section.sectionId}"]`) as HTMLElement | null
    if (!sectionNode) continue

    const chartContainer = sectionNode.querySelector('[data-report-chart="true"]') as HTMLElement | null
    const canvas = (chartContainer ?? sectionNode).querySelector('canvas') as HTMLCanvasElement | null
    if (!canvas || canvas.width === 0 || canvas.height === 0) continue

    try {
      result[section.sectionId] = canvas.toDataURL('image/png')
    } catch {
      // Continue exporting even if a chart image cannot be captured.
    }
  }

  return result
}

function ensurePageSpace(
  doc: jsPDF,
  y: number,
  requiredHeight: number,
  marginTop: number,
  marginBottom: number,
): number {
  const pageHeight = doc.internal.pageSize.getHeight()
  if (y + requiredHeight <= pageHeight - marginBottom) {
    return y
  }
  doc.addPage()
  return marginTop
}

function drawWrappedText(
  doc: jsPDF,
  text: string,
  x: number,
  y: number,
  maxWidth: number,
  lineHeight: number,
): number {
  const lines = doc.splitTextToSize(text, maxWidth) as string[]
  let cursorY = y
  for (const line of lines) {
    doc.text(line, x, cursorY)
    cursorY += lineHeight
  }
  return cursorY
}

export function exportReportAsPdf(report: ReportDto): void {
  const doc = new jsPDF({
    orientation: 'p',
    unit: 'mm',
    format: 'a4',
    compress: true,
  })

  const marginX = 12
  const marginTop = 14
  const marginBottom = 12
  const pageWidth = doc.internal.pageSize.getWidth()
  const contentWidth = pageWidth - marginX * 2

  const chartImages = collectRenderedChartImages(report)
  let y = marginTop

  doc.setFont('helvetica', 'bold')
  doc.setFontSize(18)
  doc.setTextColor(17, 24, 39)
  y = drawWrappedText(doc, report.title || 'Report', marginX, y, contentWidth, 7)

  if (report.summary?.trim()) {
    y += 1
    doc.setFont('helvetica', 'normal')
    doc.setFontSize(10.5)
    doc.setTextColor(55, 65, 81)
    y = drawWrappedText(doc, report.summary.trim(), marginX, y, contentWidth, 4.8)
  }

  y += 2
  doc.setFont('helvetica', 'normal')
  doc.setFontSize(8.5)
  doc.setTextColor(107, 114, 128)
  const created = new Date(report.createdAtUtc).toLocaleString()
  const updated = new Date(report.updatedAtUtc).toLocaleString()
  y = drawWrappedText(doc, `Created: ${created}`, marginX, y, contentWidth, 4)
  y = drawWrappedText(doc, `Updated: ${updated}`, marginX, y, contentWidth, 4)
  y += 1

  const sortedSections = [...report.sections].sort((a, b) => a.sortOrder - b.sortOrder)

  sortedSections.forEach((section, sectionIndex) => {
    y = ensurePageSpace(doc, y, 12, marginTop, marginBottom)

    doc.setDrawColor(229, 231, 235)
    doc.setLineWidth(0.2)
    doc.line(marginX, y, pageWidth - marginX, y)
    y += 4.5

    doc.setFont('helvetica', 'bold')
    doc.setFontSize(12)
    doc.setTextColor(31, 41, 55)
    y = drawWrappedText(doc, `${sectionIndex + 1}. ${section.heading}`, marginX, y, contentWidth, 5.2)

    const narrative = normalizeNarrativeText(section.narrativeText ?? '', section.heading)
    if (narrative) {
      y += 1
      y = ensurePageSpace(doc, y, 8, marginTop, marginBottom)
      doc.setFont('helvetica', 'normal')
      doc.setFontSize(10)
      doc.setTextColor(55, 65, 81)
      y = drawWrappedText(doc, narrative, marginX, y, contentWidth, 4.8)
      y += 1
    }

    const chartImage = chartImages[section.sectionId]
    if (chartImage) {
      const imageProps = doc.getImageProperties(chartImage)
      const imageWidth = contentWidth - 2
      const naturalHeight = imageWidth * (imageProps.height / imageProps.width)
      const imageHeight = Math.max(45, Math.min(115, naturalHeight))

      y = ensurePageSpace(doc, y, imageHeight + 8, marginTop, marginBottom)
      doc.setFillColor(17, 17, 19)
      doc.roundedRect(marginX, y, contentWidth, imageHeight + 2, 1.5, 1.5, 'F')
      doc.addImage(chartImage, 'PNG', marginX + 1, y + 1, imageWidth, imageHeight, undefined, 'FAST')
      y += imageHeight + 5
    }

    if (section.executionSuccess === false) {
      const errorText = section.executionErrorMessage?.trim() || 'SQL execution failed'
      y = ensurePageSpace(doc, y, 10, marginTop, marginBottom)
      doc.setFillColor(254, 242, 242)
      doc.setDrawColor(254, 202, 202)
      doc.roundedRect(marginX, y, contentWidth, 8, 1.2, 1.2, 'FD')
      doc.setFont('helvetica', 'normal')
      doc.setFontSize(8.8)
      doc.setTextColor(153, 27, 27)
      doc.text(`Execution error: ${errorText}`, marginX + 1.8, y + 5.1)
      y += 10
    }

    const rows = parseResultsJson(section.resultsJson)
    if (rows.length === 0) {
      y = ensurePageSpace(doc, y, 6, marginTop, marginBottom)
      doc.setFont('helvetica', 'italic')
      doc.setFontSize(9)
      doc.setTextColor(107, 114, 128)
      doc.text('No result rows.', marginX, y)
      y += 5
      return
    }

    const previewRows = rows.slice(0, MAX_PREVIEW_ROWS_PER_SECTION)
    const columns = Array.from(new Set(previewRows.flatMap((row) => Object.keys(row))))

    if (columns.length === 0) {
      y = ensurePageSpace(doc, y, 6, marginTop, marginBottom)
      doc.setFont('helvetica', 'italic')
      doc.setFontSize(9)
      doc.setTextColor(107, 114, 128)
      doc.text('No tabular rows.', marginX, y)
      y += 5
      return
    }

    autoTable(doc, {
      startY: y,
      margin: { left: marginX, right: marginX },
      head: [columns],
      body: previewRows.map((row) => columns.map((col) => normalizeValue(row[col]))),
      theme: 'grid',
      styles: {
        fontSize: 7,
        cellPadding: 1.2,
        overflow: 'linebreak',
        textColor: [17, 24, 39],
      },
      headStyles: {
        fillColor: [243, 244, 246],
        textColor: [17, 24, 39],
        fontStyle: 'bold',
      },
      alternateRowStyles: {
        fillColor: [250, 250, 250],
      },
      tableLineColor: [229, 231, 235],
      tableLineWidth: 0.1,
    })

    const lastAutoTable = (doc as jsPDF & { lastAutoTable?: { finalY?: number } }).lastAutoTable
    y = (lastAutoTable?.finalY ?? y) + 3

    if (rows.length > MAX_PREVIEW_ROWS_PER_SECTION) {
      y = ensurePageSpace(doc, y, 6, marginTop, marginBottom)
      doc.setFont('helvetica', 'italic')
      doc.setFontSize(8.5)
      doc.setTextColor(107, 114, 128)
      doc.text(
        `Showing first ${MAX_PREVIEW_ROWS_PER_SECTION.toLocaleString()} of ${rows.length.toLocaleString()} rows.`,
        marginX,
        y,
      )
      y += 5
    }
  })

  const fileName = `${sanitizeFileName(report.title)}-${timeStampForFile()}.pdf`
  doc.save(fileName)
}
