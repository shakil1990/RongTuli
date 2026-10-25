import { describe, expect, it } from 'vitest'
import { moveItem, moveToFront } from './reorder'

describe('moveItem', () => {
  it('swaps an entry with the one before it', () => {
    expect(moveItem(['a', 'b', 'c'], 2, -1)).toEqual(['a', 'c', 'b'])
  })

  it('swaps an entry with the one after it', () => {
    expect(moveItem(['a', 'b', 'c'], 0, 1)).toEqual(['b', 'a', 'c'])
  })

  it('leaves the list alone when the move would leave it', () => {
    expect(moveItem(['a', 'b'], 0, -1)).toEqual(['a', 'b'])
    expect(moveItem(['a', 'b'], 1, 1)).toEqual(['a', 'b'])
  })

  it('does not change the list it was given', () => {
    const items = ['a', 'b']
    moveItem(items, 0, 1)
    expect(items).toEqual(['a', 'b'])
  })
})

describe('moveToFront', () => {
  it('puts the entry first and keeps the rest in order', () => {
    expect(moveToFront(['a', 'b', 'c', 'd'], 2)).toEqual(['c', 'a', 'b', 'd'])
  })

  it('leaves the list alone for the first entry or an index outside it', () => {
    expect(moveToFront(['a', 'b'], 0)).toEqual(['a', 'b'])
    expect(moveToFront(['a', 'b'], 5)).toEqual(['a', 'b'])
  })
})
