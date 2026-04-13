import { useReducer } from 'react'
import {
  CourseDetailDto,
  ChapterDetailDto,
  EditCourseFormState,
  EditChapterNodeState,
  EditQuizState,
  EditQuestionState,
  EditOptionState,
  UpdateCourseRequest,
  NewNodeDto,
  UpdatedNodeDto,
  UpdatedContentDto,
  UpdatedQuizDto,
  UpsertQuestionDto,
} from '@/types/editCourse'

// ── State initialiser ─────────────────────────────────────────────────────────

function mapOptionFromServer(o: { id: string; text: string; isCorrect: boolean }): EditOptionState {
  return { localId: o.id, serverId: o.id, text: o.text, isCorrect: o.isCorrect }
}

function mapQuestionFromServer(q: {
  id: string
  text: string
  order: number
  options: { id: string; text: string; isCorrect: boolean }[]
}): EditQuestionState {
  return {
    localId: q.id,
    serverId: q.id,
    text: q.text,
    order: q.order,
    options: q.options.map(mapOptionFromServer),
  }
}

function mapQuizFromServer(quiz: {
  id: string
  isMandatory: boolean
  passingPercentage: number
  questions: { id: string; text: string; order: number; options: { id: string; text: string; isCorrect: boolean }[] }[]
}): EditQuizState {
  return {
    serverId: quiz.id,
    isMandatory: quiz.isMandatory,
    passingPercentage: quiz.passingPercentage,
    questions: quiz.questions.map(mapQuestionFromServer),
    isDirty: false,
  }
}

function mapChapterFromServer(chapter: ChapterDetailDto): EditChapterNodeState {
  return {
    clientId: chapter.id,
    serverId: chapter.id,
    title: chapter.title,
    order: chapter.order,
    children: chapter.children.map(mapChapterFromServer),
    htmlContent: null,         // not sent on initial load
    hasContent: chapter.hasContent,
    quiz: chapter.quiz ? mapQuizFromServer(chapter.quiz) : null,
    isNew: false,
    isDeleted: false,
    isDirty: false,
    isContentDirty: false,
  }
}

export function initEditState(dto: CourseDetailDto): EditCourseFormState {
  return {
    courseId: dto.id,
    title: dto.title,
    description: dto.description ?? '',
    languageCode: dto.languageCode,
    activeVersionId: dto.activeVersionId,
    isVersionPublished: dto.isVersionPublished,
    isCourseInfoDirty: false,
    chapters: dto.chapters.map(mapChapterFromServer),
  }
}

// ── Action types ──────────────────────────────────────────────────────────────

export type EditBuilderAction =
  | { type: 'RESET'; dto: CourseDetailDto }
  | { type: 'SET_TITLE'; title: string }
  | { type: 'SET_DESCRIPTION'; description: string }
  | { type: 'ADD_ROOT_CHAPTER' }
  | { type: 'ADD_CHILD'; parentClientId: string }
  | { type: 'DELETE_CHAPTER'; clientId: string }
  | { type: 'RESTORE_CHAPTER'; clientId: string }
  | { type: 'UPDATE_CHAPTER_TITLE'; clientId: string; title: string }
  | { type: 'MOVE_UP'; clientId: string }
  | { type: 'MOVE_DOWN'; clientId: string }
  | { type: 'SET_CONTENT'; clientId: string; html: string }
  | { type: 'ADD_QUIZ'; clientId: string }
  | { type: 'REMOVE_QUIZ'; clientId: string }
  | { type: 'UPDATE_QUIZ_MANDATORY'; clientId: string; isMandatory: boolean }
  | { type: 'UPDATE_QUIZ_PASSING'; clientId: string; passingPercentage: number }
  | { type: 'ADD_QUESTION'; clientId: string }
  | { type: 'UPDATE_QUESTION_TEXT'; clientId: string; questionId: string; text: string }
  | { type: 'DELETE_QUESTION'; clientId: string; questionId: string }
  | { type: 'ADD_OPTION'; clientId: string; questionId: string }
  | {
      type: 'UPDATE_OPTION'
      clientId: string
      questionId: string
      optionId: string
      text: string
      isCorrect: boolean
    }
  | { type: 'DELETE_OPTION'; clientId: string; questionId: string; optionId: string }

