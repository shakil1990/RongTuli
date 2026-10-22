/**
 * The Google OAuth web client the sign-in button identifies this app as. It must name the same
 * client as the API's `Google__ClientId`, or Google issues a token the API refuses.
 *
 * Empty when the deployment has no Google client configured: the storefront is unaffected, and
 * only the admin sign-in says so. It is not a secret — it ships in the bundle either way.
 */
export const googleClientId: string = import.meta.env.VITE_GOOGLE_CLIENT_ID ?? ''
