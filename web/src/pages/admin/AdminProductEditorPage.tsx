import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useAdminCall } from '../../auth/useAdminCall'
import { ImageManager } from '../../components/admin/ImageManager'
import { ProductForm } from '../../components/admin/ProductForm'
import { Button } from '../../components/common/Button'
import { Skeleton } from '../../components/common/Skeleton'
import { alertBanner, cardSurface, focusRing } from '../../components/common/styles'
import { ApiError } from '../../lib/api'
import { EMPTY_PRODUCT_FORM, productFormFrom, type ProductFormInput } from '../../lib/productForm'
import {
  adminKeys,
  createProduct,
  fetchAdminProduct,
  productInputFrom,
  refreshProducts,
  updateProduct,
} from '../../services/admin'
import { catalogKeys, fetchCategories } from '../../services/catalog'
import type { ProductDetail } from '../../types/catalog'

const PUBLISH_FAILED = 'The change did not go through. Check your connection and try again.'

const backLink = (
  <Link
    to="/admin/products"
    className={`${focusRing} inline-block rounded-lg text-sm text-muted hover:text-accent`}
  >
    ← All products
  </Link>
)

type PublishPanelProps = {
  product: ProductDetail
  onToggle: () => Promise<void>
}

function PublishPanel({ product, onToggle }: PublishPanelProps) {
  const [isSaving, setIsSaving] = useState(false)
  const [problem, setProblem] = useState<string | null>(null)
  const hasImage = product.images.length > 0

  const toggle = async () => {
    setProblem(null)
    setIsSaving(true)
    try {
      await onToggle()
    } catch (caught) {
      setProblem(caught instanceof ApiError ? caught.message : PUBLISH_FAILED)
    } finally {
      setIsSaving(false)
    }
  }

  return (
    <section aria-labelledby="publish-heading" className={`${cardSurface} space-y-3 p-5 sm:p-6`}>
      <h2 id="publish-heading" className="text-xl">
        {product.isActive ? 'Published' : 'Draft'}
      </h2>
      <p className="text-sm text-muted">
        {product.isActive
          ? 'Customers can see and order this product.'
          : hasImage
            ? 'Only admins can see this product. Publish it to put it in the shop.'
            : 'Upload at least one image, then publish it to put it in the shop.'}
      </p>

      {problem && (
        <p role="alert" className={alertBanner}>
          {problem}
        </p>
      )}

      <div className="flex flex-wrap items-center gap-3">
        <Button
          variant={product.isActive ? 'secondary' : 'primary'}
          onClick={() => void toggle()}
          disabled={isSaving || (!product.isActive && !hasImage)}
        >
          {isSaving ? 'Saving…' : product.isActive ? 'Unpublish' : 'Publish'}
        </Button>
        {product.isActive && (
          <Link
            to={`/p/${product.slug}`}
            className={`${focusRing} rounded-lg text-sm text-accent hover:underline`}
          >
            View in the shop
          </Link>
        )}
      </div>
    </section>
  )
}

/** One page for both creating (`/admin/products/new`) and editing (`/admin/products/:id`). */
export function AdminProductEditorPage() {
  const { id } = useParams()
  const productId = id === undefined ? null : Number(id)
  const isNew = productId === null

  const call = useAdminCall()
  const queryClient = useQueryClient()
  const navigate = useNavigate()

  const categories = useQuery({
    queryKey: catalogKeys.categories,
    queryFn: ({ signal }) => fetchCategories(signal),
  })

  const product = useQuery({
    queryKey: adminKeys.product(productId ?? 0),
    queryFn: ({ signal }) => call((token) => fetchAdminProduct(productId!, token, signal)),
    enabled: !isNew && Number.isInteger(productId),
  })

  const reload = () => refreshProducts(queryClient)

  const create = async (input: ProductFormInput) => {
    // Always a draft: the API will not publish a product before it has an image.
    const created = await call((token) => createProduct({ ...input, isActive: false }, token))
    await reload()
    navigate(`/admin/products/${created.id}`, { replace: true })
  }

  const save = (current: ProductDetail) => async (input: ProductFormInput) => {
    await call((token) => updateProduct(current.id, { ...input, isActive: current.isActive }, token))
    await reload()
  }

  const togglePublished = (current: ProductDetail) => async () => {
    await call((token) =>
      updateProduct(current.id, { ...productInputFrom(current), isActive: !current.isActive }, token),
    )
    await reload()
  }

  if (categories.isPending || (!isNew && product.isPending)) {
    return (
      <div className="space-y-4">
        {backLink}
        <Skeleton className="h-96 w-full" />
      </div>
    )
  }

  if (categories.isError || (!isNew && product.isError) || (!isNew && !Number.isInteger(productId))) {
    const isMissing = product.error instanceof ApiError && product.error.status === 404
    return (
      <div className="space-y-4">
        {backLink}
        <p role="alert" className={alertBanner}>
          {isMissing || !Number.isInteger(productId)
            ? 'There is no product with this address.'
            : 'The product could not be loaded. Check your connection and reload the page.'}
        </p>
      </div>
    )
  }

  if (isNew) {
    return (
      <div className="space-y-4">
        {backLink}
        <h2 className="text-2xl">New product</h2>
        <p className="text-sm text-muted">
          Saved as a draft. You can add images once it is saved, then publish it.
        </p>
        <ProductForm
          initialValues={EMPTY_PRODUCT_FORM}
          categories={categories.data}
          submitLabel="Save draft"
          onSave={create}
        />
      </div>
    )
  }

  const current = product.data!
  return (
    <div className="space-y-4">
      {backLink}
      <h2 className="text-2xl">{current.name}</h2>

      <div className="grid gap-6 lg:grid-cols-[1fr_20rem] lg:items-start">
        <div className="space-y-6">
          {/* Keyed on the product so the fields reset if the admin moves to another product. */}
          <ProductForm
            key={current.id}
            initialValues={productFormFrom(current)}
            categories={categories.data}
            submitLabel="Save changes"
            onSave={save(current)}
          />
          <ImageManager product={current} onChanged={reload} />
        </div>

        <PublishPanel product={current} onToggle={togglePublished(current)} />
      </div>
    </div>
  )
}
