import { describe, expect, it } from 'vitest'
import { formatTaka, initialsOf } from './format'

describe('formatTaka', () => {
  it('shows whole taka without decimals', () => {
    expect(formatTaka(450)).toBe('৳450')
  })

  it('groups thousands', () => {
    expect(formatTaka(12500)).toBe('৳12,500')
  })

  it('keeps two decimals when a price has them', () => {
    expect(formatTaka(99.5)).toBe('৳99.50')
  })

  it('formats zero', () => {
    expect(formatTaka(0)).toBe('৳0')
  })
})

describe('initialsOf', () => {
  it('takes the first letter of the first two words', () => {
    expect(initialsOf('Blue Cotton Shirt')).toBe('BC')
  })

  it('handles a single word', () => {
    expect(initialsOf('Cap')).toBe('C')
  })

  it('ignores extra spacing', () => {
    expect(initialsOf('  Red   Tee ')).toBe('RT')
  })

  it('returns nothing for a blank name', () => {
    expect(initialsOf('   ')).toBe('')
  })
})
