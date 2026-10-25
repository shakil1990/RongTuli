import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useCart } from '../cart/useCart'
import { PhoneVerification } from '../components/PhoneVerification'
import { Button } from '../components/common/Button'
import { EmptyState } from '../components/common/EmptyState'
import { Field } from '../components/common/Field'
import { describedBy } from '../components/common/describedBy'
import { alertBanner, buttonClasses, cardSurface, fieldControl } from '../components/common/styles'
import { ApiError } from '../lib/api'
import { formatTaka } from '../lib/format'
import {
  DEFAULT_DELIVERY_ZONE,
  DELIVERY_ZONE_LABELS,
  MAX_ADDRESS_LENGTH,
  MAX_NAME_LENGTH,
  MAX_NOTE_LENGTH,
  deliveryErrorsFromApi,
  deliveryFeeFor,
  orderLinesFromQuote,
  validateDeliveryForm,
  type DeliveryFormErrors,
  type DeliveryFormField,
  type DeliveryFormValues,
} from '../lib/orders'
import { formatPhoneForDisplay } from '../lib/phone'
import { useVerifiedPhone } from '../phone/useVerifiedPhone'
import { cartKeys, requestCartQuote } from '../services/cart'
import { fetchCheckoutOptions, orderKeys, placeOrder } from '../services/orders'
import { CART_ISSUE_MESSAGES } from '../types/cart'
import { DELIVERY_ZONES } from '../types/orders'

const OFFLINE = 'We could not reach the shop. Check your connection and try again.'
const VERIFICATION_EXPIRED =
  'Your phone verification ran out. Verify the number again, then place the order — your details are kept.'

/** Navigation state the confirmation page reads to say thank you rather than just show the order. */
export type PlacedOrderState = { placed: true }

/**
 * Checkout without an account: the customer proves their phone, says where to deliver, and
 * confirms the total they see. Everything stays on this page, so nothing typed is lost on the way.
 */
