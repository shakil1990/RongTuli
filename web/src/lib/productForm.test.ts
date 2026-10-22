import { describe, expect, it } from 'vitest'
import type { ProductDetail } from '../types/catalog'
import {
  EMPTY_PRODUCT_FORM,
  formErrorsFromApi,
  parseProductForm,
  parseSizes,
  productFormFrom,
} from './productForm'

const filled = {
  name: ' Blue Shirt ',
  categoryId: '2',
  price: '1250',
  description: ' Soft ',
  sizes: 'S, M',
}

describe('parseSizes', () => {
  it('trims and drops blanks', () => {
    expect(parseSizes(' S , M,, L ,')).toEqual(['S', 'M', 'L'])
  })

  it('drops case-insensitive repeats and keeps the first spelling', () => {
    expect(parseSizes('XL, m, xl, M')).toEqual(['XL', 'm'])
  })

  it('returns nothing for an empty field', () => {
    expect(parseSizes('  ')).toEqual([])
  })
})

describe('parseProductForm', () => {
  it('builds a trimmed request from a filled form', () => {
    expect(parseProductForm(filled)).toEqual({
      ok: true,
      input: {
        name: 'Blue Shirt',
        categoryId: 2,
        price: 1250,
        description: 'Soft',
        sizes: ['S', 'M'],
      },
    })
  })

  it('names every field it cannot build a request from', () => {
    const result = parseProductForm(EMPTY_PRODUCT_FORM)

    expect(result.ok).toBe(false)
    expect(Object.keys(result.ok ? {} : result.errors).sort()).toEqual([
      'categoryId',
      'name',
      'price',
    ])
  })

  it('rejects a price that is not a number', () => {
    const result = parseProductForm({ ...filled, price: '12a' })

    expect(result.ok ? null : result.errors.price).toMatch(/price/)
  })

  it('leaves range checks to the API', () => {
    expect(parseProductForm({ ...filled, price: '-5' }).ok).toBe(true)
  })
})

describe('productFormFrom', () => {
  it('fills the form from a stored product', () => {
    const product = {
      name: 'Cap',
      categoryId: 4,
      price: 99.5,
      description: '',
      sizes: ['S', 'M'],
    } as unknown as ProductDetail

    expect(productFormFrom(product)).toEqual({
      name: 'Cap',
      categoryId: '4',
      price: '99.5',
      description: '',
      sizes: 'S, M',
    })
  })
})

describe('formErrorsFromApi', () => {
  it('keeps the first message of each form field and ignores the rest', () => {
    expect(
      formErrorsFromApi({ price: ['Too high.', 'Second.'], isActive: ['Needs an image.'] }),
    ).toEqual({ price: 'Too high.' })
  })
})
