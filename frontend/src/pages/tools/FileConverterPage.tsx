import { lazy, Suspense } from 'react'

// eslint-disable-next-line @typescript-eslint/no-explicit-any
const FileConverter = lazy(() => import('../../../contentconverter-example/FileConverter' as any))

export function FileConverterPage() {
  return (
    <Suspense fallback={<div className="p-8 text-gray-400">Loading…</div>}>
      <FileConverter />
    </Suspense>
  )
}
