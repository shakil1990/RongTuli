/**
 * Bangladeshi mobile numbers. The API is the authority on what it accepts; these mirror its rule
 * only so the form can format as the customer types and hold back a send that is bound to fail.
 */

const NATIONAL_LENGTH = 11
const MOBILE = /^01[3-9]\d{8}$/

/** The national digits (`01712345678`) from whatever was typed or pasted, at most 11 of them. */
function nationalDigits(input: string): string {
  let digits = input.replace(/\D/g, '')
  // A pasted +880 / 880 number becomes the 0-prefixed form people write locally.
  if (digits.startsWith('880')) digits = `0${digits.slice(3)}`
  return digits.slice(0, NATIONAL_LENGTH)
}

/** Formats the phone field as it is typed: `01712-345678`. */
export function formatPhoneInput(input: string): string {
  const digits = nationalDigits(input)
  return digits.length > 5 ? `${digits.slice(0, 5)}-${digits.slice(5)}` : digits
}

/** E.164 (`+8801712345678`) for a complete mobile number, or null. */
export function toE164(input: string): string | null {
  const digits = nationalDigits(input)
  return MOBILE.test(digits) ? `+880${digits.slice(1)}` : null
}

/** The way a customer would recognise their own number: `01712-345678`. */
export function formatPhoneForDisplay(e164: string): string {
  return formatPhoneInput(e164)
}

/** Keeps only digits, at most six — what the code field accepts from typing or a pasted SMS. */
export function cleanCodeInput(input: string): string {
  return input.replace(/\D/g, '').slice(0, 6)
}

/** `m:ss` for a countdown. */
export function formatCountdown(seconds: number): string {
  const whole = Math.max(0, Math.ceil(seconds))
  return `${Math.floor(whole / 60)}:${String(whole % 60).padStart(2, '0')}`
}
