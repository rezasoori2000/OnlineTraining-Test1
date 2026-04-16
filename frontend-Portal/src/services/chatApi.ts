import { apiClient } from './apiClient'

export interface ChatMessage {
  role: 'user' | 'assistant'
  content: string
  sources?: ChatSource[]
}

export interface ChatSource {
  sourceId: string
  title: string
  type: string
}

export interface ChatRequest {
  question: string
  history?: Array<{ role: string; content: string }>
}

export interface ChatResponse {
  answer: string
  sources: ChatSource[]
}

export const chatApi = {
  send: (request: ChatRequest) =>
    apiClient.post<ChatResponse>('/chat', request),
}
