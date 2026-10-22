import { describe, expect, it } from 'vitest'
import {
  cleanCodeInput,
  formatCountdown,
  formatPhoneForDisplay,
  formatPhoneInput,
  toE164,
} from './phone'

describe('formatPhoneInput', () => {
  it('leaves a short prefix alone', () => {
    expect(formatPhoneInput('0171')).toBe('0171')
  })

  it('splits after the operator block once there is more', () => {
    expect(formatPhoneInput('01712345678')).toBe('01712-345678')
  })

  it('drops anything that is not a digit', () => {
    expect(formatPhoneInput('017 12-34a5678')).toBe('01712-345678')
  })

  it('stops at eleven digits', () => {
    expect(formatPhoneInput('0171234567899')).toBe('01712-345678')
  })

  it('turns a pasted international number into the local form', () => {
    expect(formatPhoneInput('+880 1712 345678')).toBe('01712-345678')
  })
})

describe('toE164', () => {
  it.each(['01712345678', '01712-345678', '+8801712345678', '8801712345678'])(
    'reads %s as the same number',
    (input) => {
      expect(toE164(input)).toBe('+8801712345678')
    },
  )

  it.each(['', '0171234567', '01212345678', '02912345678', 'phone'])('rejects %s', (input) => {
    expect(toE164(input)).toBeNull()
  })
})

describe('formatPhoneForDisplay', () => {
  it('shows a stored number the way it was typed', () => {
    expect(formatPhoneForDisplay('+8801712345678')).toBe('01712-345678')
  })
})

describe('cleanCodeInput', () => {
  it('keeps six digits out of a pasted message', () => {
    expect(cleanCodeInput(' 123 456 ')).toBe('123456')
  })

  it('stops at six digits', () => {
    expect(cleanCodeInput('12345678')).toBe('123456')
  })
})

describe('formatCountdown', () => {
  it.each([
    [0, '0:00'],
    [9, '0:09'],
    [60, '1:00'],
    [299.2, '5:00'],
    [-3, '0:00'],
  ])('shows %s seconds as %s', (seconds, expected) => {
    expect(formatCountdown(seconds)).toBe(expected)
  })
})
