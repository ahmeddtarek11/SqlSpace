import { useState, useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { AnimatePresence, motion } from 'framer-motion'
import {
  ChevronRight,
  Table2,
  KeyRound,
  Link,
  PanelRightClose,
  PanelRight,
  Database,
  Network,
} from 'lucide-react'
import { Skeleton } from '@/components/ui/skeleton'
import { connectionsApi } from '@/api/connections'
import { useConnectionStore } from '@/stores/connection-store'
import { cn } from '@/lib/utils'
import type { SchemaColumn, SchemaTable } from '@/types'
import { SchemaVisualizer } from './SchemaVisualizer'

// ── Column row ────────────────────────────────────────────────

function ColumnRow({ col }: { col: SchemaColumn }) {
  return (
    <div
      className="flex items-center gap-1.5 px-2 py-1 group transition-colors"
      style={{ borderRadius: 'var(--radius-sm)' }}
      onMouseEnter={(e) => { e.currentTarget.style.background = 'var(--bg-hover)' }}
      onMouseLeave={(e) => { e.currentTarget.style.background = 'transparent' }}
    >
      {col.isPrimaryKey ? (
        <KeyRound className="w-3 h-3 shrink-0" style={{ color: 'var(--warning)' }} />
      ) : col.foreignKeyName ? (
        <Link className="w-3 h-3 shrink-0" style={{ color: 'var(--accent)' }} />
      ) : (
        <span className="w-3 h-3 shrink-0 flex items-center justify-center">
          <span
            className="w-1.5 h-1.5"
            style={{
              borderRadius: 'var(--radius-pill)',
              background: col.isNullable ? 'transparent' : 'var(--text-muted)',
              border: col.isNullable ? '1px solid var(--border-strong)' : 'none',
            }}
          />
        </span>
      )}

      <span className="text-[12px] truncate flex-1" style={{ fontFamily: 'var(--font-mono)', color: 'var(--text-secondary)' }}>
        {col.name}
      </span>

      <span
        className="text-[10px] shrink-0 opacity-0 group-hover:opacity-100 transition-opacity"
        style={{ fontFamily: 'var(--font-mono)', color: 'var(--text-muted)' }}
      >
        {col.dataType}
        {col.maxLength ? `(${col.maxLength})` : ''}
      </span>
    </div>
  )
}

// ── Table node ────────────────────────────────────────────────

function TableNode({ table }: { table: SchemaTable }) {
  const [expanded, setExpanded] = useState(false)

  return (
    <div>
      <button
        onClick={() => setExpanded((v) => !v)}
        className="w-full flex items-center gap-1.5 px-2 py-1.5 text-left group transition-colors"
        style={{ borderRadius: 'var(--radius-sm)' }}
        onMouseEnter={(e) => { e.currentTarget.style.background = 'var(--bg-hover)' }}
        onMouseLeave={(e) => { e.currentTarget.style.background = 'transparent' }}
      >
        <ChevronRight
          className={cn(
            'w-3 h-3 shrink-0 transition-transform',
            expanded && 'rotate-90'
          )}
          style={{ color: 'var(--text-muted)' }}
        />
        <Table2 className="w-3.5 h-3.5 shrink-0" style={{ color: 'var(--accent)' }} />
        <span className="text-[12px] truncate flex-1" style={{ color: 'var(--text-primary)' }}>{table.name}</span>
        <span
          className="text-[10px] opacity-0 group-hover:opacity-100 transition-opacity shrink-0"
          style={{ color: 'var(--text-muted)' }}
        >
          {table.columns.length}
        </span>
      </button>

      <AnimatePresence initial={false}>
        {expanded && (
          <motion.div
            key="cols"
            initial={{ height: 0, opacity: 0 }}
            animate={{ height: 'auto', opacity: 1 }}
            exit={{ height: 0, opacity: 0 }}
            transition={{ duration: 0.15 }}
            className="overflow-hidden pl-4"
          >
            {table.columns.map((col) => (
              <ColumnRow key={col.name} col={col} />
            ))}
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  )
}

// ── Schema group ──────────────────────────────────────────────

function SchemaGroup({ schemaName, tables }: { schemaName: string; tables: SchemaTable[] }) {
  const [expanded, setExpanded] = useState(true)

  return (
    <div className="mb-1">
      <button
        onClick={() => setExpanded((v) => !v)}
        className="w-full flex items-center gap-1.5 px-2 py-1.5 text-left transition-colors"
        style={{ borderRadius: 'var(--radius-sm)' }}
        onMouseEnter={(e) => { e.currentTarget.style.background = 'var(--bg-hover)' }}
        onMouseLeave={(e) => { e.currentTarget.style.background = 'transparent' }}
      >
        <ChevronRight
          className={cn(
            'w-3 h-3 shrink-0 transition-transform',
            expanded && 'rotate-90'
          )}
          style={{ color: 'var(--text-muted)' }}
        />
        <Database className="w-3 h-3 shrink-0" style={{ color: 'var(--text-muted)' }} />
        <span className="text-[11px] font-semibold uppercase tracking-wider truncate" style={{ color: 'var(--text-tertiary)' }}>
          {schemaName}
        </span>
        <span className="text-[10px] ml-auto shrink-0" style={{ color: 'var(--text-muted)' }}>{tables.length}</span>
      </button>

      <AnimatePresence initial={false}>
        {expanded && (
          <motion.div
            key="tables"
            initial={{ height: 0, opacity: 0 }}
            animate={{ height: 'auto', opacity: 1 }}
            exit={{ height: 0, opacity: 0 }}
            transition={{ duration: 0.15 }}
            className="overflow-hidden pl-2"
          >
            {tables.map((t) => (
              <TableNode key={`${t.schema}.${t.name}`} table={t} />
            ))}
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  )
}

// ── SchemaPanel ───────────────────────────────────────────────

export function SchemaPanel() {
  const [isOpen, setIsOpen] = useState(true)
  const [visualizerOpen, setVisualizerOpen] = useState(false)
  const activeConnectionId = useConnectionStore((s) => s.activeConnectionId)

  const { data: schema, isLoading } = useQuery({
    queryKey: ['schema', activeConnectionId],
    queryFn: () => connectionsApi.schema(activeConnectionId!),
    enabled: !!activeConnectionId,
  })

  const grouped = useMemo(() => {
    if (!schema?.tables) return {}
    return schema.tables.reduce<Record<string, SchemaTable[]>>((acc, t) => {
      const key = t.schema ?? 'default'
      if (!acc[key]) acc[key] = []
      acc[key].push(t)
      return acc
    }, {})
  }, [schema])

  // Collapsed strip
  if (!isOpen) {
    return (
      <aside
        className="w-8 shrink-0 flex flex-col items-center pt-3"
        style={{
          background: 'var(--bg-surface)',
          borderLeft: '1px solid var(--border-subtle)',
        }}
      >
        <button
          onClick={() => setIsOpen(true)}
          title="Show schema"
          className="p-1 transition-colors"
          style={{ borderRadius: 'var(--radius-sm)', color: 'var(--text-tertiary)' }}
          onMouseEnter={(e) => {
            e.currentTarget.style.color = 'var(--accent)'
            e.currentTarget.style.background = 'var(--bg-hover)'
          }}
          onMouseLeave={(e) => {
            e.currentTarget.style.color = 'var(--text-tertiary)'
            e.currentTarget.style.background = 'transparent'
          }}
        >
          <PanelRight className="w-4 h-4" />
        </button>
      </aside>
    )
  }

  return (
    <aside
      className="w-64 shrink-0 flex flex-col"
      style={{
        background: 'var(--bg-surface)',
        borderLeft: '1px solid var(--border-subtle)',
      }}
    >
      {/* Header */}
      <div
        className="flex items-center justify-between px-4 py-3 shrink-0"
        style={{ borderBottom: '1px solid var(--border-subtle)' }}
      >
        <span className="text-[11px] font-semibold uppercase tracking-wider" style={{ color: 'var(--text-tertiary)' }}>
          Schema
        </span>
        <button
          onClick={() => setIsOpen(false)}
          title="Hide schema"
          className="p-1 transition-colors"
          style={{ borderRadius: 'var(--radius-sm)', color: 'var(--text-tertiary)' }}
          onMouseEnter={(e) => {
            e.currentTarget.style.color = 'var(--accent)'
            e.currentTarget.style.background = 'var(--bg-hover)'
          }}
          onMouseLeave={(e) => {
            e.currentTarget.style.color = 'var(--text-tertiary)'
            e.currentTarget.style.background = 'transparent'
          }}
        >
          <PanelRightClose className="w-3.5 h-3.5" />
        </button>
      </div>

      {/* Database name badge */}
      {schema?.database && (
        <div className="px-4 py-2 shrink-0" style={{ borderBottom: '1px solid var(--border-subtle)' }}>
          <span className="text-[11px]" style={{ fontFamily: 'var(--font-mono)', color: 'var(--text-muted)' }}>
            {schema.database}
          </span>
        </div>
      )}

      {/* Tree */}
      <div className="flex-1 min-h-0 overflow-y-auto px-2 py-2">
        {!activeConnectionId ? (
          <p className="text-[12px] text-center py-6" style={{ color: 'var(--text-muted)' }}>Select a connection</p>
        ) : isLoading ? (
          <div className="space-y-2 px-1">
            {[1, 2, 3, 4].map((i) => (
              <Skeleton key={i} className="h-6 w-full" style={{ borderRadius: 'var(--radius-sm)', background: 'var(--bg-hover)' }} />
            ))}
          </div>
        ) : !schema || Object.keys(grouped).length === 0 ? (
          <p className="text-[12px] text-center py-6" style={{ color: 'var(--text-muted)' }}>No schema available</p>
        ) : (
          Object.entries(grouped).map(([schemaName, tables]) => (
            <SchemaGroup key={schemaName} schemaName={schemaName} tables={tables} />
          ))
        )}
      </div>

      {/* Visualize button */}
      {schema && schema.tables.length > 0 && (
        <div className="shrink-0 px-3 py-3" style={{ borderTop: '1px solid var(--border-subtle)' }}>
          <button
            onClick={() => setVisualizerOpen(true)}
            className="w-full flex items-center justify-center gap-2 px-3 py-2.5 text-[12px] font-semibold transition-all"
            style={{
              borderRadius: 'var(--radius-md)',
              color: 'var(--accent)',
              background: 'var(--accent-subtle)',
              border: '1px solid rgba(77, 104, 235, 0.15)',
            }}
            onMouseEnter={(e) => {
              e.currentTarget.style.background = 'rgba(77, 104, 235, 0.18)'
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.background = 'var(--accent-subtle)'
            }}
          >
            <Network className="w-3.5 h-3.5" />
            Visualize Schema
          </button>
        </div>
      )}

      {visualizerOpen && schema && (
        <SchemaVisualizer schema={schema} onClose={() => setVisualizerOpen(false)} />
      )}
    </aside>
  )
}
