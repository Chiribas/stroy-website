export interface ArticleListItem {
  id: number
  title: string
  slug: string
  summary?: string | null
  thumbnailPath?: string | null
  publishedAt: string
  tags?: string | null
}

export interface ArticleMedia {
  id: number
  path: string
  mediaType: string
  alt?: string | null
  sortOrder: number
}

export interface Article {
  id: number
  title: string
  slug: string
  summary?: string | null
  content: string
  thumbnailPath?: string | null
  publishedAt: string
  tags?: string | null
  media: ArticleMedia[]
}

export interface PagedResult<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
}

export interface ServiceListItem {
  id: number
  title: string
  slug: string
  shortDescription?: string | null
  iconName?: string | null
  sortOrder: number
}

export interface ServiceDetail {
  id: number
  title: string
  slug: string
  shortDescription?: string | null
  iconName?: string | null
  content: string
  tag?: string | null
  sortOrder: number
  isPublished: boolean
}

// Пример выполненной работы с ценой (бывш. "прайс").
export interface ServicePrice {
  id: number
  title: string
  photoPath?: string | null
  description?: string | null
  price: number
  duration?: string | null
  articleSlug?: string | null
  tag?: string | null
  sortOrder: number
}

export interface CallbackPayload {
  phone: string
  name?: string
}

export interface ContactPayload {
  name: string
  phone: string
  message: string
}

export interface MessageResponse {
  message: string
}
