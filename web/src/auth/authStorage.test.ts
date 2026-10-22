import { beforeEach, describe, expect, it } from 'vitest'
import type { AdminSession } from '../types/auth'
import { loadSession, parseSession, saveSession, STORAGE_KEY } from './authStorage'

type Stub = {
  getItem: (key: string) => string | null
  setItem: (key: string, value: string) => void
  removeItem: (key: string) => void
}

function installStorage(stub: Stub): void {
  Object.defineProperty(globalThis, 'window', {
    value: { localStorage: stub },
    configurable: true,
    writable: true,
  })
}

function workingStorage(): Stub {
  const entries = new Map<string, string>()
  return {
    getItem: (key) => entries.get(key) ?? null,
    setItem: (key, value) => {
      entries.set(key, value)
    },
    removeItem: (key) => {
      entries.delete(key)
    },
  }
}

const NOW = new Date('2026-10-22T10:00:00Z')

function session(overrides: Partial<AdminSession> = {}): AdminSession {
  return {
    token: 'signed.jwt.value',
    expiresAt: '2026-10-22T18:00:00Z',
    email: 'admin@rongtuli.test',
    name: 'Shop Owner',
    role: 'Admin',
    ...overrides,
  }
}

const seed = (raw: string) => {
  const storage = workingStorage()
  storage.setItem(STORAGE_KEY, raw)
  installStorage(storage)
}

beforeEach(() => installStorage(workingStorage()))

describe('parseSession', () => {
  it('keeps a session that is still valid', () => {
    expect(parseSession(session(), NOW)).toEqual(session())
  })

  it.each([
    ['not an object', 'a string'],
    ['missing the token', { ...session(), token: undefined }],
    ['holding an empty token', { ...session(), token: '' }],
    ['missing the expiry', { ...session(), expiresAt: undefined }],
    ['holding an unparseable expiry', { ...session(), expiresAt: 'tomorrow' }],
    ['holding a non-string name', { ...session(), name: 42 }],
  ])('drops a stored session %s', (_, stored) => {
    expect(parseSession(stored, NOW)).toBeNull()
  })

  it('drops a session that has already expired', () => {
    expect(parseSession(session({ expiresAt: '2026-10-22T09:59:59Z' }), NOW)).toBeNull()
  })

  // Only an admin token opens an admin endpoint, so a tampered role is not worth carrying around.
  it('drops a session claiming any role but Admin', () => {
    expect(parseSession(session({ role: 'Viewer' }), NOW)).toBeNull()
  })
})

describe('loadSession', () => {
  it('reads back what saveSession stored', () => {
    saveSession(session())

    expect(loadSession(NOW)).toEqual(session())
  })

  it('returns null when nothing is stored', () => {
    expect(loadSession(NOW)).toBeNull()
  })

  it('returns null for a damaged entry rather than throwing', () => {
    seed('{ not json')

    expect(loadSession(NOW)).toBeNull()
  })

  it('returns null when storage itself throws', () => {
    installStorage({
      getItem: () => {
        throw new Error('Storage is unavailable in private mode.')
      },
      setItem: () => undefined,
      removeItem: () => undefined,
    })

    expect(loadSession(NOW)).toBeNull()
  })
})

describe('saveSession', () => {
  it('clears the stored session when given null', () => {
    saveSession(session())
    saveSession(null)

    expect(loadSession(NOW)).toBeNull()
  })
})
