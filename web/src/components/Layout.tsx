import { Link, Outlet } from 'react-router-dom'
import { AdminBadge } from './AdminBadge'
import { CartBadge } from './CartBadge'
import { CategoryMenu } from './CategoryMenu'
import { HealthStatus } from './HealthStatus'
import { focusRing } from './common/styles'

export function Layout() {
  return (
    <div className="flex min-h-screen flex-col">
      <a
        href="#main"
        className={`${focusRing} sr-only focus:not-sr-only focus:absolute focus:top-3 focus:left-3
          focus:z-6 focus:rounded-full focus:bg-accent focus:px-4 focus:py-2 focus:text-accent-ink`}
      >
        Skip to content
      </a>

      <header className="sticky top-0 z-5 border-b border-line bg-surface/90 backdrop-blur">
        <div className="mx-auto flex w-full max-w-6xl items-center justify-between gap-4 px-4 py-3">
          <Link
            to="/"
            className={`${focusRing} rounded-lg font-display text-2xl font-semibold tracking-tight`}
          >
            Rong<span className="text-accent">Tuli</span>
          </Link>

          <nav aria-label="Account and cart" className="flex items-center gap-1">
            <CartBadge />
            <AdminBadge />
          </nav>
        </div>

        <div className="mx-auto w-full max-w-6xl px-4 pb-2.5">
          <CategoryMenu />
        </div>
      </header>

      <main id="main" className="mx-auto w-full max-w-6xl flex-1 px-4 py-8">
        <Outlet />
      </main>

      <footer className="border-t border-line bg-raised">
        <div
          className="mx-auto flex w-full max-w-6xl flex-wrap items-center justify-between gap-2
            px-4 py-5 text-sm text-muted"
        >
          <p>Cash on delivery across Bangladesh · RongTuli</p>
          <HealthStatus />
        </div>
      </footer>
    </div>
  )
}
