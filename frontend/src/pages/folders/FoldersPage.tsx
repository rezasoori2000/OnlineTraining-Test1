import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { ColumnDef } from '@tanstack/react-table'
import { DataTable } from '@/components/ui/DataTable'
import { Button } from '@/components/ui/Button'
import { apiClient } from '@/services/api/apiClient'
import { FolderListItem, FolderTreeNode } from '@/types/folder'
import { formatDate, cn } from '@/utils/helpers'

/* ── Context menu ── */

interface ContextMenuState {
  x: number
  y: number
  folderId: string
  folderName: string
}

function ContextMenu({
  state,
  onClose,
  onSelect,
  onAddChild,
}: {
  state: ContextMenuState
  onClose: () => void
  onSelect: () => void
  onAddChild: () => void
}) {
  const ref = useRef<HTMLDivElement>(null)

  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) onClose()
    }
    document.addEventListener('mousedown', handler)
    return () => document.removeEventListener('mousedown', handler)
  }, [onClose])

  return (
    <div
      ref={ref}
      className="fixed z-[999999] bg-white rounded-lg shadow-lg border border-gray-200 py-1 min-w-[160px]"
      style={{ left: state.x, top: state.y }}
    >
      <button
        className="w-full text-left px-4 py-2 text-theme-sm text-gray-700 hover:bg-gray-100 flex items-center gap-2"
        onClick={onSelect}
      >
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
          <path d="M11 4H4a2 2 0 00-2 2v14a2 2 0 002 2h14a2 2 0 002-2v-7" />
          <path d="M18.5 2.5a2.121 2.121 0 013 3L12 15l-4 1 1-4 9.5-9.5z" />
        </svg>
        Select
      </button>
      <button
        className="w-full text-left px-4 py-2 text-theme-sm text-gray-700 hover:bg-gray-100 flex items-center gap-2"
        onClick={onAddChild}
      >
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
          <line x1="12" y1="5" x2="12" y2="19" />
          <line x1="5" y1="12" x2="19" y2="12" />
        </svg>
        Add Folder
      </button>
    </div>
  )
}

/* ── Tree node ── */

function TreeNode({
  node,
  depth = 0,
  onContextMenu,
}: {
  node: FolderTreeNode
  depth?: number
  onContextMenu: (e: React.MouseEvent, node: FolderTreeNode) => void
}) {
  const [expanded, setExpanded] = useState(true)
  const hasChildren = node.children.length > 0

  return (
    <div>
      <div
        className="flex items-center gap-1 px-2 py-1.5 rounded text-theme-sm text-gray-700 hover:bg-gray-100 cursor-default select-none"
        style={{ paddingLeft: `${depth * 16 + 8}px` }}
        onContextMenu={(e) => onContextMenu(e, node)}
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
          className="flex-shrink-0 text-yellow-500"
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
              depth={depth + 1}
              onContextMenu={onContextMenu}
            />
          ))}
        </div>
      )}
    </div>
  )
}

/* ── Main page ── */

export function FoldersPage() {
  const navigate = useNavigate()
  const [search, setSearch] = useState('')
  const [contextMenu, setContextMenu] = useState<ContextMenuState | null>(null)

  const { data: folders = [], isLoading, isError } = useQuery<FolderListItem[]>({
    queryKey: ['folders'],
    queryFn: () => apiClient.get<FolderListItem[]>('/folders'),
  })

  const { data: tree = [] } = useQuery<FolderTreeNode[]>({
    queryKey: ['folders-tree'],
    queryFn: () => apiClient.get<FolderTreeNode[]>('/folders/tree'),
  })

  const handleTreeContextMenu = useCallback(
    (e: React.MouseEvent, node: FolderTreeNode) => {
      e.preventDefault()
      setContextMenu({ x: e.clientX, y: e.clientY, folderId: node.id, folderName: node.name })
    },
    [],
  )

  const filtered = useMemo(() => {
    return folders.filter((f) =>
      f.name.toLowerCase().includes(search.toLowerCase()),
    )
  }, [folders, search])

  const truncate = (text: string | null, max: number) => {
    if (!text) return '—'
    return text.length > max ? text.substring(0, max) + '…' : text
  }

  const columns = useMemo<ColumnDef<FolderListItem, unknown>[]>(
    () => [
      {
        accessorKey: 'name',
        header: 'Name',
      },
      {
        accessorKey: 'description',
        header: 'Description',
        cell: ({ getValue }) => truncate(getValue() as string | null, 25),
      },
      {
        accessorKey: 'parentName',
        header: 'Parent',
        cell: ({ getValue }) => (getValue() as string | null) ?? '—',
      },
      {
        id: 'detail',
        header: 'Detail',
        cell: ({ row }) => {
          const count = row.original.childrenCount
          if (count === 0) return <span className="text-gray-400">No children</span>
          return (
            <button
              className="text-brand-500 hover:text-brand-600 underline text-theme-sm font-medium"
              onClick={() => navigate(`/folders/${row.original.id}`)}
            >
              {count} {count === 1 ? 'child' : 'children'}
            </button>
          )
        },
      },
      {
        accessorKey: 'updatedAt',
        header: 'Updated At',
        cell: ({ getValue }) => formatDate(getValue() as string),
      },
      {
        id: 'actions',
        header: 'Actions',
        cell: ({ row }) => (
          <Button
            variant="secondary"
            onClick={() => navigate(`/folders/${row.original.id}`)}
          >
            Edit
          </Button>
        ),
      },
    ],
    [navigate],
  )

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h2 className="text-title-sm font-bold text-gray-900">Folders</h2>
        <Button onClick={() => navigate('/folders/create')}>Create Folder</Button>
      </div>

      <div className="flex gap-6">
        {/* Left: Folder hierarchy tree */}
        <div className="w-72 flex-shrink-0">
          <div className="card p-4 sticky top-24 max-h-[calc(100vh-120px)] overflow-y-auto">
            <h3 className="text-theme-sm font-semibold text-gray-600 uppercase tracking-wider mb-3">
              Folder Tree
            </h3>
            <p className="text-xs text-gray-400 mb-3">Right-click a folder for options</p>
            {tree.length === 0 ? (
              <p className="text-theme-sm text-gray-400">No folders yet.</p>
            ) : (
              tree.map((root) => (
                <TreeNode
                  key={root.id}
                  node={root}
                  onContextMenu={handleTreeContextMenu}
                />
              ))
            )}
          </div>
        </div>

        {/* Right: Table */}
        <div className="flex-1">
          <div className="card p-5 xl:p-6 space-y-4">
            <div className="flex flex-col sm:flex-row gap-3">
              <input
                type="text"
                placeholder="Search by name..."
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                className="form-input sm:w-72"
              />
            </div>

            {isLoading ? (
              <p className="text-theme-sm text-gray-500 py-4">Loading folders…</p>
            ) : isError ? (
              <p className="text-theme-sm text-red-500 py-4">Failed to load folders.</p>
            ) : (
              <DataTable columns={columns} data={filtered} />
            )}
          </div>
        </div>
      </div>

      {/* Context menu */}
      {contextMenu && (
        <ContextMenu
          state={contextMenu}
          onClose={() => setContextMenu(null)}
          onSelect={() => {
            navigate(`/folders/${contextMenu.folderId}`)
            setContextMenu(null)
          }}
          onAddChild={() => {
            navigate(`/folders/create?parentId=${contextMenu.folderId}`)
            setContextMenu(null)
          }}
        />
      )}
    </div>
  )
}
