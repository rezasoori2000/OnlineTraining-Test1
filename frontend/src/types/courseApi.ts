/**
 * courseApi.ts
 * TypeScript mirrors of the backend's FullCreateCourse DTOs.
 * Must stay in sync with PGLLMS.Admin.Application.DTOs.Course.FullCreateCourseRequest.
 */

export interface FullCreateCourseInfoDto {
  title: string
  description: string
  languageCode: string
}

export interface FullCreateOptionDto {
  text: string
  isCorrect: boolean
}

export interface FullCreateQuestionDto {
  text: string
  order: number
  options: FullCreateOptionDto[]
}

export interface FullCreateQuizDto {
  isMandatory: boolean
  passingPercentage: number
  questions: FullCreateQuestionDto[]
}

export interface FullCreateChapterDto {
  clientId: string
  title: string
  order: number
  children: FullCreateChapterDto[]
  htmlContent?: string
  quiz?: FullCreateQuizDto
}

export interface FullCreateCourseRequest {
  course: FullCreateCourseInfoDto
  chapters: FullCreateChapterDto[]
}

export interface FullCreateCourseResponse {
  courseId: string
  slug: string
  courseVersionId: string
}
