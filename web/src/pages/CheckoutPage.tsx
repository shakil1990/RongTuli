import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useCart } from '../cart/useCart'
import { PhoneVerification } from '../components/PhoneVerification'
import { EmptyState } from '../components/common/EmptyState'
import { buttonClasses, cardSurface } from '../components/common/styles'
import { formatPhoneForDisplay } from '../lib/phone'
import type { VerifiedPhone } from '../types/phone'

/**
 * Checkout without an account: the customer proves their phone first, and the order is tied to
 * that number. The delivery details and order confirmation arrive with Step 9.
 */
export function CheckoutPage() {
  const { lines } = useCart()
  const [verified, setVerified] = useState<VerifiedPhone | null>(null)

  if (lines.length === 0) {
    return (
      <EmptyState
        title="Nothing to check out yet"
        description="Add a piece to your cart and come back here to place the order."
        action={
          <Link to="/" className={buttonClasses('primary', 'md', 'mt-2')}>
            Start shopping
          </Link>
        }
      />
    )
  }

  return (
    <div className="mx-auto max-w-xl space-y-6">
      <h1 className="text-3xl">Checkout</h1>

      <section aria-labelledby="checkout-phone-heading" className={`${cardSurface} space-y-4 p-5 sm:p-6`}>
        <h2 id="checkout-phone-heading" className="text-xl">
          1. Your phone
        </h2>
        {verified ? (
          <p className="text-sm" role="status">
            <span aria-hidden="true" className="text-accent">
              ✓
            </span>{' '}
            Verified <strong>{formatPhoneForDisplay(verified.phone)}</strong>.{' '}
            <button
              type="button"
              onClick={() => setVerified(null)}
              className="text-accent underline underline-offset-2 hover:text-accent-hover"
            >
              Use a different number
            </button>
          </p>
        ) : (
          <PhoneVerification onVerified={setVerified} />
        )}
      </section>

      <section aria-labelledby="checkout-delivery-heading" className={`${cardSurface} space-y-2 p-5 sm:p-6`}>
        <h2 id="checkout-delivery-heading" className="text-xl">
          2. Delivery
        </h2>
        <p className="text-sm text-muted">
          Online ordering is almost ready. Your name, address and order confirmation come next.
        </p>
      </section>

      <Link to="/cart" className={buttonClasses('ghost', 'sm')}>
        ← Back to cart
      </Link>
    </div>
  )
}
