import { useContext } from 'react'
import { VerifiedPhoneContext, type VerifiedPhoneStore } from './VerifiedPhoneContext'

export function useVerifiedPhone(): VerifiedPhoneStore {
  const store = useContext(VerifiedPhoneContext)
  if (!store) {
    throw new Error('useVerifiedPhone must be used inside a VerifiedPhoneProvider.')
  }
  return store
}
