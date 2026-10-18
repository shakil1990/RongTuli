/**
 * The cart lives in the browser (localStorage) and is re-priced by `POST /api/cart/quote`.
 * `unitPrice` is the price last seen in the storefront, kept only so the quote can flag a change.
 */
export type CartLine = {
  productId: number
  slug: string
  name: string
  size: string | null
  quantity: number
  unitPrice: number
  coverImageUrl: string | null
}

/** Identifies a line within the cart: the same product in two sizes is two lines. */
export type CartLineKey = {
  productId: number
  size: string | null
}

export type CartQuoteIssue = 'unavailable' | 'price_changed' | 'size_unavailable'

export type CartQuoteLine = {
  productId: number
  name: string | null
  slug: string | null
  coverImageUrl: string | null
  size: string | null
  quantity: number
  unitPrice: number
  lineTotal: number
  isOrderable: boolean
  issues: CartQuoteIssue[]
}

export type CartQuote = {
  items: CartQuoteLine[]
  subtotal: number
  itemCount: number
  hasIssues: boolean
}

export const CART_ISSUE_MESSAGES: Record<CartQuoteIssue, string> = {
  unavailable: 'No longer available — remove it to carry on.',
  price_changed: 'The price changed; the new price is shown.',
  size_unavailable: 'That size is no longer offered — pick another.',
}

export const MAX_CART_QUANTITY = 99
export const MAX_CART_LINES = 50
