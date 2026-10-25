import { describe, expect, it } from 'vitest'
import { parseVerifiedPhone } from './verifiedPhoneStorage'

const now = new Date('2026-10-22T12:00:00Z')
const stored = { token: 'signed.token.value', expiresAt: '2026-10-22T12:30:00Z', phone: '+8801712345678' }

describe('parseVerifiedPhone', () => {
  it('keeps a proof that has not expired', () => {
    expect(parseVerifiedPhone(stored, now)).toEqual(stored)
  })

  it('drops a proof that has expired', () => {
    expect(parseVerifiedPhone({ ...stored, expiresAt: '2026-10-22T11:59:59Z' }, now)).toBeNull()
  })

  it.each([
    ['no token', { ...stored, token: '' }],
    ['a number that is not a Bangladeshi mobile', { ...stored, phone: '01712345678' }],
    ['an unreadable expiry', { ...stored, expiresAt: 'soon' }],
    ['not an object', 'signed.token.value'],
    ['nothing', null],
  ])('drops an entry with %s', (_, value) => {
    expect(parseVerifiedPhone(value, now)).toBeNull()
  })

  it('carries over only the fields it knows', () => {
    expect(parseVerifiedPhone({ ...stored, role: 'Admin' }, now)).toEqual(stored)
  })
})
