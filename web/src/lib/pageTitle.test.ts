import { describe, expect, it } from 'vitest'
import { formatPageTitle, SITE_TITLE } from './pageTitle'

describe('formatPageTitle', () => {
  it('puts the page before the shop name', () => {
    expect(formatPageTitle('Cotton panjabi')).toBe('Cotton panjabi · RongTuli')
  })

  it('uses the site title when there is no page name', () => {
    expect(formatPageTitle(null)).toBe(SITE_TITLE)
  })

  it('treats a blank name as no name', () => {
    expect(formatPageTitle('   ')).toBe(SITE_TITLE)
  })

  it('trims the name', () => {
    expect(formatPageTitle('  Cart ')).toBe('Cart · RongTuli')
  })
})
