import type { ReactNode } from 'react'
import type { ProductSummary } from '../types/catalog'
import { EmptyState } from './common/EmptyState'
import { Skeleton } from './common/Skeleton'
import { cardSurface } from './common/styles'
import { ProductCard } from './ProductCard'

type ProductGridProps = {
  products: ProductSummary[]
  isPending: boolean
  isError: boolean
  /** Shown when the query succeeds but matches nothing, so each caller can word it itself. */
  empty: ReactNode
  skeletonCount?: number
}

const GRID = 'grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4'

export function ProductGrid({
  products,
  isPending,
  isError,
  empty,
  skeletonCount = 8,
}: ProductGridProps) {
  if (isPending) {
    return (
      <div className={GRID} aria-busy="true" aria-label="Loading products">
        {Array.from({ length: skeletonCount }, (_, index) => (
          <div key={index} className={`${cardSurface} overflow-hidden`}>
            <Skeleton className="aspect-[4/5] rounded-none" />
            <div className="space-y-2 px-4 py-3">
              <Skeleton className="h-4 w-3/4" />
              <Skeleton className="h-4 w-1/3" />
            </div>
          </div>
        ))}
      </div>
    )
  }

  if (isError) {
    return (
      <EmptyState
        title="We could not load the products"
        description="The shop may still be waking up. Give it a moment and refresh the page."
      />
    )
  }

  if (products.length === 0) {
    return <>{empty}</>
  }

  return (
    <div className={GRID}>
      {products.map((product) => (
        <ProductCard key={product.id} product={product} />
      ))}
    </div>
  )
}
