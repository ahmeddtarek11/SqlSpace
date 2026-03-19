import { useRef, useState } from 'react'
import { motion } from 'framer-motion'
import { Sparkles, Loader2, Send } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Textarea } from '@/components/ui/textarea'
import { useWorkspaceStore } from '@/stores/workspace-store'

const MIN_WORDS = 3

function wordCount(text: string) {
  return text.trim().split(/\s+/).filter(Boolean).length
}

interface Props {
  onSubmit: (prompt: string) => Promise<void>
}

export function NLPromptInput({ onSubmit }: Props) {
  const { prompt, setPrompt, isExecuting } = useWorkspaceStore()
  const [focused, setFocused] = useState(false)
  const ref = useRef<HTMLTextAreaElement>(null)

  const words = wordCount(prompt)
  const tooShort = prompt.trim().length > 0 && words < MIN_WORDS
  const canSubmit = !isExecuting && words >= MIN_WORDS

  const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Enter' && (e.ctrlKey || e.metaKey) && canSubmit) {
      e.preventDefault()
      void onSubmit(prompt)
    }
  }

  return (
    <div className="relative">
      {/* Animated gradient border on focus */}
      {focused && (
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          className="absolute -inset-[1px] rounded-xl gradient-border pointer-events-none z-0"
        />
      )}

      <div
        className={`relative z-10 flex flex-col gap-2 rounded-xl border bg-[#111113] p-3 transition-colors ${
          focused ? 'border-transparent' : 'border-white/10'
        }`}
      >
        {/* Prompt icon */}
        <div className="flex items-start gap-2">
          <div className="mt-0.5 w-6 h-6 rounded-md bg-sky-500/10 flex items-center justify-center shrink-0">
            <Sparkles className="w-3.5 h-3.5 text-sky-400" />
          </div>

          <Textarea
            ref={ref}
            value={prompt}
            onChange={(e) => setPrompt(e.target.value)}
            onFocus={() => setFocused(true)}
            onBlur={() => setFocused(false)}
            onKeyDown={handleKeyDown}
            placeholder="Ask anything about your data… e.g. 'Show me top 10 customers by revenue this month'"
            rows={3}
            disabled={isExecuting}
            className="flex-1 resize-none border-0 bg-transparent p-0 text-sm text-white placeholder:text-zinc-600 focus-visible:ring-0 focus-visible:ring-offset-0"
          />
        </div>

        {/* Footer */}
        <div className="flex items-center justify-between pl-8">
          <span className="text-xs text-zinc-600">
            {tooShort ? (
              <span className="text-amber-400">At least {MIN_WORDS} words required</span>
            ) : (
              <>
                <kbd className="px-1 py-0.5 rounded bg-[#18181b] text-zinc-500 text-xs">Ctrl</kbd>
                {' + '}
                <kbd className="px-1 py-0.5 rounded bg-[#18181b] text-zinc-500 text-xs">Enter</kbd>
                {' to run'}
              </>
            )}
          </span>

          <Button
            size="sm"
            disabled={!canSubmit}
            onClick={() => void onSubmit(prompt)}
            className="h-7 px-3 bg-sky-600 hover:bg-sky-500 text-white text-xs shadow-[0_0_15px_rgba(14,165,233,0.4)] hover:scale-105 active:scale-95 transition-all"
          >
            {isExecuting ? (
              <Loader2 className="w-3.5 h-3.5 animate-spin" />
            ) : (
              <>
                <Send className="w-3.5 h-3.5 mr-1" />
                Run
              </>
            )}
          </Button>
        </div>
      </div>
    </div>
  )
}
