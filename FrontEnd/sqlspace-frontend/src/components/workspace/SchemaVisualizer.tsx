import { useMemo } from 'react'
import {
  ReactFlow,
  Background,
  Controls,
  MiniMap,
  Handle,
  Position,
  useNodesState,
  useEdgesState,
  MarkerType,
  BackgroundVariant,
  type NodeProps,
  type Node,
  type Edge,
} from '@xyflow/react'
import '@xyflow/react/dist/style.css'
import { X, KeyRound, Link, Table2 } from 'lucide-react'
import type { ParsedSchema, SchemaTable } from '@/types'
import { useThemeStore } from '@/stores/theme-store'

// ── Dimensions ─────────────────────────────────────────────────────────────────
const TABLE_WIDTH = 240
const HEADER_H = 52
const COL_H = 26
const PADDING_B = 6
const COLS_PER_ROW = 4
const H_GAP = 60
const V_GAP = 50

function tableHeight(t: SchemaTable) {
  return HEADER_H + t.columns.length * COL_H + PADDING_B
}

// ── Custom table node ──────────────────────────────────────────────────────────
type ERNodeData = { table: SchemaTable; isDark: boolean } & Record<string, unknown>

function ERTableNode({ data }: NodeProps<Node<ERNodeData>>) {
  const { table, isDark } = data

  return (
    <div
      style={{
        width: TABLE_WIDTH,
        borderRadius: 12,
        overflow: 'hidden',
        border: isDark ? '1px solid rgba(14,165,233,0.3)' : '1px solid rgba(14,165,233,0.4)',
        background: isDark ? '#0c0c12' : '#ffffff',
        boxShadow: isDark
          ? '0 4px 24px rgba(14,165,233,0.15)'
          : '0 4px 16px rgba(14,165,233,0.08)',
      }}
    >
      <Handle
        type="target"
        position={Position.Left}
        style={{ background: '#38bdf8', border: '2px solid #0284c7', width: 8, height: 8 }}
      />
      <Handle
        type="source"
        position={Position.Right}
        style={{ background: '#38bdf8', border: '2px solid #0284c7', width: 8, height: 8 }}
      />

      {/* Header */}
      <div
        className="px-3 pt-2 pb-1.5"
        style={{
          background: isDark
            ? 'linear-gradient(135deg, #0c2a4a 0%, #082f49 100%)'
            : 'linear-gradient(135deg, #e0f2fe 0%, #bae6fd 100%)',
          borderBottom: isDark ? '1px solid rgba(14,165,233,0.2)' : '1px solid rgba(14,165,233,0.25)',
        }}
      >
        <p
          className="text-[10px] font-mono leading-none mb-0.5"
          style={{ color: isDark ? '#38bdf8' : '#0284c7' }}
        >
          {table.schema}
        </p>
        <div className="flex items-center gap-1.5">
          <Table2
            className="w-3.5 h-3.5 shrink-0"
            style={{ color: isDark ? '#7dd3fc' : '#0369a1' }}
          />
          <p
            className="text-sm font-bold truncate"
            style={{ color: isDark ? '#f0f9ff' : '#082f49' }}
          >
            {table.name}
          </p>
        </div>
      </div>

      {/* Columns */}
      <div style={{ paddingBottom: PADDING_B }}>
        {table.columns.map((col) => (
          <div
            key={col.name}
            className="flex items-center gap-2 px-3 transition-colors"
            style={{
              height: COL_H,
              borderBottom: isDark ? '1px solid rgba(255,255,255,0.04)' : '1px solid rgba(0,0,0,0.05)',
              background: 'transparent',
            }}
          >
            {col.isPrimaryKey ? (
              <KeyRound className="w-3 h-3 text-amber-400 shrink-0" />
            ) : col.foreignKeyName ? (
              <Link className="w-3 h-3 text-sky-400 shrink-0" />
            ) : (
              <span
                className="w-2.5 h-2.5 rounded-full shrink-0"
                style={{
                  background: col.isNullable ? 'transparent' : (isDark ? '#6b7280' : '#9ca3af'),
                  border: col.isNullable ? `1px solid ${isDark ? '#4b5563' : '#d1d5db'}` : 'none',
                }}
              />
            )}
            <span
              className="text-[11px] font-mono truncate flex-1"
              style={{ color: isDark ? '#e2e8f0' : '#1e1b4b' }}
            >
              {col.name}
            </span>
            <span
              className="text-[10px] font-mono shrink-0 ml-1"
              style={{ color: isDark ? '#6b7280' : '#0284c7' }}
            >
              {col.dataType}
            </span>
          </div>
        ))}
      </div>
    </div>
  )
}

