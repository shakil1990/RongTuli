import { apiPost } from '../lib/api'
import type { AdminSession } from '../types/auth'

export function exchangeGoogleToken(
  googleIdToken: string,
  signal?: AbortSignal,
): Promise<AdminSession> {
  return apiPost<AdminSession>('/api/auth/google', { idToken: googleIdToken }, signal)
}
