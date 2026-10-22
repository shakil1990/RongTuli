import { NavLink, Outlet } from 'react-router-dom'
import { focusRing } from '../common/styles'

const TAB_BASE = `rounded-full px-4 py-2 text-sm transition ${focusRing}`
const TAB_ACTIVE = 'bg-accent-soft font-medium text-accent'
const TAB_IDLE = 'text-muted hover:bg-sunken hover:text-ink'

const tabClasses = ({ isActive }: { isActive: boolean }) =>
  `${TAB_BASE} ${isActive ? TAB_ACTIVE : TAB_IDLE}`

/** The frame every admin page sits in: a title and the two areas an admin moves between. */
export function AdminLayout() {
  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-3xl">Shop admin</h1>
        <nav aria-label="Admin sections">
          <ul className="flex items-center gap-1">
            <li>
              <NavLink to="/admin/products" className={tabClasses}>
                Products
              </NavLink>
            </li>
            <li>
              <NavLink to="/admin/categories" className={tabClasses}>
                Categories
              </NavLink>
            </li>
          </ul>
        </nav>
      </div>

      <Outlet />
    </div>
  )
}
