import { useState, useRef, useEffect, useCallback } from 'react'
import { useCourse, useChapterContent } from '@/hooks/usePortalData'
import { ChapterTree } from './ChapterTree'


interface CoursePageProps {
  courseId: string
  courseTitle: string
  onBack: () => void
}

// Scales PPTXjs HTML (fixed 1280px slide widths) to fit the container.
function ScaledContent({ html }: { html: string }) {
  const wrapperRef = useRef<HTMLDivElement>(null)
  const innerRef = useRef<HTMLDivElement>(null)
  const [scale, setScale] = useState(1)

  const recalc = useCallback(() => {
    if (!wrapperRef.current || !innerRef.current) return
    const containerWidth = wrapperRef.current.offsetWidth
    const contentWidth = innerRef.current.scrollWidth
    if (contentWidth > 0 && containerWidth > 0) {
      setScale(Math.min(1, containerWidth / contentWidth))
    }
  }, [])

  useEffect(() => {
    recalc()
    const ro = new ResizeObserver(recalc)
    if (wrapperRef.current) ro.observe(wrapperRef.current)
    return () => ro.disconnect()
  }, [recalc, html])

  const innerHeight = innerRef.current?.scrollHeight ?? 0

  return (
    <div ref={wrapperRef} className="w-full overflow-hidden">
      {/* paddingBottom compensates for the height lost by scale() so the parent scrolls correctly */}
      <div style={{ height: innerHeight==0?'100%':innerHeight * scale }}>
        <div
          ref={innerRef}
          style={{ transformOrigin: 'top left', transform: `scale(${scale})` }}
          dangerouslySetInnerHTML={{ __html: html }}
        />
      </div>
    </div>
  )
}

export function CoursePage({ courseId, courseTitle, onBack }: CoursePageProps) {
  const [selectedChapterId, setSelectedChapterId] = useState<string | null>(null)
  const [isFullscreen, setIsFullscreen] = useState(false)

  const { data: course, isLoading: courseLoading } = useCourse(courseId)
  const { data: chapterContent, isLoading: contentLoading } = useChapterContent(selectedChapterId)

  return (
    <div className={`flex flex-col ${isFullscreen ? 'fixed inset-0 z-50' : 'h-full'} bg-white`}>
      {/* Top bar */}
      <header className="h-12 bg-brand-950 flex items-center px-4 shrink-0 gap-3">
        <button
          onClick={onBack}
          className="flex items-center gap-1.5 text-white/80 hover:text-white transition-colors text-sm"
        >
          <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
            <path strokeLinecap="round" strokeLinejoin="round" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
          </svg>
          Back
        </button>
        <span className="text-white/40">|</span>
        <h1 className="text-white font-semibold text-base truncate">{courseTitle}</h1>
        <button
          className="ml-auto p-1 rounded hover:bg-white/20 text-white/80 hover:text-white transition-colors"
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
      </header>

      {courseLoading && (
        <div className="flex items-center justify-center flex-1 text-gray-400 text-sm">
          Loading course…
        </div>
      )}

      {course && (
        <div className="flex flex-1 min-h-0">
          {/* Chapter tree sidebar */}
          <aside className="w-64 border-r border-gray-200 bg-white shrink-0 flex flex-col">
            <div className="px-3 py-2 border-b border-gray-100 shrink-0">
              <span className="text-xs font-bold text-brand-700 uppercase tracking-wider">Chapters</span>
            </div>
            <div className="flex-1 overflow-y-auto p-2">
              <ChapterTree
                chapters={course.chapters}
                selectedChapterId={selectedChapterId}
                onSelect={setSelectedChapterId}
              />
            </div>
          </aside>

          {/* Content area — scrolls vertically */}
          <main className="flex-1 min-w-0 overflow-y-auto">
            {!selectedChapterId && (
              <div className="flex items-center justify-center h-full text-gray-400 text-sm">
                Select a chapter to view its content
              </div>
            )}
            {selectedChapterId && contentLoading && (
              <div className="flex items-center justify-center h-full text-gray-400 text-sm">
                Loading…
              </div>
            )}
            {chapterContent && (
              <div className="p-4 min-h-full flex flex-col">
                {chapterContent.pdfDownloadUrl ? (
                  <iframe
                    src={chapterContent.pdfDownloadUrl}
                    title={chapterContent.title}
                    className="w-full flex-1 border-0 min-h-[70vh]"
                    style={{ height: '100%' }}
                  />
                ) : chapterContent.htmlContent ? (
                  <ScaledContent html={chapterContent.htmlContent} />
                ) : (
                  <p className="text-sm text-gray-400 italic">No content available.</p>
                )}
              </div>
            )}
          </main>
        </div>
      )}
    </div>
  )
}
