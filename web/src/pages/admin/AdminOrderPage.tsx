import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useAdminCall } from '../../auth/useAdminCall'
import { OrderLines } from '../../components/orders/OrderLines'
import { StatusBadge } from '../../components/orders/StatusBadge'
import { Button } from '../../components/common/Button'
import { EmptyState } from '../../components/common/EmptyState'
import { Skeleton } from '../../components/common/Skeleton'
import { alertBanner, buttonClasses, cardSurface } from '../../components/common/styles'
import { ApiError } from '../../lib/api'
import { DELIVERY_ZONE_LABELS, STATUS_ACTIONS, formatOrderDate, formatOrderNumber } from '../../lib/orders'
import { formatPhoneForDisplay } from '../../lib/phone'
import { fetchAdminOrder, orderKeys, refreshAdminOrders, setOrderStatus } from '../../services/orders'
import type { OrderStatus } from '../../types/orders'

const UPDATE_FAILED = 'The status could not be changed. Check your connection and try again.'

function readOrderId(value: string | undefined): number | null {
  const id = Number(value)
  return Number.isInteger(id) && id > 0 ? id : null
}

export function AdminOrderPage() {
  const id = readOrderId(useParams().id)
  const call = useAdminCall()
  const queryClient = useQueryClient()
  const [updatingTo, setUpdatingTo] = useState<OrderStatus | null>(null)
  const [problem, setProblem] = useState<string | null>(null)

  const { data, error, isPending } = useQuery({
    queryKey: orderKeys.adminOne(id ?? 0),
    queryFn: ({ signal }) => call((token) => fetchAdminOrder(id!, token, signal)),
    enabled: id !== null,
    retry: (count, caught) => !(caught instanceof ApiError && caught.status < 500) && count < 2,
  })

  const isNotFound = id === null || (error instanceof ApiError && error.status === 404)
  if (isNotFound) {
    return (
      <EmptyState
        title="Order not found"
        description="It may have been typed wrong. Every order is in the list."
        action={
          <Link to="/admin/orders" className={buttonClasses('primary', 'md', 'mt-2')}>
            All orders
          </Link>
        }
      />
    )
  }

  if (error) {
    return (
      <p role="alert" className={alertBanner}>
        The order could not be loaded. Check your connection and reload the page.
      </p>
    )
  }

  if (isPending) {
    return <Skeleton className="h-96 w-full" />
  }

  const { order, nextStatuses } = data

  const move = async (status: OrderStatus) => {
    if (status === 'Cancelled' && !window.confirm(`Cancel order ${formatOrderNumber(order.id)}? This cannot be undone.`)) {
      return
    }
    setProblem(null)
    setUpdatingTo(status)
    try {
      const updated = await call((token) => setOrderStatus(order.id, status, token))
      queryClient.setQueryData(orderKeys.adminOne(order.id), updated)
      await refreshAdminOrders(queryClient)
    } catch (caught) {
      // A 409 explains itself — another admin may have moved the order first.
      setProblem(caught instanceof ApiError ? caught.message : UPDATE_FAILED)
      if (caught instanceof ApiError && caught.status === 409) await refreshAdminOrders(queryClient)
    } finally {
      setUpdatingTo(null)
    }
  }

  return (
    <div className="space-y-5">
      <Link to="/admin/orders" className={buttonClasses('ghost', 'sm')}>
        ← All orders
      </Link>

      {problem && (
        <p role="alert" className={alertBanner}>
          {problem}
        </p>
      )}

      <div className="grid gap-6 lg:grid-cols-[1fr_22rem] lg:items-start">
        <section className={`${cardSurface} space-y-5 p-5 sm:p-6`}>
          <div className="flex flex-wrap items-center justify-between gap-3">
            <div>
              <h2 className="text-2xl">Order {formatOrderNumber(order.id)}</h2>
              <p className="text-xs text-muted">Placed {formatOrderDate(order.createdAt)}</p>
            </div>
            <StatusBadge status={order.status} />
          </div>

          <OrderLines order={order} />
        </section>

        <div className="space-y-6">
          <section aria-labelledby="order-status-heading" className={`${cardSurface} space-y-3 p-5`}>
            <h2 id="order-status-heading" className="text-xl">
              Status
            </h2>
            {nextStatuses.length === 0 ? (
              <p className="text-sm text-muted">
                This order is {order.status.toLowerCase()}. Nothing more to do.
              </p>
            ) : (
              <div className="flex flex-wrap gap-2">
                {nextStatuses.map((status) => (
                  <Button
                    key={status}
                    variant={status === 'Cancelled' ? 'ghost' : 'primary'}
                    size="sm"
                    disabled={updatingTo !== null}
                    onClick={() => void move(status)}
                  >
                    {updatingTo === status ? 'Saving…' : STATUS_ACTIONS[status]}
                  </Button>
                ))}
              </div>
            )}
            {order.status === 'Pending' && (
              <p className="text-xs text-muted">
                Call the customer to confirm before shipping, and check the delivery area matches the address.
              </p>
            )}
          </section>

          <section aria-labelledby="order-customer-heading" className={`${cardSurface} space-y-2 p-5 text-sm`}>
            <h2 id="order-customer-heading" className="text-xl">
              Customer
            </h2>
            <p>
              <strong>{order.customerName}</strong>
              <br />
              <a href={`tel:${order.phone}`} className="text-accent underline underline-offset-2">
                {formatPhoneForDisplay(order.phone)}
              </a>
            </p>
            <p className="whitespace-pre-line">{order.address}</p>
            <p>
              <span className="text-muted">Delivery area:</span> {DELIVERY_ZONE_LABELS[order.deliveryZone]}
            </p>
            {order.note && <p className="text-muted">Note: {order.note}</p>}
          </section>
        </div>
      </div>
    </div>
  )
}
