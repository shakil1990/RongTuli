import type { QueryClient } from '@tanstack/react-query'
import {
  apiGet,
  apiGetAsAdmin,
  apiGetAsCustomer,
  apiPostAsCustomer,
  apiPutAsAdmin,
} from '../lib/api'
import type { PagedResult } from '../types/catalog'
import type {
  AdminOrder,
  CheckoutOptions,
  Order,
  OrderCounts,
  OrderStatus,
  OrderSummary,
  PlaceOrderInput,
} from '../types/orders'

export type AdminOrderListParams = {
  status?: OrderStatus
  page?: number
}

export const ADMIN_ORDERS_PAGE_SIZE = 25

/**
 * A customer's orders are keyed on the phone, so verifying a different number never shows the
 * previous one's cached list. Admin keys sit under `admin` with the rest of the panel's data.
 */
export const orderKeys = {
  checkoutOptions: ['checkout-options'] as const,
  mine: (phone: string) => ['orders', phone] as const,
  one: (phone: string, id: number) => ['orders', phone, id] as const,
  adminAll: ['admin', 'orders'] as const,
  adminList: (params: AdminOrderListParams) => ['admin', 'orders', 'list', params] as const,
  adminCounts: ['admin', 'orders', 'counts'] as const,
  adminOne: (id: number) => ['admin', 'orders', 'one', id] as const,
}

export function fetchCheckoutOptions(signal?: AbortSignal): Promise<CheckoutOptions> {
  return apiGet<CheckoutOptions>('/api/checkout/options', signal)
}

export function placeOrder(input: PlaceOrderInput, token: string): Promise<Order> {
  return apiPostAsCustomer<Order>('/api/orders', input, token)
}

export function fetchMyOrders(token: string, signal?: AbortSignal): Promise<OrderSummary[]> {
  return apiGetAsCustomer<OrderSummary[]>('/api/orders', token, signal)
}

export function fetchMyOrder(id: number, token: string, signal?: AbortSignal): Promise<Order> {
  return apiGetAsCustomer<Order>(`/api/orders/${id}`, token, signal)
}

export function fetchAdminOrders(
  params: AdminOrderListParams,
  token: string,
  signal?: AbortSignal,
): Promise<PagedResult<OrderSummary>> {
  const query = new URLSearchParams({ pageSize: String(ADMIN_ORDERS_PAGE_SIZE) })
  if (params.status) query.set('status', params.status)
  if (params.page) query.set('page', String(params.page))

  return apiGetAsAdmin<PagedResult<OrderSummary>>(`/api/admin/orders?${query}`, token, signal)
}

export function fetchOrderCounts(token: string, signal?: AbortSignal): Promise<OrderCounts> {
  return apiGetAsAdmin<OrderCounts>('/api/admin/orders/counts', token, signal)
}

export function fetchAdminOrder(id: number, token: string, signal?: AbortSignal): Promise<AdminOrder> {
  return apiGetAsAdmin<AdminOrder>(`/api/admin/orders/${id}`, token, signal)
}

export function setOrderStatus(id: number, status: OrderStatus, token: string): Promise<AdminOrder> {
  return apiPutAsAdmin<AdminOrder>(`/api/admin/orders/${id}/status`, { status }, token)
}

/** A status change moves an order between lists and counts, so every admin order query refreshes. */
export function refreshAdminOrders(queryClient: QueryClient): Promise<void> {
  return queryClient.invalidateQueries({ queryKey: orderKeys.adminAll })
}
