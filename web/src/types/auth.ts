/** An admin session as the API handed it over, and as it is kept in storage. */
export type AdminSession = {
  token: string
  /** ISO-8601; the token is refused by the API from this moment on. */
  expiresAt: string
  email: string
  name: string
  role: string
}
