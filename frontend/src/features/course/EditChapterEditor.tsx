import { useRef, useState } from 'react'
import { EditChapterNodeState, EditQuizState } from '@/types/editCourse'
import { EditBuilderAction } from './useEditCourseBuilder'
import { convertFileToHtml } from '@/utils/convertFileToHtml'
import { apiClient } from '@/services/api/apiClient'

const MAX_FILE_BYTES = 50 * 1024 * 1024 // 50 MB

interface Props {
  node: EditChapterNodeState
  dispatch: React.Dispatch<EditBuilderAction>
}

// ── Inline quiz builder ────────────────────────────────────────────────────────

function QuizSection({
  clientId,
  quiz,
  dispatch,
}: {
  clientId: string
  quiz: EditQuizState
  dispatch: React.Dispatch<EditBuilderAction>
}) {
  return (
    <div className="border border-indigo-200 rounded-lg p-4 bg-indigo-50 space-y-3">
      <div className="flex items-center justify-between">
        <span className="text-sm font-semibold text-indigo-700">
          Quiz
          {quiz.isDirty && (
            <span className="ml-1.5 text-xs font-normal text-amber-600">(unsaved changes)</span>
          )}
        </span>
        <button
          type="button"
          onClick={() => dispatch({ type: 'REMOVE_QUIZ', clientId })}
          className="text-xs text-red-500 hover:text-red-700"
        >
          Remove Quiz
        </button>
      </div>

      <label className="flex items-center gap-2 text-sm text-gray-700">
        <input
          type="checkbox"
          checked={quiz.isMandatory}
          onChange={(e) =>
            dispatch({ type: 'UPDATE_QUIZ_MANDATORY', clientId, isMandatory: e.target.checked })
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
              dispatch({ type: 'UPDATE_QUIZ_PASSING', clientId, passingPercentage: Number(e.target.value) })
            }
            className="w-20 px-2 py-1 border border-gray-300 rounded text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
          />
        </label>
      )}

      <div className="space-y-3">
        {quiz.questions.map((q, qIdx) => (
          <div key={q.localId} className="bg-white border border-gray-200 rounded-lg p-3 space-y-2">
            <div className="flex items-start gap-2">
              <textarea
                placeholder={`Question ${qIdx + 1}`}
                value={q.text}
                rows={2}
                onChange={(e) =>
                  dispatch({ type: 'UPDATE_QUESTION_TEXT', clientId, questionId: q.localId, text: e.target.value })
                }
                className="flex-1 px-2 py-1 border border-gray-300 rounded text-sm resize-none focus:outline-none focus:ring-2 focus:ring-indigo-500"
              />
              {quiz.questions.length > 1 && (
                <button
                  type="button"
                  onClick={() => dispatch({ type: 'DELETE_QUESTION', clientId, questionId: q.localId })}
                  className="text-xs text-red-400 hover:text-red-600 mt-1"
                >
                  ✕
                </button>
              )}
            </div>

            <div className="space-y-1 pl-2">
              {q.options.map((opt) => (
                <div key={opt.localId} className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    checked={opt.isCorrect}
                    onChange={(e) =>
                      dispatch({
                        type: 'UPDATE_OPTION',
                        clientId,
                        questionId: q.localId,
                        optionId: opt.localId,
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
                        questionId: q.localId,
                        optionId: opt.localId,
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
                        dispatch({ type: 'DELETE_OPTION', clientId, questionId: q.localId, optionId: opt.localId })
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
                onClick={() => dispatch({ type: 'ADD_OPTION', clientId, questionId: q.localId })}
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

// ── Chapter editor panel ──────────────────────────────────────────────────────

export function EditChapterEditor({ node, dispatch }: Props) {
  const [converting, setConverting] = useState(false)
  const [convertError, setConvertError] = useState<string | null>(null)
  const [uploadSuccess, setUploadSuccess] = useState(false)
  const fileInputRef = useRef<HTMLInputElement>(null)

  const isLeaf = node.children.filter((c) => !(c.isNew && c.isDeleted)).length === 0

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file) return

    if (file.size > MAX_FILE_BYTES) {
      setConvertError('File exceeds the 50 MB limit.')
      return
    }
    const ext = file.name.split('.').pop()?.toLowerCase()
    if (!['pdf', 'ppt', 'pptx'].includes(ext ?? '')) {
      setConvertError('Only PDF or PPT/PPTX files are allowed.')
      return
    }

    setConvertError(null)
    setUploadSuccess(false)
    setConverting(true)

    try {
      if (ext === 'pdf') {
        // PDF: upload directly to OneDrive via the backend endpoint
        if (node.isNew || !node.serverId) {
          setConvertError('Save this section first, then upload a PDF.')
          return
        }
        const formData = new FormData()
        formData.append('pdf', file)
        await apiClient.postFormData(`/admin/chapters/${node.serverId}/pdf`, formData)
        dispatch({ type: 'MARK_PDF_UPLOADED', clientId: node.clientId })
        setUploadSuccess(true)
      } else {
        // PPTX / PPT: convert to HTML locally then batch with next Save
        const html = await convertFileToHtml(file)
        dispatch({ type: 'SET_CONTENT', clientId: node.clientId, html })
      }
    } catch (err) {
      setConvertError(err instanceof Error ? err.message : 'Upload failed.')
    } finally {
      setConverting(false)
      if (fileInputRef.current) fileInputRef.current.value = ''
    }
  }

  return (
    <div className="space-y-6">
      {/* Chapter title */}
      <div className="bg-white border border-gray-200 rounded-lg p-5 space-y-4">
        <div className="flex items-center gap-2">
          <h3 className="text-sm font-semibold text-gray-800">
            {isLeaf ? 'Section' : 'Chapter'} Details
          </h3>
          {node.isNew && (
            <span className="px-1.5 py-0.5 text-xs bg-green-100 text-green-700 border border-green-200 rounded">
              new
            </span>
          )}
          {!node.isNew && node.isDirty && (
            <span className="px-1.5 py-0.5 text-xs bg-amber-100 text-amber-700 border border-amber-200 rounded">
              unsaved changes
            </span>
          )}
        </div>

        <div>
          <label className="block text-xs font-medium text-gray-600 mb-1">Title</label>
          <input
            type="text"
            placeholder="Chapter title"
            value={node.title}
            onChange={(e) =>
              dispatch({ type: 'UPDATE_CHAPTER_TITLE', clientId: node.clientId, title: e.target.value })
            }
            className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
          />
        </div>
      </div>

      {/* Content section — leaf chapters only */}
      {isLeaf && (
        <div className="bg-white border border-gray-200 rounded-lg p-5 space-y-3">
          <h3 className="text-sm font-semibold text-gray-800">Content</h3>

          {/* Status indicator */}
          {node.isContentDirty ? (
            <div className="flex items-center gap-2 text-xs text-amber-700 bg-amber-50 border border-amber-200 rounded px-3 py-2">
              <span>●</span>
              <span>
                New content ready ({Math.round((node.htmlContent?.length ?? 0) / 1024)} KB) — will be saved on next Save.
              </span>
            </div>
          ) : uploadSuccess ? (
            <div className="flex items-center gap-2 text-xs text-green-700 bg-green-50 border border-green-200 rounded px-3 py-2">
              <span>✓</span>
              <span>PDF uploaded to OneDrive successfully.</span>
            </div>
          ) : node.hasContent ? (
            <div className="flex items-center gap-2 text-xs text-green-700 bg-green-50 border border-green-200 rounded px-3 py-2">
              <span>✓</span>
              <span>This section has stored content. Upload a new file to replace it.</span>
            </div>
          ) : (
            <div className="flex items-center gap-2 text-xs text-gray-500 bg-gray-50 border border-gray-200 rounded px-3 py-2">
              <span>○</span>
              <span>No content yet. Upload a PDF or PPT/PPTX file.</span>
            </div>
          )}

          <div>
            <label className="block text-xs font-medium text-gray-600 mb-1">
              Upload file (PDF or PPT/PPTX, max 50 MB)
            </label>
            <input
              ref={fileInputRef}
              type="file"
              accept=".pdf,.ppt,.pptx"
              onChange={handleFileChange}
              disabled={converting}
              className="block w-full text-sm text-gray-500 file:mr-3 file:py-1.5 file:px-3 file:rounded file:border-0 file:text-sm file:font-medium file:bg-indigo-50 file:text-indigo-700 hover:file:bg-indigo-100 disabled:opacity-50"
            />
            {converting && (
              <p className="mt-1 text-xs text-indigo-500 animate-pulse">Processing file…</p>
            )}
            {convertError && (
              <p className="mt-1 text-xs text-red-500">{convertError}</p>
            )}
          </div>
        </div>
      )}

      {/* Non-leaf info */}
      {!isLeaf && (
        <div className="bg-gray-50 border border-gray-200 rounded-lg p-4 text-sm text-gray-500">
          This chapter has sub-sections. Use the tree on the left to select a sub-section and add content to it.
        </div>
      )}

      {/* Quiz section — leaf chapters only */}
      {isLeaf && (
        <div className="space-y-2">
          {node.quiz === null ? (
            <button
              type="button"
              onClick={() => dispatch({ type: 'ADD_QUIZ', clientId: node.clientId })}
              className="text-xs text-indigo-600 hover:text-indigo-800 font-medium border border-indigo-200 rounded px-3 py-1.5 bg-indigo-50 hover:bg-indigo-100"
            >
              + Add Quiz
            </button>
          ) : (
            <QuizSection clientId={node.clientId} quiz={node.quiz} dispatch={dispatch} />
          )}
        </div>
      )}
    </div>
  )
}
