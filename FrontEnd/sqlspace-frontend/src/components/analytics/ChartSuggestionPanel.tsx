import { useState } from 'react'
import { Sparkles, Plus, BarChart3, TrendingUp, PieChart, ScatterChart as ScatterIcon, AreaChart as AreaIcon } from 'lucide-react'
import type { ChartSuggestion, ChartType } from '@/types'

const CHART_ICONS: Record<ChartType, React.ComponentType<{ className?: string }>> = {
  bar: BarChart3,
  line: TrendingUp,
  area: AreaIcon,
  pie: PieChart,
  scatter: ScatterIcon,
}

interface ChartSuggestionPanelProps {
  suggestions: ChartSuggestion[]
  loading: boolean
  onSave: (suggestion: ChartSuggestion) => void
  onSuggest: (prompt?: string) => void
}

export function ChartSuggestionPanel({
  suggestions, loading, onSave, onSuggest,
}: ChartSuggestionPanelProps) {
  const [prompt, setPrompt] = useState('')

  return (
    <div className="space-y-4">
      {/* Prompt input */}
      <div className="flex items-center gap-3">
        <div className="flex-1 relative">
          <input
            type="text"
            value={prompt}
            onChange={(e) => setPrompt(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === 'Enter' && !loading) {
                onSuggest(prompt || undefined)
              }
            }}
            placeholder="Describe what you want to analyze (optional)..."
            className="w-full bg-[#111113] border border-white/10 rounded-xl px-4 py-2.5 text-sm text-white placeholder:text-zinc-600 focus:outline-none focus:border-sky-500 transition-colors"
          />
        </div>
        <button
          onClick={() => onSuggest(prompt || undefined)}
          disabled={loading}
          className="flex items-center gap-2 px-4 py-2.5 rounded-xl bg-sky-600 hover:bg-sky-500 disabled:opacity-50 disabled:cursor-not-allowed text-white text-sm font-medium transition-colors shrink-0"
        >
          <Sparkles className={`w-4 h-4 ${loading ? 'animate-pulse' : ''}`} />
          {loading ? 'Analyzing...' : 'Suggest Analytics'}
        </button>
      </div>

      {/* Suggestion cards */}
      {suggestions.length > 0 && (
        <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
          {suggestions.map((suggestion, i) => {
            const Icon = CHART_ICONS[suggestion.chartType] ?? BarChart3
            return (
              <div
                key={i}
                className="bg-[#111113] border border-white/10 rounded-2xl p-5 hover:border-sky-500/30 transition-colors group"
              >
                <div className="flex items-start gap-3 mb-3">
                  <div className="w-8 h-8 rounded-lg bg-sky-500/10 flex items-center justify-center shrink-0">
                    <Icon className="w-4 h-4 text-sky-400" />
                  </div>
                  <div className="min-w-0 flex-1">
                    <h4 className="text-sm font-semibold text-white truncate">{suggestion.title}</h4>
                    <span className="text-[10px] uppercase tracking-wide text-zinc-600 font-medium">
                      {suggestion.chartType} chart
                    </span>
                  </div>
                </div>
                <p className="text-xs text-zinc-400 mb-4 line-clamp-2">{suggestion.description}</p>
                <button
                  onClick={() => onSave(suggestion)}
                  className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-medium text-sky-400 bg-sky-500/10 hover:bg-sky-500/20 transition-colors w-full justify-center"
                >
                  <Plus className="w-3.5 h-3.5" />
                  Save to Dashboard
                </button>
              </div>
            )
          })}
        </div>
      )}
    </div>
  )
}
