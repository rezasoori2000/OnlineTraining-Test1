import { useRef, useState } from 'react'
import { ChapterNodeState } from '@/types/createCourse'
import { CourseBuilderAction } from './useCourseBuilder'
import { QuizBuilder } from './QuizBuilder'
import { convertFileToHtml } from '@/utils/convertFileToHtml'

const MAX_FILE_BYTES = 50 * 1024 * 1024 // 50 MB

interface ChapterNodeProps {
  node: ChapterNodeState
  depth: number
  dispatch: React.Dispatch<CourseBuilderAction>
}

export function ChapterNode({ node, depth, dispatch }: ChapterNodeProps) {
  const [converting, setConverting] = useState(false)
  const [convertError, setConvertError] = useState<string | null>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)

  const isLeaf = node.children.length === 0

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
    setConverting(true)

    try {
      const html = await convertFileToHtml(file)
      dispatch({ type: 'SET_CONTENT', clientId: node.clientId, html })
    } catch (err) {
      setConvertError(err instanceof Error ? err.message : 'Conversion failed.')
    } finally {
      setConverting(false)
      // Reset the input so the same file can be re-selected if needed
      if (fileInputRef.current) fileInputRef.current.value = ''
    }
  }

  return (
    <div
      className="border border-gray-200 rounded-md bg-white"
      style={{ marginLeft: depth * 20 }}
    >
      {/* Chapter header row */}
      <div className="flex items-center gap-2 p-3 bg-gray-50 rounded-t-md border-b border-gray-200">
        <input
          type="text"
          placeholder="Chapter title"
          value={node.title}
          onChange={(e) =>
            dispatch({
              type: 'UPDATE_CHAPTER_TITLE',
              clientId: node.clientId,
              title: e.target.value,
            })
          }
          className="flex-1 px-3 py-1.5 border border-gray-300 rounded text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
        />

        <button
          type="button"
          onClick={() =>
            dispatch({ type: 'ADD_CHILD', parentClientId: node.clientId })
          }
          className="px-2 py-1.5 text-xs bg-indigo-50 text-indigo-700 border border-indigo-200 rounded hover:bg-indigo-100 whitespace-nowrap"
        >
          + Add Section
        </button>

        <button
          type="button"
          onClick={() =>
            dispatch({ type: 'DELETE_CHAPTER', clientId: node.clientId })
          }
          className="px-2 py-1.5 text-xs bg-red-50 text-red-600 border border-red-200 rounded hover:bg-red-100"
        >
          Delete
        </button>
      </div>

      {/* Body — only shown for leaf nodes */}
      {isLeaf && (
        <div className="p-3 space-y-3">
          {/* Content upload */}
          <div>
            <label className="block text-xs font-medium text-gray-600 mb-1">
              Content (PDF or PPT/PPTX, max 50 MB)
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
              <p className="mt-1 text-xs text-indigo-500 animate-pulse">
                Converting file to HTML…
              </p>
            )}

            {convertError && (
              <p className="mt-1 text-xs text-red-500">{convertError}</p>
            )}

            {node.htmlContent && !converting && (
              <p className="mt-1 text-xs text-green-600 font-medium">
                ✓ Content ready ({Math.round(node.htmlContent.length / 1024)} KB)
              </p>
            )}
          </div>

          {/* Quiz section */}
          {node.quiz === null ? (
            <button
              type="button"
              onClick={() => dispatch({ type: 'ADD_QUIZ', clientId: node.clientId })}
              className="text-xs text-indigo-600 hover:text-indigo-800 font-medium border border-indigo-200 rounded px-3 py-1.5 bg-indigo-50 hover:bg-indigo-100"
            >
              + Add Quiz
            </button>
          ) : (
            <QuizBuilder
              clientId={node.clientId}
              quiz={node.quiz}
              dispatch={dispatch}
            />
          )}
        </div>
      )}

      {/* Children — rendered below the header */}
      {node.children.length > 0 && (
        <div className="p-3 space-y-2">
          {node.children.map((child) => (
            <ChapterNode
              key={child.clientId}
              node={child}
              depth={0}
              dispatch={dispatch}
            />
          ))}
        </div>
      )}
    </div>
  )
}
