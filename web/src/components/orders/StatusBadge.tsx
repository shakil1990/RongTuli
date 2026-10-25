import { STATUS_BADGE } from '../../lib/orders'
import type { OrderStatus } from '../../types/orders'

export function StatusBadge({ status }: { status: OrderStatus }) {
  return (
    <span
      className={`inline-flex rounded-full px-2.5 py-0.5 text-xs font-medium ${STATUS_BADGE[status]}`}
    >
      {status}
    </span>
  )
}
