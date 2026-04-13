import { Course } from '@/types/course'

export const mockCourses: Course[] = [
  {
    id: '1',
    title: 'Introduction to React',
    status: 'Published',
    version: 3,
    updatedAt: '2026-03-15T10:30:00Z',
  },
  {
    id: '2',
    title: 'Advanced TypeScript',
    status: 'Draft',
    version: 1,
    updatedAt: '2026-04-01T08:00:00Z',
  },
  {
    id: '3',
    title: 'Tailwind CSS Fundamentals',
    status: 'Published',
    version: 2,
    updatedAt: '2026-02-20T14:15:00Z',
  },
  {
    id: '4',
    title: 'Node.js & REST APIs',
    status: 'Archived',
    version: 5,
    updatedAt: '2025-12-10T09:45:00Z',
  },
  {
    id: '5',
    title: 'SQL for Developers',
    status: 'Draft',
    version: 1,
    updatedAt: '2026-04-08T16:00:00Z',
  },
]
