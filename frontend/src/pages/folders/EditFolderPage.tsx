import { useRef, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Button } from '@/components/ui/Button'
import { apiClient } from '@/services/api/apiClient'
import {
  FolderDetail,
  FolderTreeNode,
  UpdateFolderRequest,
  FolderAttributeRequest,
  AssignCourseRequest,
} from '@/types/folder'
import { Course } from '@/types/course'
import { cn } from '@/utils/helpers'
import { convertFileToHtml } from '@/utils/convertFileToHtml'

/* ── Folder tree node component ── */

function TreeNode({
  node,
  selectedId,
  depth = 0,
}: {
  node: FolderTreeNode
  selectedId: string
  depth?: number
}) {
  const [expanded, setExpanded] = useState(true)
  const isSelected = node.id === selectedId
  const hasChildren = node.children.length > 0

  return (
    <div>
      <div
        className={cn(
          'flex items-center gap-1 px-2 py-1.5 rounded text-theme-sm transition-colors',
          isSelected
            ? 'bg-brand-50 text-brand-700 font-medium'
            : 'text-gray-700',
        )}
        style={{ paddingLeft: `${depth * 16 + 8}px` }}
      >
        {hasChildren ? (
          <button
            className="flex-shrink-0 w-4 h-4 flex items-center justify-center text-gray-400 hover:text-gray-600"
            onClick={() => setExpanded(!expanded)}
          >
            <svg
              width="12"
              height="12"
              viewBox="0 0 12 12"
              className={cn('transition-transform', expanded ? 'rotate-90' : '')}
              fill="currentColor"
            >
              <path d="M4.5 2L8.5 6L4.5 10V2Z" />
            </svg>
          </button>
        ) : (
          <span className="w-4" />
        )}
        <svg
          width="16"
          height="16"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="1.5"
          className="flex-shrink-0"
        >
          <path d="M3 7v10a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2h-6l-2-2H5a2 2 0 00-2 2z" />
        </svg>
        <span className="truncate">{node.name}</span>
      </div>
      {hasChildren && expanded && (
        <div>
          {node.children.map((child) => (
            <TreeNode
              key={child.id}
              node={child}
              selectedId={selectedId}
              depth={depth + 1}
            />
          ))}
        </div>
      )}
    </div>
  )
}

/* ── Main page ── */

