import { MessageSquareText, X } from 'lucide-react'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { RagChatPanel } from '@/components/workspace/RagChatPanel'
import { useConnectionStore } from '@/stores/connection-store'
import { useRagChatUiStore } from '@/stores/rag-chat-ui-store'

export function GlobalRagChatPopup() {
  const isPopupOpen = useRagChatUiStore((s) => s.isPopupOpen)
  const setPopupOpen = useRagChatUiStore((s) => s.setPopupOpen)
  const closePopup = useRagChatUiStore((s) => s.closePopup)

  const activeConnectionId = useConnectionStore((s) => s.activeConnectionId)
  const connections = useConnectionStore((s) => s.connections)
  const activeConnection = connections.find((c) => c.connectionId === activeConnectionId) ?? null

  return (
    <Dialog open={isPopupOpen} onOpenChange={setPopupOpen}>
      <DialogContent
        showCloseButton={false}
        className="w-[96vw] max-w-[96vw] sm:max-w-[96vw] h-[92vh] max-h-[92vh] p-0 overflow-hidden flex flex-col rounded-2xl bg-[#0c0c0e] border-white/10"
      >
        <DialogHeader className="px-5 py-4 border-b border-white/10 shrink-0">
          <div className="flex items-center justify-between gap-4">
            <div className="flex items-center gap-2">
              <MessageSquareText className="w-4 h-4 text-sky-400" />
              <DialogTitle className="text-sm font-semibold text-white">Ask AI</DialogTitle>
            </div>

            <button
              type="button"
              onClick={closePopup}
              className="h-7 w-7 inline-flex items-center justify-center rounded-md text-zinc-500 hover:text-zinc-300 hover:bg-white/5 transition-colors"
              aria-label="Close Ask AI"
            >
              <X className="w-4 h-4" />
            </button>
          </div>

          <p className="text-xs text-zinc-500 mt-2">
            {activeConnection
              ? `Connection: ${activeConnection.connectionName}`
              : 'No active connection selected'}
          </p>
        </DialogHeader>

        <div className="flex-1 min-h-0 bg-[#080809]">
          <RagChatPanel
            connectionId={activeConnectionId}
            openTrigger={isPopupOpen}
            defaultDraftOnOpen="Summarize the newly ingested context and tell me the most important insights."
          />
        </div>
      </DialogContent>
    </Dialog>
  )
}
