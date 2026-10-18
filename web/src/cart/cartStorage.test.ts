import { beforeEach, describe, expect, it } from 'vitest'
import type { CartLine } from '../types/cart'
import { loadCart, saveCart, STORAGE_KEY } from './cartStorage'

type Stub = { getItem: (key: string) => string | null; setItem: (key: string, value: string) => void }

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
  }
}

const seed = (raw: string) => {
  const storage = workingStorage()
  storage.setItem(STORAGE_KEY, raw)
  installStorage(storage)
}

const line: CartLine = {
  productId: 1,
  slug: 'blue-shirt',
  name: 'Blue Shirt',
  size: 'M',
  quantity: 2,
  unitPrice: 450,
  coverImageUrl: null,
}

describe('cart storage', () => {
  beforeEach(() => {
    installStorage(workingStorage())
  })

  it('round-trips a cart so it survives a reload', () => {
    saveCart([line])

    expect(loadCart()).toEqual([line])
  })

  it('returns an empty cart when nothing is stored', () => {
    expect(loadCart()).toEqual([])
  })

  it('returns an empty cart when storage throws, as in private mode', () => {
    installStorage({
      getItem: () => {
        throw new Error('blocked')
      },
      setItem: () => {
        throw new Error('blocked')
      },
    })

    expect(loadCart()).toEqual([])
    expect(() => saveCart([line])).not.toThrow()
  })

  it('ignores stored text that is not JSON', () => {
    seed('not json at all')

    expect(loadCart()).toEqual([])
  })

  it('ignores a stored value that is not an array', () => {
    seed(JSON.stringify({ productId: 1 }))

    expect(loadCart()).toEqual([])
  })

  it('drops tampered lines and keeps the sound ones', () => {
    seed(
      JSON.stringify([
        line,
        { ...line, productId: 'not-a-number' },
        { ...line, unitPrice: -5 },
        { ...line, name: '' },
        null,
      ]),
    )

    expect(loadCart()).toEqual([line])
  })

  it('clamps a tampered quantity into range', () => {
    seed(JSON.stringify([{ ...line, quantity: 100000 }]))

    expect(loadCart()[0]!.quantity).toBe(99)
  })

  it('normalises an empty size to no size', () => {
    seed(JSON.stringify([{ ...line, size: '' }]))

    expect(loadCart()[0]!.size).toBeNull()
  })

  it('merges a repeated product and size into one line, so one row cannot edit as two', () => {
    seed(JSON.stringify([{ ...line, quantity: 2 }, { ...line, quantity: 3 }]))

    const loaded = loadCart()

    expect(loaded).toHaveLength(1)
    expect(loaded[0]!.quantity).toBe(5)
  })

  it('caps a merged quantity at the maximum', () => {
    seed(JSON.stringify([{ ...line, quantity: 90 }, { ...line, quantity: 90 }]))

    expect(loadCart()[0]!.quantity).toBe(99)
  })

  it('keeps the same product in two sizes as two lines', () => {
    seed(JSON.stringify([{ ...line, size: 'M' }, { ...line, size: 'L' }]))

    expect(loadCart().map((entry) => entry.size)).toEqual(['M', 'L'])
  })
})