const nodeTypes = { erTable: ERTableNode }

// ── Graph builder ──────────────────────────────────────────────────────────────
function buildGraph(schema: ParsedSchema, isDark: boolean): { nodes: Node[]; edges: Edge[] } {
  const tables = schema.tables

  const rowHeights: number[] = []
  tables.forEach((t, i) => {
    const row = Math.floor(i / COLS_PER_ROW)
    const h = tableHeight(t)
    rowHeights[row] = Math.max(rowHeights[row] ?? 0, h)
  })

  const rowY: number[] = [0]
  for (let r = 0; r < rowHeights.length - 1; r++) {
    rowY.push(rowY[r] + rowHeights[r] + V_GAP)
  }

  const nameToId = new Map<string, string>()
  tables.forEach((t, i) => {
    nameToId.set(t.name.toLowerCase(), `t${i}`)
    nameToId.set(`${t.schema}.${t.name}`.toLowerCase(), `t${i}`)
  })

  const nodes: Node[] = tables.map((t, i) => ({
    id: `t${i}`,
    type: 'erTable',
    position: {
      x: (i % COLS_PER_ROW) * (TABLE_WIDTH + H_GAP),
      y: rowY[Math.floor(i / COLS_PER_ROW)],
    },
    data: { table: t, isDark } as ERNodeData,
  }))

  const edges: Edge[] = []
  tables.forEach((t, i) => {
    t.columns.forEach((col) => {
      if (!col.referencedTableName) return
      const key =
        nameToId.get(`${t.schema}.${col.referencedTableName}`.toLowerCase()) ??
        nameToId.get(col.referencedTableName.toLowerCase())
      if (!key || key === `t${i}`) return
      edges.push({
        id: `e-${i}-${col.name}`,
        source: `t${i}`,
        target: key,
        type: 'smoothstep',
        animated: true,
        label: col.name,
        labelStyle: {
          fontSize: 10,
          fill: isDark ? '#38bdf8' : '#0369a1',
          fontFamily: 'monospace',
        },
        labelBgStyle: {
          fill: isDark ? '#0c0c12' : '#f0f9ff',
          fillOpacity: 0.9,
        },
        labelBgPadding: [4, 2] as [number, number],
        style: { stroke: '#0284c7', strokeWidth: 1.5 },
        markerEnd: { type: MarkerType.ArrowClosed, color: '#0284c7', width: 16, height: 16 },
      })
    })
  })

  return { nodes, edges }
}

