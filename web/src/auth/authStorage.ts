import type { AdminSession } from '../types/auth'

/** Versioned so a later shape change can be told apart from this one. */
export const STORAGE_KEY = 'rongtuli.admin.v1'

/**
 * A stored session is untrusted — it is just text a browser kept, and anyone can edit it. Nothing
 * here grants access: the API re-checks the signature and the expiry on every request. The checks
 * below only stop a damaged or stale entry from being presented as a signed-in admin.
 */
export function parseSession(value: unknown, now: Date): AdminSession | null {
  if (typeof value !== 'object' || value === null) return null
  const session = value as Record<string, unknown>

  const { token, expiresAt, email, name, role } = session
  if (typeof token !== 'string' || token.length === 0) return null
  if (typeof expiresAt !== 'string' || typeof email !== 'string') return null
  if (typeof name !== 'string' || typeof role !== 'string') return null

  const expiry = Date.parse(expiresAt)
  if (Number.isNaN(expiry) || expiry <= now.getTime()) return null

  // Only an admin token opens an admin endpoint, so anything else is not a session worth keeping.
  if (role !== 'Admin') return null

  return { token, expiresAt, email, name, role }
}

export function loadSession(now: Date = new Date()): AdminSession | null {
  // Storage throws in private mode and returns nothing when site data is cleared.
  try {
    const raw = window.localStorage.getItem(STORAGE_KEY)
    if (!raw) return null
    return parseSession(JSON.parse(raw), now)
  } catch {
    return null
  }
}

export function saveSession(session: AdminSession | null): void {
  try {
    if (session === null) {
      window.localStorage.removeItem(STORAGE_KEY)
      return
    }
    window.localStorage.setItem(STORAGE_KEY, JSON.stringify(session))
  } catch {
    // A session that cannot be persisted still works until the tab is closed.
  }
}
