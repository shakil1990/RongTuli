import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { useAdminCall } from '../../auth/useAdminCall'
import { Pagination } from '../../components/Pagination'
import { EmptyState } from '../../components/common/EmptyState'
import { ProductThumb } from '../../components/common/ProductThumb'
import { Skeleton } from '../../components/common/Skeleton'
import {
  alertBanner,
  buttonClasses,
  cardSurface,
  fieldControl,
  focusRing,
} from '../../components/common/styles'
import { ApiError } from '../../lib/api'
import { formatTaka } from '../../lib/format'
import { adminKeys, fetchAdminProducts, refreshProducts, setProductActive } from '../../services/admin'
import type { ProductSummary } from '../../types/catalog'

const TOGGLE_FAILED = 'The product could not be updated. Check your connection and try again.'

function readPage(value: string | null): number {
  const page = Number(value)
  return Number.isInteger(page) && page > 0 ? page : 1
}

type StatusToggleProps = {
  product: ProductSummary
  disabled: boolean
  onToggle: () => void
}

/** A switch rather than a checkbox: flipping it acts at once, there is no form to submit. */
function StatusToggle({ product, disabled, onToggle }: StatusToggleProps) {
  return (
    <button
      type="button"
      role="switch"
      aria-checked={product.isActive}
      aria-label={`Published: ${product.name}`}
      onClick={onToggle}
      disabled={disabled}
      className={`${focusRing} inline-flex items-center gap-2 rounded-full text-sm disabled:opacity-50`}
    >
      <span
        aria-hidden="true"
        className={`relative h-5 w-9 rounded-full transition ${product.isActive ? 'bg-accent' : 'bg-sunken ring-1 ring-line'}`}
      >
        <span
          className={`absolute top-0.5 h-4 w-4 rounded-full bg-raised shadow transition-all ${product.isActive ? 'left-4.5' : 'left-0.5'}`}
        />
      </span>
      <span className={product.isActive ? 'text-ink' : 'text-muted'}>
        {product.isActive ? 'Published' : 'Draft'}
      </span>
    </button>
  )
}

export function AdminProductsPage() {
  const call = useAdminCall()
  const queryClient = useQueryClient()
  const [searchParams, setSearchParams] = useSearchParams()
  const search = searchParams.get('q') ?? ''
  const page = readPage(searchParams.get('page'))
  const [searchDraft, setSearchDraft] = useState(search)
  const [togglingId, setTogglingId] = useState<number | null>(null)
  const [problem, setProblem] = useState<string | null>(null)

  const { data, isPending, isError } = useQuery({
    queryKey: adminKeys.products({ search, page }),
    queryFn: ({ signal }) => call((token) => fetchAdminProducts({ search, page }, token, signal)),
  })

  const applySearch = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const next = new URLSearchParams()
    if (searchDraft.trim()) next.set('q', searchDraft.trim())
    setSearchParams(next)
  }

  const goToPage = (next: number) => {
    const params = new URLSearchParams(searchParams)
    params.set('page', String(next))
    setSearchParams(params)
  }

  const toggle = async (product: ProductSummary) => {
    setProblem(null)
    setTogglingId(product.id)
    try {
      await call((token) => setProductActive(product.id, !product.isActive, token))
      await refreshProducts(queryClient)
    } catch (caught) {
      // The API's reason is the useful part, e.g. a product with no image cannot be published.
      const reason = caught instanceof ApiError ? caught.message : TOGGLE_FAILED
      setProblem(`${product.name}: ${reason}`)
    } finally {
      setTogglingId(null)
    }
  }

  return (
    <div className="space-y-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <form role="search" onSubmit={applySearch} className="flex w-full max-w-sm gap-2">
          <label htmlFor="admin-product-search" className="sr-only">
            Search products by name
          </label>
          <input
            id="admin-product-search"
            type="search"
            value={searchDraft}
            onChange={(event) => setSearchDraft(event.target.value)}
            placeholder="Search by name"
            className={fieldControl}
          />
          <button type="submit" className={buttonClasses('secondary', 'md')}>
            Search
          </button>
        </form>

        <Link to="/admin/products/new" className={buttonClasses('primary', 'md')}>
          New product
        </Link>
      </div>

      {problem && (
        <p role="alert" className={alertBanner}>
          {problem}
        </p>
      )}

      {isError && (
        <p role="alert" className={alertBanner}>
          The product list could not be loaded. Check your connection and reload the page.
        </p>
      )}

      {isPending && !isError && (
        <div className="space-y-2">
          {Array.from({ length: 4 }, (_, index) => (
            <Skeleton key={index} className="h-16 w-full" />
          ))}
        </div>
      )}

      {data && data.items.length === 0 && (
        <EmptyState
          title={search ? 'No products match' : 'No products yet'}
          description={
            search
              ? `Nothing is called anything like “${search}”.`
              : 'Add the first piece. It starts as a draft, so nothing shows in the shop until you publish it.'
          }
          action={
            !search && (
              <Link to="/admin/products/new" className={buttonClasses('primary', 'md', 'mt-2')}>
                New product
              </Link>
            )
          }
        />
      )}

      {data && data.items.length > 0 && (
        <>
          <div className={`${cardSurface} overflow-x-auto`}>
            <table className="w-full min-w-[36rem] text-left text-sm">
              <caption className="sr-only">
                Products, newest first. {data.totalCount} in total.
              </caption>
              <thead className="border-b border-line text-xs tracking-wide text-muted uppercase">
                <tr>
                  <th scope="col" className="px-4 py-3 font-medium">
                    Product
                  </th>
                  <th scope="col" className="px-4 py-3 font-medium">
                    Category
                  </th>
                  <th scope="col" className="px-4 py-3 text-right font-medium">
                    Price
                  </th>
                  <th scope="col" className="px-4 py-3 font-medium">
                    Status
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-line">
                {data.items.map((product) => (
                  <tr key={product.id}>
                    <td className="px-4 py-3">
                      <Link
                        to={`/admin/products/${product.id}`}
                        className={`${focusRing} flex items-center gap-3 rounded-lg hover:text-accent`}
                      >
                        <ProductThumb
                          name={product.name}
                          url={product.coverImageUrl}
                          className="aspect-square w-10 shrink-0 rounded-lg"
                          sizes="2.5rem"
                          widths={[80, 160]}
                        />
                        <span className="font-medium">{product.name}</span>
                      </Link>
                    </td>
                    <td className="px-4 py-3 text-muted">{product.categorySlug}</td>
                    <td className="px-4 py-3 text-right tabular-nums">{formatTaka(product.price)}</td>
                    <td className="px-4 py-3">
                      <StatusToggle
                        product={product}
                        disabled={togglingId !== null}
                        onToggle={() => void toggle(product)}
                      />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <Pagination page={data.page} totalPages={data.totalPages} onChange={goToPage} />
        </>
      )}
    </div>
  )
}
