import { Routes, Route, Navigate } from 'react-router-dom'
import { AppLayout } from '@/components/layout/AppLayout'
import { DashboardPage } from '@/pages/dashboard/DashboardPage'
import { CoursesPage } from '@/pages/courses/CoursesPage'
import { CreateCoursePage } from '@/pages/courses/CreateCoursePage'
import { EditCoursePage } from '@/pages/courses/EditCoursePage'

export function AppRoutes() {
  return (
    <Routes>
      <Route element={<AppLayout />}>
        <Route index element={<Navigate to="/dashboard" replace />} />
        <Route path="/dashboard" element={<DashboardPage />} />
        <Route path="/courses" element={<CoursesPage />} />
        <Route path="/courses/create" element={<CreateCoursePage />} />
        <Route path="/courses/:id" element={<EditCoursePage />} />
      </Route>
    </Routes>
  )
}
