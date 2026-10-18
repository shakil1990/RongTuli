import type { CartLine } from '../types/cart'
import { MAX_CART_LINES, MAX_CART_QUANTITY } from '../types/cart'

/** Versioned so a later shape change can be told apart from this one. */
export const STORAGE_KEY = 'rongtuli.cart.v1'

/**
 * Stored carts are untrusted: a viewer can edit localStorage and an older build may have
 * written a different shape, so every line is validated before it reaches the UI. Prices
 * are re-checked by `POST /api/cart/quote` regardless.
 */
function parseLine(value: unknown): CartLine | null {
  if (typeof value !== 'object' || value === null) return null
  const line = value as Record<string, unknown>

  const { productId, slug, name, size, quantity, unitPrice, coverImageUrl } = line
  if (!Number.isInteger(productId) || (productId as number) < 1) return null
  if (typeof slug !== 'string' || slug.length === 0) return null
  if (typeof name !== 'string' || name.length === 0) return null
  if (!Number.isInteger(quantity)) return null
  if (typeof unitPrice !== 'number' || !Number.isFinite(unitPrice) || unitPrice < 0) return null

  return {
    productId: productId as number,
    slug,
    name,
    size: typeof size === 'string' && size.length > 0 ? size : null,
    quantity: Math.min(MAX_CART_QUANTITY, Math.max(1, quantity as number)),
    unitPrice,
    coverImageUrl: typeof coverImageUrl === 'string' && coverImageUrl.length > 0 ? coverImageUrl : null,
  }
}

export function loadCart(): CartLine[] {
  // Storage throws in private mode and returns nothing when site data is cleared.
  try {
    const raw = window.localStorage.getItem(STORAGE_KEY)
    if (!raw) return []

    const parsed: unknown = JSON.parse(raw)
    if (!Array.isArray(parsed)) return []

    return parsed
      .map(parseLine)
      .filter((line): line is CartLine => line !== null)
      .slice(0, MAX_CART_LINES)
  } catch {
    return []
  }
}

export function saveCart(lines: CartLine[]): void {
  try {
    window.localStorage.setItem(STORAGE_KEY, JSON.stringify(lines))
  } catch {
    // A cart that cannot be persisted still works for this visit.
  }
}
