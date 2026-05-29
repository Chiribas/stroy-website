export interface LoginPayload { username: string; password: string }
export interface AuthResponse { token: string; expiresAt: string }

export interface AdminArticle {
  id: number
  title: string
  slug: string
  summary?: string | null
  content: string
  thumbnailPath?: string | null
  publishedAt?: string | null
  media: { id: number; path: string; mediaType: string; alt?: string | null; sortOrder: number }[]
}

export interface ArticleWrite {
  title: string
  slug: string
  summary?: string | null
  content: string
  thumbnailPath?: string | null
  isPublished: boolean
}

export interface ServicePriceWrite {
  category: string
  name: string
  description?: string | null
  priceFrom: number
  priceTo?: number | null
  unit?: string | null
  sortOrder: number
}

export interface Callback {
  id: number; phone: string; name?: string | null; createdAt: string; isProcessed: boolean
}
export interface Contact {
  id: number; name: string; phone: string; message: string; createdAt: string; isProcessed: boolean
}

export interface MediaUploadResponse { mediaId?: number | null; url: string; thumbnailUrl: string }
