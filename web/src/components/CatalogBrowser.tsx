import { useQuery } from '@tanstack/react-query'
import type { ReactNode } from 'react'
import { useSearchParams } from 'react-router-dom'
import { catalogKeys, fetchProducts } from '../services/catalog'
import { DEFAULT_PRODUCT_SORT, isProductSort, type ProductSort } from '../types/catalog'
import { Pagination } from './Pagination'
import { ProductGrid } from './ProductGrid'
import { SortSelect } from './SortSelect'

type CatalogBrowserProps = {
  /** Omitted on the home page, which browses every category. */
  category?: string
  empty: ReactNode
}

/**
 * Sort and page live in the query string so a filtered view can be linked and shared,
 * and the browser's back button steps through the choices a viewer made.
 */
export function CatalogBrowser({ category, empty }: CatalogBrowserProps) {
  const [searchParams, setSearchParams] = useSearchParams()

  const sort: ProductSort = isProductSort(searchParams.get('sort'))
    ? (searchParams.get('sort') as ProductSort)
    : DEFAULT_PRODUCT_SORT
  const requestedPage = Number(searchParams.get('page'))
  const page = Number.isInteger(requestedPage) && requestedPage > 0 ? requestedPage : 1

  const { data, isPending, isError } = useQuery({
    queryKey: catalogKeys.products({ category, sort, page }),
    queryFn: ({ signal }) => fetchProducts({ category, sort, page }, signal),
    // Keeps the previous page on screen while the next one loads, so the grid does not blink.
    placeholderData: (previous) => previous,
  })

  const updateParams = (changes: Record<string, string | null>) => {
    const next = new URLSearchParams(searchParams)
    for (const [key, value] of Object.entries(changes)) {
      if (value === null) {
        next.delete(key)
      } else {
        next.set(key, value)
      }
    }
    setSearchParams(next)
  }

  const handleSortChange = (nextSort: ProductSort) => {
    // A new sort invalidates the page number: page 3 of the old order means nothing.
    updateParams({
      sort: nextSort === DEFAULT_PRODUCT_SORT ? null : nextSort,
      page: null,
    })
  }

  const handlePageChange = (nextPage: number) => {
    updateParams({ page: nextPage === 1 ? null : String(nextPage) })
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }

  const totalCount = data?.totalCount ?? 0

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <p className="text-sm text-muted" aria-live="polite">
          {isPending || isError ? ' ' : `${totalCount} ${totalCount === 1 ? 'item' : 'items'}`}
        </p>
        <SortSelect value={sort} onChange={handleSortChange} />
      </div>

      <ProductGrid
        products={data?.items ?? []}
        isPending={isPending}
        isError={isError}
        empty={empty}
      />

      {data && (
        <Pagination page={data.page} totalPages={data.totalPages} onChange={handlePageChange} />
      )}
    </div>
  )
}
