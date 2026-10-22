/** A code is on its way to `phone` (E.164, as the API stored it). */
export type CodeSent = {
  phone: string
  expiresInSeconds: number
  resendAfterSeconds: number
}

/** Proof the customer holds `phone`; checkout sends the token as a bearer header. */
export type VerifiedPhone = {
  token: string
  /** ISO-8601; the API refuses the token from this moment on. */
  expiresAt: string
  phone: string
}
