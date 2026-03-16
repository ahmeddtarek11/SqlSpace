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
    <div className="flex items-center gap-1.5 px-2 py-0.5 rounded hover:bg-(--bg-elevated) group">
      {/* PK / FK / plain icon */}
      {col.isPrimaryKey ? (
        <KeyRound className="w-3 h-3 text-amber-400 shrink-0" />
      ) : col.foreignKeyName ? (
        <Link className="w-3 h-3 text-blue-400 shrink-0" />
      ) : (
        <span className="w-3 h-3 shrink-0 flex items-center justify-center">
          <span
            className={cn(
              'w-1.5 h-1.5 rounded-full',
              col.isNullable ? 'border border-(--border-strong)' : 'bg-(--text-muted)'
            )}
          />
        </span>
      )}

      <span className="text-xs text-(--text-secondary) truncate flex-1 font-mono">{col.name}</span>

      <span className="text-[10px] text-(--text-muted) shrink-0 font-mono opacity-0 group-hover:opacity-100 transition-opacity">
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
        className="w-full flex items-center gap-1.5 px-2 py-1 rounded hover:bg-(--bg-elevated) text-left group"
      >
        <ChevronRight
          className={cn(
            'w-3 h-3 text-(--text-muted) shrink-0 transition-transform',
            expanded && 'rotate-90'
          )}
        />
        <Table2 className="w-3.5 h-3.5 text-violet-400 shrink-0" />
        <span className="text-xs text-(--text-primary) truncate flex-1">{table.name}</span>
        <span className="text-[10px] text-(--text-muted) opacity-0 group-hover:opacity-100 transition-opacity shrink-0">
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
        className="w-full flex items-center gap-1.5 px-2 py-1 rounded hover:bg-(--bg-elevated) text-left"
      >
        <ChevronRight
          className={cn(
            'w-3 h-3 text-(--text-muted) shrink-0 transition-transform',
            expanded && 'rotate-90'
          )}
        />
        <Database className="w-3 h-3 text-(--text-muted) shrink-0" />
        <span className="text-xs font-medium text-(--text-muted) uppercase tracking-wider truncate">
          {schemaName}
        </span>
        <span className="text-[10px] text-(--text-muted) ml-auto shrink-0">{tables.length}</span>
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

  // Group tables by schema name
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
      <aside className="w-8 shrink-0 flex flex-col items-center pt-3 border-l border-(--border-default) bg-(--bg-surface)">
        <button
          onClick={() => setIsOpen(true)}
          title="Show schema"
          className="p-1 rounded hover:bg-(--bg-elevated) text-(--text-muted) hover:text-violet-400 transition-colors"
        >
          <PanelRight className="w-4 h-4" />
        </button>
      </aside>
    )
  }

  return (
    <aside className="w-64 shrink-0 flex flex-col border-l border-(--border-default) bg-(--bg-surface)">
      {/* Header */}
      <div className="flex items-center justify-between px-3 py-3 border-b border-(--border-default) shrink-0">
        <span className="text-xs font-medium text-(--text-muted) uppercase tracking-wider">Schema</span>
        <button
          onClick={() => setIsOpen(false)}
          title="Hide schema"
          className="p-1 rounded hover:bg-(--bg-elevated) text-(--text-muted) hover:text-violet-400 transition-colors"
        >
          <PanelRightClose className="w-3.5 h-3.5" />
        </button>
      </div>

      {/* Database name badge */}
      {schema?.database && (
        <div className="px-3 py-2 border-b border-(--border-default) shrink-0">
          <span className="text-xs text-(--text-muted) font-mono">{schema.database}</span>
        </div>
      )}

      {/* Tree */}
      <div className="flex-1 min-h-0 overflow-y-auto px-2 py-2">
        {!activeConnectionId ? (
          <p className="text-xs text-(--text-muted) text-center py-6">Select a connection</p>
        ) : isLoading ? (
          <div className="space-y-2 px-1">
            {[1, 2, 3, 4].map((i) => (
              <Skeleton key={i} className="h-6 w-full rounded bg-(--bg-elevated)" />
            ))}
          </div>
        ) : !schema || Object.keys(grouped).length === 0 ? (
          <p className="text-xs text-(--text-muted) text-center py-6">No schema available</p>
        ) : (
          Object.entries(grouped).map(([schemaName, tables]) => (
            <SchemaGroup key={schemaName} schemaName={schemaName} tables={tables} />
          ))
        )}
      </div>

      {/* Visualize button */}
      {schema && schema.tables.length > 0 && (
        <div className="shrink-0 px-3 py-3 border-t border-(--border-default)">
          <button
            onClick={() => setVisualizerOpen(true)}
            className="w-full flex items-center justify-center gap-2 px-3 py-2 rounded-lg text-xs font-medium text-violet-400 border border-violet-500/30 bg-violet-600/10 hover:bg-violet-600/20 hover:text-violet-300 transition-colors"
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
