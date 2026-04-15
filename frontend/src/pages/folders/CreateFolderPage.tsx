import { useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { useQuery, useMutation } from '@tanstack/react-query'
import { Button } from '@/components/ui/Button'
import { apiClient } from '@/services/api/apiClient'
import {
  FolderDetail,
  FolderTreeNode,
  CreateFolderRequest,
  FolderAttributeRequest,
} from '@/types/folder'
import { cn } from '@/utils/helpers'

/* ── Tree node for parent selection ── */

function ParentTreeNode({
  node,
  selectedId,
  onSelect,
  depth = 0,
}: {
  node: FolderTreeNode
  selectedId: string | null
  onSelect: (id: string | null) => void
  depth?: number
}) {
  const [expanded, setExpanded] = useState(true)
  const isSelected = node.id === selectedId
  const hasChildren = node.children.length > 0

  return (
    <div>
      <div
        className={cn(
          'flex items-center gap-1 px-2 py-1.5 rounded cursor-pointer text-theme-sm transition-colors',
          isSelected
            ? 'bg-brand-50 text-brand-700 font-medium'
            : 'text-gray-700 hover:bg-gray-100',
        )}
        style={{ paddingLeft: `${depth * 16 + 8}px` }}
        onClick={() => onSelect(node.id)}
      >
        {hasChildren ? (
          <button
            className="flex-shrink-0 w-4 h-4 flex items-center justify-center text-gray-400 hover:text-gray-600"
            onClick={(e) => {
              e.stopPropagation()
              setExpanded(!expanded)
            }}
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
            <ParentTreeNode
              key={child.id}
              node={child}
              selectedId={selectedId}
              onSelect={onSelect}
              depth={depth + 1}
            />
          ))}
        </div>
      )}
    </div>
  )
}

/* ── Main page ── */

export function CreateFolderPage() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const initialParentId = searchParams.get('parentId')

  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [parentId, setParentId] = useState<string | null>(initialParentId)
  const [attributes, setAttributes] = useState<FolderAttributeRequest[]>([])
  const [error, setError] = useState<string | null>(null)

  // Fetch full tree for parent selection
  const { data: tree = [] } = useQuery<FolderTreeNode[]>({
    queryKey: ['folders-tree'],
    queryFn: () => apiClient.get<FolderTreeNode[]>('/folders/tree'),
  })

  const createMutation = useMutation({
    mutationFn: (req: CreateFolderRequest) =>
      apiClient.post<FolderDetail>('/folders', req),
    onSuccess: (data) => {
      navigate(`/folders/${data.id}`)
    },
    onError: (err: Error) => setError(err.message),
  })

  const handleSubmit = () => {
    if (!name.trim()) {
      setError('Name is required.')
      return
    }
    setError(null)
    createMutation.mutate({
      name: name.trim(),
      description: description.trim() || null,
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

  const parentName = parentId
    ? findNodeName(tree, parentId) ?? 'Unknown'
    : 'None (root)'

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h2 className="text-title-sm font-bold text-gray-900">Create Folder</h2>
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
        {/* Left: Parent selection tree */}
        <div className="w-72 flex-shrink-0">
          <div className="card p-4 sticky top-24 max-h-[calc(100vh-120px)] overflow-y-auto">
            <h3 className="text-theme-sm font-semibold text-gray-600 uppercase tracking-wider mb-3">
              Parent Folder
            </h3>
            <div
              className={cn(
                'px-2 py-1.5 rounded cursor-pointer text-theme-sm transition-colors mb-1',
                parentId === null
                  ? 'bg-brand-50 text-brand-700 font-medium'
                  : 'text-gray-700 hover:bg-gray-100',
              )}
              onClick={() => setParentId(null)}
            >
              🏠 Root (no parent)
            </div>
            {tree.map((root) => (
              <ParentTreeNode
                key={root.id}
                node={root}
                selectedId={parentId}
                onSelect={setParentId}
              />
            ))}
          </div>
        </div>

        {/* Right: Form */}
        <div className="flex-1">
          <div className="card p-5 xl:p-6 space-y-4">
            <h3 className="text-lg font-semibold text-gray-900">Folder Info</h3>

            <div>
              <label className="form-label">Parent</label>
              <p className="text-theme-sm text-gray-600">{parentName}</p>
            </div>

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
                onClick={handleSubmit}
                disabled={createMutation.isPending || !name.trim()}
              >
                {createMutation.isPending ? 'Creating…' : 'Create Folder'}
              </Button>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}

function findNodeName(
  nodes: FolderTreeNode[],
  id: string,
): string | null {
  for (const node of nodes) {
    if (node.id === id) return node.name
    const found = findNodeName(node.children, id)
    if (found) return found
  }
  return null
}
