import { GoogleLogin } from '@react-oauth/google'
import { useState } from 'react'
import { googleClientId } from '../auth/googleClient'
import { useAuth } from '../auth/useAuth'
import { cardSurface } from './common/styles'

const GOOGLE_FAILED = 'Google could not complete the sign-in. Try again.'

/**
 * The only place sign-in is offered. The storefront never advertises it: customers have no
 * accounts, so an admin reaches this by going to /admin.
 */
export function AdminSignIn() {
  const { signIn, isSigningIn, error } = useAuth()
  const [googleError, setGoogleError] = useState<string | null>(null)

  const problem = googleError ?? error

  return (
    <div className={`${cardSurface} mx-auto max-w-md p-8 text-center`}>
      <h1 className="font-display text-2xl font-semibold">Admin sign-in</h1>
      <p className="mt-2 text-sm text-muted">Only the RongTuli admin accounts can sign in here.</p>

      <div className="mt-6 flex justify-center">
        {googleClientId ? (
          <GoogleLogin
            onSuccess={(credential) => {
              setGoogleError(credential.credential ? null : GOOGLE_FAILED)
              if (credential.credential) {
                void signIn(credential.credential)
              }
            }}
            onError={() => setGoogleError(GOOGLE_FAILED)}
            text="signin_with"
            shape="pill"
          />
        ) : (
          <p className="text-sm text-muted">
            Sign-in is not configured for this deployment: VITE_GOOGLE_CLIENT_ID is not set.
          </p>
        )}
      </div>

      {isSigningIn && <p className="mt-4 text-sm text-muted">Signing you in…</p>}

      {problem && !isSigningIn && (
        <p role="alert" className="mt-4 rounded-xl bg-warning-soft px-4 py-3 text-sm text-warning">
          {problem}
        </p>
      )}
    </div>
  )
}
