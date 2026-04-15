import { apiClient } from './apiClient'
import type { FolderTreeNode, FolderDetail, CourseDetail, ChapterContent } from '@/types/portal'

export const portalApi = {
  getFolderTree: () => apiClient.get<FolderTreeNode[]>('/folders/tree'),
  getFolderDetail: (id: string) => apiClient.get<FolderDetail>(`/folders/${id}`),
  getCourse: (id: string) => apiClient.get<CourseDetail>(`/courses/${id}`),
  getChapterContent: (id: string) => apiClient.get<ChapterContent>(`/courses/chapters/${id}/content`),
}
