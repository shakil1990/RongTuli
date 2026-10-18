import { Link } from 'react-router-dom'
import { formatTaka } from '../lib/format'
import type { ProductSummary } from '../types/catalog'
import { ProductThumb } from './common/ProductThumb'
import { cardSurface, focusRing } from './common/styles'

export function ProductCard({ product }: { product: ProductSummary }) {
  return (
    <article>
      <Link
        to={`/p/${product.slug}`}
        className={`${cardSurface} ${focusRing} block overflow-hidden transition duration-200
          hover:-translate-y-0.5 hover:border-accent hover:shadow-lg`}
      >
        <ProductThumb
          name={product.name}
          url={product.coverImageUrl}
          sizes="(min-width: 1024px) 25vw, (min-width: 640px) 33vw, 50vw"
        />
        <div className="px-4 py-3">
          <h3 className="truncate font-sans text-sm font-medium tracking-normal">{product.name}</h3>
          <p className="mt-1 text-sm font-medium text-accent">{formatTaka(product.price)}</p>
        </div>
      </Link>
    </article>
  )
}
