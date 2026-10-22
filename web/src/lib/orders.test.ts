import { describe, expect, it } from 'vitest'
import type { CartQuote, CartQuoteLine } from '../types/cart'
import {
  deliveryErrorsFromApi,
  deliveryFeeFor,
  formatOrderDate,
  formatOrderNumber,
  orderLinesFromQuote,
  validateDeliveryForm,
} from './orders'

const valid = {
  customerName: 'Rahim Uddin',
  address: 'House 12, Road 5, Dhanmondi, Dhaka',
  note: '',
  deliveryZone: 'InsideDhaka' as const,
}

function line(overrides: Partial<CartQuoteLine> = {}): CartQuoteLine {
  return {
    productId: 1,
    name: 'Shirt',
    slug: 'shirt',
    coverImageUrl: null,
    size: 'M',
    quantity: 2,
    unitPrice: 450,
    lineTotal: 900,
    isOrderable: true,
    issues: [],
    ...overrides,
  }
}

function quote(items: CartQuoteLine[]): CartQuote {
  return { items, subtotal: 0, itemCount: 0, hasIssues: false }
}

describe('validateDeliveryForm', () => {
  it('accepts a name and a full address with no note', () => {
    expect(validateDeliveryForm(valid)).toEqual({})
  })

  it('asks for a name that is only spaces', () => {
    expect(validateDeliveryForm({ ...valid, customerName: '   ' })).toHaveProperty('customerName')
  })

  it('asks for more than a word of address', () => {
    expect(validateDeliveryForm({ ...valid, address: 'Dhaka' })).toHaveProperty('address')
  })

  it('refuses a note past the limit', () => {
    expect(validateDeliveryForm({ ...valid, note: 'x'.repeat(1001) })).toHaveProperty('note')
  })
})

describe('deliveryErrorsFromApi', () => {
  it('keeps the first message for each form field and drops the rest', () => {
    expect(
      deliveryErrorsFromApi({
        customerName: ['Name first', 'Name second'],
        address: ['Address'],
        deliveryZone: ['Zone'],
        'items[].quantity': ['Not a form field'],
      }),
    ).toEqual({ customerName: 'Name first', address: 'Address', deliveryZone: 'Zone' })
  })
})

describe('orderLinesFromQuote', () => {
  it('prices each line as the quote showed it, not as the cart stored it', () => {
    const lines = orderLinesFromQuote(quote([line({ unitPrice: 500, issues: ['price_changed'] })]), 1)

    expect(lines).toEqual([{ productId: 1, size: 'M', quantity: 2, unitPrice: 500 }])
  })

  it('has nothing to order before the quote arrives', () => {
    expect(orderLinesFromQuote(undefined, 1)).toBeNull()
  })

  it('has nothing to order when the quote is for a different cart', () => {
    expect(orderLinesFromQuote(quote([line()]), 2)).toBeNull()
  })

  it('has nothing to order while a line cannot be ordered', () => {
    const items = [line(), line({ productId: 2, isOrderable: false, issues: ['unavailable'] })]

    expect(orderLinesFromQuote(quote(items), 2)).toBeNull()
  })
})

describe('formatOrderNumber', () => {
  it('writes the id the way it is read out on a call', () => {
    expect(formatOrderNumber(42)).toBe('#42')
  })
})

describe('formatOrderDate', () => {
  it('shows Bangladesh time for a UTC timestamp', () => {
    expect(formatOrderDate('2026-10-22T18:30:00Z')).toBe('23 Oct 2026, 12:30 am')
  })

  it('reads a timestamp without a zone as UTC', () => {
    expect(formatOrderDate('2026-10-22T18:30:00')).toBe(formatOrderDate('2026-10-22T18:30:00Z'))
  })
})

describe('deliveryFeeFor', () => {
  const options = {
    deliveryOptions: [
      { zone: 'InsideDhaka' as const, fee: 80 },
      { zone: 'OutsideDhaka' as const, fee: 150 },
    ],
  }

  it('reads the fee the API quoted for each zone', () => {
    expect(deliveryFeeFor(options, 'InsideDhaka')).toBe(80)
    expect(deliveryFeeFor(options, 'OutsideDhaka')).toBe(150)
  })

  it('has no fee until the options have loaded', () => {
    expect(deliveryFeeFor(undefined, 'InsideDhaka')).toBeUndefined()
  })
})
