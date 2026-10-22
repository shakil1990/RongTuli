import type { VerifiedPhone } from '../types/phone'

/** Versioned so a later shape change can be told apart from this one. */
export const STORAGE_KEY = 'rongtuli.phone.v1'

const E164_MOBILE = /^\+8801[3-9]\d{8}$/

/**
 * A stored token is untrusted text; the API re-checks the signature and expiry on every call. These
 * checks only keep a damaged or expired entry from being shown as a verified number.
 */
export function parseVerifiedPhone(value: unknown, now: Date): VerifiedPhone | null {
  if (typeof value !== 'object' || value === null) return null
  const { token, expiresAt, phone } = value as Record<string, unknown>

  if (typeof token !== 'string' || token.length === 0) return null
  if (typeof phone !== 'string' || !E164_MOBILE.test(phone)) return null
  if (typeof expiresAt !== 'string') return null

  const expiry = Date.parse(expiresAt)
  if (Number.isNaN(expiry) || expiry <= now.getTime()) return null

  return { token, expiresAt, phone }
}

/**
 * Session storage, not local: the proof should last as long as the tab — long enough to reload
 * the confirmation page or look up an order — and no longer, since anyone holding it can read
 * that phone's orders and addresses until it expires.
 */
export function loadVerifiedPhone(now: Date = new Date()): VerifiedPhone | null {
  // Storage throws in private mode and returns nothing when site data is cleared.
  try {
    const raw = window.sessionStorage.getItem(STORAGE_KEY)
    if (!raw) return null
    return parseVerifiedPhone(JSON.parse(raw), now)
  } catch {
    return null
  }
}

export function saveVerifiedPhone(verified: VerifiedPhone | null): void {
  try {
    if (verified === null) {
      window.sessionStorage.removeItem(STORAGE_KEY)
      return
    }
    window.sessionStorage.setItem(STORAGE_KEY, JSON.stringify(verified))
  } catch {
    // A proof that cannot be persisted still works until the page is reloaded.
  }
}
