import type { CartLine, CartLineKey } from '../types/cart'
import { MAX_CART_LINES, MAX_CART_QUANTITY } from '../types/cart'

export type CartAction =
  | { type: 'add'; line: CartLine }
  | { type: 'setQuantity'; key: CartLineKey; quantity: number }
  | { type: 'remove'; key: CartLineKey }
  | { type: 'clear' }

/** A line is identified by product and size, so one product in two sizes is two lines. */
export function isSameLine(line: CartLine, key: CartLineKey): boolean {
  return line.productId === key.productId && (line.size ?? null) === (key.size ?? null)
}

export function cartCount(lines: CartLine[]): number {
  return lines.reduce((total, line) => total + line.quantity, 0)
}

export function cartSubtotal(lines: CartLine[]): number {
  return lines.reduce((total, line) => total + line.unitPrice * line.quantity, 0)
}

const clampQuantity = (quantity: number) =>
  Math.min(MAX_CART_QUANTITY, Math.max(1, Math.trunc(quantity)))

export function cartReducer(lines: CartLine[], action: CartAction): CartLine[] {
  switch (action.type) {
    case 'add': {
      const incoming = action.line
      const existing = lines.find((line) => isSameLine(line, incoming))
      if (existing) {
        // Name, price and cover come from the storefront, which is fresher than the stored line.
        return lines.map((line) =>
          line === existing
            ? { ...incoming, quantity: clampQuantity(line.quantity + incoming.quantity) }
            : line,
        )
      }

      // The cap keeps the quote request bounded; it is far above a realistic cart.
      if (lines.length >= MAX_CART_LINES) {
        return lines
      }

      return [...lines, { ...incoming, quantity: clampQuantity(incoming.quantity) }]
    }

    case 'setQuantity':
      return lines.map((line) =>
        isSameLine(line, action.key) ? { ...line, quantity: clampQuantity(action.quantity) } : line,
      )

    case 'remove':
      return lines.filter((line) => !isSameLine(line, action.key))

    case 'clear':
      return []
  }
}
