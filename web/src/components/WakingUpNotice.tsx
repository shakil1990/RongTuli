import { useEffect, useState } from 'react'
import { useApiHealth } from '../lib/useApiHealth'

/** A warm API answers well inside this; past it, the host is almost certainly starting up. */
const SLOW_AFTER_MS = 3000

/**
 * The footer status is easy to miss while the product grid sits on skeletons for most of a
 * minute. Once the first health check has been slow, this says why at the top of the page,
 * and it goes away by itself as soon as the API answers.
 */
export function WakingUpNotice() {
  const { isPending } = useApiHealth()
  const [isSlow, setIsSlow] = useState(false)

  useEffect(() => {
    if (!isPending) return
    const timer = setTimeout(() => setIsSlow(true), SLOW_AFTER_MS)
    return () => clearTimeout(timer)
  }, [isPending])

  if (!isPending || !isSlow) return null

  return (
    <p
      role="status"
      className="mb-6 flex items-center gap-3 rounded-xl border border-line bg-warning-soft px-4 py-3 text-sm"
    >
      <span aria-hidden="true" className="h-2 w-2 shrink-0 animate-pulse rounded-full bg-warning" />
      The shop is waking up after a quiet spell. This can take up to a minute, and only happens on
      the first visit.
    </p>
  )
}
