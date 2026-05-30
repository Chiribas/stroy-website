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
  tags?: string | null
  media: { id: number; path: string; mediaType: string; alt?: string | null; sortOrder: number }[]
}

export interface ArticleWrite {
  title: string
  slug: string
  summary?: string | null
  content: string
  thumbnailPath?: string | null
  isPublished: boolean
  tags?: string | null
}

// Пример работы с ценой (бывш. "прайс").
export interface ServicePriceWrite {
  title: string
  photoPath?: string | null
  description?: string | null
  price: number
  duration?: string | null
  articleSlug?: string | null
  tag?: string | null
  sortOrder: number
}

export interface ServiceWrite {
  title: string
  slug: string
  shortDescription?: string | null
  iconName?: string | null
  content: string
  tag?: string | null
  sortOrder: number
  isPublished: boolean
}
export interface AdminService extends ServiceWrite { id: number }

export interface Callback {
  id: number; phone: string; name?: string | null; createdAt: string; isProcessed: boolean
}
export interface Contact {
  id: number; name: string; phone: string; message: string; createdAt: string; isProcessed: boolean
}

export interface MediaUploadResponse { mediaId?: number | null; url: string; thumbnailUrl: string }
