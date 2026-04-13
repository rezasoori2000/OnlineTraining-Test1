import { QuizState } from '@/types/createCourse'
import { CourseBuilderAction } from './useCourseBuilder'

interface QuizBuilderProps {
  clientId: string
  quiz: QuizState
  dispatch: React.Dispatch<CourseBuilderAction>
}

export function QuizBuilder({ clientId, quiz, dispatch }: QuizBuilderProps) {
  return (
    <div className="mt-3 border border-indigo-200 rounded-md p-3 bg-indigo-50 space-y-3">
      <div className="flex items-center justify-between">
        <span className="text-sm font-semibold text-indigo-700">Quiz</span>
        <button
          type="button"
          onClick={() => dispatch({ type: 'REMOVE_QUIZ', clientId })}
          className="text-xs text-red-500 hover:text-red-700"
        >
          Remove Quiz
        </button>
      </div>

      {/* Mandatory setting */}
      <label className="flex items-center gap-2 text-sm text-gray-700">
        <input
          type="checkbox"
          checked={quiz.isMandatory}
          onChange={(e) =>
            dispatch({
              type: 'UPDATE_QUIZ_MANDATORY',
              clientId,
              isMandatory: e.target.checked,
            })
          }
          className="rounded border-gray-300 text-indigo-600"
        />
        Mandatory to pass
      </label>

      {quiz.isMandatory && (
        <label className="flex items-center gap-2 text-sm text-gray-700">
          <span className="whitespace-nowrap">Passing %:</span>
          <input
            type="number"
            min={1}
            max={100}
            value={quiz.passingPercentage}
            onChange={(e) =>
              dispatch({
                type: 'UPDATE_QUIZ_PASSING',
                clientId,
                passingPercentage: Number(e.target.value),
              })
            }
            className="w-20 px-2 py-1 border border-gray-300 rounded text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
          />
        </label>
      )}

      {/* Questions */}
      <div className="space-y-3">
        {quiz.questions.map((q, qIdx) => (
          <div key={q.id} className="bg-white border border-gray-200 rounded p-3 space-y-2">
            <div className="flex items-start gap-2">
              <textarea
                placeholder={`Question ${qIdx + 1}`}
                value={q.text}
                rows={2}
                onChange={(e) =>
                  dispatch({
                    type: 'UPDATE_QUESTION_TEXT',
                    clientId,
                    questionId: q.id,
                    text: e.target.value,
                  })
                }
                className="flex-1 px-2 py-1 border border-gray-300 rounded text-sm resize-none focus:outline-none focus:ring-2 focus:ring-indigo-500"
              />
              {quiz.questions.length > 1 && (
                <button
                  type="button"
                  onClick={() =>
                    dispatch({ type: 'DELETE_QUESTION', clientId, questionId: q.id })
                  }
                  className="text-xs text-red-400 hover:text-red-600 mt-1"
                >
                  ✕
                </button>
              )}
            </div>

            {/* Options */}
            <div className="space-y-1 pl-2">
              {q.options.map((opt) => (
                <div key={opt.id} className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    checked={opt.isCorrect}
                    onChange={(e) =>
                      dispatch({
                        type: 'UPDATE_OPTION',
                        clientId,
                        questionId: q.id,
                        optionId: opt.id,
                        text: opt.text,
                        isCorrect: e.target.checked,
                      })
                    }
                    className="rounded border-gray-300 text-green-600"
                    title="Mark as correct"
                  />
                  <input
                    type="text"
                    placeholder="Option text"
                    value={opt.text}
                    onChange={(e) =>
                      dispatch({
                        type: 'UPDATE_OPTION',
                        clientId,
                        questionId: q.id,
                        optionId: opt.id,
                        text: e.target.value,
                        isCorrect: opt.isCorrect,
                      })
                    }
                    className="flex-1 px-2 py-1 border border-gray-300 rounded text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
                  />
                  {q.options.length > 2 && (
                    <button
                      type="button"
                      onClick={() =>
                        dispatch({
                          type: 'DELETE_OPTION',
                          clientId,
                          questionId: q.id,
                          optionId: opt.id,
                        })
                      }
                      className="text-xs text-red-400 hover:text-red-600"
                    >
                      ✕
                    </button>
                  )}
                </div>
              ))}

              <button
                type="button"
                onClick={() =>
                  dispatch({ type: 'ADD_OPTION', clientId, questionId: q.id })
                }
                className="text-xs text-indigo-500 hover:text-indigo-700 mt-1"
              >
                + Add Option
              </button>
            </div>
          </div>
        ))}
      </div>

      <button
        type="button"
        onClick={() => dispatch({ type: 'ADD_QUESTION', clientId })}
        className="text-xs text-indigo-600 hover:text-indigo-800 font-medium"
      >
        + Add Question
      </button>
    </div>
  )
}
