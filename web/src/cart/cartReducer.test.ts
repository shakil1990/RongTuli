import { describe, expect, it } from 'vitest'
import { MAX_CART_LINES, MAX_CART_QUANTITY, type CartLine } from '../types/cart'
import { cartCount, cartReducer, cartSubtotal } from './cartReducer'

const line = (overrides: Partial<CartLine> = {}): CartLine => ({
  productId: 1,
  slug: 'blue-shirt',
  name: 'Blue Shirt',
  size: 'M',
  quantity: 1,
  unitPrice: 450,
  coverImageUrl: null,
  ...overrides,
})

describe('cartReducer', () => {
  it('adds a new line', () => {
    const result = cartReducer([], { type: 'add', line: line() })

    expect(result).toHaveLength(1)
    expect(result[0]).toMatchObject({ productId: 1, size: 'M', quantity: 1 })
  })

  it('merges a repeat of the same product and size by adding the quantities', () => {
    const existing = [line({ quantity: 2 })]

    const result = cartReducer(existing, { type: 'add', line: line({ quantity: 3 }) })

    expect(result).toHaveLength(1)
    expect(result[0]!.quantity).toBe(5)
  })

  it('keeps the same product in a different size as its own line', () => {
    const existing = [line({ size: 'M' })]

    const result = cartReducer(existing, { type: 'add', line: line({ size: 'L' }) })

    expect(result.map((entry) => entry.size)).toEqual(['M', 'L'])
  })

  it('refreshes name and price from the newly added line', () => {
    const existing = [line({ name: 'Old name', unitPrice: 400 })]

    const result = cartReducer(existing, { type: 'add', line: line({ name: 'New name', unitPrice: 500 }) })

    expect(result[0]).toMatchObject({ name: 'New name', unitPrice: 500 })
  })

  it('caps the quantity when merging would exceed the maximum', () => {
    const existing = [line({ quantity: MAX_CART_QUANTITY })]

    const result = cartReducer(existing, { type: 'add', line: line({ quantity: 5 }) })

    expect(result[0]!.quantity).toBe(MAX_CART_QUANTITY)
  })

  it('refuses a new line once the line cap is reached', () => {
    const full = Array.from({ length: MAX_CART_LINES }, (_, index) =>
      line({ productId: index + 1 }),
    )

    const result = cartReducer(full, { type: 'add', line: line({ productId: 999 }) })

    expect(result).toHaveLength(MAX_CART_LINES)
  })

  it('sets a quantity and clamps it into range', () => {
    const existing = [line({ quantity: 4 })]
    const key = { productId: 1, size: 'M' }

    expect(cartReducer(existing, { type: 'setQuantity', key, quantity: 7 })[0]!.quantity).toBe(7)
    expect(cartReducer(existing, { type: 'setQuantity', key, quantity: 0 })[0]!.quantity).toBe(1)
    expect(cartReducer(existing, { type: 'setQuantity', key, quantity: 500 })[0]!.quantity).toBe(
      MAX_CART_QUANTITY,
    )
  })

  it('removes only the matching product and size', () => {
    const existing = [line({ size: 'M' }), line({ size: 'L' })]

    const result = cartReducer(existing, { type: 'remove', key: { productId: 1, size: 'M' } })

    expect(result.map((entry) => entry.size)).toEqual(['L'])
  })

  it('treats a sizeless line as its own key', () => {
    const existing = [line({ size: null })]

    const result = cartReducer(existing, { type: 'remove', key: { productId: 1, size: null } })

    expect(result).toEqual([])
  })

  it('clears every line', () => {
    expect(cartReducer([line(), line({ productId: 2 })], { type: 'clear' })).toEqual([])
  })
})

describe('cart totals', () => {
  it('counts every unit, not every line', () => {
    expect(cartCount([line({ quantity: 2 }), line({ productId: 2, quantity: 3 })])).toBe(5)
  })

  it('totals quantity times unit price', () => {
    expect(cartSubtotal([line({ quantity: 2, unitPrice: 450 }), line({ productId: 2, unitPrice: 200 })])).toBe(
      1100,
    )
  })

  it('reports zero for an empty cart', () => {
    expect(cartCount([])).toBe(0)
    expect(cartSubtotal([])).toBe(0)
  })
})
