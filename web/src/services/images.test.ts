import { afterEach, describe, expect, it, vi } from 'vitest'
import type { UploadTicket } from '../types/images'
import { imageFileProblem, uploadImage } from './images'

const ticket: UploadTicket = {
  uploadUrl: 'https://api.cloudinary.com/v1_1/mxtqngum/image/upload',
  apiKey: '123',
  folder: 'rongtuli/products',
  timestamp: 1700000000,
  signature: 'abc',
  maxFileBytes: 1024,
  allowedContentTypes: ['image/jpeg', 'image/webp'],
}

const file = (type: string, size: number) =>
  new File([new Uint8Array(size)], 'shirt.jpg', { type })

function stubFetch(response: Partial<Response> & { json?: () => Promise<unknown> }) {
  const fetchMock = vi.fn().mockResolvedValue({ ok: true, status: 200, ...response })
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('imageFileProblem', () => {
  it('accepts an allowed type within the limit', () => {
    expect(imageFileProblem(file('image/jpeg', 512), ticket)).toBeNull()
  })

  it('names the allowed formats for anything else', () => {
    expect(imageFileProblem(file('image/gif', 10), ticket)).toBe('Choose a JPEG, WEBP image.')
  })

  it('rejects a file over the limit', () => {
    expect(imageFileProblem(file('image/jpeg', 2048), ticket)).toMatch(/larger than/)
  })
})

describe('uploadImage', () => {
  it('posts the signed parameters and returns the stored URL', async () => {
    const fetchMock = stubFetch({
      json: () => Promise.resolve({ secure_url: 'https://res.cloudinary.com/x.jpg', public_id: 'rongtuli/products/x' }),
    })

    const uploaded = await uploadImage(file('image/jpeg', 512), ticket)

    expect(uploaded).toEqual({
      url: 'https://res.cloudinary.com/x.jpg',
      publicId: 'rongtuli/products/x',
    })
    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit]
    const body = init.body as FormData
    expect(url).toBe(ticket.uploadUrl)
    expect([...body.keys()].sort()).toEqual([
      'api_key',
      'file',
      'folder',
      'signature',
      'timestamp',
    ])
    expect(body.get('timestamp')).toBe('1700000000')
  })

  it('does not call the host for a file it can already reject', async () => {
    const fetchMock = stubFetch({})

    await expect(uploadImage(file('image/gif', 10), ticket)).rejects.toThrow(/Choose a/)
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('reports a rejected upload', async () => {
    stubFetch({ ok: false, status: 401 })

    await expect(uploadImage(file('image/jpeg', 512), ticket)).rejects.toThrow(/status 401/)
  })

  it('reports a response with no URL in it', async () => {
    stubFetch({ json: () => Promise.resolve({}) })

    await expect(uploadImage(file('image/jpeg', 512), ticket)).rejects.toThrow(/no URL/)
  })
})
