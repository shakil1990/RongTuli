import { describe, expect, it } from 'vitest'
import { PAGE_GAP, pageWindow } from './pagination'

describe('pageWindow', () => {
  it('lists every page when there are few enough to show', () => {
    expect(pageWindow(1, 5)).toEqual([1, 2, 3, 4, 5])
  })

  it('gaps the middle when the current page is near the start', () => {
    expect(pageWindow(2, 20)).toEqual([1, 2, 3, PAGE_GAP, 20])
  })

  it('gaps both sides when the current page is in the middle', () => {
    expect(pageWindow(10, 20)).toEqual([1, PAGE_GAP, 9, 10, 11, PAGE_GAP, 20])
  })

  it('gaps the middle when the current page is near the end', () => {
    expect(pageWindow(19, 20)).toEqual([1, PAGE_GAP, 18, 19, 20])
  })

  it('never repeats the first or last page', () => {
    expect(pageWindow(1, 20)).toEqual([1, 2, PAGE_GAP, 20])
    expect(pageWindow(20, 20)).toEqual([1, PAGE_GAP, 19, 20])
  })
})
