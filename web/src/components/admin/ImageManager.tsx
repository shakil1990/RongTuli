import { useRef, useState, type ChangeEvent } from 'react'
import { useAdminCall } from '../../auth/useAdminCall'
import { ApiError } from '../../lib/api'
import { moveItem, moveToFront } from '../../lib/reorder'
import {
  attachImage,
  deleteImage,
  reorderImages,
  requestUploadTicket,
  uploadImage,
} from '../../services/images'
import type { ProductDetail } from '../../types/catalog'
import { MAX_PRODUCT_IMAGES } from '../../types/images'
import { ProductThumb } from '../common/ProductThumb'
import { alertBanner, buttonClasses, cardSurface, focusRing } from '../common/styles'

const CHANGE_FAILED = 'That did not go through. Check your connection and try again.'

const ICON_BUTTON = `rounded-full border border-line bg-raised px-2.5 py-1 text-xs text-ink transition
  hover:border-accent hover:text-accent disabled:cursor-not-allowed disabled:opacity-40 ${focusRing}`

type ImageManagerProps = {
  product: ProductDetail
  /** Called after every change the API accepted, so the page can reload the product. */
  onChanged: () => Promise<void>
}

function messageOf(caught: unknown): string {
  if (caught instanceof ApiError) return caught.message
  // `uploadImage` throws plain errors whose message is already written for the admin.
  return caught instanceof Error && caught.message ? caught.message : CHANGE_FAILED
}

/**
 * Uploads go one file at a time: each needs its own signed ticket, and the API checks the
 * five-image limit per ticket, so a sequence stops cleanly at the limit instead of racing it.
 * Progress is counted per file; `fetch` cannot report bytes sent.
 */
export function ImageManager({ product, onChanged }: ImageManagerProps) {
  const call = useAdminCall()
  const fileInput = useRef<HTMLInputElement>(null)
  const [status, setStatus] = useState<string | null>(null)
  const [problem, setProblem] = useState<string | null>(null)

  const images = product.images
  const isBusy = status !== null
  const remaining = MAX_PRODUCT_IMAGES - images.length

  /** Runs one change at a time and reports what went wrong in words meant for the admin. */
  const run = async (busyMessage: string, change: () => Promise<void>) => {
    setProblem(null)
    setStatus(busyMessage)
    try {
      await change()
    } catch (caught) {
      setProblem(messageOf(caught))
    } finally {
      await onChanged()
      setStatus(null)
    }
  }

  const handleFiles = async (event: ChangeEvent<HTMLInputElement>) => {
    const chosen = Array.from(event.target.files ?? [])
    // Cleared straight away so choosing the same file again still fires a change.
    event.target.value = ''
    if (chosen.length === 0) return

    const files = chosen.slice(0, remaining)
    const skipped = chosen.length - files.length

    await run(`Uploading 1 of ${files.length}…`, async () => {
      for (const [index, file] of files.entries()) {
        setStatus(`Uploading ${index + 1} of ${files.length}: ${file.name}…`)
        const ticket = await call((token) => requestUploadTicket(product.id, token))
        const uploaded = await uploadImage(file, ticket)
        await call((token) => attachImage(product.id, uploaded, token))
      }
    })

    // A failed upload says more than the limit does, so it is not overwritten.
    if (skipped > 0) {
      setProblem(
        (current) =>
          current ??
          `A product can have ${MAX_PRODUCT_IMAGES} images, so the last ${skipped} you chose were not uploaded.`,
      )
    }
  }

  const saveOrder = (ordered: typeof images) =>
    run('Saving the new order…', async () => {
      await call((token) => reorderImages(product.id, ordered.map((image) => image.id), token))
    })

  const remove = (imageId: number) => {
    if (!window.confirm('Delete this image? It is removed from the image host too.')) return
    void run('Deleting the image…', async () => {
      await call((token) => deleteImage(product.id, imageId, token))
    })
  }

  return (
    <section aria-labelledby="images-heading" className={`${cardSurface} space-y-4 p-5 sm:p-6`}>
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h2 id="images-heading" className="text-xl">
            Images
          </h2>
          <p className="text-sm text-muted">
            {images.length} of {MAX_PRODUCT_IMAGES}. The first image is the cover.
          </p>
        </div>

        <input
          ref={fileInput}
          type="file"
          accept="image/*"
          multiple
          onChange={(event) => void handleFiles(event)}
          className="sr-only"
          tabIndex={-1}
          aria-hidden="true"
        />
        <button
          type="button"
          className={buttonClasses('secondary', 'md')}
          onClick={() => fileInput.current?.click()}
          disabled={isBusy || remaining <= 0}
        >
          {remaining <= 0 ? 'Image limit reached' : 'Upload images'}
        </button>
      </div>

      <p aria-live="polite" className="text-sm text-muted empty:hidden">
        {status}
      </p>

      {problem && (
        <p role="alert" className={alertBanner}>
          {problem}
        </p>
      )}

      {images.length === 0 ? (
        <p className="rounded-xl border border-dashed border-line px-4 py-8 text-center text-sm text-muted">
          No images yet. A product needs at least one before it can be published.
        </p>
      ) : (
        <ol className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-5">
          {images.map((image, index) => (
            <li key={image.id} className="space-y-2">
              <div className="relative">
                <ProductThumb
                  name={`${product.name}, image ${index + 1}`}
                  url={image.url}
                  className="aspect-[4/5] rounded-xl"
                  sizes="(min-width: 1024px) 12rem, 45vw"
                />
                {index === 0 && (
                  <span className="absolute top-2 left-2 rounded-full bg-accent px-2 py-0.5 text-xs font-medium text-accent-ink">
                    Cover
                  </span>
                )}
              </div>

              <div className="flex flex-wrap gap-1">
                <button
                  type="button"
                  className={ICON_BUTTON}
                  onClick={() => void saveOrder(moveItem(images, index, -1))}
                  disabled={isBusy || index === 0}
                  aria-label={`Move image ${index + 1} earlier`}
                >
                  ←
                </button>
                <button
                  type="button"
                  className={ICON_BUTTON}
                  onClick={() => void saveOrder(moveItem(images, index, 1))}
                  disabled={isBusy || index === images.length - 1}
                  aria-label={`Move image ${index + 1} later`}
                >
                  →
                </button>
                {index > 0 && (
                  <button
                    type="button"
                    className={ICON_BUTTON}
                    onClick={() => void saveOrder(moveToFront(images, index))}
                    disabled={isBusy}
                  >
                    Make cover
                  </button>
                )}
                <button
                  type="button"
                  className={ICON_BUTTON}
                  onClick={() => remove(image.id)}
                  disabled={isBusy}
                  aria-label={`Delete image ${index + 1}`}
                >
                  Delete
                </button>
              </div>
            </li>
          ))}
        </ol>
      )}
    </section>
  )
}
