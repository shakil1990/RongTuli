import { useQuery } from '@tanstack/react-query'
import { NavLink, Outlet } from 'react-router-dom'
import { useAdminCall } from '../../auth/useAdminCall'
import { fetchOrderCounts, orderKeys } from '../../services/orders'
import { focusRing } from '../common/styles'

const TAB_BASE = `rounded-full px-4 py-2 text-sm transition ${focusRing}`
const TAB_ACTIVE = 'bg-accent-soft font-medium text-accent'
const TAB_IDLE = 'text-muted hover:bg-sunken hover:text-ink'

// The new-order alert: checked again every minute while the panel is open.
const COUNTS_REFRESH_MS = 60 * 1000

const tabClasses = ({ isActive }: { isActive: boolean }) =>
  `${TAB_BASE} ${isActive ? TAB_ACTIVE : TAB_IDLE}`

/** The frame every admin page sits in: a title and the areas an admin moves between. */
export function AdminLayout() {
  const call = useAdminCall()
  const { data: counts } = useQuery({
    queryKey: orderKeys.adminCounts,
    queryFn: ({ signal }) => call((token) => fetchOrderCounts(token, signal)),
    refetchInterval: COUNTS_REFRESH_MS,
  })
  const pending = counts?.Pending ?? 0

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-3xl">Shop admin</h1>
        <nav aria-label="Admin sections">
          <ul className="flex items-center gap-1">
            <li>
              <NavLink
                to="/admin/orders"
                className={tabClasses}
                aria-label={pending > 0 ? `Orders, ${pending} pending` : undefined}
              >
                Orders
                {pending > 0 && (
                  <span
                    aria-hidden="true"
                    className="ml-1.5 inline-flex h-5 min-w-5 items-center justify-center rounded-full bg-accent
                      px-1 text-xs font-semibold text-accent-ink tabular-nums"
                  >
                    {pending}
                  </span>
                )}
              </NavLink>
            </li>
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
