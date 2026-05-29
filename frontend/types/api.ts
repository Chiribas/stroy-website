export interface ArticleListItem {
  id: number
  title: string
  slug: string
  summary?: string | null
  thumbnailPath?: string | null
  publishedAt: string
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
  media: ArticleMedia[]
}

export interface PagedResult<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
}

export interface ServicePrice {
  id: number
  category: string
  name: string
  description?: string | null
  priceFrom: number
  priceTo?: number | null
  unit?: string | null
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
