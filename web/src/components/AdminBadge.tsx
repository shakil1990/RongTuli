import { Link } from 'react-router-dom'
import { useAuth } from '../auth/useAuth'
import { Button } from './common/Button'
import { focusRing } from './common/styles'

/**
 * Nothing at all when signed out — customers never sign in, so the storefront header has no
 * reason to offer it. A signed-in admin gets their way back to the panel and a way out.
 */
export function AdminBadge() {
  const { admin, signOut } = useAuth()
  if (!admin) return null

  return (
    <div className="flex items-center gap-1">
      <Link
        to="/admin"
        className={`${focusRing} hidden rounded-full px-3 py-2 text-sm text-muted hover:text-ink sm:inline`}
      >
        {admin.name}
      </Link>
      <Button variant="secondary" size="sm" onClick={signOut}>
        Sign out
      </Button>
    </div>
  )
}
