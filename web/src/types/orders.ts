/** Mirrors `api/Shop.Api/Orders/OrderDtos.cs`. */

export const ORDER_STATUSES = ['Pending', 'Confirmed', 'Shipped', 'Delivered', 'Cancelled'] as const

export type OrderStatus = (typeof ORDER_STATUSES)[number]

export const DELIVERY_ZONES = ['InsideDhaka', 'OutsideDhaka'] as const

export type DeliveryZone = (typeof DELIVERY_ZONES)[number]

export type OrderItem = {
  productId: number
  productName: string
  size: string | null
  unitPrice: number
  quantity: number
  lineTotal: number
}

export type Order = {
  id: number
  status: OrderStatus
  customerName: string
  /** E.164, as verified — or as typed, while phone verification is off. */
  phone: string
  address: string
  note: string | null
  deliveryZone: DeliveryZone
  subtotal: number
  deliveryFee: number
  total: number
  /** ISO-8601, UTC. */
  createdAt: string
  items: OrderItem[]
}

export type OrderSummary = {
  id: number
  status: OrderStatus
  customerName: string
  phone: string
  deliveryZone: DeliveryZone
  itemCount: number
  total: number
  createdAt: string
}

export type AdminOrder = {
  order: Order
  /** The statuses the API will accept next; empty once an order is delivered or cancelled. */
  nextStatuses: OrderStatus[]
}

export type OrderCounts = Record<OrderStatus, number>

export type DeliveryOption = {
  zone: DeliveryZone
  fee: number
}

export type CheckoutOptions = {
  deliveryOptions: DeliveryOption[]
  /** False while the shop takes orders on the typed number alone, with no texted code. */
  requirePhoneVerification: boolean
}

/** One cart line as the order endpoint takes it: `unitPrice` is the price the customer was shown. */
export type PlaceOrderLine = {
  productId: number
  size: string | null
  quantity: number
  unitPrice: number
}

export type PlaceOrderInput = {
  customerName: string
  phone: string
  address: string
  note: string | null
  deliveryZone: DeliveryZone
  items: PlaceOrderLine[]
}
