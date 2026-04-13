import { AppProviders } from '@/app/providers'
import { AppRoutes } from '@/app/routes/AppRoutes'

export function App() {
  return (
    <AppProviders>
      <AppRoutes />
    </AppProviders>
  )
}
