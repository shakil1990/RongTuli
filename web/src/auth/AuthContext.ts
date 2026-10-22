import { createContext } from 'react'
import type { AdminSession } from '../types/auth'

export type AuthStore = {
  /** The signed-in admin, or null. Never set for a non-admin: they are not given a token. */
  admin: AdminSession | null
  /** Exchanges a Google ID token for a RongTuli session. Resolves once the attempt is over. */
  signIn: (googleIdToken: string) => Promise<void>
  signOut: () => void
  isSigningIn: boolean
  /** Why the last attempt failed, in words meant for the person who tried. */
  error: string | null
}

/**
 * A context rather than a store library, for the same reason the cart is one: three consumers
 * (the header, the admin route guard and Step 7's admin panel) and no dependency needed.
 */
export const AuthContext = createContext<AuthStore | null>(null)