export function EditFolderPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  // Form state
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [attributes, setAttributes] = useState<FolderAttributeRequest[]>([])
  const [parentId, setParentId] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [initialized, setInitialized] = useState(false)

  // Content state
  const [htmlContent, setHtmlContent] = useState<string>('')
  const [isConverting, setIsConverting] = useState(false)
  const [contentCollapsed, setContentCollapsed] = useState(false)
  const fileInputRef = useRef<HTMLInputElement>(null)

  // Course assign modal
  const [showCourseModal, setShowCourseModal] = useState(false)

  // Fetch folder detail
  const { data: folder, isLoading: folderLoading } = useQuery<FolderDetail>({
    queryKey: ['folder', id],
    queryFn: () => apiClient.get<FolderDetail>(`/folders/${id}`),
    enabled: !!id,
  })

  // Fetch full tree
  const { data: tree = [] } = useQuery<FolderTreeNode[]>({
    queryKey: ['folders-tree'],
    queryFn: () => apiClient.get<FolderTreeNode[]>('/folders/tree'),
  })

  // Fetch all courses for assignment
  const { data: allCourses = [] } = useQuery<Course[]>({
    queryKey: ['courses'],
    queryFn: () => apiClient.get<Course[]>('/courses'),
    enabled: showCourseModal,
  })

  // Initialize form when folder data arrives
  if (folder && !initialized) {
    setName(folder.name)
    setDescription(folder.description ?? '')
    setHtmlContent(folder.htmlContent ?? '')
    setParentId(folder.parentId)
    setAttributes(
      folder.attributes.map((a) => ({ key: a.key, value: a.value })),
    )
    setInitialized(true)
  }

  // Update mutation
  const updateMutation = useMutation({
    mutationFn: (req: UpdateFolderRequest) =>
      apiClient.put<FolderDetail>(`/folders/${id}`, req),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['folder', id] })
      queryClient.invalidateQueries({ queryKey: ['folders'] })
      queryClient.invalidateQueries({ queryKey: ['folders-tree'] })
      setError(null)
    },
    onError: (err: Error) => setError(err.message),
  })

  // Assign course mutation
  const assignMutation = useMutation({
    mutationFn: (req: AssignCourseRequest) =>
      apiClient.post(`/folders/${id}/courses`, req),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['folder', id] })
      setShowCourseModal(false)
    },
    onError: (err: Error) => setError(err.message),
  })

  // Remove course mutation
  const removeMutation = useMutation({
    mutationFn: (courseId: string) =>
      apiClient.delete(`/folders/${id}/courses/${courseId}`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['folder', id] })
    },
    onError: (err: Error) => setError(err.message),
  })

  const handleSave = () => {
    updateMutation.mutate({
      name: name.trim(),
      description: description.trim() || null,
      htmlContent: htmlContent.trim() || null,
      parentId,
      attributes: attributes.length > 0 ? attributes : null,
    })
  }

  const handleAddAttribute = () => {
    setAttributes([...attributes, { key: '', value: '' }])
  }

  const handleRemoveAttribute = (index: number) => {
    setAttributes(attributes.filter((_, i) => i !== index))
  }

  const handleAttributeChange = (
    index: number,
    field: 'key' | 'value',
    value: string,
  ) => {
    const updated = [...attributes]
    updated[index] = { ...updated[index], [field]: value }
    setAttributes(updated)
  }

  // Courses not already assigned
  const availableCourses = allCourses.filter(
    (c) => !folder?.courses.some((fc) => fc.courseId === c.id),
  )

  const handleFileImport = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file) return

    const ext = file.name.split('.').pop()?.toLowerCase()
    if (!ext || !['pdf', 'pptx', 'ppt'].includes(ext)) {
      setError('Only PDF and PPTX/PPT files are supported.')
      return
    }
    if (file.size > 50 * 1024 * 1024) {
      setError('File size must be under 50 MB.')
      return
    }

    setIsConverting(true)
    setError(null)
    try {
      const html = await convertFileToHtml(file)
      setHtmlContent(html)
      setContentCollapsed(false)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'File conversion failed.')
    } finally {
      setIsConverting(false)
      if (fileInputRef.current) fileInputRef.current.value = ''
    }
  }

  if (folderLoading) {
    return <p className="text-theme-sm text-gray-500 p-6">Loading folder…</p>
  }

  if (!folder) {
    return <p className="text-theme-sm text-red-500 p-6">Folder not found.</p>
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h2 className="text-title-sm font-bold text-gray-900">Edit Folder</h2>
        <Button variant="secondary" onClick={() => navigate('/folders')}>
          Back to Folders
        </Button>
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 rounded-lg px-4 py-3 text-theme-sm">
          {error}
        </div>
      )}

      <div className="flex gap-6">
        {/* Left: Folder tree */}
        <div className="w-72 flex-shrink-0">
          <div className="card p-4 sticky top-24 max-h-[calc(100vh-120px)] overflow-y-auto">
            <h3 className="text-theme-sm font-semibold text-gray-600 uppercase tracking-wider mb-3">
              Folder Tree
            </h3>
            {tree.length === 0 ? (
              <p className="text-theme-sm text-gray-400">No folders yet.</p>
            ) : (
              tree.map((root) => (
                <TreeNode
                  key={root.id}
                  node={root}
                  selectedId={id!}
                />
              ))
            )}
          </div>
        </div>

        {/* Right: Edit form */}
        <div className="flex-1 space-y-6">
          {/* Basic info */}
          <div className="card p-5 xl:p-6 space-y-4">
            <h3 className="text-lg font-semibold text-gray-900">Folder Info</h3>

            <div>
              <label className="form-label">Name</label>
              <input
                type="text"
                value={name}
                onChange={(e) => setName(e.target.value)}
                className="form-input w-full"
                placeholder="Folder name…"
              />
            </div>

            <div>
              <label className="form-label">Description</label>
              <textarea
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                className="form-textarea w-full"
                rows={3}
                placeholder="Description…"
              />
            </div>

            {/* Attributes */}
            <div>
              <div className="flex items-center justify-between mb-2">
                <label className="form-label mb-0">Attributes</label>
                <Button
                  variant="secondary"
                  className="text-xs px-3 py-1.5"
                  onClick={handleAddAttribute}
                >
                  + Add Attribute
                </Button>
              </div>
              {attributes.length === 0 ? (
                <p className="text-theme-sm text-gray-400">No attributes.</p>
              ) : (
                <div className="space-y-2">
                  {attributes.map((attr, i) => (
                    <div key={i} className="flex gap-2 items-start">
                      <input
                        type="text"
                        value={attr.key}
                        onChange={(e) =>
                          handleAttributeChange(i, 'key', e.target.value)
                        }
                        className="form-input flex-1"
                        placeholder="Key"
                      />
                      <input
                        type="text"
                        value={attr.value}
                        onChange={(e) =>
                          handleAttributeChange(i, 'value', e.target.value)
                        }
                        className="form-input flex-1"
                        placeholder="Value"
                      />
                      <Button
                        variant="danger"
                        className="text-xs px-2 py-2"
                        onClick={() => handleRemoveAttribute(i)}
                      >
                        ✕
                      </Button>
                    </div>
                  ))}
                </div>
              )}
            </div>

            <div className="pt-2">
              <Button
                onClick={handleSave}
                disabled={updateMutation.isPending || !name.trim()}
              >
                {updateMutation.isPending ? 'Saving…' : 'Save Changes'}
              </Button>
            </div>
          </div>

          {/* Content — collapsible */}
          <div className="card overflow-hidden">
            <button
              className="w-full flex items-center justify-between p-5 xl:px-6 text-left hover:bg-gray-50 transition-colors"
              onClick={() => setContentCollapsed(!contentCollapsed)}
            >
              <h3 className="text-lg font-semibold text-gray-900">Content</h3>
              <svg
                width="20"
                height="20"
                viewBox="0 0 20 20"
                fill="currentColor"
                className={cn(
                  'text-gray-400 transition-transform',
                  contentCollapsed ? '' : 'rotate-180',
                )}
              >
                <path
                  fillRule="evenodd"
                  d="M5.23 7.21a.75.75 0 011.06.02L10 11.168l3.71-3.938a.75.75 0 111.08 1.04l-4.25 4.5a.75.75 0 01-1.08 0l-4.25-4.5a.75.75 0 01.02-1.06z"
                  clipRule="evenodd"
                />
              </svg>
            </button>

            {!contentCollapsed && (
              <div className="px-5 xl:px-6 pb-5 xl:pb-6 space-y-4 border-t border-gray-100 pt-4">
                {/* Import button */}
                <div className="flex items-center gap-3">
                  <input
                    ref={fileInputRef}
                    type="file"
                    accept=".pdf,.pptx,.ppt"
                    onChange={handleFileImport}
                    className="hidden"
                  />
                  <Button
                    variant="secondary"
                    onClick={() => fileInputRef.current?.click()}
                    disabled={isConverting}
                  >
                    {isConverting ? 'Converting…' : 'Import PPTX / PDF'}
                  </Button>
                  {isConverting && (
                    <span className="text-theme-sm text-gray-500">
                      Converting file to HTML…
                    </span>
                  )}
                </div>

                {/* HTML textarea */}
                <div>
                  <label className="form-label">HTML Content</label>
                  <textarea
                    value={htmlContent}
                    onChange={(e) => setHtmlContent(e.target.value)}
                    className="form-textarea w-full font-mono text-xs"
                    rows={10}
                    placeholder="Enter HTML content or import a file…"
                  />
                </div>

                {/* HTML preview */}
                {htmlContent && (
                  <div>
                    <label className="form-label">Preview</label>
                    <div
                      className="border border-gray-200 rounded-lg p-4 bg-white max-h-[500px] overflow-y-auto"
                      dangerouslySetInnerHTML={{ __html: htmlContent }}
                    />
                  </div>
                )}
              </div>
            )}
          </div>

          {/* Assigned Courses */}
          <div className="card p-5 xl:p-6 space-y-4">
            <div className="flex items-center justify-between">
              <h3 className="text-lg font-semibold text-gray-900">
                Assigned Courses
              </h3>
              <Button onClick={() => setShowCourseModal(true)}>
                + Add Course
              </Button>
            </div>

            {folder.courses.length === 0 ? (
              <p className="text-theme-sm text-gray-400 py-4">
                No courses assigned to this folder.
              </p>
            ) : (
              <div className="overflow-x-auto rounded-lg border border-gray-200">
                <table className="min-w-full divide-y divide-gray-200 bg-white text-sm">
                  <thead className="bg-gray-50">
                    <tr>
                      <th className="px-4 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">
                        Title
                      </th>
                      <th className="px-4 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">
                        Status
                      </th>
                      <th className="px-4 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">
                        Actions
                      </th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-100">
                    {folder.courses.map((fc) => (
                      <tr
                        key={fc.courseId}
                        className="hover:bg-gray-50 transition-colors"
                      >
                        <td className="px-4 py-3 text-gray-700">{fc.title}</td>
                        <td className="px-4 py-3">
                          <span
                            className={cn(
                              'inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium',
                              fc.status === 'Published'
                                ? 'bg-green-100 text-green-800'
                                : fc.status === 'Draft'
                                  ? 'bg-yellow-100 text-yellow-800'
                                  : 'bg-gray-100 text-gray-600',
                            )}
                          >
                            {fc.status}
                          </span>
                        </td>
                        <td className="px-4 py-3">
                          <Button
                            variant="danger"
                            className="text-xs px-3 py-1.5"
                            onClick={() => removeMutation.mutate(fc.courseId)}
                            disabled={removeMutation.isPending}
                          >
                            Remove
                          </Button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>

          {/* Children */}
          {folder.children.length > 0 && (
            <div className="card p-5 xl:p-6 space-y-4">
              <h3 className="text-lg font-semibold text-gray-900">
                Children Folders
              </h3>
              <div className="overflow-x-auto rounded-lg border border-gray-200">
                <table className="min-w-full divide-y divide-gray-200 bg-white text-sm">
                  <thead className="bg-gray-50">
                    <tr>
                      <th className="px-4 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">
                        Name
                      </th>
                      <th className="px-4 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">
                        Actions
                      </th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-100">
                    {folder.children.map((child) => (
                      <tr
                        key={child.id}
                        className="hover:bg-gray-50 transition-colors"
                      >
                        <td className="px-4 py-3 text-gray-700">
                          {child.name}
                        </td>
                        <td className="px-4 py-3">
                          <Button
                            variant="secondary"
                            className="text-xs"
                            onClick={() => navigate(`/folders/${child.id}`)}
                          >
                            Edit
                          </Button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}
        </div>
      </div>

      {/* Course assignment modal */}
      {showCourseModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center">
          <div
            className="absolute inset-0 bg-black/40"
            onClick={() => setShowCourseModal(false)}
          />
          <div className="relative bg-white rounded-xl shadow-lg w-full max-w-lg mx-4 p-6 max-h-[80vh] overflow-y-auto">
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-lg font-semibold text-gray-900">
                Add Course to Folder
              </h3>
              <button
                className="text-gray-400 hover:text-gray-600"
                onClick={() => setShowCourseModal(false)}
              >
                ✕
              </button>
            </div>

            {availableCourses.length === 0 ? (
              <p className="text-theme-sm text-gray-400 py-4">
                No available courses to assign.
              </p>
            ) : (
              <div className="space-y-2">
                {availableCourses.map((course) => (
                  <div
                    key={course.id}
                    className="flex items-center justify-between p-3 rounded-lg border border-gray-200 hover:bg-gray-50"
                  >
                    <div>
                      <div className="font-medium text-gray-900 text-theme-sm">
                        {course.title}
                      </div>
                      <div className="text-xs text-gray-500">
                        {course.status} · v{course.version}
                      </div>
                    </div>
                    <Button
                      className="text-xs px-3 py-1.5"
                      onClick={() =>
                        assignMutation.mutate({ courseId: course.id })
                      }
                      disabled={assignMutation.isPending}
                    >
                      Add
                    </Button>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  )
}
