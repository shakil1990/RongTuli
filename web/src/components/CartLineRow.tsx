import { Link } from 'react-router-dom'
import { formatTaka } from '../lib/format'
import type { CartLine, CartQuoteLine } from '../types/cart'
import { CART_ISSUE_MESSAGES } from '../types/cart'
import { ProductThumb } from './common/ProductThumb'
import { QuantityStepper } from './common/QuantityStepper'
import { focusRing } from './common/styles'

type CartLineRowProps = {
  line: CartLine
  /** From `POST /api/cart/quote`; absent while the quote is in flight. */
  quote: CartQuoteLine | undefined
  onQuantityChange: (quantity: number) => void
  onRemove: () => void
}

export function CartLineRow({ line, quote, onQuantityChange, onRemove }: CartLineRowProps) {
  const unitPrice = quote?.unitPrice ?? line.unitPrice
  const lineTotal = quote?.lineTotal ?? line.unitPrice * line.quantity
  const issues = quote?.issues ?? []
  const isOrderable = quote?.isOrderable ?? true

  return (
    <li className="flex gap-4 py-5">
      <Link
        to={`/p/${line.slug}`}
        className={`${focusRing} shrink-0 overflow-hidden rounded-xl`}
        tabIndex={-1}
        aria-hidden="true"
      >
        <ProductThumb
          name={line.name}
          url={quote?.coverImageUrl ?? line.coverImageUrl}
          className="aspect-[4/5] w-20"
          sizes="80px"
        />
      </Link>

      <div className="flex min-w-0 flex-1 flex-col gap-3">
        <div className="flex flex-wrap items-start justify-between gap-x-4 gap-y-1">
          <div className="min-w-0">
            <h3 className="font-sans text-base font-medium tracking-normal">
              <Link to={`/p/${line.slug}`} className="hover:text-accent hover:underline">
                {quote?.name ?? line.name}
              </Link>
            </h3>
            {line.size && <p className="mt-0.5 text-sm text-muted">Size {line.size}</p>}
          </div>
          <p className="text-right">
            <span className={`font-medium ${isOrderable ? '' : 'text-muted line-through'}`}>
              {formatTaka(lineTotal)}
            </span>
            {line.quantity > 1 && (
              <span className="block text-xs text-muted">{formatTaka(unitPrice)} each</span>
            )}
          </p>
        </div>

        {issues.length > 0 && (
          <ul className="space-y-1">
            {issues.map((issue) => (
              <li
                key={issue}
                className="rounded-lg bg-warning-soft px-2.5 py-1.5 text-xs text-warning"
              >
                {CART_ISSUE_MESSAGES[issue]}
              </li>
            ))}
          </ul>
        )}

        <div className="flex items-center gap-3">
          <QuantityStepper
            quantity={line.quantity}
            label={line.name}
            onChange={onQuantityChange}
          />
          <button
            type="button"
            onClick={onRemove}
            className={`${focusRing} rounded-full px-2 py-1 text-sm text-muted transition
              hover:text-accent hover:underline`}
            aria-label={`Remove ${line.name} from the cart`}
          >
            Remove
          </button>
        </div>
      </div>
    </li>
  )
}
