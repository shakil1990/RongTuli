/** Mirrors the DTOs returned by the image endpoints in `api/Shop.Api/Images`. */

export type UploadTicket = {
  uploadUrl: string
  apiKey: string
  folder: string
  timestamp: number
  signature: string
  /** Size limit the browser checks before uploading; the API owns the number. */
  maxFileBytes: number
  allowedContentTypes: string[]
}

export type UploadedImage = {
  url: string
  publicId: string
}

/**
 * Mirrors `ImageValidation.MaxImagesPerProduct`. Shown to the admin as "2 of 5" and used to stop
 * a selection that cannot fit before it uploads; the API still refuses a sixth image on its own.
 */
export const MAX_PRODUCT_IMAGES = 5
