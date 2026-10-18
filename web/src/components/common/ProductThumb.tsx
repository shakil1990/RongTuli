import { initialsOf } from '../../lib/format'

type ProductThumbProps = {
  name: string
  url: string | null
  /** Aspect and size come from the caller; the image fills whatever box it is given. */
  className?: string
  /** `eager` for the one image above the fold, `lazy` for everything in a grid. */
  loading?: 'eager' | 'lazy'
  /** Candidate widths for the browser to choose from once Cloudinary serves the images. */
  sizes?: string
}

export function ProductThumb({
  name,
  url,
  className = 'aspect-[4/5]',
  loading = 'lazy',
  sizes,
}: ProductThumbProps) {
  return (
    <div className={`relative overflow-hidden bg-sunken ${className}`}>
      {url ? (
        <img
          src={url}
          alt={name}
          loading={loading}
          decoding="async"
          sizes={sizes}
          className="h-full w-full object-cover"
        />
      ) : (
        // Step 5 adds real photography; until then the initials keep the grid even.
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
