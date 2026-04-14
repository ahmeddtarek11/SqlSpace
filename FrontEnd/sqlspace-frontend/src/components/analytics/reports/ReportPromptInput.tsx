import { useState } from 'react'
import { Send, Loader2, Sparkles } from 'lucide-react'

interface ReportPromptInputProps {
  onGenerate: (prompt: string) => void
  isGenerating: boolean
}

export function ReportPromptInput({ onGenerate, isGenerating }: ReportPromptInputProps) {
  const [prompt, setPrompt] = useState('')

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    const trimmed = prompt.trim()
    if (trimmed.length < 5 || isGenerating) return
    onGenerate(trimmed)
  }

  return (
    <div className="flex flex-col items-center justify-center h-full text-center px-8">
      <div className="w-16 h-16 rounded-2xl bg-sky-500/10 border border-sky-500/20 flex items-center justify-center mb-5">
        <Sparkles className="w-7 h-7 text-sky-400" />
      </div>
      <h2 className="text-lg font-semibold text-zinc-200 mb-1">Generate a Report</h2>
      <p className="text-zinc-500 text-sm mb-8 max-w-sm">
        Describe what you want to know. SqlSpace will plan sections, run the SQL, and write the insights.
      </p>

      <form onSubmit={handleSubmit} className="w-full max-w-xl flex flex-col gap-3">
        <textarea
          value={prompt}
          onChange={(e) => setPrompt(e.target.value)}
          placeholder="e.g. Give me an overview of sales performance this quarter…"
          rows={3}
          disabled={isGenerating}
          className="w-full resize-none bg-[#111113] border border-white/10 rounded-xl px-4 py-3 text-sm text-zinc-200 placeholder:text-zinc-600 focus:outline-none focus:border-sky-500 focus:ring-1 focus:ring-sky-500 transition-colors disabled:opacity-50"
        />
        <button
          type="submit"
          disabled={prompt.trim().length < 5 || isGenerating}
          className="flex items-center justify-center gap-2 px-6 py-2.5 rounded-xl bg-sky-500 hover:bg-sky-400 disabled:opacity-40 disabled:cursor-not-allowed text-white text-sm font-medium transition-colors"
        >
          {isGenerating ? (
            <>
              <Loader2 className="w-4 h-4 animate-spin" />
              Generating report…
            </>
          ) : (
            <>
              <Send className="w-4 h-4" />
              Generate Report
            </>
          )}
        </button>
      </form>
    </div>
  )
}
