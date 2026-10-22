import { imageSrcSet, imageUrl } from '../../lib/cloudinary'
import { initialsOf } from '../../lib/format'

type ProductThumbProps = {
  name: string
  url: string | null
  /** Aspect and size come from the caller; the image fills whatever box it is given. */
  className?: string
  /** `eager` for the one image above the fold, `lazy` for everything in a grid. */
  loading?: 'eager' | 'lazy'
  /** How much of the viewport the image takes, so the browser can pick a width from `srcSet`. */
  sizes?: string
  /** Widths worth offering for this box; the default ladder suits a grid or gallery image. */
  widths?: readonly number[]
}

export function ProductThumb({
  name,
  url,
  className = 'aspect-[4/5]',
  loading = 'lazy',
  sizes,
  widths,
}: ProductThumbProps) {
  return (
    <div className={`relative overflow-hidden bg-sunken ${className}`}>
      {url ? (
        <img
          src={imageUrl(url)}
          srcSet={imageSrcSet(url, widths)}
          alt={name}
          loading={loading}
          decoding="async"
          sizes={sizes}
          className="h-full w-full object-cover"
        />
      ) : (
        // A product with no photograph yet: the initials keep the grid even.
        <div
          aria-hidden="true"
          className="flex h-full w-full items-center justify-center bg-accent-soft"
        >
          <span className="font-display text-3xl font-medium text-accent opacity-80">
            {initialsOf(name)}
          </span>
        </div>
      )}
    </div>
  )
}
