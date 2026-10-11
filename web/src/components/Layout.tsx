import { Link, Outlet } from 'react-router-dom'
import { HealthStatus } from './HealthStatus'

export function Layout() {
  return (
    <div className="flex min-h-screen flex-col">
      <header className="flex items-center justify-between border-b px-4 py-3">
        <Link to="/" className="text-xl font-semibold">
          RongTuli
        </Link>
        <nav className="flex items-center gap-4">
          <Link to="/cart">Cart (0)</Link>
          <button type="button">Sign in</button>
        </nav>
      </header>
      <main className="flex-1 px-4 py-6">
        <Outlet />
      </main>
      <footer className="border-t px-4 py-3 text-sm text-gray-600">
        <HealthStatus />
      </footer>
    </div>
  )
}
