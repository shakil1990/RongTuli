import { apiPost } from '../lib/api'
import type { CodeSent, VerifiedPhone } from '../types/phone'

export function sendPhoneCode(phone: string, signal?: AbortSignal): Promise<CodeSent> {
  return apiPost<CodeSent>('/api/phone/otp', { phone }, signal)
}

export function verifyPhoneCode(
  phone: string,
  code: string,
  signal?: AbortSignal,
): Promise<VerifiedPhone> {
  return apiPost<VerifiedPhone>('/api/phone/verify', { phone, code }, signal)
}
