import { useState } from 'react'
import Editor from '@monaco-editor/react'
import { Copy, Check, ChevronDown, ChevronUp } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { motion, AnimatePresence } from 'framer-motion'

interface Props {
  sql: string
  explanation?: string
  onChange?: (sql: string) => void
  readOnly?: boolean
}

export function SQLPreview({ sql, explanation, onChange, readOnly = false }: Props) {
  const [copied, setCopied] = useState(false)
  const [showExplanation, setShowExplanation] = useState(false)

  const handleCopy = () => {
    void navigator.clipboard.writeText(sql)
    setCopied(true)
    setTimeout(() => setCopied(false), 2000)
  }

  return (
    <div className="flex flex-col rounded-xl border border-white/10 overflow-hidden bg-[#111113]">
      {/* Toolbar */}
      <div className="flex items-center justify-between px-4 py-2 border-b border-white/10 bg-[#18181b]">
        <div className="flex items-center gap-2">
          <Badge variant="secondary" className="bg-sky-500/10 text-sky-400 border-sky-500/20 text-xs font-mono">
            SQL
          </Badge>
          {!readOnly && (
            <span className="text-xs text-zinc-600">Editable</span>
          )}
        </div>
        <div className="flex items-center gap-1">
          {explanation && (
            <Button
              variant="ghost"
              size="sm"
              className="h-7 px-2 text-xs text-zinc-500 hover:text-zinc-300 hover:bg-white/5"
              onClick={() => setShowExplanation((v) => !v)}
            >
              Explain
              {showExplanation ? <ChevronUp className="w-3 h-3 ml-1" /> : <ChevronDown className="w-3 h-3 ml-1" />}
            </Button>
          )}
          <Button
            variant="ghost"
            size="icon"
            className="w-7 h-7 text-zinc-500 hover:text-zinc-200 hover:bg-white/5"
            onClick={handleCopy}
          >
            {copied ? <Check className="w-3.5 h-3.5 text-green-400" /> : <Copy className="w-3.5 h-3.5" />}
          </Button>
        </div>
      </div>

      {/* Explanation */}
      <AnimatePresence>
        {showExplanation && explanation && (
          <motion.div
            initial={{ height: 0, opacity: 0 }}
            animate={{ height: 'auto', opacity: 1 }}
            exit={{ height: 0, opacity: 0 }}
            className="overflow-hidden"
          >
            <p className="px-4 py-2 text-xs text-zinc-400 bg-sky-500/5 border-b border-white/10">
              {explanation}
            </p>
          </motion.div>
        )}
      </AnimatePresence>

      {/* Monaco editor */}
      <Editor
        height="160px"
        language="sql"
        value={sql}
        onChange={(v) => onChange?.(v ?? '')}
        options={{
          readOnly,
          minimap: { enabled: false },
          scrollBeyondLastLine: false,
          fontSize: 13,
          lineNumbers: 'off',
          folding: false,
          glyphMargin: false,
          lineDecorationsWidth: 8,
          lineNumbersMinChars: 0,
          renderLineHighlight: 'none',
          overviewRulerLanes: 0,
          hideCursorInOverviewRuler: true,
          wordWrap: 'on',
          padding: { top: 12, bottom: 12 },
        }}
        theme="vs-dark"
      />
    </div>
  )
}
