const apiUrl: string | undefined = import.meta.env.VITE_API_URL

if (!apiUrl) {
  throw new Error('VITE_API_URL is not set. Copy .env.example to .env.')
}

/** Field name (as the API spells it, e.g. `price`) to the sentences that field failed with. */
export type FieldErrors = Record<string, string[]>

/** Thrown for any non-2xx response so callers can tell a 404 from a transport failure. */
export class ApiError extends Error {
  readonly status: number
  /** Present on a 400 validation problem, so a form can show each message beside its field. */
  readonly fieldErrors: FieldErrors
  /** Present on a 429, so the page can count down to when trying again will work. */
  readonly retryAfterSeconds: number | null

  constructor(
    status: number,
    message: string,
    fieldErrors: FieldErrors = {},
    retryAfterSeconds: number | null = null,
  ) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.fieldErrors = fieldErrors
    this.retryAfterSeconds = retryAfterSeconds
  }
}

type Problem = { message: string; fieldErrors: FieldErrors; retryAfterSeconds: number | null }

function readFieldErrors(value: unknown): FieldErrors {
  if (typeof value !== 'object' || value === null) return {}

  const fieldErrors: FieldErrors = {}
  for (const [field, messages] of Object.entries(value)) {
    if (Array.isArray(messages)) {
      fieldErrors[field] = messages.filter((message): message is string => typeof message === 'string')
    }
  }
  return fieldErrors
}

/**
 * The API answers failures with RFC 7807 problem details, and its `detail` is written to be read
 * by a person — "This Google account is not a RongTuli admin." — so it is preferred over a
 * status-code sentence whenever one is there. A validation problem carries no `detail`, only
 * per-field `errors`, so its first message stands in for one. A 429 also carries
 * `retryAfterSeconds` in the body, because a cross-origin page cannot read the Retry-After header.
 */
async function readProblem(response: Response, path: string): Promise<Problem> {
  const fallback = `Request to ${path} failed with status ${response.status}`
  try {
    const problem: unknown = await response.json()
    if (typeof problem === 'object' && problem !== null) {
      const { detail, errors, retryAfterSeconds } = problem as Record<string, unknown>
      const fieldErrors = readFieldErrors(errors)
      const firstFieldMessage = Object.values(fieldErrors).flat()[0]
      const message = typeof detail === 'string' && detail.length > 0 ? detail : firstFieldMessage
      return {
        message: message ?? fallback,
        fieldErrors,
        retryAfterSeconds:
          typeof retryAfterSeconds === 'number' && retryAfterSeconds > 0 ? retryAfterSeconds : null,
      }
    }
  } catch {
    // An error response with no JSON body is normal; the status still says enough.
  }
  return { message: fallback, fieldErrors: {}, retryAfterSeconds: null }
}

async function request<T>(path: string, init: RequestInit, token?: string | null): Promise<T> {
  const headers = new Headers(init.headers)
  if (token) {
    headers.set('Authorization', `Bearer ${token}`)
  }

  const response = await fetch(`${apiUrl}${path}`, { ...init, headers })
  if (!response.ok) {
    const { message, fieldErrors, retryAfterSeconds } = await readProblem(response, path)
    throw new ApiError(response.status, message, fieldErrors, retryAfterSeconds)
  }

  // A delete answers 204 with no body to parse.
  if (response.status === 204) {
    return undefined as T
  }
  return (await response.json()) as T
}

function jsonInit(method: string, body: unknown, signal?: AbortSignal): RequestInit {
  return {
    method,
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
    signal,
  }
}

export function apiGet<T>(path: string, signal?: AbortSignal): Promise<T> {
  return request<T>(path, { signal })
}

export function apiPost<T>(path: string, body: unknown, signal?: AbortSignal): Promise<T> {
  return request<T>(path, jsonInit('POST', body, signal))
}

/**
 * Admin calls are kept separate rather than reading a token from module state, so every request
 * that depends on being signed in says so where it is written.
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
  return request<T>(path, jsonInit('POST', body, signal), token)
}

export function apiPutAsAdmin<T>(
  path: string,
  body: unknown,
  token: string,
  signal?: AbortSignal,
): Promise<T> {
  return request<T>(path, jsonInit('PUT', body, signal), token)
}

export function apiDeleteAsAdmin(path: string, token: string, signal?: AbortSignal): Promise<void> {
  return request<void>(path, { method: 'DELETE', signal }, token)
}
