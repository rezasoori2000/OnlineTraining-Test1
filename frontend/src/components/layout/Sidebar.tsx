import { useCallback } from 'react'
import { NavLink, useLocation } from 'react-router-dom'
import { useSidebar } from '@/context/SidebarContext'

const DashboardIcon = () => (
  <svg width="22" height="22" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
    <rect x="3" y="3" width="7" height="7" rx="1.5" stroke="currentColor" strokeWidth="1.5" />
    <rect x="14" y="3" width="7" height="7" rx="1.5" stroke="currentColor" strokeWidth="1.5" />
    <rect x="3" y="14" width="7" height="7" rx="1.5" stroke="currentColor" strokeWidth="1.5" />
    <rect x="14" y="14" width="7" height="7" rx="1.5" stroke="currentColor" strokeWidth="1.5" />
  </svg>
)

const CoursesIcon = () => (
  <svg width="22" height="22" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
    <path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" />
    <path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z" stroke="currentColor" strokeWidth="1.5" strokeLinejoin="round" />
  </svg>
)

const FoldersIcon = () => (
  <svg width="22" height="22" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
    <path d="M3 7v10a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2h-6l-2-2H5a2 2 0 00-2 2z" stroke="currentColor" strokeWidth="1.5" strokeLinejoin="round" />
  </svg>
)

const ConverterIcon = () => (
  <svg width="22" height="22" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
    <path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8l-6-6z" stroke="currentColor" strokeWidth="1.5" strokeLinejoin="round" />
    <path d="M14 2v6h6M9 15l2 2 4-4" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" />
  </svg>
)

type NavItem = {
  name: string
  icon: React.ReactNode
  path: string
}

const navItems: NavItem[] = [
  { icon: <DashboardIcon />, name: 'Dashboard', path: '/dashboard' },
  { icon: <CoursesIcon />, name: 'Courses', path: '/courses' },
  { icon: <FoldersIcon />, name: 'Folders', path: '/folders' },
  { icon: <ConverterIcon />, name: 'File Converter', path: '/file-converter' },
]

export function Sidebar() {
  const { isExpanded, isMobileOpen, isHovered, setIsHovered } = useSidebar()
  const location = useLocation()

  const isActive = useCallback(
    (path: string) => location.pathname.startsWith(path),
    [location.pathname],
  )

  const showText = isExpanded || isHovered || isMobileOpen

  return (
    <aside
      className={[
        'fixed top-0 left-0 flex flex-col h-screen px-4',
        'bg-white dark:bg-gray-900 border-r border-gray-200 dark:border-gray-800',
        'transition-all duration-300 ease-in-out z-50',
        isExpanded || isMobileOpen
          ? 'w-[290px]'
          : isHovered
          ? 'w-[290px]'
          : 'w-[90px]',
        isMobileOpen ? 'translate-x-0' : '-translate-x-full',
        'lg:translate-x-0',
      ].join(' ')}
      onMouseEnter={() => !isExpanded && setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
    >
      {/* Logo / brand */}
      <div className={`py-7 flex ${!showText ? 'justify-center' : 'justify-start'}`}>
        {showText ? (
          <span className="text-xl font-extrabold text-gray-900 dark:text-white tracking-tight">
            PGLLMS
          </span>
        ) : (
          <span className="text-xl font-extrabold text-brand-500">P</span>
        )}
      </div>

      {/* Nav */}
      <div className="flex flex-col overflow-y-auto no-scrollbar flex-1">
        <nav>
          {showText && (
            <h2 className="mb-3 text-xs uppercase text-gray-400 font-medium tracking-wider px-3">
              Menu
            </h2>
          )}
          <ul className="flex flex-col gap-1.5">
            {navItems.map((item) => {
              const active = isActive(item.path)
              return (
                <li key={item.path}>
                  <NavLink
                    to={item.path}
                    className={[
                      'menu-item group',
                      active ? 'menu-item-active' : 'menu-item-inactive',
                      !showText ? 'lg:justify-center' : '',
                    ].join(' ')}
                  >
                    <span
                      className={`flex-shrink-0 ${
                        active ? 'menu-item-icon-active' : 'menu-item-icon-inactive'
                      }`}
                    >
                      {item.icon}
                    </span>
                    {showText && <span>{item.name}</span>}
                  </NavLink>
                </li>
              )
            })}
          </ul>
        </nav>
      </div>
    </aside>
  )
}


