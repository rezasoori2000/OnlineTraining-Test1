import { ChatBox } from './ChatBox'

interface ChatPageProps {
  onBack: () => void
}

export function ChatPage({ onBack }: ChatPageProps) {
  return (
    <div className="h-full flex flex-col">
      {/* Top bar */}
      <header className="h-12 bg-brand-950 flex items-center px-4 shrink-0 gap-3">
        <button
          onClick={onBack}
          className="text-brand-300 hover:text-white transition-colors"
          title="Go back"
        >
          <svg className="w-5 h-5" viewBox="0 0 24 24" fill="none"
            stroke="currentColor" strokeWidth="2">
            <path d="M19 12H5M12 19l-7-7 7-7" />
          </svg>
        </button>
        <div className="flex items-center gap-2">
          <svg className="w-5 h-5 text-brand-300" viewBox="0 0 24 24" fill="none"
            stroke="currentColor" strokeWidth="1.5">
            <path d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0
              01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9
              3.582 9 8z" />
          </svg>
          <h1 className="text-white font-semibold text-base tracking-wide">Course Assistant</h1>
        </div>
        <span className="ml-auto text-xs text-brand-400">
          Powered by local AI · answers from indexed course content
        </span>
      </header>

      {/* Full-size chat box centered */}
      <div className="flex-1 min-h-0 flex justify-center bg-gray-50">
        <div className="w-full max-w-3xl bg-white shadow-sm border-x border-gray-200 flex flex-col">
          <ChatBox compact={false} />
        </div>
      </div>
    </div>
  )
}
