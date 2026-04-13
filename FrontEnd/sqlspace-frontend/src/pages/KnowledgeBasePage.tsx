import { useRef, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  BookOpen, Upload, FileText, CheckCircle2,
  XCircle, Clock, Loader2, ChevronDown, AlertCircle,
} from 'lucide-react'
import { toast } from 'sonner'
import { cn } from '@/lib/utils'
import { connectionsApi } from '@/api/connections'
import { knowledgeBaseApi } from '@/api/knowledge-base'
import type { KnowledgeDocument } from '@/types'

type Tab = 'documents' | 'upload'

const ROLE_OPTIONS = [
  { value: 'admin',        label: 'Admins only' },
  { value: 'full_access',  label: 'Full access users' },
  { value: 'restricted',  label: 'Restricted users' },
]

const STATUS_CONFIG: Record<KnowledgeDocument['status'], { label: string; icon: React.ElementType; className: string }> = {
  Pending:    { label: 'Pending',    icon: Clock,         className: 'bg-zinc-500/10 text-zinc-400 border-zinc-500/20' },
  Processing: { label: 'Processing', icon: Loader2,       className: 'bg-sky-500/10 text-sky-400 border-sky-500/20' },
  Indexed:    { label: 'Indexed',    icon: CheckCircle2,  className: 'bg-green-500/10 text-green-400 border-green-500/20' },
  Failed:     { label: 'Failed',     icon: XCircle,       className: 'bg-red-500/10 text-red-400 border-red-500/20' },
}

export default function KnowledgeBasePage() {
  const [tab, setTab] = useState<Tab>('documents')
  const [selectedConnectionId, setSelectedConnectionId] = useState<string>('')

  const { data: connections = [], isLoading: loadingConnections } = useQuery({
    queryKey: ['connections-for-kb'],
    queryFn: () => connectionsApi.list(),
  })

  return (
    <div className="flex flex-col h-full px-6 py-8 min-h-0">
      {/* Header */}
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-white tracking-tight">Knowledge Base</h1>
        <p className="text-zinc-400 mt-1">Upload documents so you can ask questions about them in the workspace.</p>
      </div>

      {/* Connection selector */}
      <div className="mb-6">
        <label className="block text-xs font-medium text-zinc-400 mb-1.5">Connection</label>
        <div className="relative w-72">
          <select
            value={selectedConnectionId}
            onChange={(e) => setSelectedConnectionId(e.target.value)}
            className="w-full appearance-none bg-[#111113] border border-white/10 text-sm text-zinc-200 rounded-lg px-3 py-2 pr-8 focus:outline-none focus:border-sky-500 focus:ring-1 focus:ring-sky-500 transition-colors"
          >
            <option value="">Select a connection…</option>
            {connections.map((c) => (
              <option key={c.connectionId} value={c.connectionId}>
                {c.connectionName}
              </option>
            ))}
          </select>
          <ChevronDown className="absolute right-2.5 top-1/2 -translate-y-1/2 w-3.5 h-3.5 text-zinc-500 pointer-events-none" />
        </div>
      </div>

      {!selectedConnectionId ? (
        <div className="flex-1 flex items-center justify-center">
          <div className="text-center">
            <BookOpen className="w-10 h-10 text-zinc-600 mx-auto mb-3" />
            <p className="text-zinc-500 text-sm">Select a connection to get started.</p>
          </div>
        </div>
      ) : (
        <>
          {/* Tabs */}
          <div className="flex gap-1 mb-6 border-b border-white/10 pb-px">
            {([
              { id: 'documents', label: 'Documents',      icon: FileText },
              { id: 'upload',    label: 'Upload',         icon: Upload },
            ] as { id: Tab; label: string; icon: React.ElementType }[]).map(({ id, label, icon: Icon }) => (
              <button
                key={id}
                onClick={() => setTab(id)}
                className={cn(
                  'flex items-center gap-2 px-4 py-2 text-sm font-medium rounded-t-lg transition-colors border-b-2 -mb-px',
                  tab === id
                    ? 'text-sky-400 border-sky-500 bg-sky-500/5'
                    : 'text-zinc-400 border-transparent hover:text-zinc-200 hover:bg-white/5'
                )}
              >
                <Icon className="w-3.5 h-3.5" />
                {label}
              </button>
            ))}
          </div>

          {/* Tab content */}
          <div className="flex-1 min-h-0 overflow-y-auto">
            {tab === 'documents' && <DocumentsTab connectionId={selectedConnectionId} />}
            {tab === 'upload'    && <UploadTab    connectionId={selectedConnectionId} onSuccess={() => setTab('documents')} />}
          </div>
        </>
      )}
    </div>
  )
}

