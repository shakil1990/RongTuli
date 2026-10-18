const apiUrl: string | undefined = import.meta.env.VITE_API_URL

if (!apiUrl) {
  throw new Error('VITE_API_URL is not set. Copy .env.example to .env.')
}

/** Thrown for any non-2xx response so callers can tell a 404 from a transport failure. */
export class ApiError extends Error {
  readonly status: number

  constructor(status: number, message: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

export async function apiGet<T>(path: string, signal?: AbortSignal): Promise<T> {
  const response = await fetch(`${apiUrl}${path}`, { signal })
  if (!response.ok) {
    throw new ApiError(response.status, `Request to ${path} failed with status ${response.status}`)
  }
  return (await response.json()) as T
}

export async function apiPost<T>(path: string, body: unknown, signal?: AbortSignal): Promise<T> {
  const response = await fetch(`${apiUrl}${path}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
    signal,
  })
  if (!response.ok) {
    throw new ApiError(response.status, `Request to ${path} failed with status ${response.status}`)
  }
  return (await response.json()) as T
}
