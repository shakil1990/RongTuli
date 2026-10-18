import { apiGet } from '../lib/api'
import type { Category, PagedResult, ProductDetail, ProductSort, ProductSummary } from '../types/catalog'

export type ProductListParams = {
  category?: string
  sort?: ProductSort
  page?: number
  pageSize?: number
}

/**
 * Query keys are shaped so TanStack Query caches each category/sort/page combination
 * separately and a category change does not reuse another category's page.
 */
export const catalogKeys = {
  categories: ['categories'] as const,
  products: (params: ProductListParams) => ['products', params] as const,
  product: (slug: string) => ['product', slug] as const,
}

export function fetchCategories(signal?: AbortSignal): Promise<Category[]> {
  return apiGet<Category[]>('/api/categories', signal)
}

export function fetchProducts(
  params: ProductListParams,
  signal?: AbortSignal,
): Promise<PagedResult<ProductSummary>> {
  const query = new URLSearchParams()
  if (params.category) query.set('category', params.category)
  if (params.sort) query.set('sort', params.sort)
  if (params.page) query.set('page', String(params.page))
  if (params.pageSize) query.set('pageSize', String(params.pageSize))

  const suffix = query.size > 0 ? `?${query}` : ''
  return apiGet<PagedResult<ProductSummary>>(`/api/products${suffix}`, signal)
}

export function fetchProduct(slug: string, signal?: AbortSignal): Promise<ProductDetail> {
  return apiGet<ProductDetail>(`/api/products/${encodeURIComponent(slug)}`, signal)
}
