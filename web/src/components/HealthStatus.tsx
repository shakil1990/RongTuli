import { useApiHealth } from '../lib/useApiHealth'

/**
 * Says so in the footer when the API is down, and stays out of the way otherwise: a healthy
 * shop needs no status line, and a slow cold start is already explained by `WakingUpNotice`
 * at the top of the page.
 */
export function HealthStatus() {
  const { data, isPending, isError } = useApiHealth()

  const isDown = isError || (!isPending && data?.status !== 'healthy')
  if (!isDown) return null

  return (
    <p role="status" className="flex items-center gap-2">
      <span aria-hidden="true" className="h-2 w-2 rounded-full bg-warning" />
      The shop service is unavailable right now. Please try again in a few minutes.
    </p>
  )
}
