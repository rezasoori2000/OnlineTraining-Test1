import { useState } from 'react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { useFolderTree, useFolderDetail, useCourse } from '@/hooks/usePortalData'
import { FolderTree } from '@/components/FolderTree'
import { FolderContent } from '@/components/FolderContent'
import { CourseList } from '@/components/CourseList'
import { CourseViewer } from '@/components/CourseViewer'

const queryClient = new QueryClient({
  defaultOptions: { queries: { refetchOnWindowFocus: false } },
})

export function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <PortalLayout />
    </QueryClientProvider>
  )
}

function PortalLayout() {
  const [selectedFolderId, setSelectedFolderId] = useState<string | null>(null)
  const [selectedCourseId, setSelectedCourseId] = useState<string | null>(null)

  const { data: tree, isLoading: treeLoading } = useFolderTree()
  const { data: folder } = useFolderDetail(selectedFolderId)
  const { data: course } = useCourse(selectedCourseId)

  const handleFolderSelect = (id: string) => {
    setSelectedFolderId(id)
    setSelectedCourseId(null)
  }

  return (
    <div className="h-screen flex flex-col">
      {/* Top bar */}
      <header className="h-12 bg-brand-950 flex items-center px-4 shrink-0">
        <h1 className="text-white font-semibold text-base">PGLLMS Portal</h1>
      </header>

      <div className="flex flex-1 min-h-0">
        {/* Left sidebar — Folder tree */}
        <aside className="w-64 border-r border-gray-200 bg-white shrink-0 flex flex-col">
          <div className="px-3 py-2 border-b border-gray-200">
            <h2 className="text-sm font-semibold text-gray-600 uppercase tracking-wide">Folders</h2>
          </div>
          <div className="flex-1 overflow-y-auto p-2">
            {treeLoading && <div className="text-sm text-gray-400 p-2">Loading...</div>}
            {tree && (
              <FolderTree
                nodes={tree}
                selectedId={selectedFolderId}
                onSelect={handleFolderSelect}
              />
            )}
          </div>
        </aside>

        {/* Right content area */}
        <main className="flex-1 flex flex-col min-h-0">
          {!selectedFolderId && (
            <div className="flex items-center justify-center h-full text-gray-400 text-sm">
              Select a folder to get started
            </div>
          )}

          {selectedFolderId && folder && (
            <>
              {/* Top section — Folder content (max 50% height) */}
              <div className="max-h-[50%] border-b border-gray-200 bg-white overflow-hidden shrink-0">
                <FolderContent folder={folder} />
              </div>

              {/* Bottom section — Course list + Course viewer */}
              <div className="flex flex-1 min-h-0">
                {/* Bottom left — Course list */}
                <div className="w-72 border-r border-gray-200 bg-white shrink-0 overflow-hidden">
                  <CourseList
                    courses={folder.courses}
                    selectedCourseId={selectedCourseId}
                    onSelect={setSelectedCourseId}
                  />
                </div>

                {/* Bottom right — Course content viewer */}
                <div className="flex-1 bg-white overflow-hidden">
                  {!selectedCourseId && (
                    <div className="flex items-center justify-center h-full text-gray-400 text-sm">
                      Select a course to view its content
                    </div>
                  )}
                  {selectedCourseId && course && (
                    <CourseViewer course={course} />
                  )}
                </div>
              </div>
            </>
          )}
        </main>
      </div>
    </div>
  )
}