export function CheckoutPage() {
  const { lines, clear } = useCart()
  const { verified, remember, forget } = useVerifiedPhone()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [values, setValues] = useState<DeliveryFormValues>({
    customerName: '',
    address: '',
    note: '',
    deliveryZone: DEFAULT_DELIVERY_ZONE,
  })
  const [errors, setErrors] = useState<DeliveryFormErrors>({})
  const [problem, setProblem] = useState<string | null>(null)
  const [isPlacing, setIsPlacing] = useState(false)

  // The same key as the cart page, so arriving from the cart reuses the quote it just showed.
  const { data: quote, isError: isQuoteError } = useQuery({
    queryKey: cartKeys.quote(lines),
    queryFn: ({ signal }) => requestCartQuote(lines, signal),
    enabled: lines.length > 0,
  })

  const { data: options, isError: isOptionsError } = useQuery({
    queryKey: orderKeys.checkoutOptions,
    queryFn: ({ signal }) => fetchCheckoutOptions(signal),
    staleTime: 5 * 60 * 1000,
  })

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

  const pricedQuote = quote && quote.items.length === lines.length ? quote : undefined
  const orderLines = orderLinesFromQuote(pricedQuote, lines.length)
  const hasBlockedLine = pricedQuote?.items.some((item) => !item.isOrderable) ?? false
  const deliveryFee = deliveryFeeFor(options, values.deliveryZone)
  const total = pricedQuote && deliveryFee !== undefined ? pricedQuote.subtotal + deliveryFee : undefined
  const canPlace = verified !== null && orderLines !== null && total !== undefined && !isPlacing

  const update = (field: Exclude<DeliveryFormField, 'deliveryZone'>) => (value: string) => {
    setValues((current) => ({ ...current, [field]: value }))
    setErrors((current) => ({ ...current, [field]: undefined }))
  }

  const handlePlace = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setProblem(null)

    const formErrors = validateDeliveryForm(values)
    setErrors(formErrors)
    if (!verified) {
      setProblem('Verify your phone number first — step 1 above.')
      return
    }
    if (Object.keys(formErrors).length > 0 || !orderLines) return

    setIsPlacing(true)
    try {
      const order = await placeOrder(
        {
          customerName: values.customerName.trim(),
          phone: verified.phone,
          address: values.address.trim(),
          note: values.note.trim() || null,
          deliveryZone: values.deliveryZone,
          items: orderLines,
        },
        verified.token,
      )
      // The confirmation page opens on the order just returned, without asking for it again.
      queryClient.setQueryData(orderKeys.one(verified.phone, order.id), order)
      void queryClient.invalidateQueries({ queryKey: orderKeys.mine(verified.phone) })
      navigate(`/orders/${order.id}`, { state: { placed: true } satisfies PlacedOrderState })
      clear()
    } catch (caught) {
      if (!(caught instanceof ApiError)) {
        setProblem(OFFLINE)
      } else if (caught.status === 401 || caught.fieldErrors.phone) {
        forget()
        setProblem(VERIFICATION_EXPIRED)
      } else if (caught.status === 409) {
        // A price moved or a piece was withdrawn: show the new figures and let them confirm again.
        setProblem(caught.message)
        await queryClient.invalidateQueries({ queryKey: cartKeys.quote(lines) })
      } else {
        const fieldErrors = deliveryErrorsFromApi(caught.fieldErrors)
        setErrors(fieldErrors)
        setProblem(Object.keys(fieldErrors).length > 0 ? null : caught.message)
      }
    } finally {
      setIsPlacing(false)
    }
  }

  return (
    <div className="space-y-6">
      <h1 className="text-3xl">Checkout</h1>

      <div className="grid gap-6 lg:grid-cols-[1fr_22rem] lg:items-start">
        <div className="space-y-6">
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
                  onClick={forget}
                  className="text-accent underline underline-offset-2 hover:text-accent-hover"
                >
                  Use a different number
                </button>
              </p>
            ) : (
              <PhoneVerification onVerified={remember} />
            )}
          </section>

          <form
            id="checkout-form"
            onSubmit={(event) => void handlePlace(event)}
            noValidate
            aria-labelledby="checkout-delivery-heading"
            className={`${cardSurface} space-y-4 p-5 sm:p-6`}
          >
            <h2 id="checkout-delivery-heading" className="text-xl">
              2. Delivery
            </h2>

            <Field id="checkout-name" label="Name" error={errors.customerName}>
              <input
                id="checkout-name"
                name="customerName"
                autoComplete="name"
                value={values.customerName}
                onChange={(event) => update('customerName')(event.target.value)}
                maxLength={MAX_NAME_LENGTH}
                className={fieldControl}
                {...describedBy('checkout-name', errors.customerName, false)}
              />
            </Field>

            <Field
              id="checkout-address"
              label="Delivery address"
              hint="House, road, area and city, so the courier can find you."
              error={errors.address}
            >
              <textarea
                id="checkout-address"
                name="address"
                autoComplete="street-address"
                value={values.address}
                onChange={(event) => update('address')(event.target.value)}
                rows={3}
                maxLength={MAX_ADDRESS_LENGTH}
                className={fieldControl}
                {...describedBy('checkout-address', errors.address, true)}
              />
            </Field>

            <fieldset className="space-y-2" aria-describedby={errors.deliveryZone ? 'checkout-zone-error' : undefined}>
              <legend className="mb-1.5 block text-sm font-medium">Delivery area</legend>
              <div className="grid gap-2 sm:grid-cols-2">
                {DELIVERY_ZONES.map((zone) => {
                  const fee = deliveryFeeFor(options, zone)
                  return (
                    <label
                      key={zone}
                      className={`flex cursor-pointer items-center justify-between gap-3 rounded-xl border px-3 py-2.5
                        text-sm transition has-[:checked]:bg-accent-soft
                        has-[:focus-visible]:ring-2 has-[:focus-visible]:ring-accent ${
                          values.deliveryZone === zone ? 'border-accent' : 'border-line hover:border-accent'
                        }`}
                    >
                      <span className="flex items-center gap-2.5">
                        <input
                          type="radio"
                          name="deliveryZone"
                          value={zone}
                          checked={values.deliveryZone === zone}
                          onChange={() => {
                            setValues((current) => ({ ...current, deliveryZone: zone }))
                            setErrors((current) => ({ ...current, deliveryZone: undefined }))
                          }}
                          className="accent-accent"
                        />
                        {DELIVERY_ZONE_LABELS[zone]}
                      </span>
                      <span className="tabular-nums text-muted">{fee !== undefined ? formatTaka(fee) : '—'}</span>
                    </label>
                  )
                })}
              </div>
              {errors.deliveryZone && (
                <p id="checkout-zone-error" className="text-sm text-warning">
                  {errors.deliveryZone}
                </p>
              )}
            </fieldset>

            <Field
              id="checkout-note"
              label="Note for the courier (optional)"
              hint="A landmark, or the best time to call."
              error={errors.note}
            >
              <textarea
                id="checkout-note"
                name="note"
                value={values.note}
                onChange={(event) => update('note')(event.target.value)}
                rows={2}
                maxLength={MAX_NOTE_LENGTH}
                className={fieldControl}
                {...describedBy('checkout-note', errors.note, true)}
              />
            </Field>

            <p className="text-sm text-muted">
              Payment: <strong className="text-ink">cash on delivery</strong>. You pay the courier when
              the order arrives.
            </p>
          </form>
        </div>

        <aside aria-labelledby="checkout-summary-heading" className={`${cardSurface} space-y-4 p-5 lg:sticky lg:top-32`}>
          <h2 id="checkout-summary-heading" className="text-xl">
            Your order
          </h2>

          {problem && (
            <p role="alert" className={alertBanner}>
              {problem}
            </p>
          )}

          {isQuoteError && (
            <p role="alert" className={alertBanner}>
              We could not check prices just now. Reload the page to try again.
            </p>
          )}

          {pricedQuote ? (
            <ul className="divide-y divide-line text-sm">
              {pricedQuote.items.map((item, index) => (
                <li key={`${item.productId}-${item.size ?? ''}`} className="space-y-1 py-2.5">
                  <div className="flex justify-between gap-3">
                    <span className="min-w-0">
                      {item.name ?? lines[index]?.name}
                      <span className="text-muted">
                        {item.size && ` · ${item.size}`} × {item.quantity}
                      </span>
                    </span>
                    <span className={`tabular-nums ${item.isOrderable ? '' : 'text-muted line-through'}`}>
                      {formatTaka(item.isOrderable ? item.lineTotal : item.unitPrice * item.quantity)}
                    </span>
                  </div>
                  {item.issues.map((issue) => (
                    <p key={issue} className="rounded-lg bg-warning-soft px-2.5 py-1.5 text-xs text-warning">
                      {CART_ISSUE_MESSAGES[issue]}
                    </p>
                  ))}
                </li>
              ))}
            </ul>
          ) : (
            !isQuoteError && <p className="text-sm text-muted">Checking prices and availability…</p>
          )}

          {hasBlockedLine && (
            <p role="alert" className="text-sm text-warning">
              Something in your cart cannot be ordered.{' '}
              <Link to="/cart" className="underline underline-offset-2">
                Fix it in your cart
              </Link>
              .
            </p>
          )}

          <dl className="space-y-2 border-t border-line pt-4 text-sm">
            <div className="flex justify-between">
              <dt className="text-muted">Subtotal</dt>
              <dd className="tabular-nums" aria-live="polite">
                {pricedQuote ? formatTaka(pricedQuote.subtotal) : '—'}
              </dd>
            </div>
            <div className="flex justify-between">
              <dt className="text-muted">Delivery, {DELIVERY_ZONE_LABELS[values.deliveryZone].toLowerCase()}</dt>
              <dd className="tabular-nums" aria-live="polite">
                {deliveryFee !== undefined ? formatTaka(deliveryFee) : isOptionsError ? 'Unavailable' : '—'}
              </dd>
            </div>
            <div className="flex justify-between text-base font-medium">
              <dt>Total</dt>
              <dd className="tabular-nums" aria-live="polite">
                {total !== undefined ? formatTaka(total) : '—'}
              </dd>
            </div>
          </dl>

          <Button type="submit" form="checkout-form" size="lg" className="w-full" disabled={!canPlace}>
            {isPlacing ? 'Placing order…' : 'Place order'}
          </Button>
          {!verified && <p className="text-xs text-muted">Verify your phone to place the order.</p>}
        </aside>
      </div>

      <Link to="/cart" className={buttonClasses('ghost', 'sm')}>
        ← Back to cart
      </Link>
    </div>
  )
}
