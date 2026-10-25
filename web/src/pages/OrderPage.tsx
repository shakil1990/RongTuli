import { useQuery } from '@tanstack/react-query'
import { Link, useLocation, useParams } from 'react-router-dom'
import { OrderLines } from '../components/orders/OrderLines'
import { StatusBadge } from '../components/orders/StatusBadge'
import { VerifyToContinue } from '../components/orders/VerifyToContinue'
import { EmptyState } from '../components/common/EmptyState'
import { Skeleton } from '../components/common/Skeleton'
import { alertBanner, buttonClasses, cardSurface } from '../components/common/styles'
import { ApiError } from '../lib/api'
import { STATUS_DESCRIPTIONS, formatOrderDate, formatOrderNumber } from '../lib/orders'
import { formatPhoneForDisplay } from '../lib/phone'
import { useCustomerCall } from '../phone/useCustomerCall'
import { useVerifiedPhone } from '../phone/useVerifiedPhone'
import { fetchMyOrder, orderKeys } from '../services/orders'
import type { Order } from '../types/orders'
import type { PlacedOrderState } from './CheckoutPage'

function readOrderId(value: string | undefined): number | null {
  const id = Number(value)
  return Number.isInteger(id) && id > 0 ? id : null
}

function OrderDetails({ id }: { id: number }) {
  const { verified } = useVerifiedPhone()
  const call = useCustomerCall()
  const phone = verified?.phone ?? ''

  const { data: order, error, isPending } = useQuery({
    queryKey: orderKeys.one(phone, id),
    queryFn: ({ signal }) => call((token) => fetchMyOrder(id, token, signal)),
    // A 404 will not turn into an order by asking again.
    retry: (count, caught) => !(caught instanceof ApiError && caught.status < 500) && count < 2,
  })

  if (isPending) {
    return <Skeleton className="h-80 w-full" />
  }

  if (error) {
    return error instanceof ApiError && error.status === 404 ? (
      <EmptyState
        title={`No order ${formatOrderNumber(id)} for this number`}
        description="Orders are found by the phone they were placed with. Check the number, or look through all your orders."
        action={
          <Link to="/orders" className={buttonClasses('primary', 'md', 'mt-2')}>
            Your orders
          </Link>
        }
      />
    ) : (
      <p role="alert" className={alertBanner}>
        The order could not be loaded. Check your connection and reload the page.
      </p>
    )
  }

  return <OrderView order={order} justPlaced={false} />
}

function OrderView({ order, justPlaced }: { order: Order; justPlaced: boolean }) {
  return (
    <div className="grid gap-6 lg:grid-cols-[1fr_22rem] lg:items-start">
      <section className={`${cardSurface} space-y-5 p-5 sm:p-6`}>
        <div className="space-y-2">
          <h1 className="text-3xl">
            {justPlaced ? 'Thank you — your order is in' : `Order ${formatOrderNumber(order.id)}`}
          </h1>
          {justPlaced && (
            <p className="text-sm">
              Your order number is <strong className="text-lg">{formatOrderNumber(order.id)}</strong>. Keep it
              handy if you call us.
            </p>
          )}
          <p className="flex flex-wrap items-center gap-2 text-sm text-muted">
            <StatusBadge status={order.status} />
            <span>{STATUS_DESCRIPTIONS[order.status]}</span>
          </p>
          <p className="text-xs text-muted">Placed {formatOrderDate(order.createdAt)}</p>
        </div>

        <OrderLines order={order} />
      </section>

      <aside className={`${cardSurface} space-y-3 p-5 text-sm`}>
        <h2 className="text-xl">Delivery</h2>
        <p>
          <strong>{order.customerName}</strong>
          <br />
          {formatPhoneForDisplay(order.phone)}
        </p>
        <p className="whitespace-pre-line">{order.address}</p>
        {order.note && <p className="text-muted">Note: {order.note}</p>}
        <p className="text-muted">Cash on delivery. Pay the courier when it arrives.</p>
        <div className="flex flex-wrap gap-2 pt-2">
          <Link to="/orders" className={buttonClasses('secondary', 'sm')}>
            All your orders
          </Link>
          <Link to="/" className={buttonClasses('ghost', 'sm')}>
            Keep shopping
          </Link>
        </div>
      </aside>
    </div>
  )
}

/** The confirmation page after checkout, and where a customer comes back to check the status. */
export function OrderPage() {
  const id = readOrderId(useParams().id)
  const state = useLocation().state as Partial<PlacedOrderState> | null

  if (id === null) {
    return (
      <EmptyState
        title="That is not an order number"
        description="Order numbers are whole numbers, like #42."
        action={
          <Link to="/orders" className={buttonClasses('primary', 'md', 'mt-2')}>
            Your orders
          </Link>
        }
      />
    )
  }

  // Straight from checkout the order is already in hand, so no phone proof is needed to show it.
  // The state is kept in the history entry, so a reload still finds it. An entry saved by an older
  // version of the page holds no order, and falls through to verification.
  if (state?.order?.id === id) {
    return <OrderView order={state.order} justPlaced />
  }

  return (
    <VerifyToContinue
      title={`See order ${formatOrderNumber(id)}`}
      description="Enter the phone number the order was placed with. We text a code to make sure it is yours."
    >
      <OrderDetails id={id} />
    </VerifyToContinue>
  )
}
