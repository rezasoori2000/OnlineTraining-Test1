// Local state shapes for the Create Course form.
// These are richer than the API DTOs (they carry local IDs for React key management).

export interface OptionState {
  id: string
  text: string
  isCorrect: boolean
}

export interface QuestionState {
  id: string
  text: string
  order: number
  options: OptionState[]
}

export interface QuizState {
  isMandatory: boolean
  passingPercentage: number
  questions: QuestionState[]
}

export interface ChapterNodeState {
  clientId: string
  title: string
  order: number
  children: ChapterNodeState[]
  /** null = not yet uploaded (leaf node only) */
  htmlContent: string | null
  quiz: QuizState | null
}

export interface CreateCourseFormState {
  title: string
  description: string
  chapters: ChapterNodeState[]
}
