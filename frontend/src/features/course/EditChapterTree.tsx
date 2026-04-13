import { EditChapterNodeState } from '@/types/editCourse'
import { EditBuilderAction } from './useEditCourseBuilder'
import { EditChapterNode } from './EditChapterNode'

interface EditChapterTreeProps {
  chapters: EditChapterNodeState[]
  dispatch: React.Dispatch<EditBuilderAction>
  selectedChapterId: string | null
  onSelectChapter: (clientId: string) => void
}

export function EditChapterTree({
  chapters,
  dispatch,
  selectedChapterId,
  onSelectChapter,
}: EditChapterTreeProps) {
  const visible = chapters.filter((ch) => !(ch.isNew && ch.isDeleted))

  return (
    <div className="space-y-0.5">
      {visible.map((chapter) => (
        <EditChapterNode
          key={chapter.clientId}
          node={chapter}
          siblings={visible}
          depth={0}
          dispatch={dispatch}
          selectedChapterId={selectedChapterId}
          onSelectChapter={onSelectChapter}
        />
      ))}

      <button
        type="button"
        onClick={() => dispatch({ type: 'ADD_ROOT_CHAPTER' })}
        className="w-full mt-2 py-1.5 border border-dashed border-gray-300 rounded text-xs text-gray-400 hover:border-indigo-400 hover:text-indigo-600 transition-colors"
      >
        + Add Chapter
      </button>
    </div>
  )
}
