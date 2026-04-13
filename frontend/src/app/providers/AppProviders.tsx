import { ReactNode } from 'react'
import { SidebarProvider } from '@/context/SidebarContext'
import { QueryProvider } from './QueryProvider'
import { RouterProvider } from './RouterProvider'

interface AppProvidersProps {
  children: ReactNode
}

export function AppProviders({ children }: AppProvidersProps) {
  return (
    <RouterProvider>
      <QueryProvider>
        <SidebarProvider>
          {children}
        </SidebarProvider>
      </QueryProvider>
    </RouterProvider>
  )
}
