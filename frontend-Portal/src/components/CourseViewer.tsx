import { useState } from 'react'
import type { CourseDetail } from '@/types/portal'
import { ChapterTree } from './ChapterTree'
import { useChapterContent } from '@/hooks/usePortalData'

interface CourseViewerProps {
  course: CourseDetail
}

export function CourseViewer({ course }: CourseViewerProps) {
  const [selectedChapterId, setSelectedChapterId] = useState<string | null>(null)
  const [isFullscreen, setIsFullscreen] = useState(false)
  const { data: chapterContent, isLoading } = useChapterContent(selectedChapterId)

  const viewerContent = (
    <div className={`flex flex-col ${isFullscreen ? 'fixed inset-0 z-50 bg-white' : 'h-full'}`}>
      {/* Header */}
      <div className="flex items-center justify-between px-3 py-2 border-b border-gray-200 bg-gray-50 shrink-0">
        <h3 className="text-sm font-semibold text-gray-700 truncate">{course.title}</h3>
        <button
          className="p-1 rounded hover:bg-gray-200 text-gray-500 transition-colors"
          onClick={() => setIsFullscreen(!isFullscreen)}
          title={isFullscreen ? 'Exit fullscreen' : 'Fullscreen'}
        >
          {isFullscreen ? (
            <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M9 9L4 4m0 0v4m0-4h4m6 10l5 5m0 0v-4m0 4h-4M9 15l-5 5m0 0h4m-4 0v-4m10-10l5-5m0 0h-4m4 0v4" />
            </svg>
          ) : (
            <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M4 8V4m0 0h4M4 4l5 5m11-1V4m0 0h-4m4 0l-5 5M4 16v4m0 0h4m-4 0l5-5m11 5v-4m0 4h-4m4 0l-5-5" />
            </svg>
          )}
        </button>
      </div>

      <div className="flex flex-1 min-h-0">
        {/* Chapter sidebar */}
        <div className="w-56 border-r border-gray-200 overflow-y-auto shrink-0 bg-white p-2">
          <ChapterTree
            chapters={course.chapters}
            selectedChapterId={selectedChapterId}
            onSelect={setSelectedChapterId}
          />
        </div>

        {/* Content area */}
        <div className="flex-1 overflow-y-auto p-4">
          {!selectedChapterId && (
            <div className="flex items-center justify-center h-full text-gray-400 text-sm">
              Select a chapter to view its content
            </div>
          )}
          {selectedChapterId && isLoading && (
            <div className="flex items-center justify-center h-full text-gray-400 text-sm">
              Loading...
            </div>
          )}
          {chapterContent && (
            <div>
              <h4 className="text-base font-semibold text-gray-900 mb-3">{chapterContent.title}</h4>
              {chapterContent.htmlContent ? (
                <div
                  className="pptx-html-content"
                  dangerouslySetInnerHTML={{ __html: chapterContent.htmlContent }}
                />
              ) : (
                <p className="text-sm text-gray-400 italic">No content available.</p>
              )}
            </div>
          )}
        </div>
      </div>
    </div>
  )

  return viewerContent
}