// ── Documents Tab ──────────────────────────────────────────────────────────────

function DocumentsTab({ connectionId }: { connectionId: string }) {
  const { data: docs = [], isLoading } = useQuery({
    queryKey: ['knowledge-docs', connectionId],
    queryFn: () => knowledgeBaseApi.listDocuments(connectionId),
    refetchInterval: (query) => {
      const docs = query.state.data ?? []
      return docs.some((d) => d.status === 'Processing' || d.status === 'Pending') ? 4000 : false
    },
  })

  if (isLoading) return <LoadingRows />

  if (docs.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center py-20 text-center">
        <FileText className="w-10 h-10 text-zinc-600 mb-3" />
        <p className="text-zinc-400 text-sm font-medium">No documents yet</p>
        <p className="text-zinc-600 text-xs mt-1">Upload a PDF, DOCX, or TXT file to get started.</p>
      </div>
    )
  }

  return (
    <div className="rounded-xl border border-white/10 overflow-hidden">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b border-white/10 bg-white/[0.02]">
            <th className="text-left px-4 py-3 text-xs font-medium text-zinc-500 uppercase tracking-wider">File</th>
            <th className="text-left px-4 py-3 text-xs font-medium text-zinc-500 uppercase tracking-wider">Status</th>
            <th className="text-left px-4 py-3 text-xs font-medium text-zinc-500 uppercase tracking-wider">Chunks</th>
            <th className="text-left px-4 py-3 text-xs font-medium text-zinc-500 uppercase tracking-wider">Uploaded</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-white/5">
          {docs.map((doc) => {
            const cfg = STATUS_CONFIG[doc.status]
            const Icon = cfg.icon
            return (
              <tr key={doc.documentId} className="hover:bg-white/[0.02] transition-colors">
                <td className="px-4 py-3">
                  <div className="flex items-center gap-2.5">
                    <FileText className="w-4 h-4 text-zinc-500 shrink-0" />
                    <span className="text-zinc-200 font-medium truncate max-w-xs">{doc.fileName}</span>
                  </div>
                  {doc.errorMessage && (
                    <p className="text-xs text-red-400 mt-0.5 ml-6 truncate">{doc.errorMessage}</p>
                  )}
                </td>
                <td className="px-4 py-3">
                  <span className={cn('inline-flex items-center gap-1.5 px-2 py-0.5 text-xs font-medium rounded-full border', cfg.className)}>
                    <Icon className={cn('w-3 h-3', doc.status === 'Processing' && 'animate-spin')} />
                    {cfg.label}
                  </span>
                </td>
                <td className="px-4 py-3 text-zinc-400">
                  {doc.status === 'Indexed' ? doc.chunksCreated : '—'}
                </td>
                <td className="px-4 py-3 text-zinc-500 text-xs">
                  {new Date(doc.createdAt).toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' })}
                </td>
              </tr>
            )
          })}
        </tbody>
      </table>
    </div>
  )
}

// ── Upload Tab ─────────────────────────────────────────────────────────────────

