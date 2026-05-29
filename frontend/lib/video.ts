// Converts a user-provided video reference into a safe embeddable iframe URL.
// Accepts: full <iframe> embed code, an embed URL, or a normal page URL for
// YouTube / Rutube / VK (VK Video). Returns null if nothing recognisable.
// Hosts must match the backend HtmlSanitizer whitelist (youtube.com, rutube.ru, vk.com).
export function toEmbedUrl(input: string): string | null {
  const raw = (input ?? '').trim()
  if (!raw) return null

  // 1) Pasted <iframe ... src="..."> embed code → take the src.
  const iframeMatch = raw.match(/<iframe[^>]*\ssrc=["']([^"']+)["']/i)
  if (iframeMatch) return iframeMatch[1]

  // 2) Already a trusted embed URL.
  if (/^https:\/\/(?:www\.)?youtube\.com\/embed\//i.test(raw)) return raw
  if (/^https:\/\/rutube\.ru\/play\/embed\//i.test(raw)) return raw
  if (/^https:\/\/vk\.com\/video_ext\.php/i.test(raw)) return raw

  // 3) YouTube watch / share / shorts.
  let m = raw.match(/(?:youtube\.com\/watch\?(?:[^#]*&)?v=|youtu\.be\/|youtube\.com\/shorts\/)([\w-]{6,})/i)
  if (m) return `https://www.youtube.com/embed/${m[1]}`

  // 4) Rutube video page.
  m = raw.match(/rutube\.ru\/video\/([0-9a-f]+)/i)
  if (m) return `https://rutube.ru/play/embed/${m[1]}`

  // 5) VK / VK Video page (video or clip) → video_ext embed.
  //    For private videos paste the embed code from "Поделиться → Экспортировать"
  //    (it carries the required hash); a bare page URL only works for public videos.
  m = raw.match(/(?:vk\.com|vkvideo\.ru)\/(?:video|clip)(-?\d+)_(\d+)/i)
  if (m) return `https://vk.com/video_ext.php?oid=${m[1]}&id=${m[2]}&hd=2`

  return null
}
