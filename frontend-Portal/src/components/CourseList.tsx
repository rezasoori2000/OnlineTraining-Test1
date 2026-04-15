import type { FolderCourse } from '@/types/portal'

interface CourseListProps {
  courses: FolderCourse[]
  selectedCourseId: string | null
  onSelect: (courseId: string) => void
}

export function CourseList({ courses, selectedCourseId, onSelect }: CourseListProps) {
  if (courses.length === 0) {
    return (
      <div className="p-4 text-sm text-gray-400 italic">
        No courses in this folder.
      </div>
    )
  }

  return (
    <div className="overflow-y-auto h-full">
      <div className="p-3 border-b border-gray-200">
        <h3 className="text-sm font-semibold text-gray-700">Courses</h3>
      </div>
      <div className="divide-y divide-gray-100">
        {courses.map(course => (
          <div
            key={course.courseId}
            className={`px-3 py-2.5 cursor-pointer transition-colors ${
              selectedCourseId === course.courseId
                ? 'bg-brand-50 border-l-2 border-brand-500'
                : 'hover:bg-gray-50 border-l-2 border-transparent'
            }`}
            onClick={() => onSelect(course.courseId)}
          >
            <div className="text-sm font-medium text-gray-800 truncate">{course.title}</div>
            {course.description && (
              <div className="text-xs text-gray-500 truncate mt-0.5">{course.description}</div>
            )}
          </div>
        ))}
      </div>
    </div>
  )
}