function UploadTab({ connectionId, onSuccess }: { connectionId: string; onSuccess: () => void }) {
  const qc = useQueryClient()
  const inputRef = useRef<HTMLInputElement>(null)
  const [file, setFile] = useState<File | null>(null)
  const [allowedRoles, setAllowedRoles] = useState<string[]>(['admin', 'full_access'])
  const [dragging, setDragging] = useState(false)

  const { mutate: upload, isPending } = useMutation({
    mutationFn: () => knowledgeBaseApi.uploadDocument(connectionId, file!, allowedRoles),
    onSuccess: (result) => {
      toast.success(`"${result.fileName}" queued — ${result.chunksCreated} chunks created.`)
      qc.invalidateQueries({ queryKey: ['knowledge-docs', connectionId] })
      setFile(null)
      onSuccess()
    },
    onError: (err: Error) => toast.error(err.message),
  })

  const toggleRole = (role: string) => {
    setAllowedRoles((prev) =>
      prev.includes(role) ? prev.filter((r) => r !== role) : [...prev, role]
    )
  }

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault()
    setDragging(false)
    const dropped = e.dataTransfer.files[0]
    if (dropped) setFile(dropped)
  }

  return (
    <div className="max-w-lg space-y-6">
      {/* Drop zone */}
      <div
        onClick={() => inputRef.current?.click()}
        onDragOver={(e) => { e.preventDefault(); setDragging(true) }}
        onDragLeave={() => setDragging(false)}
        onDrop={handleDrop}
        className={cn(
          'border-2 border-dashed rounded-xl p-10 flex flex-col items-center justify-center text-center cursor-pointer transition-colors',
          dragging
            ? 'border-sky-500 bg-sky-500/5'
            : file
            ? 'border-green-500/40 bg-green-500/5'
            : 'border-white/10 hover:border-white/20 hover:bg-white/[0.02]'
        )}
      >
        <input
          ref={inputRef}
          type="file"
          accept=".pdf,.docx,.txt"
          className="hidden"
          onChange={(e) => setFile(e.target.files?.[0] ?? null)}
        />
        {file ? (
          <>
            <CheckCircle2 className="w-8 h-8 text-green-400 mb-2" />
            <p className="text-sm font-medium text-zinc-200">{file.name}</p>
            <p className="text-xs text-zinc-500 mt-0.5">{(file.size / 1024 / 1024).toFixed(2)} MB</p>
            <button
              onClick={(e) => { e.stopPropagation(); setFile(null) }}
              className="mt-2 text-xs text-zinc-500 hover:text-zinc-300 underline"
            >
              Remove
            </button>
          </>
        ) : (
          <>
            <Upload className="w-8 h-8 text-zinc-600 mb-2" />
            <p className="text-sm text-zinc-400">Drop a file here or <span className="text-sky-400">browse</span></p>
            <p className="text-xs text-zinc-600 mt-1">PDF, DOCX, TXT — max 20 MB</p>
          </>
        )}
      </div>

      {/* Role selector */}
      <div>
        <label className="block text-xs font-medium text-zinc-400 mb-2">Who can see this document?</label>
        <div className="space-y-2">
          {ROLE_OPTIONS.map(({ value, label }) => (
            <label key={value} className="flex items-center gap-3 cursor-pointer group">
              <div
                onClick={() => toggleRole(value)}
                className={cn(
                  'w-4 h-4 rounded border flex items-center justify-center transition-colors shrink-0',
                  allowedRoles.includes(value)
                    ? 'bg-sky-500 border-sky-500'
                    : 'border-white/20 bg-transparent group-hover:border-white/40'
                )}
              >
                {allowedRoles.includes(value) && (
                  <svg className="w-2.5 h-2.5 text-white" fill="none" viewBox="0 0 10 10">
                    <path d="M1.5 5l2.5 2.5 4.5-4.5" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" />
                  </svg>
                )}
              </div>
              <span className="text-sm text-zinc-300">{label}</span>
            </label>
          ))}
        </div>
        {allowedRoles.length === 0 && (
          <p className="flex items-center gap-1.5 text-xs text-amber-400 mt-2">
            <AlertCircle className="w-3.5 h-3.5" /> Select at least one role.
          </p>
        )}
      </div>

      {/* Submit */}
      <button
        onClick={() => upload()}
        disabled={!file || allowedRoles.length === 0 || isPending}
        className="flex items-center justify-center gap-2 w-full py-2.5 px-4 rounded-lg bg-sky-500 hover:bg-sky-400 disabled:opacity-40 disabled:cursor-not-allowed text-white text-sm font-medium transition-colors shadow-[0_0_15px_rgba(14,165,233,0.3)]"
      >
        {isPending ? <Loader2 className="w-4 h-4 animate-spin" /> : <Upload className="w-4 h-4" />}
        {isPending ? 'Uploading…' : 'Upload Document'}
      </button>
    </div>
  )
}

// ── Helpers ────────────────────────────────────────────────────────────────────

function LoadingRows() {
  return (
    <div className="space-y-2 animate-pulse">
      {Array.from({ length: 4 }).map((_, i) => (
        <div key={i} className="h-12 bg-white/5 rounded-lg" />
      ))}
    </div>
  )
}
