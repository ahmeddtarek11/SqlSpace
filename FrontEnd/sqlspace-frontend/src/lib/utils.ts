import { type ClassValue, clsx } from 'clsx'
import { twMerge } from 'tailwind-merge'

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}

export function formatMs(ms: number): string {
  if (ms < 1000) return `${parseFloat(ms.toFixed(2))}ms`
  return `${(ms / 1000).toFixed(2)}s`
}

export function formatNumber(n: number): string {
  return new Intl.NumberFormat().format(n)
}

export function formatDate(iso: string): string {
  return new Intl.DateTimeFormat('en-US', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(iso))
}

const SIMPLE_SQL_SERVER_IDENTIFIER = /^[A-Za-z_][A-Za-z0-9_$#]*$/

const SQL_CLAUSE_BREAKS = [
  'UNION ALL',
  'ORDER BY',
  'GROUP BY',
  'LEFT OUTER JOIN',
  'RIGHT OUTER JOIN',
  'FULL OUTER JOIN',
  'LEFT JOIN',
  'RIGHT JOIN',
  'FULL JOIN',
  'INNER JOIN',
  'CROSS JOIN',
  'OUTER JOIN',
  'SELECT',
  'FROM',
  'WHERE',
  'HAVING',
  'JOIN',
  'ON',
  'UNION',
  'LIMIT',
  'OFFSET',
  'WITH',
] as const

export function stripSimpleSqlServerBrackets(sql: string): string {
  return sql.replace(/\[([^\]\r\n]+)\]/g, (fullMatch, identifier: string) => {
    const trimmed = identifier.trim()
    return SIMPLE_SQL_SERVER_IDENTIFIER.test(trimmed) ? trimmed : fullMatch
  })
}

export function formatSqlForDisplay(sql: string): string {
  if (!sql) return ''

  const normalized = stripSimpleSqlServerBrackets(sql)
    .replace(/\r\n/g, '\n')
    .replace(/\n+/g, ' ')
    .replace(/\s{2,}/g, ' ')
    .trim()

  if (!normalized) return ''

  let pretty = normalized

  for (const clause of SQL_CLAUSE_BREAKS) {
    const clausePattern = clause.replace(/\s+/g, '\\s+')
    const atStart = new RegExp(`^${clausePattern}(?=\\s|$)`, 'i')
    const withLeadingWhitespace = new RegExp(`\\s+${clausePattern}(?=\\s|$)`, 'gi')
    pretty = pretty.replace(atStart, clause)
    pretty = pretty.replace(withLeadingWhitespace, `\n${clause}`)
  }

  return pretty.replace(/\n{2,}/g, '\n').trim()
}

export function formatSqlSingleLineForDisplay(sql: string): string {
  return formatSqlForDisplay(sql)
    .replace(/\s*\n+\s*/g, ' ')
    .replace(/\s{2,}/g, ' ')
    .trim()
}

export function truncate(str: string, max = 80): string {
  return str.length <= max ? str : str.slice(0, max) + '…'
}

export function timeAgo(iso: string): string {
  const seconds = Math.floor((Date.now() - new Date(iso).getTime()) / 1000)
  if (seconds < 60) return 'just now'
  const minutes = Math.floor(seconds / 60)
  if (minutes < 60) return `${minutes}m ago`
  const hours = Math.floor(minutes / 60)
  if (hours < 24) return `${hours}h ago`
  const days = Math.floor(hours / 24)
  if (days < 30) return `${days}d ago`
  const months = Math.floor(days / 30)
  return `${months}mo ago`
}

export type MetricSentiment = 'positive' | 'negative' | 'neutral'

export function extractHeroMetric(
  data: Record<string, unknown>[],
  columns: string[],
  insight?: string | null,
): { value: string; label: string; sentiment: MetricSentiment } {
  // Try extracting a key metric from insight text
  if (insight) {
    const pctMatch = insight.match(/([+-]?\d+(?:\.\d+)?)\s*%/)
    if (pctMatch) {
      const num = parseFloat(pctMatch[1])
      return {
        value: `${num >= 0 && !pctMatch[0].startsWith('-') ? '' : ''}${pctMatch[0].trim()}`,
        label: 'from insight',
        sentiment: num > 0 ? 'positive' : num < 0 ? 'negative' : 'neutral',
      }
    }
    const dollarMatch = insight.match(/\$[\d,]+(?:\.\d+)?/)
    if (dollarMatch) {
      return { value: dollarMatch[0], label: 'from insight', sentiment: 'neutral' }
    }
    const bigNumMatch = insight.match(/\b(\d{1,3}(?:,\d{3})+(?:\.\d+)?)\b/)
    if (bigNumMatch) {
      return { value: bigNumMatch[0], label: 'from insight', sentiment: 'neutral' }
    }
  }

  // Fall back to data: find first numeric column and aggregate
  if (data.length > 0 && columns.length > 0) {
    const numericCol = columns.find((col) => typeof data[0][col] === 'number')
    if (numericCol) {
      const values = data.map((row) => Number(row[numericCol]) || 0)
      const total = values.reduce((a, b) => a + b, 0)
      return {
        value: formatNumber(Math.round(total * 100) / 100),
        label: numericCol,
        sentiment: 'neutral',
      }
    }
  }

  // Last resort: row count
  return {
    value: data.length > 0 ? formatNumber(data.length) : '—',
    label: 'rows',
    sentiment: 'neutral',
  }
}
