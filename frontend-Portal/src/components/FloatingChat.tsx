import { useState } from 'react'
import { ChatBox } from './ChatBox'

interface FloatingChatProps {
  onOpenFullPage: () => void
}

export function FloatingChat({ onOpenFullPage }: FloatingChatProps) {
  const [open, setOpen] = useState(false)

  return (
    <div className="fixed bottom-5 right-5 z-50 flex flex-col items-end gap-2">
      {/* Expanded chat panel */}
      {open && (
        <div className="w-[360px] h-[500px] bg-white rounded-2xl shadow-2xl border border-gray-200
          flex flex-col overflow-hidden">
          {/* Header */}
          <div className="flex items-center justify-between px-4 py-3 bg-brand-950 shrink-0">
            <div className="flex items-center gap-2">
              <svg className="w-4 h-4 text-brand-300" viewBox="0 0 24 24" fill="none"
                stroke="currentColor" strokeWidth="1.5">
                <path d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0
                  01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9
                  3.582 9 8z" />
              </svg>
              <span className="text-white text-sm font-semibold">Course Assistant</span>
            </div>
            <div className="flex items-center gap-1">
              {/* Expand to full page */}
              <button
                onClick={() => { setOpen(false); onOpenFullPage() }}
                title="Open full page"
                className="p-1.5 rounded-lg text-brand-300 hover:text-white hover:bg-white/10
                  transition-colors"
              >
                <svg className="w-4 h-4" viewBox="0 0 24 24" fill="none"
                  stroke="currentColor" strokeWidth="2">
                  <path d="M15 3h6v6M9 21H3v-6M21 3l-7 7M3 21l7-7" />
                </svg>
              </button>
              {/* Close */}
              <button
                onClick={() => setOpen(false)}
                className="p-1.5 rounded-lg text-brand-300 hover:text-white hover:bg-white/10
                  transition-colors"
              >
                <svg className="w-4 h-4" viewBox="0 0 24 24" fill="none"
                  stroke="currentColor" strokeWidth="2">
                  <path d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>
          </div>

          {/* Chat body */}
          <div className="flex-1 min-h-0">
            <ChatBox compact />
          </div>
        </div>
      )}

      {/* Floating toggle button */}
      <button
        onClick={() => setOpen(!open)}
        className="w-13 h-13 w-[52px] h-[52px] rounded-full bg-brand-950 shadow-xl
          hover:bg-brand-800 transition-colors flex items-center justify-center"
        title="Course Assistant"
      >
        {open ? (
          <svg className="w-5 h-5 text-white" viewBox="0 0 24 24" fill="none"
            stroke="currentColor" strokeWidth="2">
            <path d="M6 18L18 6M6 6l12 12" />
          </svg>
        ) : (
          <svg className="w-5 h-5 text-white" viewBox="0 0 24 24" fill="none"
            stroke="currentColor" strokeWidth="1.5">
            <path d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0
              01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9
              3.582 9 8z" />
          </svg>
        )}
      </button>
    </div>
  )
}
