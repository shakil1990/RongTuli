/**
 * Cloudinary delivery URLs carry their resizing in the path, so one stored URL serves every size.
 * `f_auto` and `q_auto` let Cloudinary choose the format and quality, and `c_limit` stops it
 * enlarging a small original — together that is what keeps the free plan's bandwidth in budget.
 *
 * Anything that is not a Cloudinary delivery URL is handed back untouched, so seed data and
 * placeholder images keep working.
 */
const CLOUDINARY_HOST = 'https://res.cloudinary.com/'
const UPLOAD_SEGMENT = '/image/upload/'

/** Widths the browser may pick from: a grid thumbnail at the small end, a 2x gallery image at the large. */
export const IMAGE_WIDTHS = [320, 480, 640, 960, 1280] as const

/** The width used for the plain `src`, for browsers that ignore `srcSet`. */
export const DEFAULT_IMAGE_WIDTH = 640

export function imageUrl(url: string, width: number = DEFAULT_IMAGE_WIDTH): string {
  const insertAt = transformPosition(url)
  if (insertAt === null) {
    return url
  }

  return `${url.slice(0, insertAt)}c_limit,f_auto,q_auto,w_${width}/${url.slice(insertAt)}`
}

/** A `srcSet` for a Cloudinary image, or undefined when the URL cannot be resized. */
export function imageSrcSet(
  url: string,
  widths: readonly number[] = IMAGE_WIDTHS,
): string | undefined {
  if (transformPosition(url) === null) {
    return undefined
  }

  return widths.map((width) => `${imageUrl(url, width)} ${width}w`).join(', ')
}

function transformPosition(url: string): number | null {
  const marker = url.indexOf(UPLOAD_SEGMENT)
  if (!url.startsWith(CLOUDINARY_HOST) || marker === -1) {
    return null
  }

  return marker + UPLOAD_SEGMENT.length
}
