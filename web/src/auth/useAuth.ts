import { useContext } from 'react'
import { AuthContext, type AuthStore } from './AuthContext'

export function useAuth(): AuthStore {
  const store = useContext(AuthContext)
  if (!store) {
    throw new Error('useAuth must be used inside an AuthProvider.')
  }
  return store
}
