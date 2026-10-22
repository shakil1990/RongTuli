import type { UploadTicket, UploadedImage } from '../types/images'

/**
 * Image bytes never pass through our API: the admin's browser asks for a signed ticket and posts
 * the file straight to the image host, then reports back where it landed.
 *
 * Step 7 fetches the ticket through the admin API client, which needs the bearer token that
 * Step 3 adds. Everything below only needs the ticket, so it is already testable and final.
 */

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
