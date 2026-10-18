import { createContext } from 'react'
import type { CartLine, CartLineKey } from '../types/cart'

export type CartStore = {
  lines: CartLine[]
  /** Total quantity across every line — what the header badge shows. */
  count: number
  /** Local estimate from the stored prices; `POST /api/cart/quote` is authoritative. */
  subtotal: number
  addLine: (line: CartLine) => void
  setQuantity: (key: CartLineKey, quantity: number) => void
  removeLine: (key: CartLineKey) => void
  clear: () => void
}

/**
 * Deliberately a context rather than a third-party store: the cart has three consumers
 * (header badge, product page, cart page) and needs no dependency. `useCart` is the only
 * way components reach it, so swapping the implementation later touches one file.
 */
export const CartContext = createContext<CartStore | null>(null)