// ── Helpers ───────────────────────────────────────────────────────────────────

function uid(): string {
  return crypto.randomUUID()
}

function newChapter(order: number): EditChapterNodeState {
  return {
    clientId: uid(),
    title: '',
    order,
    children: [],
    htmlContent: null,
    hasContent: false,
    quiz: null,
    isNew: true,
    isDeleted: false,
    isDirty: false,
    isContentDirty: false,
  }
}

function newQuestion(order: number): EditQuestionState {
  return {
    localId: uid(),
    text: '',
    order,
    options: [
      { localId: uid(), text: '', isCorrect: false },
      { localId: uid(), text: '', isCorrect: false },
    ],
  }
}

function newQuiz(): EditQuizState {
  return {
    isMandatory: false,
    passingPercentage: 0,
    questions: [newQuestion(0)],
    isDirty: true,
  }
}

function mapChapter(
  chapters: EditChapterNodeState[],
  clientId: string,
  updater: (node: EditChapterNodeState) => EditChapterNodeState,
): EditChapterNodeState[] {
  return chapters.map((ch) => {
    if (ch.clientId === clientId) return updater(ch)
    return { ...ch, children: mapChapter(ch.children, clientId, updater) }
  })
}

function moveItem<T>(arr: T[], index: number, direction: -1 | 1): T[] {
  const next = index + direction
  if (next < 0 || next >= arr.length) return arr
  const out = [...arr]
  ;[out[index], out[next]] = [out[next], out[index]]
  return out.map((item, i) => ({ ...(item as object), order: i }) as T)
}

function findIndexByClientId(
  siblings: EditChapterNodeState[],
  clientId: string,
): number {
  return siblings.findIndex((n) => n.clientId === clientId)
}

/** Apply reorder in the sibling list that contains the given clientId (searching recursively). */
function reorderInTree(
  chapters: EditChapterNodeState[],
  clientId: string,
  direction: -1 | 1,
): EditChapterNodeState[] {
  const idx = findIndexByClientId(chapters, clientId)
  if (idx !== -1) {
    // Found at this level — reorder siblings and mark both as dirty
    const moved = moveItem(chapters, idx, direction)
    return moved.map((ch) => {
      if (ch.clientId === clientId || chapters[idx]?.clientId === ch.clientId) {
        return ch.isNew ? ch : { ...ch, isDirty: true }
      }
      return ch
    })
  }
  return chapters.map((ch) => ({
    ...ch,
    children: reorderInTree(ch.children, clientId, direction),
  }))
}

// ── Reducer ───────────────────────────────────────────────────────────────────

