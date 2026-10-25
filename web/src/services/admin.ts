import type { QueryClient } from '@tanstack/react-query'
import { apiDeleteAsAdmin, apiGetAsAdmin, apiPostAsAdmin, apiPutAsAdmin } from '../lib/api'
import type { CategoryInput, ProductInput } from '../types/admin'
import type { Category, PagedResult, ProductDetail, ProductSummary } from '../types/catalog'

export type AdminProductListParams = {
  search?: string
  page?: number
}

/** The admin list shows drafts too; 25 rows is a screenful for a small shop. */
export const ADMIN_PAGE_SIZE = 25

/**
 * Admin data is cached apart from the storefront's, because the same product reads differently
 * here (drafts included). Everything sits under `admin` so one invalidation refreshes the panel.
 */
export const adminKeys = {
  all: ['admin'] as const,
  products: (params: AdminProductListParams) => ['admin', 'products', params] as const,
  product: (id: number) => ['admin', 'product', id] as const,
}

export function fetchAdminProducts(
  params: AdminProductListParams,
  token: string,
  signal?: AbortSignal,
): Promise<PagedResult<ProductSummary>> {
  const query = new URLSearchParams({ pageSize: String(ADMIN_PAGE_SIZE) })
  if (params.search) query.set('q', params.search)
  if (params.page) query.set('page', String(params.page))

  return apiGetAsAdmin<PagedResult<ProductSummary>>(`/api/admin/products?${query}`, token, signal)
}

export function fetchAdminProduct(
  id: number,
  token: string,
  signal?: AbortSignal,
): Promise<ProductDetail> {
  return apiGetAsAdmin<ProductDetail>(`/api/admin/products/${id}`, token, signal)
}

export function createProduct(input: ProductInput, token: string): Promise<ProductDetail> {
  return apiPostAsAdmin<ProductDetail>('/api/admin/products', input, token)
}

export function updateProduct(
  id: number,
  input: ProductInput,
  token: string,
): Promise<ProductDetail> {
  return apiPutAsAdmin<ProductDetail>(`/api/admin/products/${id}`, input, token)
}

/** The request body that leaves a stored product exactly as it is. */
export function productInputFrom(product: ProductDetail): ProductInput {
  return {
    name: product.name,
    categoryId: product.categoryId,
    description: product.description,
    price: product.price,
    sizes: product.sizes,
    isActive: product.isActive,
  }
}

/**
 * The API has no endpoint for the flag alone, so this re-sends the stored product with only the
 * flag changed. It reads the product first rather than trusting a list row, which carries neither
 * the description nor the sizes.
 */
export async function setProductActive(
  id: number,
  isActive: boolean,
  token: string,
): Promise<ProductDetail> {
  const product = await fetchAdminProduct(id, token)
  return updateProduct(id, { ...productInputFrom(product), isActive }, token)
}

export function createCategory(input: CategoryInput, token: string): Promise<Category> {
  return apiPostAsAdmin<Category>('/api/admin/categories', input, token)
}

export function updateCategory(id: number, input: CategoryInput, token: string): Promise<Category> {
  return apiPutAsAdmin<Category>(`/api/admin/categories/${id}`, input, token)
}

export function deleteCategory(id: number, token: string): Promise<void> {
  return apiDeleteAsAdmin(`/api/admin/categories/${id}`, token)
}

/**
 * Products show up in the panel and on the storefront, so a change to one refreshes both. The
 * storefront keys are prefixes of `catalogKeys.products(...)` and `catalogKeys.product(slug)`.
 */
export async function refreshProducts(queryClient: QueryClient): Promise<void> {
  await Promise.all(
    [adminKeys.all, ['products'], ['product']].map((queryKey) =>
      queryClient.invalidateQueries({ queryKey }),
    ),
  )
}
