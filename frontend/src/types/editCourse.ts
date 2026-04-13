// Types for the Edit Course feature.
// API response types mirror backend DTOs (camelCase).
// Edit state types add change-tracking metadata.

// ── API Response (GET /api/courses/:id) ───────────────────────────────────────

export interface OptionDetailDto {
  id: string
  text: string
  isCorrect: boolean
}

export interface QuestionDetailDto {
  id: string
  text: string
  order: number
  options: OptionDetailDto[]
}

export interface QuizDetailDto {
  id: string
  isMandatory: boolean
  passingPercentage: number
  questions: QuestionDetailDto[]
}

export interface ChapterDetailDto {
  id: string
  title: string
  order: number
  parentId: string | null
  hasChildren: boolean
  hasContent: boolean
  quiz: QuizDetailDto | null
  children: ChapterDetailDto[]
}

export interface CourseDetailDto {
  id: string
  slug: string
  status: string
  title: string
  description: string | null
  languageCode: string
  activeVersionId: string
  activeVersionNumber: number
  isVersionPublished: boolean
  chapters: ChapterDetailDto[]
}

// ── Edit state ─────────────────────────────────────────────────────────────────

export interface EditOptionState {
  localId: string
  serverId?: string
  text: string
  isCorrect: boolean
}

export interface EditQuestionState {
  localId: string
  serverId?: string
  text: string
  order: number
  options: EditOptionState[]
}

export interface EditQuizState {
  serverId?: string
  isMandatory: boolean
  passingPercentage: number
  questions: EditQuestionState[]
  isDirty: boolean
}

export interface EditChapterNodeState {
  /** Stable React key. Equals serverId for existing nodes, a fresh UUID for new ones. */
  clientId: string
  /** Undefined means this is a brand-new node the server doesn't know yet. */
  serverId?: string
  title: string
  order: number
  children: EditChapterNodeState[]
  htmlContent: string | null
  /** True when the server already has HTML stored for this chapter (even if not loaded here). */
  hasContent: boolean
  quiz: EditQuizState | null
  isNew: boolean
  isDeleted: boolean
  isDirty: boolean
  isContentDirty: boolean
}

export interface EditCourseFormState {
  courseId: string
  title: string
  description: string
  languageCode: string
  status: string
  activeVersionId: string
  isVersionPublished: boolean
  isCourseInfoDirty: boolean
  chapters: EditChapterNodeState[]
}

// ── PUT payload shapes (mirror backend UpdateCourseRequest DTOs) ───────────────

export interface UpsertOptionDto {
  text: string
  isCorrect: boolean
}

export interface UpsertQuestionDto {
  text: string
  order: number
  options: UpsertOptionDto[]
}

export interface NewQuizDto {
  isMandatory: boolean
  passingPercentage: number
  questions: UpsertQuestionDto[]
}

export interface UpdatedQuizDto {
  chapterId: string
  isMandatory: boolean
  passingPercentage: number
  questions: UpsertQuestionDto[]
}

export interface NewNodeDto {
  clientId: string
  parentRef: string | null
  title: string
  order: number
  htmlContent: string | null
  quiz: NewQuizDto | null
}

export interface UpdatedNodeDto {
  id: string
  title: string
  order: number
}

export interface UpdatedContentDto {
  chapterId: string
  htmlContent: string
}

export interface UpdateCourseInfoDto {
  title: string
  description: string | null
  languageCode: string
  status: string
}

export interface UpdateCourseRequest {
  courseInfo: UpdateCourseInfoDto
  archivePreviousVersion: boolean
  updatedNodes: UpdatedNodeDto[]
  newNodes: NewNodeDto[]
  deletedNodeIds: string[]
  updatedContents: UpdatedContentDto[]
  updatedQuizzes: UpdatedQuizDto[]
}