function reducer(state: EditCourseFormState, action: EditBuilderAction): EditCourseFormState {
  switch (action.type) {
    case 'RESET':
      return initEditState(action.dto)

    case 'SET_TITLE':
      return { ...state, title: action.title, isCourseInfoDirty: true }

    case 'SET_DESCRIPTION':
      return { ...state, description: action.description, isCourseInfoDirty: true }

    case 'ADD_ROOT_CHAPTER':
      return {
        ...state,
        chapters: [...state.chapters, newChapter(state.chapters.length)],
      }

    case 'ADD_CHILD':
      return {
        ...state,
        chapters: mapChapter(state.chapters, action.parentClientId, (parent) => ({
          ...parent,
          htmlContent: null,
          quiz: null,
          isDirty: parent.isNew ? false : true,
          children: [...parent.children, newChapter(parent.children.length)],
        })),
      }

    case 'DELETE_CHAPTER':
      return {
        ...state,
        chapters: mapChapter(state.chapters, action.clientId, (ch) =>
          ch.isNew
            ? { ...ch, isDeleted: true } // will be filtered out server-side — just hide locally
            : { ...ch, isDeleted: true },
        ),
      }

    case 'RESTORE_CHAPTER':
      return {
        ...state,
        chapters: mapChapter(state.chapters, action.clientId, (ch) => ({
          ...ch,
          isDeleted: false,
        })),
      }

    case 'UPDATE_CHAPTER_TITLE':
      return {
        ...state,
        chapters: mapChapter(state.chapters, action.clientId, (ch) => ({
          ...ch,
          title: action.title,
          isDirty: ch.isNew ? false : true,
        })),
      }

    case 'MOVE_UP':
      return {
        ...state,
        chapters: reorderInTree(state.chapters, action.clientId, -1),
      }

    case 'MOVE_DOWN':
      return {
        ...state,
        chapters: reorderInTree(state.chapters, action.clientId, 1),
      }

    case 'SET_CONTENT':
      return {
        ...state,
        chapters: mapChapter(state.chapters, action.clientId, (ch) => ({
          ...ch,
          htmlContent: action.html,
          hasContent: true,
          isContentDirty: !ch.isNew,
        })),
      }

    case 'ADD_QUIZ':
      return {
        ...state,
        chapters: mapChapter(state.chapters, action.clientId, (ch) => ({
          ...ch,
          quiz: newQuiz(),
        })),
      }

    case 'REMOVE_QUIZ':
      return {
        ...state,
        chapters: mapChapter(state.chapters, action.clientId, (ch) => ({
          ...ch,
          quiz: null,
        })),
      }

    case 'UPDATE_QUIZ_MANDATORY':
      return {
        ...state,
        chapters: mapChapter(state.chapters, action.clientId, (ch) => ({
          ...ch,
          quiz: ch.quiz
            ? { ...ch.quiz, isMandatory: action.isMandatory, isDirty: !ch.isNew }
            : null,
        })),
      }

    case 'UPDATE_QUIZ_PASSING':
      return {
        ...state,
        chapters: mapChapter(state.chapters, action.clientId, (ch) => ({
          ...ch,
          quiz: ch.quiz
            ? {
                ...ch.quiz,
                passingPercentage: action.passingPercentage,
                isDirty: !ch.isNew,
              }
            : null,
        })),
      }

    case 'ADD_QUESTION':
      return {
        ...state,
        chapters: mapChapter(state.chapters, action.clientId, (ch) => ({
          ...ch,
          quiz: ch.quiz
            ? {
                ...ch.quiz,
                questions: [
                  ...ch.quiz.questions,
                  newQuestion(ch.quiz.questions.length),
                ],
                isDirty: !ch.isNew,
              }
            : null,
        })),
      }

    case 'UPDATE_QUESTION_TEXT':
      return {
        ...state,
        chapters: mapChapter(state.chapters, action.clientId, (ch) => ({
          ...ch,
          quiz: ch.quiz
            ? {
                ...ch.quiz,
                questions: ch.quiz.questions.map((q) =>
                  q.localId === action.questionId ? { ...q, text: action.text } : q,
                ),
                isDirty: !ch.isNew,
              }
            : null,
        })),
      }

    case 'DELETE_QUESTION':
      return {
        ...state,
        chapters: mapChapter(state.chapters, action.clientId, (ch) => ({
          ...ch,
          quiz: ch.quiz
            ? {
                ...ch.quiz,
                questions: ch.quiz.questions
                  .filter((q) => q.localId !== action.questionId)
                  .map((q, i) => ({ ...q, order: i })),
                isDirty: !ch.isNew,
              }
            : null,
        })),
      }

    case 'ADD_OPTION':
      return {
        ...state,
        chapters: mapChapter(state.chapters, action.clientId, (ch) => ({
          ...ch,
          quiz: ch.quiz
            ? {
                ...ch.quiz,
                questions: ch.quiz.questions.map((q) =>
                  q.localId === action.questionId
                    ? {
                        ...q,
                        options: [...q.options, { localId: uid(), text: '', isCorrect: false }],
                      }
                    : q,
                ),
                isDirty: !ch.isNew,
              }
            : null,
        })),
      }

    case 'UPDATE_OPTION':
      return {
        ...state,
        chapters: mapChapter(state.chapters, action.clientId, (ch) => ({
          ...ch,
          quiz: ch.quiz
            ? {
                ...ch.quiz,
                questions: ch.quiz.questions.map((q) =>
                  q.localId === action.questionId
                    ? {
                        ...q,
                        options: q.options.map((o) =>
                          o.localId === action.optionId
                            ? { ...o, text: action.text, isCorrect: action.isCorrect }
                            : o,
                        ),
                      }
                    : q,
                ),
                isDirty: !ch.isNew,
              }
            : null,
        })),
      }

    case 'DELETE_OPTION':
      return {
        ...state,
        chapters: mapChapter(state.chapters, action.clientId, (ch) => ({
          ...ch,
          quiz: ch.quiz
            ? {
                ...ch.quiz,
                questions: ch.quiz.questions.map((q) =>
                  q.localId === action.questionId
                    ? {
                        ...q,
                        options: q.options.filter((o) => o.localId !== action.optionId),
                      }
                    : q,
                ),
                isDirty: !ch.isNew,
              }
            : null,
        })),
      }

    default:
      return state
  }
}

