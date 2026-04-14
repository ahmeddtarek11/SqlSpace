import { useEffect, useRef, useState } from 'react'
import { Send, Loader2, FileText, Sparkles, MessageSquare, AlertCircle, ChevronDown } from 'lucide-react'
import { cn } from '@/lib/utils'
import { useRagChatStore, selectConnectionChat } from '@/stores/rag-chat-store'
import type { ChatMessage } from '@/types'

interface RagChatPanelProps {
  connectionId: string | null
  openTrigger?: boolean
  defaultDraftOnOpen?: string
}

export function RagChatPanel({
  connectionId,
  openTrigger = false,
  defaultDraftOnOpen,
}: RagChatPanelProps) {
  const loadHistory = useRagChatStore((s) => s.loadHistory)
  const sendMessage = useRagChatStore((s) => s.sendMessage)
  const chat = useRagChatStore((s) =>
    connectionId ? selectConnectionChat(connectionId)(s) : null
  )

  const [draft, setDraft] = useState('')
  const scrollRef = useRef<HTMLDivElement>(null)
  const wasOpenRef = useRef(false)

  useEffect(() => {
    if (connectionId) void loadHistory(connectionId)
  }, [connectionId, loadHistory])

  useEffect(() => {
    const el = scrollRef.current
    if (!el) return
    el.scrollTop = el.scrollHeight
  }, [chat?.messages.length, chat?.isSending])

  useEffect(() => {
    const openedNow = openTrigger && !wasOpenRef.current
    if (openedNow && defaultDraftOnOpen) {
      setDraft((current) => (current.trim().length > 0 ? current : defaultDraftOnOpen))
    }
    wasOpenRef.current = openTrigger
  }, [openTrigger, defaultDraftOnOpen])

  if (!connectionId) {
    return (
      <div className="flex flex-col items-center justify-center h-full text-center select-none">
        <div className="w-16 h-16 rounded-2xl bg-[#111113] border border-white/5 flex items-center justify-center mb-4">
          <MessageSquare className="w-7 h-7 text-zinc-600" />
        </div>
        <p className="text-zinc-600 text-sm">Select a connection to start chatting</p>
      </div>
    )
  }

  const messages = chat?.messages ?? []
  const isLoading = chat?.isLoading ?? false
  const isSending = chat?.isSending ?? false
  const error = chat?.error ?? null

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    const trimmed = draft.trim()
    if (trimmed.length < 2 || isSending) return
    setDraft('')
    void sendMessage(connectionId, trimmed)
  }

  const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault()
      handleSubmit(e as unknown as React.FormEvent)
    }
  }

  return (
    <div className="flex flex-col h-full min-h-0 bg-[#080809]">
      {/* Message list */}
      <div ref={scrollRef} className="flex-1 min-h-0 overflow-y-auto px-6 py-6">
        {isLoading && messages.length === 0 ? (
          <div className="space-y-3 animate-pulse max-w-2xl mx-auto">
            <div className="h-4 bg-white/5 rounded w-3/4" />
            <div className="h-4 bg-white/5 rounded w-full" />
            <div className="h-4 bg-white/5 rounded w-5/6" />
          </div>
        ) : messages.length === 0 ? (
          <div className="flex flex-col items-center justify-center h-full text-center select-none">
            <div className="w-16 h-16 rounded-2xl bg-sky-500/10 border border-sky-500/20 flex items-center justify-center mb-4">
              <Sparkles className="w-7 h-7 text-sky-400" />
            </div>
            <p className="text-zinc-300 text-sm font-medium">Ask anything about your data and uploaded docs</p>
            <p className="text-zinc-600 text-xs mt-2 max-w-sm">
              Your questions and answers are saved for this connection, so you can pick up where you left off.
            </p>
          </div>
        ) : (
          <div className="max-w-3xl mx-auto space-y-4">
            {messages.map((msg) => (
              <MessageBubble key={msg.messageId} message={msg} />
            ))}
            {isSending && <TypingIndicator />}
          </div>
        )}
      </div>

      {/* Error banner */}
      {error && (
        <div className="shrink-0 border-t border-red-500/20 bg-red-500/5 px-6 py-2">
          <div className="max-w-3xl mx-auto flex items-center gap-2 text-xs text-red-300">
            <AlertCircle className="w-3.5 h-3.5 shrink-0" />
            <span className="truncate">{error}</span>
          </div>
        </div>
      )}

      {/* Input */}
      <form
        onSubmit={handleSubmit}
        className="shrink-0 border-t border-white/10 bg-[#0d0d0f] px-6 py-4"
      >
        <div className="max-w-3xl mx-auto flex gap-2 items-end">
          <textarea
            value={draft}
            onChange={(e) => setDraft(e.target.value)}
            onKeyDown={handleKeyDown}
            placeholder="Ask a question about your documents…"
            rows={1}
            className="flex-1 resize-none bg-[#111113] border border-white/10 rounded-lg px-4 py-2.5 text-sm text-zinc-200 placeholder:text-zinc-600 focus:outline-none focus:border-sky-500 focus:ring-1 focus:ring-sky-500 transition-colors max-h-40"
          />
          <button
            type="submit"
            disabled={draft.trim().length < 2 || isSending}
            className="flex items-center gap-2 px-4 py-2.5 rounded-lg bg-sky-500 hover:bg-sky-400 disabled:opacity-40 disabled:cursor-not-allowed text-white text-sm font-medium transition-colors shrink-0"
          >
            {isSending ? <Loader2 className="w-4 h-4 animate-spin" /> : <Send className="w-4 h-4" />}
            Send
          </button>
        </div>
      </form>
    </div>
  )
}

