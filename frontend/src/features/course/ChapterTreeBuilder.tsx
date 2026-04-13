import { ChapterNodeState } from '@/types/createCourse'
import { CourseBuilderAction } from './useCourseBuilder'
import { ChapterNode } from './ChapterNode'

interface ChapterTreeBuilderProps {
  chapters: ChapterNodeState[]
  dispatch: React.Dispatch<CourseBuilderAction>
}

export function ChapterTreeBuilder({ chapters, dispatch }: ChapterTreeBuilderProps) {
  return (
    <div className="space-y-3">
      {chapters.map((chapter) => (
        <ChapterNode
          key={chapter.clientId}
          node={chapter}
          depth={0}
          dispatch={dispatch}
        />
      ))}

      <button
        type="button"
        onClick={() => dispatch({ type: 'ADD_ROOT_CHAPTER' })}
        className="w-full py-2 border-2 border-dashed border-gray-300 rounded-md text-sm text-gray-500 hover:border-indigo-400 hover:text-indigo-600 transition-colors"
      >
        + Add Chapter
      </button>
    </div>
  )
}
