/**
 * Shared Tailwind class recipes. They live in a plain module (not a component file) so
 * links and buttons can wear the same clothes without one importing the other.
 */

export type ButtonVariant = 'primary' | 'secondary' | 'ghost'
export type ButtonSize = 'sm' | 'md' | 'lg'

/** Keyboard focus must stay visible on every interactive element (WCAG 2.4.7). */
export const focusRing =
  'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-surface'

const BUTTON_BASE = `inline-flex items-center justify-center gap-2 rounded-full font-medium transition
  disabled:cursor-not-allowed disabled:opacity-50 ${focusRing}`

const BUTTON_VARIANTS: Record<ButtonVariant, string> = {
  primary: 'bg-accent text-accent-ink hover:bg-accent-hover',
  secondary: 'border border-line bg-raised text-ink hover:border-accent hover:text-accent',
  ghost: 'text-muted hover:bg-sunken hover:text-ink',
}

const BUTTON_SIZES: Record<ButtonSize, string> = {
  sm: 'px-3 py-1.5 text-sm',
  md: 'px-5 py-2.5 text-sm',
  lg: 'px-7 py-3 text-base',
}

export function buttonClasses(
  variant: ButtonVariant = 'primary',
  size: ButtonSize = 'md',
  extra = '',
): string {
  return `${BUTTON_BASE} ${BUTTON_VARIANTS[variant]} ${BUTTON_SIZES[size]} ${extra}`.trim()
}

/** A raised panel: product cards, cart lines, the order summary. */
export const cardSurface = 'rounded-2xl border border-line bg-raised'
