import { useCallback } from 'react'
import { ApiError } from '../lib/api'
import { useVerifiedPhone } from './useVerifiedPhone'

export type CustomerCall = <T>(call: (token: string) => Promise<T>) => Promise<T>

/**
 * Runs a request with the verified-phone token. A 401 means the proof expired, so it is dropped
 * and the page shows the phone panel again rather than an error the customer cannot fix.
 *
 * Only for components that render once a phone is verified.
 */
export function useCustomerCall(): CustomerCall {
  const { verified, forget } = useVerifiedPhone()
  const token = verified?.token ?? ''

  return useCallback(
    async <T,>(call: (token: string) => Promise<T>): Promise<T> => {
      try {
        return await call(token)
      } catch (caught) {
        if (caught instanceof ApiError && caught.status === 401) {
          forget()
        }
        throw caught
      }
    },
    [token, forget],
  )
}
