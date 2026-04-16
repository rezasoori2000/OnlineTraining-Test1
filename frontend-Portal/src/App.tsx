import { useState } from 'react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { CoursePage } from '@/components/CoursePage'
import type { FolderDetail } from '@/types/portal'
import { MainPage } from './components/MainPage'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      refetchOnWindowFocus: false,
      staleTime: 5 * 60 * 1000,   // 5 min — serve cached data without a network trip
      gcTime: 30 * 60 * 1000,     // 30 min — keep unused data in memory
    },
  },
})

export function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <PortalRouter />
    </QueryClientProvider>
  )
}

type Page = { type: 'main' } | { type: 'course'; courseId: string; courseTitle: string }

function PortalRouter() {
  const [page, setPage] = useState<Page>({ type: 'main' })
  const [selectedFolderId, setSelectedFolderId] = useState<string | null>(null)

  if (page.type === 'course') {
    return (
      <CoursePage
        courseId={page.courseId}
        courseTitle={page.courseTitle}
        onBack={() => setPage({ type: 'main' })}
      />
    )
  }

  return (
    <MainPage
      selectedFolderId={selectedFolderId}
      onFolderSelect={setSelectedFolderId}
      onCourseOpen={(courseId, courseTitle) =>
        setPage({ type: 'course', courseId, courseTitle })
      }
    />
  )
}

export function FolderPanel({ folder }: { folder: FolderDetail }) {
  const hasHtml = folder.htmlContent && folder.htmlContent.trim().length > 0

  return (
    <div className="flex flex-col h-full">
      {/* Folder title + description */}
      <div className="px-6 py-4 border-b border-gray-200 shrink-0">
        <h2 className="text-lg font-bold text-gray-900">{folder.name}</h2>
        {folder.description && (
          <p className="text-sm text-gray-500 mt-1">{folder.description}</p>
        )}
      </div>

      {/* HTML content — fills remaining space */}
      <div className="flex-1  px-6 py-4 ov ">
        {hasHtml ? (
          <div
            className="pptx-html-content"
            dangerouslySetInnerHTML={{ __html: folder.htmlContent! }}
          />
        ) : (
          <div className="flex items-center justify-center h-full text-gray-400 text-sm italic">
            No content available for this folder.
          </div>
        )}
      </div>
    </div>
  )
}
