import { useReducer } from 'react'
import {
  ChapterNodeState,
  CreateCourseFormState,
  OptionState,
  QuestionState,
  QuizState,
} from '@/types/createCourse'

// ── Action types ──────────────────────────────────────────────────────────────

type Action =
  | { type: 'SET_TITLE'; title: string }
  | { type: 'SET_DESCRIPTION'; description: string }
  | { type: 'ADD_ROOT_CHAPTER' }
  | { type: 'ADD_CHILD'; parentClientId: string }
  | { type: 'DELETE_CHAPTER'; clientId: string }
  | { type: 'UPDATE_CHAPTER_TITLE'; clientId: string; title: string }
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

function newChapter(order: number): ChapterNodeState {
  return {
    clientId: uid(),
    title: '',
    order,
    children: [],
    htmlContent: null,
    quiz: null,
  }
}

function newQuestion(order: number): QuestionState {
  return {
    id: uid(),
    text: '',
    order,
    options: [
      { id: uid(), text: '', isCorrect: false },
      { id: uid(), text: '', isCorrect: false },
    ],
  }
}

function newQuiz(): QuizState {
  return {
    isMandatory: false,
    passingPercentage: 0,
    questions: [newQuestion(0)],
  }
}

/** Recursively map over the chapter tree, applying an updater when clientId matches. */
function mapChapter(
  chapters: ChapterNodeState[],
  clientId: string,
  updater: (node: ChapterNodeState) => ChapterNodeState,
): ChapterNodeState[] {
  return chapters.map((ch) => {
    if (ch.clientId === clientId) return updater(ch)
    return { ...ch, children: mapChapter(ch.children, clientId, updater) }
  })
}

/** Recursively remove a chapter by clientId at any depth. */
function filterChapters(
  chapters: ChapterNodeState[],
  clientId: string,
): ChapterNodeState[] {
  return chapters
    .filter((ch) => ch.clientId !== clientId)
    .map((ch) => ({ ...ch, children: filterChapters(ch.children, clientId) }))
}

// ── Reducer ───────────────────────────────────────────────────────────────────

function reducer(
  state: CreateCourseFormState,
  action: Action,
): CreateCourseFormState {
  switch (action.type) {
    case 'SET_TITLE':
      return { ...state, title: action.title }

    case 'SET_DESCRIPTION':
      return { ...state, description: action.description }

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
          // When adding first child: clear existing content + quiz
          htmlContent: null,
          quiz: null,
          children: [...parent.children, newChapter(parent.children.length)],
        })),
      }

    case 'DELETE_CHAPTER':
      return { ...state, chapters: filterChapters(state.chapters, action.clientId) }

    case 'UPDATE_CHAPTER_TITLE':
      return {
        ...state,
        chapters: mapChapter(state.chapters, action.clientId, (ch) => ({
          ...ch,
          title: action.title,
        })),
      }

    case 'SET_CONTENT':
      return {
        ...state,
        chapters: mapChapter(state.chapters, action.clientId, (ch) => ({
          ...ch,
          htmlContent: action.html,
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
          quiz: ch.quiz ? { ...ch.quiz, isMandatory: action.isMandatory } : ch.quiz,
        })),
      }

    case 'UPDATE_QUIZ_PASSING':
      return {
        ...state,
        chapters: mapChapter(state.chapters, action.clientId, (ch) => ({
          ...ch,
          quiz: ch.quiz
            ? { ...ch.quiz, passingPercentage: action.passingPercentage }
            : ch.quiz,
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
              }
            : ch.quiz,
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
                  q.id === action.questionId ? { ...q, text: action.text } : q,
                ),
              }
            : ch.quiz,
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
                questions: ch.quiz.questions.filter(
                  (q) => q.id !== action.questionId,
                ),
              }
            : ch.quiz,
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
                  q.id === action.questionId
                    ? {
                        ...q,
                        options: [...q.options, { id: uid(), text: '', isCorrect: false }],
                      }
                    : q,
                ),
              }
            : ch.quiz,
        })),
      }

    case 'UPDATE_OPTION': {
      const updateOption = (opts: OptionState[]): OptionState[] =>
        opts.map((o) =>
          o.id === action.optionId
            ? { ...o, text: action.text, isCorrect: action.isCorrect }
            : o,
        )
      return {
        ...state,
        chapters: mapChapter(state.chapters, action.clientId, (ch) => ({
          ...ch,
          quiz: ch.quiz
            ? {
                ...ch.quiz,
                questions: ch.quiz.questions.map((q) =>
                  q.id === action.questionId
                    ? { ...q, options: updateOption(q.options) }
                    : q,
                ),
              }
            : ch.quiz,
        })),
      }
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
                  q.id === action.questionId
                    ? {
                        ...q,
                        options: q.options.filter((o) => o.id !== action.optionId),
                      }
                    : q,
                ),
              }
            : ch.quiz,
        })),
      }

    default:
      return state
  }
}

// ── Initial state ─────────────────────────────────────────────────────────────

const initialState: CreateCourseFormState = {
  title: '',
  description: '',
  chapters: [],
}

// ── Public hook ───────────────────────────────────────────────────────────────

export function useCourseBuilder() {
  const [state, dispatch] = useReducer(reducer, initialState)
  return { state, dispatch }
}

export type { Action as CourseBuilderAction }
