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
