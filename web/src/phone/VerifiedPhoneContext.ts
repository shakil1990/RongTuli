import { createContext } from 'react'
import type { VerifiedPhone } from '../types/phone'

export type VerifiedPhoneStore = {
  /** The phone this tab has proved by text code, or null once it expires or is dropped. */
  verified: VerifiedPhone | null
  remember: (verified: VerifiedPhone) => void
  /** Drops the proof: on "use a different number", and when the API answers 401. */
  forget: () => void
}

/**
 * Shared by checkout, the order confirmation and the order lookup, so a customer verifies once
 * and is not asked again on the next page.
 */
export const VerifiedPhoneContext = createContext<VerifiedPhoneStore | null>(null)
