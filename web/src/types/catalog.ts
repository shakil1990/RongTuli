/** Mirrors the DTOs returned by the catalog endpoints in `api/Shop.Api/Catalog`. */

export type Category = {
  id: number
  name: string
  slug: string
  sortOrder: number
}

export type ProductSummary = {
  id: number
  name: string
  slug: string
  price: number
  categorySlug: string
  coverImageUrl: string | null
  createdAt: string
  /** Always true on the storefront; the admin list also returns drafts. */
  isActive: boolean
}

export type ProductImage = {
  id: number
  url: string
  sortOrder: number
}

export type ProductDetail = {
  id: number
  name: string
  slug: string
  description: string
  price: number
  categoryId: number
  categoryName: string
  categorySlug: string
  sizes: string[]
  isActive: boolean
  createdAt: string
  images: ProductImage[]
}

export type PagedResult<T> = {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export const PRODUCT_SORTS = ['newest', 'price_asc', 'price_desc'] as const

export type ProductSort = (typeof PRODUCT_SORTS)[number]

export const DEFAULT_PRODUCT_SORT: ProductSort = 'newest'

export const SORT_LABELS: Record<ProductSort, string> = {
  newest: 'Newest arrivals',
  price_asc: 'Price: low to high',
  price_desc: 'Price: high to low',
}

export function isProductSort(value: string | null): value is ProductSort {
  return value !== null && (PRODUCT_SORTS as readonly string[]).includes(value)
}
