import { useQuery } from '@tanstack/react-query'
import { apiGet } from '../lib/api'

/**
 * The free API host sleeps when idle, so a cold start can take a while. Saying so in the
 * footer explains the wait instead of leaving the storefront looking broken.
 */
export function HealthStatus() {
  const { data, isPending, isError } = useQuery({
    queryKey: ['health'],
    queryFn: ({ signal }) => apiGet<{ status: string }>('/health', signal),
    retry: false,
  })

  const isHealthy = !isPending && !isError && data?.status === 'healthy'
  const label = isPending ? 'waking up…' : isError ? 'unavailable' : (data?.status ?? 'unknown')

  return (
    <span className="flex items-center gap-2">
      <span
        aria-hidden="true"
        className={`h-2 w-2 rounded-full ${
          isPending ? 'animate-pulse bg-warning' : isHealthy ? 'bg-accent' : 'bg-warning'
        }`}
      />
      Shop service: {label}
    </span>
  )
}
