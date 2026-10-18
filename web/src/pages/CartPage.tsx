import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { useCart } from '../cart/useCart'
import { CartLineRow } from '../components/CartLineRow'
import { Button } from '../components/common/Button'
import { EmptyState } from '../components/common/EmptyState'
import { buttonClasses, cardSurface } from '../components/common/styles'
import { formatTaka } from '../lib/format'
import { cartKeys, requestCartQuote } from '../services/cart'

export function CartPage() {
  const { lines, count, subtotal, setQuantity, removeLine, clear } = useCart()

  // The query key carries the lines, so any edit re-quotes and the result always
  // lines up with the cart that produced it.
  const { data: quote, isFetching, isError } = useQuery({
    queryKey: cartKeys.quote(lines),
    queryFn: ({ signal }) => requestCartQuote(lines, signal),
    enabled: lines.length > 0,
  })

  if (lines.length === 0) {
    return (
      <EmptyState
        title="Your cart is empty"
        description="Pieces you add will wait here, even if you close the tab."
        action={
          <Link to="/" className={buttonClasses('primary', 'md', 'mt-2')}>
            Start shopping
          </Link>
        }
      />
    )
  }

  // The endpoint answers one priced line per line sent, in order, so a quote is read by
  // position. The length check makes a divergence fail safe rather than show one product's
  // price and issues against another's row.
  const pricedQuote = quote && quote.items.length === lines.length ? quote : undefined

  const blockedCount = pricedQuote?.items.filter((item) => !item.isOrderable).length ?? 0
  const displayedSubtotal = pricedQuote?.subtotal ?? subtotal

  // Checkout opens only once the cart on screen has come back priced: until then nothing has
  // checked that every line can still be ordered. A quote that failed outright does not hold
  // it shut, since the warning above says the totals are confirmed at checkout instead.
  const isAwaitingQuote = pricedQuote === undefined && !isError

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-3xl">
          Your cart <span className="text-muted">({count})</span>
        </h1>
        <Button variant="ghost" size="sm" onClick={clear}>
          Clear cart
        </Button>
      </div>

      {isError && (
        <p role="alert" className="rounded-xl bg-warning-soft px-4 py-3 text-sm text-warning">
          We could not re-check prices just now. The totals below are the ones last seen in the
          shop and will be confirmed at checkout.
        </p>
      )}

      <div className="grid gap-6 lg:grid-cols-[1fr_20rem] lg:items-start">
        <ul className={`${cardSurface} divide-y divide-line px-4`}>
          {lines.map((line, index) => (
            <CartLineRow
              key={`${line.productId}-${line.size ?? ''}`}
              line={line}
              quote={pricedQuote?.items[index]}
              onQuantityChange={(quantity) =>
                setQuantity({ productId: line.productId, size: line.size }, quantity)
              }
              onRemove={() => removeLine({ productId: line.productId, size: line.size })}
            />
          ))}
        </ul>

        <aside className={`${cardSurface} space-y-4 p-5 lg:sticky lg:top-32`}>
          <h2 className="text-xl">Summary</h2>

          <dl className="space-y-2 text-sm">
            <div className="flex items-center justify-between">
              <dt className="text-muted">Subtotal</dt>
              <dd className="font-medium" aria-live="polite">
                {formatTaka(displayedSubtotal)}
              </dd>
            </div>
            <div className="flex items-center justify-between">
              <dt className="text-muted">Delivery</dt>
              <dd className="text-muted">Added at checkout</dd>
            </div>
          </dl>

          <p className="text-xs text-muted" aria-live="polite">
            {isFetching ? 'Checking prices and availability…' : 'Prices checked against the shop.'}
          </p>

          {blockedCount > 0 || isAwaitingQuote ? (
            <>
              <Button size="lg" className="w-full" disabled>
                Checkout
              </Button>
              {blockedCount > 0 && (
                <p role="alert" className="text-xs text-warning">
                  {blockedCount === 1
                    ? 'One item needs attention before you can check out.'
                    : `${blockedCount} items need attention before you can check out.`}
                </p>
              )}
            </>
          ) : (
            <Link to="/checkout" className={buttonClasses('primary', 'lg', 'w-full')}>
              Checkout
            </Link>
          )}

          <p className="text-xs text-muted">
            Cash on delivery only. You pay the courier when the order arrives.
          </p>
        </aside>
      </div>
    </div>
  )
}
