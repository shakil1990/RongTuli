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

/**
 * The API answers failures with RFC 7807 problem details, and its `detail` is written to be read
 * by a person — "This Google account is not a RongTuli admin." — so it is preferred over a
 * status-code sentence whenever one is there.
 */
async function failureMessage(response: Response, path: string): Promise<string> {
  const fallback = `Request to ${path} failed with status ${response.status}`
  try {
    const problem: unknown = await response.json()
    if (typeof problem === 'object' && problem !== null) {
      const detail = (problem as Record<string, unknown>).detail
      if (typeof detail === 'string' && detail.length > 0) return detail
    }
  } catch {
    // An error response with no JSON body is normal; the status still says enough.
  }
  return fallback
}

async function request<T>(path: string, init: RequestInit, token?: string | null): Promise<T> {
  const headers = new Headers(init.headers)
  if (token) {
    headers.set('Authorization', `Bearer ${token}`)
  }

  const response = await fetch(`${apiUrl}${path}`, { ...init, headers })
  if (!response.ok) {
    throw new ApiError(response.status, await failureMessage(response, path))
  }
  return (await response.json()) as T
}

export function apiGet<T>(path: string, signal?: AbortSignal): Promise<T> {
  return request<T>(path, { signal })
}

export function apiPost<T>(path: string, body: unknown, signal?: AbortSignal): Promise<T> {
  return request<T>(path, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
    signal,
  })
}

/**
 * Admin calls are kept separate rather than reading a token from module state, so every request
 * that depends on being signed in says so where it is written. Step 7's admin panel uses these.
 */
export function apiGetAsAdmin<T>(path: string, token: string, signal?: AbortSignal): Promise<T> {
  return request<T>(path, { signal }, token)
}

export function apiPostAsAdmin<T>(
  path: string,
  body: unknown,
  token: string,
  signal?: AbortSignal,
): Promise<T> {
  return request<T>(
    path,
    {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
      signal,
    },
    token,
  )
}
