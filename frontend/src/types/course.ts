export type CourseStatus = 'Draft' | 'Published' | 'Archived'

export interface Course {
  id: string
  title: string
  status: CourseStatus
  version: number
  updatedAt: string
}
