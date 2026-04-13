// Shared TypeScript models / interfaces

export interface PaginatedResponse<T> {
  items: T[]
  totalCount: number
  pageNumber: number
  pageSize: number
}

export interface ApiError {
  message: string
  statusCode: number
}
