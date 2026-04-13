import { Outlet } from 'react-router-dom'
import { Sidebar } from './Sidebar'
import { Header } from './Header'
import { useSidebar } from '@/context/SidebarContext'

export function AppLayout() {
  const { isExpanded, isMobileOpen, isHovered } = useSidebar()
  const sidebarExpanded = isExpanded || isMobileOpen || isHovered

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-950">
      <Sidebar />

      {/* Main content area — offset by sidebar width */}
      <div
        className={[
          'flex flex-col min-h-screen transition-all duration-300 ease-in-out',
          sidebarExpanded ? 'lg:ml-[290px]' : 'lg:ml-[90px]',
        ].join(' ')}
      >
        <Header />
        <main className="flex-1 p-6">
          <Outlet />
        </main>
      </div>

      {/* Mobile overlay */}
      {isMobileOpen && (
        <div
          className="fixed inset-0 z-40 bg-black/50 lg:hidden"
          onClick={() => {
            /* backdrop click handled by SidebarContext toggle */
          }}
        />
      )}
    </div>
  )
}

