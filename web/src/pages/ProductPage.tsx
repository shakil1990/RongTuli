import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useCart } from '../cart/useCart'
import { Button } from '../components/common/Button'
import { EmptyState } from '../components/common/EmptyState'
import { QuantityStepper } from '../components/common/QuantityStepper'
import { Skeleton } from '../components/common/Skeleton'
import { buttonClasses } from '../components/common/styles'
import { ProductGallery } from '../components/ProductGallery'
import { SizePicker } from '../components/SizePicker'
import { ApiError } from '../lib/api'
import { formatTaka } from '../lib/format'
import { catalogKeys, fetchProduct } from '../services/catalog'

export function ProductPage() {
  const { slug = '' } = useParams()

  // The route keeps this page mounted when only the slug changes, so the key remounts it:
  // the size and quantity chosen for one product must not carry over to the next.
  return <ProductView key={slug} slug={slug} />
}

function ProductView({ slug }: { slug: string }) {
  const { addLine } = useCart()

  const [size, setSize] = useState<string | null>(null)
  const [quantity, setQuantity] = useState(1)
  const [sizeError, setSizeError] = useState(false)
  const [isAdded, setIsAdded] = useState(false)

  // Kept as the result object rather than destructured, so the status flags narrow `data`.
  const productQuery = useQuery({
    queryKey: catalogKeys.product(slug),
    queryFn: ({ signal }) => fetchProduct(slug, signal),
    retry: (failureCount, queryError) =>
      !(queryError instanceof ApiError && queryError.status === 404) && failureCount < 2,
  })

  if (productQuery.isPending) {
    return (
      <div className="grid gap-8 lg:grid-cols-2">
        <Skeleton className="aspect-[4/5] rounded-2xl" />
        <div className="space-y-4">
          <Skeleton className="h-9 w-2/3" />
          <Skeleton className="h-6 w-28" />
          <Skeleton className="h-24 w-full" />
          <Skeleton className="h-12 w-48 rounded-full" />
        </div>
      </div>
    )
  }

  if (productQuery.isError) {
    const { error } = productQuery
    const isMissing = error instanceof ApiError && error.status === 404
    return (
      <EmptyState
        title={isMissing ? 'We could not find that product' : 'We could not load that product'}
        description={
          isMissing
            ? 'It may have sold out or been withdrawn from the shop.'
            : 'The shop may still be waking up. Give it a moment and try again.'
        }
        action={
          <Link to="/" className={buttonClasses('primary', 'md', 'mt-2')}>
            Browse everything
          </Link>
        }
      />
    )
  }

  const product = productQuery.data

  // A chosen size is only as good as the product's current size list: a refetch can drop the
  // size that was picked, and a size no longer offered would reach the cart as a line the
  // quote then refuses.
  const selectedSize = size !== null && product.sizes.includes(size) ? size : null

  const handleAddToCart = () => {
    if (product.sizes.length > 0 && selectedSize === null) {
      setSizeError(true)
      return
    }

    setSizeError(false)
    addLine({
      productId: product.id,
      slug: product.slug,
      name: product.name,
      size: selectedSize,
      quantity,
      unitPrice: product.price,
      coverImageUrl: product.images[0]?.url ?? null,
    })
    setIsAdded(true)
  }

  return (
    <div className="space-y-6">
      <nav aria-label="Breadcrumb" className="text-sm text-muted">
        <Link to="/" className="hover:text-accent hover:underline">
          Shop
        </Link>
        <span aria-hidden="true"> / </span>
        <Link to={`/c/${product.categorySlug}`} className="hover:text-accent hover:underline">
          {product.categoryName}
        </Link>
        <span aria-hidden="true"> / </span>
        <span className="text-ink">{product.name}</span>
      </nav>

      <div className="grid gap-8 lg:grid-cols-2 lg:gap-12">
        <ProductGallery name={product.name} images={product.images} />

        <div className="space-y-6">
          <div className="space-y-2">
            <h1 className="text-3xl leading-tight sm:text-4xl">{product.name}</h1>
            <p className="text-2xl font-medium text-accent">{formatTaka(product.price)}</p>
          </div>

          {product.description && (
            <p className="leading-relaxed whitespace-pre-line text-muted">{product.description}</p>
          )}

          {product.sizes.length > 0 && (
            <SizePicker
              sizes={product.sizes}
              value={selectedSize}
              onChange={(nextSize) => {
                setSize(nextSize)
                setSizeError(false)
                setIsAdded(false)
              }}
              errorMessage={sizeError ? 'Choose a size before adding to the cart.' : undefined}
            />
          )}

          <div className="flex flex-wrap items-center gap-3">
            <QuantityStepper
              quantity={quantity}
              label={product.name}
              onChange={(next) => {
                setQuantity(next)
                setIsAdded(false)
              }}
            />
            <Button size="lg" onClick={handleAddToCart}>
              Add to cart
            </Button>
          </div>

          {isAdded && (
            <p role="status" className="flex flex-wrap items-center gap-2 text-sm text-muted">
              Added to your cart.
              <Link to="/cart" className="font-medium text-accent hover:underline">
                View cart
              </Link>
            </p>
          )}

          <p className="border-t border-line pt-5 text-sm text-muted">
            Cash on delivery. Pay the courier when your order arrives.
          </p>
        </div>
      </div>
    </div>
  )
}
