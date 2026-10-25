import { useQuery } from '@tanstack/react-query'
import { apiGet } from './api'

/** The API's health, fetched once per page load and shared by everything that shows it. */
export function useApiHealth() {
  return useQuery({
    queryKey: ['health'],
    queryFn: ({ signal }) => apiGet<{ status: string }>('/health', signal),
    retry: false,
  })
}
