/** Mirrors the request bodies of the admin endpoints in `api/Shop.Api/Catalog`. */

export type CategoryInput = {
  name: string
  sortOrder: number | null
}

export type ProductInput = {
  name: string
  categoryId: number
  description: string
  price: number
  sizes: string[]
  isActive: boolean
}
