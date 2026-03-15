import { toast } from 'sonner'
import { TooltipProvider } from '@/components/ui/tooltip'
import { NLPromptInput } from '@/components/workspace/NLPromptInput'
import { SQLPreview } from '@/components/workspace/SQLPreview'
import { ResultsTable } from '@/components/workspace/ResultsTable'
import { SchemaPanel } from '@/components/workspace/SchemaPanel'
import { ConnectionSidebar } from '@/components/connections/ConnectionSidebar'
import { queriesApi } from '@/api/queries'
import { useWorkspaceStore } from '@/stores/workspace-store'
import { useConnectionStore } from '@/stores/connection-store'
import { AlertCircle } from 'lucide-react'

export default function WorkspacePage() {
  const {
    generatedSQL,
    explanation,
    result,
    error,
    setGeneratedSQL,
    setExplanation,
    setResult,
    setExecuting,
    setError,
  } = useWorkspaceStore()
  const activeConnectionId = useConnectionStore((s) => s.activeConnectionId)

  const handleSubmit = async (prompt: string) => {
    if (!activeConnectionId) {
      toast.error('Select a connection first')
      return
    }
    setExecuting(true)
    setError(null)
    try {
      const data = await queriesApi.execute({ connectionId: activeConnectionId, userPrompt: prompt })
      setGeneratedSQL(data.sql)
      setExplanation(data.explanation)
      setResult(data.result)
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Query failed'
      setError(msg)
      toast.error(msg)
    } finally {
      setExecuting(false)
    }
  }

  return (
    <TooltipProvider>
      <div className="flex h-full overflow-hidden">
        {/* Left: connections */}
        <ConnectionSidebar />

        {/* Center: workspace */}
        <div className="flex-1 flex flex-col overflow-hidden p-4 gap-4">
          {/* Prompt */}
          <NLPromptInput onSubmit={handleSubmit} />

          {/* SQL + Results */}
          <div className="flex flex-col gap-4 overflow-y-auto flex-1">
            {generatedSQL && (
              <SQLPreview
                sql={generatedSQL}
                explanation={explanation}
                onChange={setGeneratedSQL}
              />
            )}

            {error && (
              <div className="flex items-start gap-2 rounded-xl border border-red-500/30 bg-red-500/10 px-4 py-3">
                <AlertCircle className="w-4 h-4 text-red-400 mt-0.5 shrink-0" />
                <p className="text-sm text-red-300">{error}</p>
              </div>
            )}

            {result && <ResultsTable result={result} />}

            {!generatedSQL && !error && (
              <div className="flex flex-col items-center justify-center flex-1 text-center py-16 select-none">
                <div className="w-16 h-16 rounded-2xl bg-violet-600/10 border border-violet-500/20 flex items-center justify-center mb-4">
                  <span className="text-2xl">✦</span>
                </div>
                <p className="text-(--text-muted) text-sm max-w-xs">
                  Ask a question in plain English and SqlSpace will generate and run the SQL for you.
                </p>
              </div>
            )}
          </div>
        </div>

        {/* Right: schema */}
        <SchemaPanel />
      </div>
    </TooltipProvider>
  )
}
