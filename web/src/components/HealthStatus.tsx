import { useQuery } from '@tanstack/react-query'
import { apiGet } from '../lib/api'

export function HealthStatus() {
  const { data, isPending, isError } = useQuery({
    queryKey: ['health'],
    queryFn: () => apiGet<{ status: string }>('/health'),
    retry: false,
  })

  if (isPending) return <span>API: waking up…</span>
  if (isError) return <span>API: unavailable</span>
  return <span>API: {data.status}</span>
}
