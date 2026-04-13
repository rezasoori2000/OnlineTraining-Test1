import { useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { ColumnDef } from '@tanstack/react-table'
import { DataTable } from '@/components/ui/DataTable'
import { Button } from '@/components/ui/Button'
import { apiClient } from '@/services/api/apiClient'
import { Course, CourseStatus } from '@/types/course'
import { formatDate } from '@/utils/helpers'

const STATUS_OPTIONS: Array<{ value: CourseStatus | 'All'; label: string }> = [
  { value: 'All', label: 'All' },
  { value: 'Draft', label: 'Draft' },
  { value: 'Published', label: 'Published' },
  { value: 'Archived', label: 'Archived' },
]

const STATUS_BADGE: Record<CourseStatus, string> = {
  Draft: 'bg-yellow-100 text-yellow-800',
  Published: 'bg-green-100 text-green-800',
  Archived: 'bg-gray-100 text-gray-600',
}

export function CoursesPage() {
  const navigate = useNavigate()
  const [search, setSearch] = useState('')
  const [statusFilter, setStatusFilter] = useState<CourseStatus | 'All'>('All')

  const { data: courses = [], isLoading, isError } = useQuery<Course[]>({
    queryKey: ['courses'],
    queryFn: () => apiClient.get<Course[]>('/courses'),
  })

  const filtered = useMemo(() => {
    return courses.filter((course) => {
      const matchesSearch = course.title.toLowerCase().includes(search.toLowerCase())
      const matchesStatus = statusFilter === 'All' || course.status === statusFilter
      return matchesSearch && matchesStatus
    })
  }, [courses, search, statusFilter])

  const columns = useMemo<ColumnDef<Course, unknown>[]>(
    () => [
      {
        accessorKey: 'title',
        header: 'Title',
      },
      {
        accessorKey: 'status',
        header: 'Status',
        cell: ({ getValue }) => {
          const status = getValue() as CourseStatus
          return (
            <span
              className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${STATUS_BADGE[status]}`}
            >
              {status}
            </span>
          )
        },
      },
      {
        accessorKey: 'version',
        header: 'Version',
        cell: ({ getValue }) => `v${getValue() as number}`,
      },
      {
        accessorKey: 'updatedAt',
        header: 'Updated At',
        cell: ({ getValue }) => formatDate(getValue() as string),
      },
      {
        id: 'actions',
        header: 'Actions',
        cell: ({ row }) => (
          <Button
            variant="secondary"
            onClick={() => navigate(`/courses/${row.original.id}`)}
          >
            Edit
          </Button>
        ),
      },
    ],
    [navigate],
  )

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h2 className="text-title-sm font-bold text-gray-900">Courses</h2>
        <Button onClick={() => navigate('/courses/create')}>Create Course</Button>
      </div>

      {/* Filters + Table card */}
      <div className="card p-5 xl:p-6 space-y-4">
        {/* Filters */}
        <div className="flex flex-col sm:flex-row gap-3">
          <input
            type="text"
            placeholder="Search by title..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="form-input sm:w-72"
          />
          <select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value as CourseStatus | 'All')}
            className="form-select sm:w-44"
          >
            {STATUS_OPTIONS.map((opt) => (
              <option key={opt.value} value={opt.value}>
                {opt.label}
              </option>
            ))}
          </select>
        </div>

        {/* Table */}
        {isLoading ? (
          <p className="text-theme-sm text-gray-500 py-4">Loading courses…</p>
        ) : isError ? (
          <p className="text-theme-sm text-red-500 py-4">Failed to load courses. Make sure the backend is running.</p>
        ) : (
          <DataTable columns={columns} data={filtered} />
        )}
      </div>
    </div>
  )
}