// ── Payload builder ───────────────────────────────────────────────────────────

function buildQuizPayload(
  quiz: EditQuizState,
): { isMandatory: boolean; passingPercentage: number; questions: UpsertQuestionDto[] } {
  return {
    isMandatory: quiz.isMandatory,
    passingPercentage: quiz.passingPercentage,
    questions: quiz.questions.map((q) => ({
      text: q.text,
      order: q.order,
      options: q.options.map((o) => ({ text: o.text, isCorrect: o.isCorrect })),
    })),
  }
}

function walkNodes(
  node: EditChapterNodeState,
  parentRef: string | null,
  result: {
    deletedNodeIds: string[]
    newNodes: NewNodeDto[]
    updatedNodes: UpdatedNodeDto[]
    updatedContents: UpdatedContentDto[]
    updatedQuizzes: UpdatedQuizDto[]
  },
) {
  if (node.isDeleted && !node.isNew) {
    result.deletedNodeIds.push(node.serverId!)
    // cascade — don't visit children
    return
  }

  if (node.isDeleted && node.isNew) {
    // user deleted something they just added — skip entirely
    return
  }

  const isLeaf = node.children.length === 0

  if (node.isNew) {
    const newNode: NewNodeDto = {
      clientId: node.clientId,
      parentRef,
      title: node.title,
      order: node.order,
      htmlContent: isLeaf ? node.htmlContent : null,
      quiz: isLeaf && node.quiz ? { ...buildQuizPayload(node.quiz) } : null,
    }
    result.newNodes.push(newNode)
    // Children of a new node reference its clientId
    for (const child of node.children) {
      walkNodes(child, node.clientId, result)
    }
    return
  }

  // ── Existing node ──
  if (node.isDirty) {
    result.updatedNodes.push({ id: node.serverId!, title: node.title, order: node.order })
  }
  if (node.isContentDirty && node.htmlContent) {
    result.updatedContents.push({ chapterId: node.serverId!, htmlContent: node.htmlContent })
  }
  if (isLeaf && node.quiz?.isDirty) {
    result.updatedQuizzes.push({
      chapterId: node.serverId!,
      ...buildQuizPayload(node.quiz),
    })
  }

  for (const child of node.children) {
    walkNodes(child, node.serverId!, result)
  }
}

export function buildUpdatePayload(state: EditCourseFormState): UpdateCourseRequest {
  const result = {
    deletedNodeIds: [] as string[],
    newNodes: [] as NewNodeDto[],
    updatedNodes: [] as UpdatedNodeDto[],
    updatedContents: [] as UpdatedContentDto[],
    updatedQuizzes: [] as UpdatedQuizDto[],
  }

  for (const chapter of state.chapters) {
    walkNodes(chapter, null, result)
  }

  return {
    courseInfo: {
      title: state.title,
      description: state.description || null,
      languageCode: state.languageCode,
    },
    ...result,
  }
}

// ── Tree lookup ───────────────────────────────────────────────────────────────

export function findNode(
  chapters: EditChapterNodeState[],
  clientId: string,
): EditChapterNodeState | null {
  for (const ch of chapters) {
    if (ch.clientId === clientId) return ch
    const found = findNode(ch.children, clientId)
    if (found) return found
  }
  return null
}

// ── Hook ──────────────────────────────────────────────────────────────────────

export function useEditCourseBuilder(initialDto: CourseDetailDto) {
  const [state, dispatch] = useReducer(reducer, initialDto, initEditState)
  return { state, dispatch }
}
