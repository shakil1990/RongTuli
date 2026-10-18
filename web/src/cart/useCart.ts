import { useContext } from 'react'
import { CartContext, type CartStore } from './CartContext'

export function useCart(): CartStore {
  const store = useContext(CartContext)
  if (!store) {
    throw new Error('useCart must be used inside a CartProvider.')
  }
  return store
}
