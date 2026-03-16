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

// ── Dimensions ─────────────────────────────────────────────────────────────────
const TABLE_WIDTH = 240
const HEADER_H = 52 // schema label + table name
const COL_H = 26
const PADDING_B = 6
const COLS_PER_ROW = 4
const H_GAP = 60
const V_GAP = 50

function tableHeight(t: SchemaTable) {
  return HEADER_H + t.columns.length * COL_H + PADDING_B
}

// ── Custom table node ──────────────────────────────────────────────────────────
type ERNodeData = { table: SchemaTable } & Record<string, unknown>

function ERTableNode({ data }: NodeProps<Node<ERNodeData>>) {
  const table = data.table

  return (
    <div
      style={{ width: TABLE_WIDTH }}
      className="rounded-xl overflow-hidden shadow-2xl border border-violet-500/30 bg-[#13132a]"
    >
      {/* Incoming handle — left edge centre */}
      <Handle
        type="target"
        position={Position.Left}
        style={{ background: '#a78bfa', border: '2px solid #7c3aed', width: 8, height: 8 }}
      />
      {/* Outgoing handle — right edge centre */}
      <Handle
        type="source"
        position={Position.Right}
        style={{ background: '#60a5fa', border: '2px solid #2563eb', width: 8, height: 8 }}
      />

      {/* Header */}
      <div
        className="px-3 pt-2 pb-1.5 border-b border-violet-500/20"
        style={{ background: 'linear-gradient(135deg, #4c1d95 0%, #312e81 100%)' }}
      >
        <p className="text-[10px] text-violet-400 font-mono leading-none mb-0.5">{table.schema}</p>
        <div className="flex items-center gap-1.5">
          <Table2 className="w-3.5 h-3.5 text-violet-300 shrink-0" />
          <p className="text-sm font-bold text-(--text-primary) truncate">{table.name}</p>
        </div>
      </div>

      {/* Columns */}
      <div style={{ paddingBottom: PADDING_B }}>
        {table.columns.map((col) => (
          <div
            key={col.name}
            className="flex items-center gap-2 px-3 border-b border-white/5 last:border-0 hover:bg-white/5 transition-colors"
            style={{ height: COL_H }}
          >
            {col.isPrimaryKey ? (
              <KeyRound className="w-3 h-3 text-amber-400 shrink-0" />
            ) : col.foreignKeyName ? (
              <Link className="w-3 h-3 text-blue-400 shrink-0" />
            ) : (
              <span
                className="w-2.5 h-2.5 rounded-full shrink-0"
                style={{
                  background: col.isNullable ? 'transparent' : '#6b7280',
                  border: col.isNullable ? '1px solid #4b5563' : 'none',
                }}
              />
            )}
            <span className="text-[11px] font-mono text-gray-200 truncate flex-1">{col.name}</span>
            <span className="text-[10px] font-mono text-gray-500 shrink-0 ml-1">{col.dataType}</span>
          </div>
        ))}
      </div>
    </div>
  )
}

const nodeTypes = { erTable: ERTableNode }

// ── Graph builder ──────────────────────────────────────────────────────────────
function buildGraph(schema: ParsedSchema): { nodes: Node[]; edges: Edge[] } {
  const tables = schema.tables

  // Row max heights for non-overlapping vertical placement
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

  // Name → node id map (by bare name and by schema.name)
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
    data: { table: t } as ERNodeData,
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
        labelStyle: { fontSize: 10, fill: '#9ca3af', fontFamily: 'monospace' },
        labelBgStyle: { fill: '#13132a', fillOpacity: 0.9 },
        labelBgPadding: [4, 2] as [number, number],
        style: { stroke: '#6366f1', strokeWidth: 1.5 },
        markerEnd: { type: MarkerType.ArrowClosed, color: '#6366f1', width: 16, height: 16 },
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
  const { nodes: init, edges: initEdges } = useMemo(() => buildGraph(schema), [schema])
  const [nodes, , onNodesChange] = useNodesState(init)
  const [edges, , onEdgesChange] = useEdgesState(initEdges)

  return (
    <div className="fixed inset-0 z-50 flex flex-col" style={{ background: '#0b0b1a' }}>
      {/* Top bar */}
      <div
        className="flex items-center justify-between px-5 py-3 shrink-0 border-b"
        style={{ background: '#0f0f23', borderColor: '#1e1e3f' }}
      >
        <div className="flex items-center gap-3">
          <div className="w-7 h-7 rounded-lg bg-violet-600/20 border border-violet-500/30 flex items-center justify-center">
            <Table2 className="w-3.5 h-3.5 text-violet-400" />
          </div>
          <div>
            <p className="text-sm font-semibold text-(--text-primary)">Schema Visualizer</p>
            <p className="text-xs text-gray-500 font-mono">{schema.database} · {schema.tables.length} tables</p>
          </div>
        </div>

        <div className="flex items-center gap-3">
          {/* Legend */}
          <div className="hidden sm:flex items-center gap-4 text-[11px] text-gray-500 mr-2">
            <span className="flex items-center gap-1.5">
              <KeyRound className="w-3 h-3 text-amber-400" />Primary key
            </span>
            <span className="flex items-center gap-1.5">
              <Link className="w-3 h-3 text-blue-400" />Foreign key
            </span>
            <span className="flex items-center gap-1.5">
              <span className="w-2.5 h-2.5 rounded-full border border-gray-600 inline-block" />Nullable
            </span>
            <span className="flex items-center gap-1.5">
              <span className="w-2.5 h-2.5 rounded-full bg-gray-500 inline-block" />Not null
            </span>
          </div>

          <button
            onClick={onClose}
            className="p-1.5 rounded-lg transition-colors text-gray-400 hover:text-(--text-primary) hover:bg-white/10"
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
        >
          <Background
            color="#1e1e3f"
            variant={BackgroundVariant.Dots}
            gap={24}
            size={1.5}
          />
          <Controls
            style={{ background: '#0f0f23', border: '1px solid #1e1e3f', borderRadius: 8 }}
          />
          <MiniMap
            nodeColor="#6366f1"
            maskColor="rgba(11,11,26,0.7)"
            style={{
              background: '#0f0f23',
              border: '1px solid #1e1e3f',
              borderRadius: 8,
            }}
          />
        </ReactFlow>
      </div>
    </div>
  )
}
