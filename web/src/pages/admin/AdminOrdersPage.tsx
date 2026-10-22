import { useQuery } from '@tanstack/react-query'
import { Link, useSearchParams } from 'react-router-dom'
import { useAdminCall } from '../../auth/useAdminCall'
import { Pagination } from '../../components/Pagination'
import { StatusBadge } from '../../components/orders/StatusBadge'
import { EmptyState } from '../../components/common/EmptyState'
import { Skeleton } from '../../components/common/Skeleton'
import { alertBanner, cardSurface, focusRing } from '../../components/common/styles'
import { formatTaka } from '../../lib/format'
import { DELIVERY_ZONE_LABELS, formatOrderDate, formatOrderNumber } from '../../lib/orders'
import { formatPhoneForDisplay } from '../../lib/phone'
import { fetchAdminOrders, fetchOrderCounts, orderKeys } from '../../services/orders'
import { ORDER_STATUSES, type OrderStatus } from '../../types/orders'

function readStatus(value: string | null): OrderStatus | undefined {
  return ORDER_STATUSES.find((status) => status === value)
}

function readPage(value: string | null): number {
  const page = Number(value)
  return Number.isInteger(page) && page > 0 ? page : 1
}

const FILTER_BASE = `rounded-full px-3 py-1.5 text-sm transition ${focusRing}`

export function AdminOrdersPage() {
  const call = useAdminCall()
  const [searchParams, setSearchParams] = useSearchParams()
  const status = readStatus(searchParams.get('status'))
  const page = readPage(searchParams.get('page'))

  const { data, isPending, isError } = useQuery({
    queryKey: orderKeys.adminList({ status, page }),
    queryFn: ({ signal }) => call((token) => fetchAdminOrders({ status, page }, token, signal)),
  })

  const { data: counts } = useQuery({
    queryKey: orderKeys.adminCounts,
    queryFn: ({ signal }) => call((token) => fetchOrderCounts(token, signal)),
  })

  const filterTo = (next: OrderStatus | undefined) => {
    const params = new URLSearchParams()
    if (next) params.set('status', next)
    setSearchParams(params)
  }

  const goToPage = (next: number) => {
    const params = new URLSearchParams(searchParams)
    params.set('page', String(next))
    setSearchParams(params)
  }

  const filters: { label: string; value: OrderStatus | undefined; count?: number }[] = [
    { label: 'All', value: undefined },
    ...ORDER_STATUSES.map((value) => ({ label: value, value, count: counts?.[value] })),
  ]

  return (
    <div className="space-y-5">
      <div role="group" aria-label="Filter orders by status" className="flex flex-wrap gap-1.5">
        {filters.map((filter) => {
          const isActive = filter.value === status
          return (
            <button
              key={filter.label}
              type="button"
              onClick={() => filterTo(filter.value)}
              aria-pressed={isActive}
              className={`${FILTER_BASE} ${isActive ? 'bg-accent-soft font-medium text-accent' : 'text-muted hover:bg-sunken hover:text-ink'}`}
            >
              {filter.label}
              {filter.count !== undefined && <span className="ml-1.5 tabular-nums">{filter.count}</span>}
            </button>
          )
        })}
      </div>

      {isError && (
        <p role="alert" className={alertBanner}>
          The order list could not be loaded. Check your connection and reload the page.
        </p>
      )}

      {isPending && !isError && (
        <div className="space-y-2">
          {Array.from({ length: 4 }, (_, index) => (
            <Skeleton key={index} className="h-14 w-full" />
          ))}
        </div>
      )}

      {data && data.items.length === 0 && (
        <EmptyState
          title={status ? `No ${status.toLowerCase()} orders` : 'No orders yet'}
          description={
            status
              ? 'Nothing is at this stage right now.'
              : 'Orders show up here as soon as a customer places one.'
          }
        />
      )}

      {data && data.items.length > 0 && (
        <>
          <div className={`${cardSurface} overflow-x-auto`}>
            <table className="w-full min-w-[40rem] text-left text-sm">
              <caption className="sr-only">
                {status ?? 'All'} orders, newest first. {data.totalCount} in total.
              </caption>
              <thead className="border-b border-line text-xs tracking-wide text-muted uppercase">
                <tr>
                  <th scope="col" className="px-4 py-3 font-medium">
                    Order
                  </th>
                  <th scope="col" className="px-4 py-3 font-medium">
                    Customer
                  </th>
                  <th scope="col" className="px-4 py-3 text-right font-medium">
                    Items
                  </th>
                  <th scope="col" className="px-4 py-3 text-right font-medium">
                    Total
                  </th>
                  <th scope="col" className="px-4 py-3 font-medium">
                    Status
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-line">
                {data.items.map((order) => (
                  <tr key={order.id}>
                    <td className="px-4 py-3">
                      <Link
                        to={`/admin/orders/${order.id}`}
                        className={`${focusRing} rounded-lg font-medium hover:text-accent hover:underline`}
                      >
                        {formatOrderNumber(order.id)}
                      </Link>
                      <span className="block text-xs text-muted">{formatOrderDate(order.createdAt)}</span>
                    </td>
                    <td className="px-4 py-3">
                      {order.customerName}
                      <span className="block text-xs text-muted">
                        {formatPhoneForDisplay(order.phone)} · {DELIVERY_ZONE_LABELS[order.deliveryZone]}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-right tabular-nums">{order.itemCount}</td>
                    <td className="px-4 py-3 text-right tabular-nums">{formatTaka(order.total)}</td>
                    <td className="px-4 py-3">
                      <StatusBadge status={order.status} />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <Pagination page={data.page} totalPages={data.totalPages} onChange={goToPage} label="Order pages" />
        </>
      )}
    </div>
  )
}
