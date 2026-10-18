import { useSearchParams } from 'react-router-dom'
import { CatalogBrowser } from '../components/CatalogBrowser'
import { EmptyState } from '../components/common/EmptyState'

const PROMISES = ['Cash on delivery', 'Delivered across Bangladesh', 'No card needed']

export function HomePage() {
  const [searchParams] = useSearchParams()

  // The hero would only push the grid down once a viewer starts sorting or paging.
  const isBrowsing = searchParams.has('sort') || searchParams.has('page')

  return (
    <div className="space-y-10">
      {!isBrowsing && (
        <section className="rounded-3xl border border-line bg-accent-soft px-6 py-12 sm:px-12 sm:py-16">
          <p className="text-xs font-semibold tracking-[0.2em] text-accent uppercase">
            New season
          </p>
          <h1 className="mt-3 max-w-xl text-4xl leading-tight sm:text-5xl">
            Everyday clothing, in colours worth keeping.
          </h1>
          <p className="mt-4 max-w-md text-muted">
            Pieces for men, women and kids. Order now and pay cash when it reaches your door.
          </p>
          <ul className="mt-7 flex flex-wrap gap-2">
            {PROMISES.map((promise) => (
              <li
                key={promise}
                className="rounded-full border border-accent/20 bg-raised px-3.5 py-1.5 text-sm text-ink"
              >
                {promise}
              </li>
            ))}
          </ul>
        </section>
      )}

      <section aria-labelledby="heading-catalog" className="space-y-6">
        <h2 id="heading-catalog" className="text-2xl">
          {isBrowsing ? 'All products' : 'Shop everything'}
        </h2>
        <CatalogBrowser
          empty={
            <EmptyState
              title="Nothing in the shop yet"
              description="Products appear here as soon as the shop adds them. Check back shortly."
            />
          }
        />
      </section>
    </div>
  )
}
