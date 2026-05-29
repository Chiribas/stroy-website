import { describe, it, expect } from 'vitest'
import { toEmbedUrl } from '~/lib/video'

describe('toEmbedUrl', () => {
  it('converts a YouTube watch URL', () => {
    expect(toEmbedUrl('https://www.youtube.com/watch?v=dQw4w9WgXcQ'))
      .toBe('https://www.youtube.com/embed/dQw4w9WgXcQ')
  })

  it('converts a youtu.be short URL', () => {
    expect(toEmbedUrl('https://youtu.be/dQw4w9WgXcQ'))
      .toBe('https://www.youtube.com/embed/dQw4w9WgXcQ')
  })

  it('keeps an existing YouTube embed URL', () => {
    expect(toEmbedUrl('https://www.youtube.com/embed/dQw4w9WgXcQ'))
      .toBe('https://www.youtube.com/embed/dQw4w9WgXcQ')
  })

  it('converts a Rutube video page URL', () => {
    expect(toEmbedUrl('https://rutube.ru/video/abc123def456/'))
      .toBe('https://rutube.ru/play/embed/abc123def456')
  })

  it('converts a VK Video page URL to a video_ext embed', () => {
    expect(toEmbedUrl('https://vk.com/video-220754053_456239018'))
      .toBe('https://vk.com/video_ext.php?oid=-220754053&id=456239018&hd=2')
  })

  it('converts a vkvideo.ru page URL', () => {
    expect(toEmbedUrl('https://vkvideo.ru/video-220754053_456239018'))
      .toBe('https://vk.com/video_ext.php?oid=-220754053&id=456239018&hd=2')
  })

  it('extracts src from a pasted iframe embed code', () => {
    const code = '<iframe src="https://vk.com/video_ext.php?oid=-1&id=2&hash=abc" width="640"></iframe>'
    expect(toEmbedUrl(code)).toBe('https://vk.com/video_ext.php?oid=-1&id=2&hash=abc')
  })

  it('returns null for unrecognised input', () => {
    expect(toEmbedUrl('just some text')).toBeNull()
    expect(toEmbedUrl('')).toBeNull()
  })
})
