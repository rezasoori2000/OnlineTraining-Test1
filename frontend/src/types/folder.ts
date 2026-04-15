export interface FolderListItem {
  id: string
  name: string
  description: string | null
  parentName: string | null
  childrenCount: number
  updatedAt: string
}

export interface FolderDetail {
  id: string
  name: string
  description: string | null
  htmlContent: string | null
  parentId: string | null
  parentName: string | null
  attributes: FolderAttributeDto[]
  courses: FolderCourseDto[]
  children: FolderChildDto[]
}

export interface FolderAttributeDto {
  id: string
  key: string
  value: string
}

export interface FolderCourseDto {
  courseId: string
  title: string
  status: string
}

export interface FolderChildDto {
  id: string
  name: string
}

export interface FolderTreeNode {
  id: string
  name: string
  parentId: string | null
  children: FolderTreeNode[]
}

export interface CreateFolderRequest {
  name: string
  description: string | null
  parentId: string | null
  attributes: FolderAttributeRequest[] | null
}

export interface UpdateFolderRequest {
  name: string
  description: string | null
  htmlContent: string | null
  parentId: string | null
  attributes: FolderAttributeRequest[] | null
}

export interface FolderAttributeRequest {
  key: string
  value: string
}

export interface AssignCourseRequest {
  courseId: string
}
