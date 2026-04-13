import { useLocation } from 'react-router-dom'
import { useSidebar } from '@/context/SidebarContext'

const routeLabels: Record<string, string> = {
  '/dashboard': 'Dashboard',
  '/courses': 'Courses',
  '/courses/create': 'Create Course',
}

function getPageTitle(pathname: string): string {
  const match = Object.keys(routeLabels)
    .sort((a, b) => b.length - a.length)
    .find((key) => pathname.startsWith(key))
  return match ? routeLabels[match] : 'Page'
}

const HamburgerIcon = () => (
  <svg width="16" height="12" viewBox="0 0 16 12" fill="none" xmlns="http://www.w3.org/2000/svg">
    <path
      fillRule="evenodd"
      clipRule="evenodd"
      d="M0.583 1C0.583 0.586 0.919 0.25 1.333 0.25H14.667C15.081 0.25 15.417 0.586 15.417 1C15.417 1.414 15.081 1.75 14.667 1.75L1.333 1.75C0.919 1.75 0.583 1.414 0.583 1ZM0.583 11C0.583 10.586 0.919 10.25 1.333 10.25L14.667 10.25C15.081 10.25 15.417 10.586 15.417 11C15.417 11.414 15.081 11.75 14.667 11.75L1.333 11.75C0.919 11.75 0.583 11.414 0.583 11ZM1.333 5.25C0.919 5.25 0.583 5.586 0.583 6C0.583 6.414 0.919 6.75 1.333 6.75L7.999 6.75C8.413 6.75 8.749 6.414 8.749 6C8.749 5.586 8.413 5.25 7.999 5.25L1.333 5.25Z"
      fill="currentColor"
    />
  </svg>
)

export function Header() {
  const location = useLocation()
  const pageTitle = getPageTitle(location.pathname)
  const { toggleSidebar, toggleMobileSidebar } = useSidebar()

  const handleToggle = () => {
    if (window.innerWidth >= 1024) {
      toggleSidebar()
    } else {
      toggleMobileSidebar()
    }
  }

  return (
    <header className="sticky top-0 flex w-full bg-white border-b border-gray-200 z-[99999] dark:border-gray-800 dark:bg-gray-900 lg:border-b">
      <div className="flex items-center justify-between w-full px-4 py-3 lg:px-6">
        {/* Left: toggle + breadcrumb */}
        <div className="flex items-center gap-4">
          <button
            onClick={handleToggle}
            className="flex items-center justify-center w-10 h-10 text-gray-500 rounded-lg border border-gray-200 hover:bg-gray-100 dark:border-gray-800 dark:text-gray-400 dark:hover:bg-gray-800"
            aria-label="Toggle Sidebar"
          >
            <HamburgerIcon />
          </button>
          <div>
            <p className="text-xs text-gray-400">Pages / {pageTitle}</p>
            <h1 className="text-lg font-bold text-gray-900 dark:text-white leading-tight">
              {pageTitle}
            </h1>
          </div>
        </div>
      </div>
    </header>
  )
}


