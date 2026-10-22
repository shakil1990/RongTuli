import { formatTaka } from '../../lib/format'
import { DELIVERY_ZONE_LABELS } from '../../lib/orders'
import type { Order } from '../../types/orders'

/** An order's lines and totals, as the customer was charged — the names and prices it was placed with. */
export function OrderLines({ order }: { order: Order }) {
  return (
    <div className="space-y-4">
      <ul className="divide-y divide-line">
        {order.items.map((item, index) => (
          <li key={index} className="flex items-start justify-between gap-4 py-3 text-sm">
            <div className="min-w-0">
              <p className="font-medium">{item.productName}</p>
              <p className="text-muted">
                {item.size && <>Size {item.size} · </>}
                {item.quantity} × {formatTaka(item.unitPrice)}
              </p>
            </div>
            <p className="font-medium tabular-nums">{formatTaka(item.lineTotal)}</p>
          </li>
        ))}
      </ul>

      <dl className="space-y-2 border-t border-line pt-4 text-sm">
        <div className="flex justify-between">
          <dt className="text-muted">Subtotal</dt>
          <dd className="tabular-nums">{formatTaka(order.subtotal)}</dd>
        </div>
        <div className="flex justify-between">
          <dt className="text-muted">Delivery, {DELIVERY_ZONE_LABELS[order.deliveryZone].toLowerCase()}</dt>
          <dd className="tabular-nums">{formatTaka(order.deliveryFee)}</dd>
        </div>
        <div className="flex justify-between text-base font-medium">
          <dt>Total, cash on delivery</dt>
          <dd className="tabular-nums">{formatTaka(order.total)}</dd>
        </div>
      </dl>
    </div>
  )
}
