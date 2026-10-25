import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { StatusBadge } from '../components/orders/StatusBadge'
import { VerifyToContinue } from '../components/orders/VerifyToContinue'
import { EmptyState } from '../components/common/EmptyState'
import { Skeleton } from '../components/common/Skeleton'
import { alertBanner, buttonClasses, cardSurface, focusRing } from '../components/common/styles'
import { formatTaka } from '../lib/format'
import { formatOrderDate, formatOrderNumber } from '../lib/orders'
import { useCustomerCall } from '../phone/useCustomerCall'
import { useVerifiedPhone } from '../phone/useVerifiedPhone'
import { fetchMyOrders, orderKeys } from '../services/orders'

function OrderList() {
  const { verified } = useVerifiedPhone()
  const call = useCustomerCall()
  const phone = verified?.phone ?? ''

  const { data: orders, isPending, isError } = useQuery({
    queryKey: orderKeys.mine(phone),
    queryFn: ({ signal }) => call((token) => fetchMyOrders(token, signal)),
  })

  if (isPending) {
    return (
      <div className="space-y-2">
        {Array.from({ length: 3 }, (_, index) => (
          <Skeleton key={index} className="h-20 w-full" />
        ))}
      </div>
    )
  }

  if (isError) {
    return (
      <p role="alert" className={alertBanner}>
        Your orders could not be loaded. Check your connection and reload the page.
      </p>
    )
  }

  if (orders.length === 0) {
    return (
      <EmptyState
        title="No orders for this number yet"
        description="Orders placed with this phone will show here, with where each one has got to."
        action={
          <Link to="/" className={buttonClasses('primary', 'md', 'mt-2')}>
            Start shopping
          </Link>
        }
      />
    )
  }

  return (
    <ul className={`${cardSurface} divide-y divide-line`}>
      {orders.map((order) => (
        <li key={order.id}>
          <Link
            to={`/orders/${order.id}`}
            className={`${focusRing} flex flex-wrap items-center justify-between gap-3 rounded-2xl px-4 py-4
              transition hover:bg-sunken sm:px-5`}
          >
            <span className="space-y-0.5">
              <span className="block font-medium">Order {formatOrderNumber(order.id)}</span>
              <span className="block text-xs text-muted">
                {formatOrderDate(order.createdAt)} · {order.itemCount}{' '}
                {order.itemCount === 1 ? 'item' : 'items'}
              </span>
            </span>
            <span className="flex items-center gap-3">
              <span className="font-medium tabular-nums">{formatTaka(order.total)}</span>
              <StatusBadge status={order.status} />
            </span>
          </Link>
        </li>
      ))}
    </ul>
  )
}

/** A customer's orders, found by re-verifying the phone they were placed with. */
export function OrdersPage() {
  return (
    <VerifyToContinue
      title="Your orders"
      description="There is no account to sign in to: enter the phone number you ordered with, and we text a code to make sure it is yours."
    >
      <h1 className="text-3xl">Your orders</h1>
      <OrderList />
    </VerifyToContinue>
  )
}