// ── Modal ──────────────────────────────────────────────────────────────────────
export function SchemaVisualizer({
  schema,
  onClose,
}: {
  schema: ParsedSchema
  onClose: () => void
}) {
  const { theme } = useThemeStore()
  const isDark = theme === 'dark'

  const { nodes: init, edges: initEdges } = useMemo(
    () => buildGraph(schema, isDark),
    [schema, isDark],
  )
  const [nodes, , onNodesChange] = useNodesState(init)
  const [edges, , onEdgesChange] = useEdgesState(initEdges)

  // Theme-derived values
  const canvasBg    = isDark ? '#080809' : '#f0f9ff'
  const topBarBg    = isDark ? '#0d0d0f' : '#ffffff'
  const topBarBorder = isDark ? '#0c3a5e' : '#bae6fd'
  const dotColor    = isDark ? 'rgba(14,165,233,0.25)' : 'rgba(14,165,233,0.12)'
  const controlsBg  = isDark ? '#0d0d0f' : '#ffffff'
  const controlsBorder = isDark ? '#0c3a5e' : '#bae6fd'
  const minimapMask = isDark ? 'rgba(8,8,9,0.75)' : 'rgba(240,249,255,0.75)'
  const textPrimary = isDark ? '#f0f9ff' : '#082f49'
  const textMuted   = isDark ? '#6b7280' : '#0284c7'
  const legendText  = isDark ? '#6b7280' : '#0369a1'
  const closeBtnHover = isDark ? 'hover:bg-white/10' : 'hover:bg-sky-100'

  return (
    <div className="fixed inset-0 z-50 flex flex-col" style={{ background: canvasBg }}>
      {/* Top bar */}
      <div
        className="flex items-center justify-between px-5 py-3 shrink-0 border-b"
        style={{ background: topBarBg, borderColor: topBarBorder }}
      >
        <div className="flex items-center gap-3">
          <div
            className="w-7 h-7 rounded-lg flex items-center justify-center"
            style={{
              background: isDark ? 'rgba(14,165,233,0.2)' : '#e0f2fe',
              border: `1px solid ${isDark ? 'rgba(14,165,233,0.3)' : 'rgba(14,165,233,0.4)'}`,
            }}
          >
            <Table2 className="w-3.5 h-3.5" style={{ color: isDark ? '#a78bfa' : '#7c3aed' }} />
          </div>
          <div>
            <p className="text-sm font-semibold" style={{ color: textPrimary }}>
              Schema Visualizer
            </p>
            <p className="text-xs font-mono" style={{ color: textMuted }}>
              {schema.database} · {schema.tables.length} tables
            </p>
          </div>
        </div>

        <div className="flex items-center gap-3">
          {/* Legend */}
          <div className="hidden sm:flex items-center gap-4 text-[11px] mr-2" style={{ color: legendText }}>
            <span className="flex items-center gap-1.5">
              <KeyRound className="w-3 h-3 text-amber-400" /> Primary key
            </span>
            <span className="flex items-center gap-1.5">
              <Link className="w-3 h-3 text-sky-400" /> Foreign key
            </span>
            <span className="flex items-center gap-1.5">
              <span
                className="w-2.5 h-2.5 rounded-full inline-block"
                style={{ border: `1px solid ${isDark ? '#4b5563' : '#7dd3fc'}` }}
              />
              Nullable
            </span>
            <span className="flex items-center gap-1.5">
              <span
                className="w-2.5 h-2.5 rounded-full inline-block"
                style={{ background: isDark ? '#6b7280' : '#9ca3af' }}
              />
              Not null
            </span>
          </div>

          <button
            onClick={onClose}
            className={`p-1.5 rounded-lg transition-colors ${closeBtnHover}`}
            style={{ color: textMuted }}
          >
            <X className="w-4 h-4" />
          </button>
        </div>
      </div>

      {/* Canvas */}
      <div className="flex-1 min-h-0">
        <ReactFlow
          nodes={nodes}
          edges={edges}
          onNodesChange={onNodesChange}
          onEdgesChange={onEdgesChange}
          nodeTypes={nodeTypes}
          fitView
          fitViewOptions={{ padding: 0.15 }}
          minZoom={0.05}
          maxZoom={2}
          deleteKeyCode={null}
          colorMode={isDark ? 'dark' : 'light'}
        >
          <Background
            color={dotColor}
            variant={BackgroundVariant.Dots}
            gap={24}
            size={1.5}
          />
          <Controls
            style={{
              background: controlsBg,
              border: `1px solid ${controlsBorder}`,
              borderRadius: 8,
            }}
          />
          <MiniMap
            nodeColor={isDark ? '#0284c7' : '#38bdf8'}
            maskColor={minimapMask}
            style={{
              background: controlsBg,
              border: `1px solid ${controlsBorder}`,
              borderRadius: 8,
            }}
          />
        </ReactFlow>
      </div>
    </div>
  )
}
