import { useState } from 'react'
import type { ProductImage } from '../types/catalog'
import { ProductThumb } from './common/ProductThumb'
import { focusRing } from './common/styles'

type ProductGalleryProps = {
  name: string
  images: ProductImage[]
}

export function ProductGallery({ name, images }: ProductGalleryProps) {
  const [selectedIndex, setSelectedIndex] = useState(0)

  // Guards against a stale index if the product's images change while the page is open.
  const selected = images[selectedIndex] ?? images[0]

  return (
    <div className="space-y-3">
      <ProductThumb
        name={name}
        url={selected?.url ?? null}
        loading="eager"
        className="aspect-[4/5] rounded-2xl border border-line"
        sizes="(min-width: 1024px) 50vw, 100vw"
      />

      {images.length > 1 && (
        <ul className="flex flex-wrap gap-2">
          {images.map((image, index) => (
            <li key={image.id}>
              <button
                type="button"
                onClick={() => setSelectedIndex(index)}
                aria-label={`Show image ${index + 1} of ${images.length}`}
                aria-current={index === selectedIndex}
                className={`${focusRing} overflow-hidden rounded-xl border-2 transition ${
                  index === selectedIndex ? 'border-accent' : 'border-transparent hover:border-line'
                }`}
              >
                <ProductThumb
                  name={name}
                  url={image.url}
                  className="aspect-square w-16"
                  sizes="64px"
                />
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
