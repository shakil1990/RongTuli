import { apiPost } from '../lib/api'
import type { CartLine, CartQuote } from '../types/cart'

export const cartKeys = {
  /** Keyed on the lines themselves so editing the cart re-quotes it. */
  quote: (lines: CartLine[]) =>
    [
      'cart-quote',
      lines.map((line) => [line.productId, line.size, line.quantity, line.unitPrice]),
    ] as const,
}

export function requestCartQuote(lines: CartLine[], signal?: AbortSignal): Promise<CartQuote> {
  return apiPost<CartQuote>(
    '/api/cart/quote',
    {
      items: lines.map((line) => ({
        productId: line.productId,
        size: line.size,
        quantity: line.quantity,
        unitPrice: line.unitPrice,
      })),
    },
    signal,
  )
}
