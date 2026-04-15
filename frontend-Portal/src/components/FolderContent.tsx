import type { FolderDetail } from '@/types/portal'

interface FolderContentProps {
  folder: FolderDetail
}

export function FolderContent({ folder }: FolderContentProps) {
  const hasHtml = folder.htmlContent && folder.htmlContent.trim().length > 0

  return (
    <div className="h-full overflow-y-auto p-4">
      <h2 className="text-lg font-semibold text-gray-900 mb-2">{folder.name}</h2>
      {folder.description && !hasHtml && (
        <p className="text-sm text-gray-600">{folder.description}</p>
      )}
      {hasHtml && (
        <div
          className="pptx-html-content"
          dangerouslySetInnerHTML={{ __html: folder.htmlContent! }}
        />
      )}
      {!hasHtml && !folder.description && (
        <p className="text-sm text-gray-400 italic">No content available for this folder.</p>
      )}
    </div>
  )
}
