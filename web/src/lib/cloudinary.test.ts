import { describe, expect, it } from 'vitest'
import { imageSrcSet, imageUrl } from './cloudinary'

const stored =
  'https://res.cloudinary.com/mxtqngum/image/upload/v1700000000/rongtuli/products/blue-shirt.jpg'

describe('imageUrl', () => {
  it('inserts the transform ahead of the version and public ID', () => {
    expect(imageUrl(stored, 480)).toBe(
      'https://res.cloudinary.com/mxtqngum/image/upload/c_limit,f_auto,q_auto,w_480/v1700000000/rongtuli/products/blue-shirt.jpg',
    )
  })

  it('leaves a URL from anywhere else alone', () => {
    expect(imageUrl('https://images.example.com/shirt.jpg', 480)).toBe(
      'https://images.example.com/shirt.jpg',
    )
    expect(imageUrl('/placeholder.png', 480)).toBe('/placeholder.png')
  })

  it('leaves a Cloudinary URL that is not an image delivery URL alone', () => {
    const video = 'https://res.cloudinary.com/mxtqngum/video/upload/v1/clip.mp4'

    expect(imageUrl(video, 480)).toBe(video)
  })
})

describe('imageSrcSet', () => {
  it('offers each width with its descriptor', () => {
    expect(imageSrcSet(stored, [320, 640])).toBe(
      `${imageUrl(stored, 320)} 320w, ${imageUrl(stored, 640)} 640w`,
    )
  })

  it('is undefined when the image cannot be resized', () => {
    expect(imageSrcSet('https://images.example.com/shirt.jpg')).toBeUndefined()
  })
})