function MessageBubble({ message }: { message: ChatMessage }) {
  if (message.role === 'user') {
    return (
      <div className="flex justify-end">
        <div className="max-w-[80%] rounded-2xl rounded-br-sm bg-sky-500 text-white px-4 py-2.5 text-sm leading-relaxed whitespace-pre-wrap shadow-[0_0_15px_rgba(14,165,233,0.2)]">
          {message.content}
        </div>
      </div>
    )
  }

  return <AssistantMessage message={message} />
}

function AssistantMessage({ message }: { message: ChatMessage }) {
  const [sourcesOpen, setSourcesOpen] = useState(false)
  const hasError = !!message.errorMessage && !message.content
  const sources = message.sources ?? []

  return (
    <div className="flex justify-start">
      <div className="max-w-[85%] w-full space-y-2">
        <div
          className={cn(
            'rounded-2xl rounded-bl-sm border px-4 py-3 text-sm leading-relaxed whitespace-pre-wrap',
            hasError
              ? 'border-red-500/30 bg-red-500/5 text-red-300'
              : 'border-white/10 bg-white/[0.02] text-zinc-200'
          )}
        >
          {hasError ? (
            <div className="flex items-start gap-2">
              <AlertCircle className="w-4 h-4 mt-0.5 shrink-0" />
              <span>{message.errorMessage}</span>
            </div>
          ) : (
            message.content
          )}
          {message.tokensUsed != null && !hasError && (
            <div className="mt-2 text-[10px] text-zinc-600 uppercase tracking-wider">
              {message.tokensUsed} tokens
            </div>
          )}
        </div>

        {sources.length > 0 && (
          <div>
            <button
              type="button"
              onClick={() => setSourcesOpen((v) => !v)}
              className="flex items-center gap-1.5 text-xs font-medium text-zinc-500 hover:text-zinc-300 transition-colors"
            >
              <ChevronDown
                className={cn('w-3 h-3 transition-transform', sourcesOpen && 'rotate-180')}
              />
              Sources ({sources.length})
            </button>
            {sourcesOpen && (
              <div className="mt-2 space-y-2">
                {sources.map((src) => (
                  <div
                    key={src.chunkId}
                    className="rounded-lg border border-white/10 bg-white/[0.02] px-3 py-2"
                  >
                    <div className="flex items-center justify-between mb-1">
                      <div className="flex items-center gap-2 min-w-0">
                        <FileText className="w-3.5 h-3.5 text-zinc-500 shrink-0" />
                        <span className="text-xs font-medium text-zinc-300 truncate">
                          {src.fileName}
                        </span>
                      </div>
                      <span className="text-xs text-zinc-600 shrink-0 ml-2">
                        {(src.relevanceScore * 100).toFixed(0)}%
                      </span>
                    </div>
                    <p className="text-xs text-zinc-500 leading-relaxed line-clamp-3">
                      {src.excerpt}
                    </p>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  )
}

function TypingIndicator() {
  return (
    <div className="flex justify-start">
      <div className="rounded-2xl rounded-bl-sm border border-white/10 bg-white/[0.02] px-4 py-3 flex items-center gap-1.5">
        <span className="w-1.5 h-1.5 rounded-full bg-zinc-500 animate-pulse" />
        <span className="w-1.5 h-1.5 rounded-full bg-zinc-500 animate-pulse [animation-delay:150ms]" />
        <span className="w-1.5 h-1.5 rounded-full bg-zinc-500 animate-pulse [animation-delay:300ms]" />
      </div>
    </div>
  )
}
