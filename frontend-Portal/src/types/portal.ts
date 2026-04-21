// ── Folder Types ──

export interface FolderTreeNode {
  id: string
  name: string
  parentId: string | null
  children: FolderTreeNode[]
}

export interface FolderDetail {
  id: string
  name: string
  description: string | null
  htmlContent: string | null
  courses: FolderCourse[]
}

export interface FolderCourse {
  courseId: string
  title: string
  description: string | null
}

// ── Course Types ──

export interface CourseDetail {
  id: string
  title: string
  description: string | null
  chapters: ChapterNode[]
}

export interface ChapterNode {
  id: string
  title: string
  order: number
  hasContent: boolean
  children: ChapterNode[]
}

export interface ChapterContent {
  chapterId: string
  title: string
  htmlContent: string | null
  pdfDownloadUrl?: string | null
}
