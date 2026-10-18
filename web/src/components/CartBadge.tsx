import { Link } from 'react-router-dom'
import { useCart } from '../cart/useCart'
import { focusRing } from './common/styles'

export function CartBadge() {
  const { count } = useCart()

  return (
    <Link
      to="/cart"
      className={`${focusRing} relative flex items-center gap-2 rounded-full px-3 py-2 text-sm
        font-medium transition hover:bg-sunken`}
      aria-label={count === 0 ? 'Cart, empty' : `Cart, ${count} ${count === 1 ? 'item' : 'items'}`}
    >
      <svg
        aria-hidden="true"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.6"
        strokeLinecap="round"
        strokeLinejoin="round"
        className="h-5 w-5"
      >
        <path d="M6 8h12l-1 11a2 2 0 0 1-2 1.8H9A2 2 0 0 1 7 19L6 8Z" />
        <path d="M9.5 8V6.5a2.5 2.5 0 0 1 5 0V8" />
      </svg>
      <span className="hidden sm:inline">Cart</span>
      {count > 0 && (
        <span
          aria-hidden="true"
          className="absolute -top-0.5 right-0.5 flex h-5 min-w-5 items-center justify-center
            rounded-full bg-accent px-1 text-xs font-semibold text-accent-ink tabular-nums"
        >
          {count}
        </span>
      )}
    </Link>
  )
}
