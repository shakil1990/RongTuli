import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import type { VerifiedPhone } from '../types/phone'
import { VerifiedPhoneContext, type VerifiedPhoneStore } from './VerifiedPhoneContext'
import { loadVerifiedPhone, saveVerifiedPhone } from './verifiedPhoneStorage'

export function VerifiedPhoneProvider({ children }: { children: ReactNode }) {
  const [verified, setVerified] = useState<VerifiedPhone | null>(loadVerifiedPhone)

  const remember = useCallback((next: VerifiedPhone) => {
    setVerified(next)
    saveVerifiedPhone(next)
  }, [])

  const forget = useCallback(() => {
    setVerified(null)
    saveVerifiedPhone(null)
  }, [])

  // Dropped the moment it expires, so the page asks for a new code before the API has to refuse one.
  useEffect(() => {
    if (!verified) return
    const timer = window.setTimeout(forget, Math.max(0, Date.parse(verified.expiresAt) - Date.now()))
    return () => window.clearTimeout(timer)
  }, [verified, forget])

  const store = useMemo<VerifiedPhoneStore>(
    () => ({ verified, remember, forget }),
    [verified, remember, forget],
  )

  return <VerifiedPhoneContext.Provider value={store}>{children}</VerifiedPhoneContext.Provider>
}
