import { PAGE_GAP, pageWindow } from '../lib/pagination'
import { focusRing } from './common/styles'

type PaginationProps = {
  page: number
  totalPages: number
  onChange: (page: number) => void
  /** What a screen reader calls this set of pages. */
  label?: string
}

const STEP = `rounded-full border border-line px-3 py-1.5 text-sm text-ink transition
  hover:border-accent hover:text-accent disabled:cursor-not-allowed disabled:opacity-40`

export function Pagination({ page, totalPages, onChange, label = 'Product pages' }: PaginationProps) {
  if (totalPages <= 1) {
    return null
  }

  return (
    <nav aria-label={label} className="flex items-center justify-center gap-1.5">
      <button
        type="button"
        className={`${STEP} ${focusRing}`}
        onClick={() => onChange(page - 1)}
        disabled={page <= 1}
        aria-label="Go to the previous page"
      >
        Previous
      </button>

      {pageWindow(page, totalPages).map((slot, index) =>
        slot === PAGE_GAP ? (
          <span key={`gap-${index}`} aria-hidden="true" className="px-1 text-muted">
            …
          </span>
        ) : (
          <button
            key={slot}
            type="button"
            className={`${focusRing} h-9 w-9 rounded-full text-sm tabular-nums transition ${
              slot === page
                ? 'bg-accent font-medium text-accent-ink'
                : 'text-ink hover:bg-sunken'
            }`}
            onClick={() => onChange(slot)}
            aria-label={`Go to page ${slot}`}
            aria-current={slot === page ? 'page' : undefined}
          >
            {slot}
          </button>
        ),
      )}

      <button
        type="button"
        className={`${STEP} ${focusRing}`}
        onClick={() => onChange(page + 1)}
        disabled={page >= totalPages}
        aria-label="Go to the next page"
      >
        Next
      </button>
    </nav>
  )
}
