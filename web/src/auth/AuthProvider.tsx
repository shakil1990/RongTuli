import { googleLogout } from '@react-oauth/google'
import { useCallback, useMemo, useState, type ReactNode } from 'react'
import { ApiError } from '../lib/api'
import { exchangeGoogleToken } from '../services/auth'
import type { AdminSession } from '../types/auth'
import { AuthContext, type AuthStore } from './AuthContext'
import { loadSession, saveSession } from './authStorage'

const SIGN_IN_FAILED = 'Sign-in failed. Check your connection and try again.'

export function AuthProvider({ children }: { children: ReactNode }) {
  // Read storage lazily so a reload lands straight on the admin panel instead of the sign-in card.
  const [admin, setAdmin] = useState<AdminSession | null>(loadSession)
  const [isSigningIn, setIsSigningIn] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const remember = useCallback((session: AdminSession | null) => {
    setAdmin(session)
    saveSession(session)
  }, [])

  const signIn = useCallback(
    async (googleIdToken: string) => {
      setIsSigningIn(true)
      setError(null)
      try {
        remember(await exchangeGoogleToken(googleIdToken))
      } catch (caught) {
        // 401 and 403 both carry a sentence written for the person; anything else is a fault on
        // our side or the network's, and saying which status it was would not help them.
        const isExpected = caught instanceof ApiError && (caught.status === 401 || caught.status === 403)
        setError(isExpected ? caught.message : SIGN_IN_FAILED)
        remember(null)
      } finally {
        setIsSigningIn(false)
      }
    },
    [remember],
  )

  const signOut = useCallback(() => {
    // Clears Google's own session state too, so the next sign-in asks which account to use.
    googleLogout()
    setError(null)
    remember(null)
  }, [remember])

  const store = useMemo<AuthStore>(
    () => ({ admin, signIn, signOut, isSigningIn, error }),
    [admin, signIn, signOut, isSigningIn, error],
  )

  return <AuthContext.Provider value={store}>{children}</AuthContext.Provider>
}
