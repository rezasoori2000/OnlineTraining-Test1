import { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Button } from '@/components/ui/Button'
import { EditChapterTree } from '@/features/course/EditChapterTree'
import { EditChapterEditor } from '@/features/course/EditChapterEditor'
import { useEditCourseBuilder, buildUpdatePayload, findNode } from '@/features/course/useEditCourseBuilder'
import { apiClient } from '@/services/api/apiClient'
import type { CourseDetailDto, UpdateCourseRequest } from '@/types/editCourse'

const EMPTY_COURSE: CourseDetailDto = {
  id: '',
  slug: '',
  status: '',
  title: '',
  description: null,
  languageCode: 'en',
  activeVersionId: '',
  activeVersionNumber: 0,
  isVersionPublished: false,
  chapters: [],
}

export function EditCoursePage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [selectedChapterId, setSelectedChapterId] = useState<string | null>(null)

  // ── Fetch course detail ─────────────────────────────────────────────────────
  const { data: courseDto, isLoading, isError, error } = useQuery<CourseDetailDto, Error>({
    queryKey: ['course', id],
    queryFn: () => apiClient.get<CourseDetailDto>(`/courses/${id}`),
    enabled: !!id,
    staleTime: 0,
  })

  // ── Builder state ──────────────────────────────────────────────────────────
  const { state, dispatch } = useEditCourseBuilder(courseDto ?? EMPTY_COURSE)

  useEffect(() => {
    if (courseDto) dispatch({ type: 'RESET', dto: courseDto })
  }, [courseDto])

  // Auto-deselect if the selected chapter was deleted
  useEffect(() => {
    if (!selectedChapterId) return
    const node = findNode(state.chapters, selectedChapterId)
    if (!node || node.isDeleted) setSelectedChapterId(null)
  }, [state.chapters, selectedChapterId])

  // ── Save mutation ──────────────────────────────────────────────────────────
  const mutation = useMutation({
    mutationFn: (payload: UpdateCourseRequest) =>
      apiClient.put<CourseDetailDto>(`/courses/${id}`, payload),
    onSuccess: (updated) => {
      queryClient.setQueryData(['course', id], updated)
      queryClient.invalidateQueries({ queryKey: ['courses'] })
      dispatch({ type: 'RESET', dto: updated })
    },
  })

  const handleSave = () => mutation.mutate(buildUpdatePayload(state))

  // ── Loading / error states ─────────────────────────────────────────────────
  if (isLoading) {
    return <div className="p-8 text-center text-gray-500 animate-pulse">Loading course…</div>
  }
  if (isError) {
    return (
      <div className="p-8 text-center text-red-600">
        Failed to load course: {error?.message}
      </div>
    )
  }

  const selectedNode = selectedChapterId ? findNode(state.chapters, selectedChapterId) : null

  // ── Two-column layout ──────────────────────────────────────────────────────
  return (
    <div className="flex gap-0 -m-6 min-h-full">

      {/* ── Left: sticky structure panel ───────────────────────────────────── */}
      <aside className="w-64 shrink-0 bg-white border-r border-gray-200 flex flex-col sticky top-0 self-start max-h-screen">
        {/* Panel header */}
        <div className="px-4 py-3 border-b border-gray-200 shrink-0">
          <p className="text-xs font-semibold text-gray-500 uppercase tracking-wide">Structure</p>
        </div>

        {/* Course Info nav item */}
        <button
          type="button"
          onClick={() => setSelectedChapterId(null)}
          className={`flex items-center gap-2 px-4 py-2.5 text-sm border-b border-gray-100 transition-colors text-left ${
            selectedChapterId === null
              ? 'bg-indigo-50 text-indigo-800 font-medium'
              : 'text-gray-600 hover:bg-gray-50'
          }`}
        >
          <span className="text-base">📋</span>
          Course Info
        </button>

        {/* Chapter tree (scrollable) */}
        <div className="flex-1 overflow-y-auto p-3">
          <EditChapterTree
            chapters={state.chapters}
            dispatch={dispatch}
            selectedChapterId={selectedChapterId}
            onSelectChapter={setSelectedChapterId}
          />
        </div>
      </aside>

      {/* ── Right: header + editor ─────────────────────────────────────────── */}
      <div className="flex-1 min-w-0 flex flex-col">
        {/* Sticky page header */}
        <div className="shrink-0 px-6 py-4 bg-white border-b border-gray-200 flex items-center justify-between sticky top-0 z-10">
          <div>
            <h1 className="text-xl font-bold text-gray-900">Edit Course</h1>
            <p className="mt-0.5 text-xs text-gray-500">
              Version {courseDto?.activeVersionNumber ?? '–'}
              {state.isVersionPublished ? (
                <span className="ml-1.5 px-1.5 py-0.5 bg-green-100 text-green-700 rounded">
                  Published — saving will create a new draft version
                </span>
              ) : (
                <span className="ml-1.5 px-1.5 py-0.5 bg-amber-100 text-amber-700 rounded">
                  Draft
                </span>
              )}
            </p>
          </div>
          <div className="flex items-center gap-3">
            <button
              type="button"
              onClick={() => navigate('/courses')}
              className="px-4 py-2 text-sm text-gray-600 border border-gray-300 rounded-md hover:bg-gray-50"
            >
              Cancel
            </button>
            <Button type="button" onClick={handleSave} disabled={mutation.isPending}>
              {mutation.isPending ? 'Saving…' : 'Save Changes'}
            </Button>
          </div>
        </div>

        {/* Editor area */}
        <div className="flex-1 p-6">
          {/* Banners */}
          {mutation.isError && (
            <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded-md text-sm text-red-700">
              {(mutation.error as Error)?.message ?? 'Save failed.'}
            </div>
          )}
          {mutation.isSuccess && courseDto?.status === 'NewVersionCreated' && (
            <div className="mb-4 p-3 bg-blue-50 border border-blue-200 rounded-md text-sm text-blue-700">
              The previous version was published. A new draft version has been created with your changes.
            </div>
          )}

          {/* Content: course info form or chapter editor */}
          {selectedNode === null ? (
            /* Course Info form */
            <div className="max-w-2xl space-y-5">
              <div className="bg-white border border-gray-200 rounded-lg p-6 space-y-4">
                <h2 className="text-sm font-semibold text-gray-800">Course Info</h2>
                <div>
                  <label className="block text-xs font-medium text-gray-600 mb-1">
                    Title <span className="text-red-500">*</span>
                  </label>
                  <input
                    type="text"
                    value={state.title}
                    onChange={(e) => dispatch({ type: 'SET_TITLE', title: e.target.value })}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
                    placeholder="Course title"
                  />
                </div>
                <div>
                  <label className="block text-xs font-medium text-gray-600 mb-1">Description</label>
                  <textarea
                    rows={4}
                    value={state.description}
                    onChange={(e) => dispatch({ type: 'SET_DESCRIPTION', description: e.target.value })}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 resize-none"
                    placeholder="Brief course description"
                  />
                </div>
              </div>
              <p className="text-xs text-gray-400">
                Select a chapter or section in the tree on the left to edit its content.
              </p>
            </div>
          ) : (
            /* Chapter / Section editor */
            <div className="max-w-2xl">
              <EditChapterEditor node={selectedNode} dispatch={dispatch} />
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
