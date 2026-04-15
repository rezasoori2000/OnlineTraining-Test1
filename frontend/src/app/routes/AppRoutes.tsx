import { Routes, Route, Navigate } from 'react-router-dom'
import { AppLayout } from '@/components/layout/AppLayout'
import { DashboardPage } from '@/pages/dashboard/DashboardPage'
import { CoursesPage } from '@/pages/courses/CoursesPage'
import { CreateCoursePage } from '@/pages/courses/CreateCoursePage'
import { EditCoursePage } from '@/pages/courses/EditCoursePage'
import { FoldersPage } from '@/pages/folders/FoldersPage'
import { CreateFolderPage } from '@/pages/folders/CreateFolderPage'
import { EditFolderPage } from '@/pages/folders/EditFolderPage'

export function AppRoutes() {
  return (
    <Routes>
      <Route element={<AppLayout />}>
        <Route index element={<Navigate to="/dashboard" replace />} />
        <Route path="/dashboard" element={<DashboardPage />} />
        <Route path="/courses" element={<CoursesPage />} />
        <Route path="/courses/create" element={<CreateCoursePage />} />
        <Route path="/courses/:id" element={<EditCoursePage />} />
        <Route path="/folders" element={<FoldersPage />} />
        <Route path="/folders/create" element={<CreateFolderPage />} />
        <Route path="/folders/:id" element={<EditFolderPage />} />
      </Route>
    </Routes>
  )
}
