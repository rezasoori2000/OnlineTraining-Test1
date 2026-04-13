import { ReactNode } from 'react'
import { QueryProvider } from './QueryProvider'
import { RouterProvider } from './RouterProvider'

interface AppProvidersProps {
  children: ReactNode
}

export function AppProviders({ children }: AppProvidersProps) {
  return (
    <RouterProvider>
      <QueryProvider>
        {children}
      </QueryProvider>
    </RouterProvider>
  )
}
