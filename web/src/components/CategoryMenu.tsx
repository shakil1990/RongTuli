import { useQuery } from '@tanstack/react-query'
import { NavLink } from 'react-router-dom'
import { catalogKeys, fetchCategories } from '../services/catalog'
import { Skeleton } from './common/Skeleton'
import { focusRing } from './common/styles'

const LINK_BASE = `shrink-0 rounded-full px-3.5 py-1.5 text-sm transition ${focusRing}`
const LINK_ACTIVE = 'bg-accent-soft font-medium text-accent'
const LINK_IDLE = 'text-muted hover:bg-sunken hover:text-ink'

const linkClasses = ({ isActive }: { isActive: boolean }) =>
  `${LINK_BASE} ${isActive ? LINK_ACTIVE : LINK_IDLE}`

export function CategoryMenu() {
  const { data, isPending, isError } = useQuery({
    queryKey: catalogKeys.categories,
    queryFn: ({ signal }) => fetchCategories(signal),
    staleTime: 5 * 60 * 1000,
  })

  // The menu is navigation, not content: if it cannot load, the rest of the page still works.
  if (isError) {
    return null
  }

  return (
    // Scrolls sideways by swipe on a narrow phone, without drawing a scrollbar. The padding
    // leaves room for the links' focus ring, which used to overflow the list vertically and make
    // the browser draw a vertical scrollbar beside the menu.
    <nav
      aria-label="Product categories"
      className="-mx-1 -my-1 overflow-x-auto overflow-y-hidden [scrollbar-width:none]"
    >
      <ul className="flex items-center gap-1 p-1">
        <li>
          <NavLink to="/" end className={linkClasses}>
            Everything
          </NavLink>
        </li>
        {isPending
          ? Array.from({ length: 4 }, (_, index) => (
              <li key={index}>
                <Skeleton className="h-8 w-20 rounded-full" />
              </li>
            ))
          : (data ?? []).map((category) => (
              <li key={category.id}>
                <NavLink to={`/c/${category.slug}`} className={linkClasses}>
                  {category.name}
                </NavLink>
              </li>
            ))}
      </ul>
    </nav>
  )
}
