import { useMemo, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQuery } from '@tanstack/react-query'
import {
  ArrowLeft,
  Database,
  TerminalSquare,
  Info,
  AlertTriangle,
  Code2,
  Play,
} from 'lucide-react'
import { toast } from 'sonner'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { queriesApi } from '@/api/queries'
import { useWorkspaceStore } from '@/stores/workspace-store'
import { useConnectionStore } from '@/stores/connection-store'

type DetailTab = 'sql' | 'explain'

export default function HistoryDetailPage() {
  const { queryId } = useParams<{ queryId: string }>()
  const navigate = useNavigate()
  const { setPrompt, setGeneratedSQL, setExplanation, setResult } = useWorkspaceStore()
  const { setActiveConnection } = useConnectionStore()
  const [activeTab, setActiveTab] = useState<DetailTab>('sql')

  const { data: detail, isLoading } = useQuery({
    queryKey: ['history-detail', queryId],
    queryFn: () => (queryId ? queriesApi.historyById(queryId) : Promise.resolve(null)),
    enabled: Boolean(queryId),
  })

  const rerunMutation = useMutation({
    mutationFn: async () => {
      if (!queryId) throw new Error('Missing query id')
      return queriesApi.rerun(queryId)
    },
    onSuccess: (result) => {
      if (detail?.connectionId) {
        setActiveConnection(detail.connectionId)
      }
      setPrompt(detail?.userPrompt ?? '')
      setGeneratedSQL(result.sql)
      setExplanation(result.explanation)
      setResult(result.result)
      toast.success('Query loaded in workspace')
      navigate('/workspace')
    },
    onError: (error: Error) => {
      toast.error(error.message || 'Failed to rerun query')
    },
  })

  const formattedDate = useMemo(() => {
    if (!detail?.executedAt) return '-'
    return new Intl.DateTimeFormat('en-US', {
      month: 'short',
      day: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
      hour12: false,
    }).format(new Date(detail.executedAt))
  }, [detail?.executedAt])

  if (isLoading) {
    return (
      <div className="px-6 py-8 w-full h-full min-h-0 overflow-y-auto">
        <div className="max-w-5xl mx-auto text-zinc-500">Loading query details...</div>
      </div>
    )
  }

  if (!detail) {
    return (
      <div className="px-6 py-8 w-full h-full min-h-0 overflow-y-auto">
        <div className="max-w-5xl mx-auto">
          <Link to="/history" className="inline-flex items-center gap-2 text-sm font-medium text-zinc-400 hover:text-white mb-6 transition-colors">
            <ArrowLeft className="w-4 h-4" /> Back to History
          </Link>
          <div className="bg-[#111113] border border-white/10 rounded-2xl p-6 text-zinc-400">
            Query history item was not found.
          </div>
        </div>
      </div>
    )
  }

  const isSuccess = detail.status === 'Success'

  return (
    <div className="px-6 py-8 w-full h-full min-h-0 overflow-y-auto">
      <div className="max-w-5xl mx-auto">
        <Link to="/history" className="inline-flex items-center gap-2 text-sm font-medium text-zinc-400 hover:text-white mb-6 transition-colors">
          <ArrowLeft className="w-4 h-4" /> Back to History
        </Link>

        <div className="bg-[#111113] border border-white/10 rounded-2xl p-6 mb-8 shadow-sm">
          <div className="flex flex-col md:flex-row justify-between gap-6">
            <div className="flex-1">
              <div className="text-sm font-medium text-sky-400 mb-2 font-mono flex items-center gap-2">
                <TerminalSquare className="w-4 h-4" /> PROMPT
              </div>
              <h1 className="text-xl md:text-2xl font-semibold text-white leading-snug">
                "{detail.userPrompt || 'No prompt provided'}"
              </h1>
            </div>

            <div className="flex flex-col items-end gap-3 shrink-0">
              {isSuccess ? (
                <Badge variant="outline" className="bg-green-500/10 text-green-400 border-green-500/20 text-sm px-3 py-1">Success</Badge>
              ) : (
                <Badge variant="outline" className="bg-red-500/10 text-red-400 border-red-500/20 text-sm px-3 py-1">Failed</Badge>
              )}
              <Button
                onClick={() => rerunMutation.mutate()}
                disabled={rerunMutation.isPending}
                className="bg-white/5 border border-white/10 hover:bg-white/10 text-white px-4 py-2 rounded-lg text-sm font-medium flex items-center gap-2 transition-all"
                variant="outline"
              >
                <Play className="w-4 h-4 text-sky-400" />
                {rerunMutation.isPending ? 'Rerunning...' : 'Rerun in Workspace'}
              </Button>
            </div>
          </div>

          <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mt-8 pt-6 border-t border-white/5">
            <div>
              <div className="text-xs text-zinc-500 uppercase tracking-wider mb-1">Connection</div>
              <div className="text-sm font-medium text-zinc-200 flex items-center gap-2">
                <Database className="w-4 h-4 text-zinc-400" /> {detail.connectionName}
              </div>
            </div>
            <div>
              <div className="text-xs text-zinc-500 uppercase tracking-wider mb-1">Execution Time</div>
              <div className="text-sm font-medium text-zinc-200 font-mono">{detail.executionTimeMs ?? 0}ms</div>
            </div>
            <div>
              <div className="text-xs text-zinc-500 uppercase tracking-wider mb-1">Rows Returned</div>
              <div className="text-sm font-medium text-zinc-200 font-mono">{detail.rowsReturned ?? 0}</div>
            </div>
            <div>
              <div className="text-xs text-zinc-500 uppercase tracking-wider mb-1">Date</div>
              <div className="text-sm font-medium text-zinc-200">{formattedDate}</div>
            </div>
          </div>
        </div>

        {!isSuccess && detail.errorMessage && (
          <div className="mb-8 p-4 bg-red-500/10 border border-red-500/20 rounded-xl flex items-start gap-3">
            <AlertTriangle className="w-5 h-5 text-red-400 shrink-0 mt-0.5" />
            <div>
              <h4 className="text-sm font-semibold text-red-400 mb-1">Execution Error</h4>
              <p className="text-sm text-red-200/80 font-mono">{detail.errorMessage}</p>
            </div>
          </div>
        )}

        <div className="bg-[#111113] border border-white/10 rounded-2xl overflow-hidden shadow-sm flex flex-col">
          <div className="flex border-b border-white/10 bg-[#18181b] px-4">
            <button
              onClick={() => setActiveTab('sql')}
              className={`px-4 py-3 text-sm font-medium border-b-2 flex items-center gap-2 transition-colors ${activeTab === 'sql' ? 'border-sky-500 text-sky-400 bg-sky-500/5' : 'border-transparent text-zinc-400 hover:text-zinc-200 hover:bg-white/5'}`}
            >
              <Code2 className="w-4 h-4" /> Generated SQL
            </button>
            <button
              onClick={() => setActiveTab('explain')}
              className={`px-4 py-3 text-sm font-medium border-b-2 flex items-center gap-2 transition-colors ${activeTab === 'explain' ? 'border-sky-500 text-sky-400 bg-sky-500/5' : 'border-transparent text-zinc-400 hover:text-zinc-200 hover:bg-white/5'}`}
            >
              <Info className="w-4 h-4" /> AI Explanation
            </button>
          </div>

          <div className="p-6 bg-[#0d0d0f]">
            {activeTab === 'sql' && (
              <pre className="font-mono text-sm leading-relaxed text-zinc-300 overflow-x-auto whitespace-pre-wrap">
                {detail.generatedSql || 'No SQL generated'}
              </pre>
            )}

            {activeTab === 'explain' && (
              <div className="prose prose-invert max-w-none text-zinc-300">
                {detail.llmResponse ? (
                  <p className="leading-relaxed">{detail.llmResponse}</p>
                ) : (
                  <p className="text-zinc-500 italic">No explanation available for this query.</p>
                )}
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  )
}
