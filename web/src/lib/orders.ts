import type { CartQuote } from '../types/cart'
import type { CheckoutOptions, DeliveryZone, OrderStatus, PlaceOrderLine } from '../types/orders'

/**
 * Limits mirror `OrderValidation` in the API, which is the authority. The form checks them only to
 * point at an empty field before a round trip; the API's own messages replace these when it answers.
 */
export const MAX_NAME_LENGTH = 200
export const MIN_ADDRESS_LENGTH = 10
export const MAX_ADDRESS_LENGTH = 500
export const MAX_NOTE_LENGTH = 1000

export type DeliveryFormValues = {
  customerName: string
  address: string
  note: string
  /** Always one of the two: the radio group starts on a choice and cannot be cleared. */
  deliveryZone: DeliveryZone
}

export type DeliveryFormField = keyof DeliveryFormValues

export type DeliveryFormErrors = Partial<Record<DeliveryFormField, string>>

const DELIVERY_FIELDS: DeliveryFormField[] = ['customerName', 'address', 'note', 'deliveryZone']

export function validateDeliveryForm(values: DeliveryFormValues): DeliveryFormErrors {
  const errors: DeliveryFormErrors = {}
  const name = values.customerName.trim()
  const address = values.address.trim()

  if (name.length === 0) errors.customerName = 'Enter the name to deliver to.'
  else if (name.length > MAX_NAME_LENGTH)
    errors.customerName = `Keep the name to ${MAX_NAME_LENGTH} characters.`

  if (address.length < MIN_ADDRESS_LENGTH)
    errors.address = 'Enter the full address — house, road, area and city.'
  else if (address.length > MAX_ADDRESS_LENGTH)
    errors.address = `Keep the address to ${MAX_ADDRESS_LENGTH} characters.`

  if (values.note.trim().length > MAX_NOTE_LENGTH)
    errors.note = `Keep the note to ${MAX_NOTE_LENGTH} characters.`

  return errors
}

/** The first API message for each form field, so it shows beside the field it is about. */
export function deliveryErrorsFromApi(fieldErrors: Record<string, string[]>): DeliveryFormErrors {
  const errors: DeliveryFormErrors = {}
  for (const field of DELIVERY_FIELDS) {
    const message = fieldErrors[field]?.[0]
    if (message) errors[field] = message
  }
  return errors
}

/**
 * The lines to order, priced as the quote on screen showed them. Sending the quote's prices —
 * not the ones the cart stored when each piece was added — is what lets the customer accept a
 * price change they have seen; the API refuses the order if a price moved after that.
 *
 * Null while the quote is missing, does not line up with the cart, or has a line that cannot be
 * ordered: there is nothing yet that the customer could knowingly confirm.
 */
export function orderLinesFromQuote(quote: CartQuote | undefined, lineCount: number): PlaceOrderLine[] | null {
  if (!quote || quote.items.length !== lineCount) return null
  if (quote.items.some((item) => !item.isOrderable)) return null

  return quote.items.map((item) => ({
    productId: item.productId,
    size: item.size,
    quantity: item.quantity,
    unitPrice: item.unitPrice,
  }))
}

/** How an order's status reads to the customer who placed it. */
export const STATUS_DESCRIPTIONS: Record<OrderStatus, string> = {
  Pending: 'Received. We will call to confirm it before it ships.',
  Confirmed: 'Confirmed and being packed.',
  Shipped: 'On its way with the courier. Pay in cash when it arrives.',
  Delivered: 'Delivered. Thank you for shopping with RongTuli.',
  Cancelled: 'Cancelled. Nothing is owed.',
}

/** The word on the button that moves an order to a status. */
export const STATUS_ACTIONS: Record<OrderStatus, string> = {
  Pending: 'Mark pending',
  Confirmed: 'Confirm',
  Shipped: 'Mark shipped',
  Delivered: 'Mark delivered',
  Cancelled: 'Cancel order',
}

/** Badge colours per status, from the design tokens so dark mode follows. */
export const STATUS_BADGE: Record<OrderStatus, string> = {
  Pending: 'bg-warning-soft text-warning',
  Confirmed: 'bg-accent-soft text-accent',
  Shipped: 'bg-accent-soft text-accent',
  Delivered: 'bg-sunken text-ink',
  Cancelled: 'bg-sunken text-muted line-through',
}

/** Most orders go to Dhaka itself, so checkout opens with that zone chosen. */
export const DEFAULT_DELIVERY_ZONE: DeliveryZone = 'InsideDhaka'

export const DELIVERY_ZONE_LABELS: Record<DeliveryZone, string> = {
  InsideDhaka: 'Inside Dhaka City',
  OutsideDhaka: 'Outside Dhaka City',
}

/** The fee the API quoted for a zone, or undefined until the options have loaded. */
export function deliveryFeeFor(options: CheckoutOptions | undefined, zone: DeliveryZone): number | undefined {
  return options?.deliveryOptions.find((option) => option.zone === zone)?.fee
}

/** How an order number is written everywhere it is shown, so a customer can read it out on a call. */
export function formatOrderNumber(id: number): string {
  return `#${id}`
}

const orderDate = new Intl.DateTimeFormat('en-GB', {
  day: 'numeric',
  month: 'short',
  year: 'numeric',
  hour: 'numeric',
  minute: '2-digit',
  hour12: true,
  timeZone: 'Asia/Dhaka',
})

/** In Bangladesh time, whatever the viewer's own zone: the shop and its customers are there. */
export function formatOrderDate(iso: string): string {
  // The API's timestamps are UTC but may come without a zone suffix.
  const hasZone = /[zZ]|[+-]\d{2}:?\d{2}$/.test(iso)
  return orderDate.format(new Date(hasZone ? iso : `${iso}Z`))
}
