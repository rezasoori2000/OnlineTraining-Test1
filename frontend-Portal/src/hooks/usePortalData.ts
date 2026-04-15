import { useQuery } from '@tanstack/react-query'
import { portalApi } from '@/services/portalApi'

export function useFolderTree() {
  return useQuery({
    queryKey: ['folderTree'],
    queryFn: portalApi.getFolderTree,
  })
}

export function useFolderDetail(id: string | null) {
  return useQuery({
    queryKey: ['folderDetail', id],
    queryFn: () => portalApi.getFolderDetail(id!),
    enabled: !!id,
  })
}

export function useCourse(id: string | null) {
  return useQuery({
    queryKey: ['course', id],
    queryFn: () => portalApi.getCourse(id!),
    enabled: !!id,
  })
}

export function useChapterContent(id: string | null) {
  return useQuery({
    queryKey: ['chapterContent', id],
    queryFn: () => portalApi.getChapterContent(id!),
    enabled: !!id,
  })
}
