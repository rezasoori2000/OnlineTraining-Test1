import { useState } from 'react'
import type { FolderTreeNode } from '@/types/portal'

interface FolderTreeProps {
  nodes: FolderTreeNode[]
  selectedId: string | null
  onSelect: (id: string) => void
}

export function FolderTree({ nodes, selectedId, onSelect }: FolderTreeProps) {
  return (
    <div className="overflow-y-auto h-full">
      {nodes.map(node => (
        <TreeNode
          key={node.id}
          node={node}
          selectedId={selectedId}
          onSelect={onSelect}
          depth={0}
        />
      ))}
    </div>
  )
}

function TreeNode({
  node,
  selectedId,
  onSelect,
  depth,
}: {
  node: FolderTreeNode
  selectedId: string | null
  onSelect: (id: string) => void
  depth: number
}) {
  const [expanded, setExpanded] = useState(true)
  const hasChildren = node.children.length > 0
  const isSelected = node.id === selectedId

  return (
    <div>
      <div
        className={`flex items-center gap-1 px-2 py-1.5 cursor-pointer rounded-md transition-colors ${
          isSelected
            ? 'bg-brand-100 text-brand-700 font-medium'
            : 'hover:bg-gray-100 text-gray-700'
        }`}
        style={{ paddingLeft: `${depth * 16 + 8}px` }}
        onClick={() => onSelect(node.id)}
      >
        {hasChildren ? (
          <button
            className="w-4 h-4 flex items-center justify-center text-gray-400 shrink-0"
            onClick={e => {
              e.stopPropagation()
              setExpanded(!expanded)
            }}
          >
            <svg
              className={`w-3 h-3 transition-transform ${expanded ? 'rotate-90' : ''}`}
              fill="currentColor"
              viewBox="0 0 20 20"
            >
              <path d="M6 4l8 6-8 6V4z" />
            </svg>
          </button>
        ) : (
          <span className="w-4 shrink-0" />
        )}
        <svg className="w-4 h-4 text-yellow-500 shrink-0" fill="currentColor" viewBox="0 0 20 20">
          <path d="M2 6a2 2 0 012-2h5l2 2h5a2 2 0 012 2v6a2 2 0 01-2 2H4a2 2 0 01-2-2V6z" />
        </svg>
        <span className="truncate text-sm">{node.name}</span>
      </div>
      {hasChildren && expanded && (
        <div>
          {node.children.map(child => (
            <TreeNode
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
