import { useCallback } from 'react'
import { ApiError } from '../lib/api'
import { useAuth } from './useAuth'

export type AdminCall = <T>(call: (token: string) => Promise<T>) => Promise<T>

/**
 * Runs an admin request with the signed-in admin's token. A 401 means the token has expired or
 * was revoked, so the session is dropped and `RequireAdmin` shows the sign-in card again, rather
 * than the panel filling up with failures the admin cannot fix from where they are.
 *
 * Only for components inside `RequireAdmin`, which guarantees there is a session to use.
 */
export function useAdminCall(): AdminCall {
  const { admin, signOut } = useAuth()
  const token = admin?.token ?? ''

  return useCallback(
    async <T,>(call: (token: string) => Promise<T>): Promise<T> => {
      try {
        return await call(token)
      } catch (caught) {
        if (caught instanceof ApiError && caught.status === 401) {
          signOut()
        }
        throw caught
      }
    },
    [token, signOut],
  )
}
