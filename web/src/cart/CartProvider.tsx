import { useEffect, useMemo, useReducer, type ReactNode } from 'react'
import type { CartLine, CartLineKey } from '../types/cart'
import { CartContext, type CartStore } from './CartContext'
import { cartCount, cartReducer, cartSubtotal } from './cartReducer'
import { loadCart, saveCart } from './cartStorage'

export function CartProvider({ children }: { children: ReactNode }) {
  // Read storage lazily so the first render already has the stored cart and the badge never flickers.
  const [lines, dispatch] = useReducer(cartReducer, undefined, loadCart)

  useEffect(() => {
    saveCart(lines)
  }, [lines])

  const store = useMemo<CartStore>(
    () => ({
      lines,
      count: cartCount(lines),
      subtotal: cartSubtotal(lines),
      addLine: (line: CartLine) => dispatch({ type: 'add', line }),
      setQuantity: (key: CartLineKey, quantity: number) =>
        dispatch({ type: 'setQuantity', key, quantity }),
      removeLine: (key: CartLineKey) => dispatch({ type: 'remove', key }),
      clear: () => dispatch({ type: 'clear' }),
    }),
    [lines],
  )

  return <CartContext.Provider value={store}>{children}</CartContext.Provider>
}
