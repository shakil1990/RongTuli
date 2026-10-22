import type { ReactNode } from 'react'
import { useAuth } from '../auth/useAuth'
import { AdminSignIn } from './AdminSignIn'

/**
 * Shows the sign-in card in place of the page rather than redirecting, so the admin lands back
 * where they were headed once they are in. The API is what actually enforces this — the guard
 * only keeps a signed-out person from seeing a panel whose every request would answer 401.
 */
export function RequireAdmin({ children }: { children: ReactNode }) {
  const { admin } = useAuth()
  return admin ? <>{children}</> : <AdminSignIn />
}
