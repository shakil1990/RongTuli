import { apiDeleteAsAdmin, apiPostAsAdmin, apiPutAsAdmin } from '../lib/api'
import type { ProductImage } from '../types/catalog'
import type { UploadTicket, UploadedImage } from '../types/images'

/**
 * Image bytes never pass through our API: the admin's browser asks for a signed ticket and posts
 * the file straight to the image host, then reports back where it landed.
 */

const imagesPath = (productId: number) => `/api/admin/products/${productId}/images`

export function requestUploadTicket(productId: number, token: string): Promise<UploadTicket> {
  return apiPostAsAdmin<UploadTicket>(`${imagesPath(productId)}/upload-ticket`, {}, token)
}

/** Records an image the host has already accepted; it goes on the end of the product's list. */
export function attachImage(
  productId: number,
  uploaded: UploadedImage,
  token: string,
): Promise<ProductImage> {
  return apiPostAsAdmin<ProductImage>(imagesPath(productId), uploaded, token)
}

export function deleteImage(productId: number, imageId: number, token: string): Promise<void> {
  return apiDeleteAsAdmin(`${imagesPath(productId)}/${imageId}`, token)
}

/** Lists every image of the product in its new order; the first becomes the cover. */
export function reorderImages(
  productId: number,
  imageIds: number[],
  token: string,
): Promise<ProductImage[]> {
  return apiPutAsAdmin<ProductImage[]>(`${imagesPath(productId)}/order`, { imageIds }, token)
}

/** A human-readable reason the file cannot be uploaded, or null when it is fine. */
export function imageFileProblem(file: File, ticket: UploadTicket): string | null {
  if (!ticket.allowedContentTypes.includes(file.type)) {
    const allowed = ticket.allowedContentTypes
      .map((type) => type.replace('image/', '').toUpperCase())
      .join(', ')
    return `Choose a ${allowed} image.`
  }

  if (file.size > ticket.maxFileBytes) {
    const limit = Math.round(ticket.maxFileBytes / 1024 / 1024)
    return `This image is larger than ${limit} MB. Save it smaller and try again.`
  }

  return null
}

export async function uploadImage(
  file: File,
  ticket: UploadTicket,
  signal?: AbortSignal,
): Promise<UploadedImage> {
  const problem = imageFileProblem(file, ticket)
  if (problem) {
    throw new Error(problem)
  }

  // Exactly the parameters the API signed, or the host rejects the signature.
  const form = new FormData()
  form.append('file', file)
  form.append('api_key', ticket.apiKey)
  form.append('folder', ticket.folder)
  form.append('timestamp', String(ticket.timestamp))
  form.append('signature', ticket.signature)

  const response = await fetch(ticket.uploadUrl, { method: 'POST', body: form, signal })
  if (!response.ok) {
    throw new Error(`The image host rejected the upload (status ${response.status}).`)
  }

  const payload = (await response.json()) as { secure_url?: string; public_id?: string }
  if (!payload.secure_url || !payload.public_id) {
    throw new Error('The image host accepted the file but returned no URL.')
  }

  return { url: payload.secure_url, publicId: payload.public_id }
}
