import { useState } from 'react'
import { EditChapterNodeState } from '@/types/editCourse'
import { EditBuilderAction } from './useEditCourseBuilder'

interface EditChapterNodeProps {
  node: EditChapterNodeState
  /** All siblings (used to compute isFirst/isLast for reorder buttons) */
  siblings: EditChapterNodeState[]
  depth: number
  dispatch: React.Dispatch<EditBuilderAction>
  selectedChapterId: string | null
  onSelectChapter: (clientId: string) => void
}

export function EditChapterNode({
  node,
  siblings,
  depth,
  dispatch,
  selectedChapterId,
  onSelectChapter,
}: EditChapterNodeProps) {
  const [expanded, setExpanded] = useState(true)
  const hasChildren = node.children.filter((c) => !(c.isNew && c.isDeleted)).length > 0
  const isSelected = node.clientId === selectedChapterId

  const visibleSiblings = siblings.filter((s) => !(s.isNew && s.isDeleted))
  const idx = visibleSiblings.indexOf(node)
  const isFirst = idx <= 0
  const isLast = idx >= visibleSiblings.length - 1

  if (node.isDeleted) {
    return (
      <div style={{ paddingLeft: `${depth * 16}px` }}>
        <div className="flex items-center gap-2 px-2 py-1.5 rounded opacity-50">
          <span className="flex-1 text-sm text-red-500 line-through truncate">
            {node.title || '(untitled)'}
          </span>
          {!node.isNew && (
            <button
              type="button"
              onClick={() => dispatch({ type: 'RESTORE_CHAPTER', clientId: node.clientId })}
              className="text-xs text-indigo-500 hover:text-indigo-700 shrink-0"
            >
              Restore
            </button>
          )}
        </div>
      </div>
    )
  }

  return (
    <div>
      <div
        className={`group flex items-center gap-1 rounded-md cursor-pointer select-none transition-colors ${
          isSelected
            ? 'bg-indigo-50 text-indigo-800'
            : 'hover:bg-gray-100 text-gray-700'
        }`}
        style={{ paddingLeft: `${8 + depth * 16}px`, paddingRight: 8, paddingTop: 6, paddingBottom: 6 }}
        onClick={() => onSelectChapter(node.clientId)}
      >
        {/* Expand/collapse */}
        <button
          type="button"
          className="w-4 h-4 shrink-0 flex items-center justify-center text-gray-400 hover:text-gray-700"
          onClick={(e) => {
            e.stopPropagation()
            if (hasChildren) setExpanded((x) => !x)
          }}
        >
          {hasChildren ? (
            expanded ? '▾' : '▸'
          ) : (
            <span className="block w-1.5 h-1.5 rounded-full bg-gray-300 mx-auto" />
          )}
        </button>

        {/* Title */}
        <span className="flex-1 text-sm truncate">
          {node.title || <span className="italic text-gray-400">Untitled</span>}
        </span>

        {/* Content indicator for leaf nodes */}
        {!hasChildren && node.hasContent && (
          <span className="shrink-0 w-1.5 h-1.5 rounded-full bg-green-400" title="Has content" />
        )}

        {/* Change badges */}
        {node.isNew && (
          <span className="hidden group-hover:inline px-1 text-xs bg-green-100 text-green-700 rounded">
            new
          </span>
        )}
        {!node.isNew && (node.isDirty || node.isContentDirty) && (
          <span className="hidden group-hover:inline px-1 text-xs bg-amber-100 text-amber-700 rounded">
            edited
          </span>
        )}

        {/* Action controls — appear on hover */}
        <div className="hidden group-hover:flex items-center gap-0.5 shrink-0">
          <button
            type="button"
            title="Add sub-section"
            onClick={(e) => { e.stopPropagation(); dispatch({ type: 'ADD_CHILD', parentClientId: node.clientId }) }}
            className="px-1 py-0.5 text-xs text-gray-400 hover:text-indigo-600"
          >
            +
          </button>
          <button
            type="button"
            title="Move up"
            disabled={isFirst}
            onClick={(e) => { e.stopPropagation(); dispatch({ type: 'MOVE_UP', clientId: node.clientId }) }}
            className="px-0.5 py-0.5 text-xs text-gray-400 hover:text-gray-700 disabled:opacity-25"
          >
            ↑
          </button>
          <button
            type="button"
            title="Move down"
            disabled={isLast}
            onClick={(e) => { e.stopPropagation(); dispatch({ type: 'MOVE_DOWN', clientId: node.clientId }) }}
            className="px-0.5 py-0.5 text-xs text-gray-400 hover:text-gray-700 disabled:opacity-25"
          >
            ↓
          </button>
          <button
            type="button"
            title="Delete"
            onClick={(e) => { e.stopPropagation(); dispatch({ type: 'DELETE_CHAPTER', clientId: node.clientId }) }}
            className="px-0.5 py-0.5 text-xs text-gray-400 hover:text-red-600"
          >
            ✕
          </button>
        </div>
      </div>

      {hasChildren && expanded && (
        <div>
          {node.children.map((child) => (
            <EditChapterNode
              key={child.clientId}
              node={child}
              siblings={node.children}
              depth={depth + 1}
              dispatch={dispatch}
              selectedChapterId={selectedChapterId}
              onSelectChapter={onSelectChapter}
            />
          ))}
        </div>
      )}
    </div>
  )
}
