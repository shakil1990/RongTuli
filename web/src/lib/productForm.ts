import type { ProductInput } from '../types/admin'
import type { ProductDetail } from '../types/catalog'

/** What the product form holds: text exactly as typed, parsed only on submit. */
export type ProductFormValues = {
  name: string
  categoryId: string
  price: string
  description: string
  /** Comma-separated, e.g. "S, M, L". */
  sizes: string
}

export type ProductFormField = keyof ProductFormValues

export type ProductFormErrors = Partial<Record<ProductFormField, string>>

/** Publishing is a separate action, so the form builds everything except the flag. */
export type ProductFormInput = Omit<ProductInput, 'isActive'>

export type ProductFormResult =
  | { ok: true; input: ProductFormInput }
  | { ok: false; errors: ProductFormErrors }

export const EMPTY_PRODUCT_FORM: ProductFormValues = {
  name: '',
  categoryId: '',
  price: '',
  description: '',
  sizes: '',
}

const FORM_FIELDS: ProductFormField[] = ['name', 'categoryId', 'price', 'description', 'sizes']

export function productFormFrom(product: ProductDetail): ProductFormValues {
  return {
    name: product.name,
    categoryId: String(product.categoryId),
    price: String(product.price),
    description: product.description,
    sizes: product.sizes.join(', '),
  }
}

/** Splits on commas, trims, drops blanks and case-insensitive repeats, keeping the typed order. */
export function parseSizes(text: string): string[] {
  const seen = new Set<string>()
  const sizes: string[] = []
  for (const size of text.split(',').map((part) => part.trim())) {
    const key = size.toLowerCase()
    if (size.length > 0 && !seen.has(key)) {
      seen.add(key)
      sizes.push(size)
    }
  }
  return sizes
}

/**
 * Only what the browser must settle before it can build a request: a name, a chosen category and
 * a price that is a number. Limits (lengths, decimals, how many sizes) are the API's to enforce,
 * and its messages come back to the same fields, so each rule is written down once.
 */
export function parseProductForm(values: ProductFormValues): ProductFormResult {
  const errors: ProductFormErrors = {}

  const name = values.name.trim()
  if (name.length === 0) errors.name = 'Give the product a name.'

  const categoryId = Number(values.categoryId)
  if (!values.categoryId || !Number.isInteger(categoryId)) errors.categoryId = 'Choose a category.'

  const priceText = values.price.trim()
  const price = Number(priceText)
  if (priceText.length === 0 || !Number.isFinite(price)) {
    errors.price = 'Enter a price in taka, e.g. 1250.'
  }

  if (Object.keys(errors).length > 0) {
    return { ok: false, errors }
  }

  return {
    ok: true,
    input: {
      name,
      categoryId,
      price,
      description: values.description.trim(),
      sizes: parseSizes(values.sizes),
    },
  }
}

/** Maps the API's field names onto the form's, keeping the first message for each field. */
export function formErrorsFromApi(fieldErrors: Record<string, string[]>): ProductFormErrors {
  const errors: ProductFormErrors = {}
  for (const field of FORM_FIELDS) {
    const message = fieldErrors[field]?.[0]
    if (message) errors[field] = message
  }
  return errors
}
