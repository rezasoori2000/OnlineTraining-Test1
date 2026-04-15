import { useState } from 'react'
import type { ChapterNode } from '@/types/portal'

interface ChapterTreeProps {
  chapters: ChapterNode[]
  selectedChapterId: string | null
  onSelect: (id: string) => void
}

export function ChapterTree({ chapters, selectedChapterId, onSelect }: ChapterTreeProps) {
  return (
    <div className="overflow-y-auto">
      {chapters.map(ch => (
        <ChapterTreeNode
          key={ch.id}
          node={ch}
          selectedId={selectedChapterId}
          onSelect={onSelect}
          depth={0}
        />
      ))}
    </div>
  )
}

function ChapterTreeNode({
  node,
  selectedId,
  onSelect,
  depth,
}: {
  node: ChapterNode
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
        className={`flex items-center gap-1 px-2 py-1 rounded-md text-sm transition-colors ${
          isSelected
            ? 'bg-brand-100 text-brand-700 font-medium'
            : node.hasContent
              ? 'hover:bg-gray-100 text-gray-700 cursor-pointer'
              : 'text-gray-500'
        }`}
        style={{ paddingLeft: `${depth * 14 + 6}px` }}
        onClick={() => {
          if (node.hasContent) onSelect(node.id)
        }}
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
        <svg className="w-3.5 h-3.5 shrink-0 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
          <path strokeLinecap="round" strokeLinejoin="round" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
        </svg>
        <span className="truncate">{node.title}</span>
      </div>
      {hasChildren && expanded && (
        <div>
          {node.children.map(child => (
            <ChapterTreeNode
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
