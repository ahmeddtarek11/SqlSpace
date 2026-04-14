import { Bot, Loader2, Sparkles } from 'lucide-react'
import { cn } from '@/lib/utils'

type AskAiButtonSize = 'icon' | 'pill'

interface AskAiButtonProps {
  onClick: () => void
  disabled?: boolean
  loading?: boolean
  size?: AskAiButtonSize
  label?: string
  title?: string
  ariaLabel?: string
  className?: string
}

export function AskAiButton({
  onClick,
  disabled = false,
  loading = false,
  size = 'icon',
  label = 'Ask AI',
  title,
  ariaLabel,
  className,
}: AskAiButtonProps) {
  const isIcon = size === 'icon'

  return (
    <button
      type="button"
      onClick={onClick}
      disabled={disabled || loading}
      title={title ?? label}
      aria-label={ariaLabel ?? label}
      className={cn(
        'inline-flex items-center justify-center gap-1.5 rounded-lg border border-cyan-400/30 bg-cyan-500/15 text-cyan-200 transition-colors hover:bg-cyan-500/25 hover:text-cyan-50 disabled:opacity-40 disabled:cursor-not-allowed',
        isIcon ? 'h-8 w-8' : 'h-8 px-3 text-xs font-semibold tracking-wide',
        className,
      )}
    >
      {loading ? (
        <Loader2 className={cn(isIcon ? 'w-4 h-4 animate-spin' : 'w-3.5 h-3.5 animate-spin')} />
      ) : (
        <>
          <Bot className={cn(isIcon ? 'w-4 h-4' : 'w-3.5 h-3.5')} />
          {!isIcon && (
            <>
              <span>{label}</span>
              <Sparkles className="w-3 h-3" />
            </>
          )}
        </>
      )}
    </button>
  )
}
