import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import { Button } from '@/components/ui/Button'
import { ChapterTreeBuilder } from '@/features/course/ChapterTreeBuilder'
import { useCourseBuilder } from '@/features/course/useCourseBuilder'
import { ChapterNodeState, QuizState, QuestionState } from '@/types/createCourse'
import { apiClient } from '@/services/api/apiClient'
import type {
  FullCreateCourseRequest,
  FullCreateChapterDto,
  FullCreateQuizDto,
  FullCreateQuestionDto,
} from '@/types/courseApi'

// ── State → API DTO transformer ───────────────────────────────────────────────

function mapQuestion(q: QuestionState): FullCreateQuestionDto {
  return {
    text: q.text,
    order: q.order,
    options: q.options.map((o) => ({ text: o.text, isCorrect: o.isCorrect })),
  }
}

function mapQuiz(quiz: QuizState): FullCreateQuizDto {
  return {
    isMandatory: quiz.isMandatory,
    passingPercentage: quiz.passingPercentage,
    questions: quiz.questions.map(mapQuestion),
  }
}

function mapChapter(ch: ChapterNodeState): FullCreateChapterDto {
  return {
    clientId: ch.clientId,
    title: ch.title,
    order: ch.order,
    children: ch.children.map(mapChapter),
    htmlContent: ch.htmlContent ?? undefined,
    quiz: ch.quiz ? mapQuiz(ch.quiz) : undefined,
  }
}

// ── Component ─────────────────────────────────────────────────────────────────

export function CreateCoursePage() {
  const navigate = useNavigate()
  const { state, dispatch } = useCourseBuilder()
  const [submitError, setSubmitError] = useState<string | null>(null)

  const mutation = useMutation({
    mutationFn: (payload: FullCreateCourseRequest) =>
      apiClient.post<{ courseId: string }>('/courses/full-create', payload),
    onSuccess: () => navigate('/courses'),
    onError: (err: Error) => setSubmitError(err.message),
  })

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    setSubmitError(null)

    // Basic client-side guard
    if (!state.title.trim()) {
      setSubmitError('Course title is required.')
      return
    }
    if (!state.description.trim()) {
      setSubmitError('Course description is required.')
      return
    }

    const payload: FullCreateCourseRequest = {
      course: {
        title: state.title.trim(),
        description: state.description.trim(),
        languageCode: 'en',
      },
      chapters: state.chapters.map(mapChapter),
    }

    mutation.mutate(payload)
  }

  const isSubmitting = mutation.isPending

  return (
    <form onSubmit={handleSubmit} className="max-w-3xl mx-auto space-y-6">
      {/* Page header */}
      <div className="flex items-center justify-between">
        <h2 className="text-title-sm font-bold text-gray-900">Create Course</h2>
        <Button type="button" variant="secondary" onClick={() => navigate('/courses')}>
          Cancel
        </Button>
      </div>

      {/* Course info card */}
      <div className="card p-5 xl:p-6 space-y-5">
        <h3 className="text-base font-semibold text-gray-800">
          Course Information
        </h3>

        <div>
          <label className="form-label">
            Title <span className="text-red-500">*</span>
          </label>
          <input
            type="text"
            value={state.title}
            onChange={(e) => dispatch({ type: 'SET_TITLE', title: e.target.value })}
            placeholder="Enter course title"
            maxLength={500}
            className="form-input"
          />
        </div>

        <div>
          <label className="form-label">
            Description <span className="text-red-500">*</span>
          </label>
          <textarea
            value={state.description}
            onChange={(e) =>
              dispatch({ type: 'SET_DESCRIPTION', description: e.target.value })
            }
            placeholder="Enter course description"
            rows={4}
            maxLength={2000}
            className="form-textarea"
          />
        </div>
      </div>

      {/* Chapter tree card */}
      <div className="card p-5 xl:p-6 space-y-4">
        <h3 className="text-base font-semibold text-gray-800">Chapters</h3>
        <ChapterTreeBuilder chapters={state.chapters} dispatch={dispatch} />
      </div>

      {/* Error */}
      {(submitError) && (
        <p className="text-theme-sm text-red-600 bg-red-50 border border-red-200 rounded-lg px-4 py-3">
          {submitError}
        </p>
      )}

      {/* Submit */}
      <div className="pt-1">
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Saving…' : 'Save Course'}
        </Button>
      </div>
    </form>
  )
}
