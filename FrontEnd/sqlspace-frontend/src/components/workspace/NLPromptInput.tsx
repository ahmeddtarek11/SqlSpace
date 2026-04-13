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
    <div className="relative h-full">
      {/* Focus glow */}
      {focused && (
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          className="absolute -inset-[1px] pointer-events-none z-0"
          style={{
            borderRadius: 'var(--radius-lg)',
            boxShadow: '0 0 0 1px var(--accent), 0 0 20px var(--accent-glow)',
          }}
        />
      )}

      <div
        className="relative z-10 flex flex-col gap-2 p-4 transition-all h-full"
        style={{
          borderRadius: 'var(--radius-lg)',
          background: 'var(--bg-elevated)',
          border: focused ? '1px solid var(--accent)' : '1px solid var(--border-default)',
        }}
      >
        {/* Prompt icon + textarea */}
        <div className="flex items-start gap-3 flex-1">
          <div
            className="mt-1 w-7 h-7 flex items-center justify-center shrink-0"
            style={{
              borderRadius: 'var(--radius-md)',
              background: 'var(--accent-subtle)',
            }}
          >
            <Sparkles className="w-3.5 h-3.5" style={{ color: 'var(--accent)' }} />
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
            className="flex-1 resize-none border-0 bg-transparent p-0 text-[14px] placeholder:text-[var(--text-muted)] focus-visible:ring-0 focus-visible:ring-offset-0"
            style={{
              color: 'var(--text-primary)',
              fontFamily: 'var(--font-sans)',
            }}
          />
        </div>

        {/* Footer */}
        <div className="flex items-center justify-between pl-10">
          <span className="text-[11px]" style={{ color: 'var(--text-muted)' }}>
            {tooShort ? (
              <span style={{ color: 'var(--warning)' }}>At least {MIN_WORDS} words required</span>
            ) : (
              <>
                <kbd
                  className="px-1.5 py-0.5 text-[10px]"
                  style={{
                    borderRadius: 'var(--radius-sm)',
                    background: 'var(--bg-overlay)',
                    color: 'var(--text-tertiary)',
                    border: '1px solid var(--border-subtle)',
                  }}
                >
                  Ctrl
                </kbd>
                {' + '}
                <kbd
                  className="px-1.5 py-0.5 text-[10px]"
                  style={{
                    borderRadius: 'var(--radius-sm)',
                    background: 'var(--bg-overlay)',
                    color: 'var(--text-tertiary)',
                    border: '1px solid var(--border-subtle)',
                  }}
                >
                  Enter
                </kbd>
                {' to run'}
              </>
            )}
          </span>

          <Button
            size="sm"
            disabled={!canSubmit}
            onClick={() => void onSubmit(prompt)}
            className="h-8 px-4 text-[12px] font-semibold text-white transition-all active:scale-[0.97]"
            style={{
              borderRadius: 'var(--radius-md)',
              background: canSubmit ? 'var(--accent)' : 'var(--bg-overlay)',
              boxShadow: canSubmit ? '0 0 12px var(--accent-glow)' : 'none',
            }}
          >
            {isExecuting ? (
              <Loader2 className="w-3.5 h-3.5 animate-spin" />
            ) : (
              <>
                <Send className="w-3.5 h-3.5 mr-1.5" />
                Run
              </>
            )}
          </Button>
        </div>
      </div>
    </div>
  )
}
