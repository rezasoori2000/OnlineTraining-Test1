import { useState, useRef, useEffect } from 'react'
import { chatApi, type ChatMessage, type ChatSource } from '../services/chatApi'

interface ChatBoxProps {
  /** When true renders in compact floating mode; false = full-page mode */
  compact?: boolean
}

export function ChatBox({ compact = false }: ChatBoxProps) {
  const [messages, setMessages] = useState<ChatMessage[]>([])
  const [input, setInput] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const bottomRef = useRef<HTMLDivElement>(null)
  const inputRef = useRef<HTMLTextAreaElement>(null)

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages, loading])

  const send = async () => {
    const question = input.trim()
    if (!question || loading) return

    const userMsg: ChatMessage = { role: 'user', content: question }
    setMessages(prev => [...prev, userMsg])
    setInput('')
    setLoading(true)
    setError(null)

    try {
      const history = messages.slice(-6).map(m => ({ role: m.role, content: m.content }))
      const res = await chatApi.send({ question, history })
      const assistantMsg: ChatMessage = {
        role: 'assistant',
        content: res.answer,
        sources: res.sources,
      }
      setMessages(prev => [...prev, assistantMsg])
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to get a response.')
    } finally {
      setLoading(false)
      setTimeout(() => inputRef.current?.focus(), 50)
    }
  }

  const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault()
      send()
    }
  }

  const msgClass = compact ? 'text-xs' : 'text-sm'
  const inputRows = compact ? 2 : 3

  return (
    <div className="flex flex-col h-full">
      {/* Message list */}
      <div className="flex-1 overflow-y-auto px-3 py-3 space-y-3">
        {messages.length === 0 && (
          <div className="flex flex-col items-center justify-center h-full text-center text-gray-400 px-4">
            <svg className="w-10 h-10 mb-3 opacity-40" viewBox="0 0 24 24" fill="none"
              stroke="currentColor" strokeWidth="1.5">
              <path d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0
                01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9
                3.582 9 8z" />
            </svg>
            <p className={`${msgClass} font-medium text-gray-500`}>Ask anything about your courses</p>
            <p className="text-xs text-gray-400 mt-1">Answers are based on course content only</p>
          </div>
        )}

        {messages.map((msg, i) => (
          <div key={i} className={`flex ${msg.role === 'user' ? 'justify-end' : 'justify-start'}`}>
            <div className={`max-w-[85%] ${msg.role === 'user'
              ? 'bg-brand-700 text-white rounded-2xl rounded-tr-sm px-3 py-2'
              : 'bg-gray-100 text-gray-800 rounded-2xl rounded-tl-sm px-3 py-2'
            }`}>
              <p className={`${msgClass} whitespace-pre-wrap leading-relaxed`}>{msg.content}</p>
              {msg.sources && msg.sources.length > 0 && (
                <div className="mt-2 flex flex-wrap gap-1">
                  {msg.sources.map((s, si) => (
                    <SourceBadge key={si} source={s} />
                  ))}
                </div>
              )}
            </div>
          </div>
        ))}

        {loading && (
          <div className="flex justify-start">
            <div className="bg-gray-100 rounded-2xl rounded-tl-sm px-3 py-2">
              <TypingIndicator />
            </div>
          </div>
        )}

        {error && (
          <div className="bg-red-50 border border-red-200 text-red-700 rounded-lg px-3 py-2 text-xs">
            {error}
          </div>
        )}

        <div ref={bottomRef} />
      </div>

      {/* Input area */}
      <div className="shrink-0 border-t border-gray-200 px-3 py-2 bg-white">
        <div className="flex items-end gap-2">
          <textarea
            ref={inputRef}
            value={input}
            onChange={e => setInput(e.target.value)}
            onKeyDown={handleKeyDown}
            rows={inputRows}
            disabled={loading}
            placeholder="Ask a question… (Enter to send, Shift+Enter for new line)"
            className="flex-1 resize-none text-sm border border-gray-300 rounded-xl px-3 py-2
              focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent
              disabled:opacity-50 disabled:bg-gray-50 placeholder:text-gray-400"
          />
          <button
            onClick={send}
            disabled={loading || !input.trim()}
            className="shrink-0 w-9 h-9 rounded-xl bg-brand-700 hover:bg-brand-800
              disabled:opacity-40 disabled:cursor-not-allowed flex items-center justify-center
              transition-colors"
          >
            <svg className="w-4 h-4 text-white" viewBox="0 0 24 24" fill="none"
              stroke="currentColor" strokeWidth="2">
              <path d="M22 2L11 13M22 2L15 22L11 13L2 9L22 2Z" />
            </svg>
          </button>
        </div>
        <p className="text-xs text-gray-400 mt-1.5 text-center">
          Answers are generated from indexed course content
        </p>
      </div>
    </div>
  )
}

function SourceBadge({ source }: { source: ChatSource }) {
  const icon = source.type === 'folder' ? '📁' : '📖'
  return (
    <span className="inline-flex items-center gap-1 bg-white/20 text-white/90 text-xs
      px-2 py-0.5 rounded-full border border-white/30">
      <span>{icon}</span>
      <span className="truncate max-w-[120px]">{source.title}</span>
    </span>
  )
}

function TypingIndicator() {
  return (
    <div className="flex items-center gap-1 py-1">
      {[0, 1, 2].map(i => (
        <span
          key={i}
          className="w-2 h-2 bg-gray-400 rounded-full animate-bounce"
          style={{ animationDelay: `${i * 0.15}s` }}
        />
      ))}
    </div>
  )
}
