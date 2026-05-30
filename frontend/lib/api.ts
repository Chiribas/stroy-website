import type {
  Article, ArticleListItem, PagedResult, ServicePrice,
  ServiceListItem, ServiceDetail,
  CallbackPayload, ContactPayload, MessageResponse,
} from '~/types/api'

type Fetcher = <T>(url: string, opts?: Record<string, unknown>) => Promise<T>

export function createApi(fetcher: Fetcher, baseURL: string) {
  const url = (path: string) => `${baseURL}${path}`
  return {
    getArticles(page = 1, pageSize = 12, tag?: string) {
      return fetcher<PagedResult<ArticleListItem>>(url('/api/articles'), {
        query: { page, pageSize, ...(tag ? { tag } : {}) },
      })
    },
    getArticle(slug: string) {
      return fetcher<Article>(url(`/api/articles/${slug}`))
    },
    getServices() {
      return fetcher<ServiceListItem[]>(url('/api/services'))
    },
    getService(slug: string) {
      return fetcher<ServiceDetail>(url(`/api/services/${slug}`))
    },
    getPrices() {
      return fetcher<ServicePrice[]>(url('/api/prices'))
    },
    sendCallback(body: CallbackPayload) {
      return fetcher<MessageResponse>(url('/api/callbacks'), { method: 'POST', body })
    },
    sendContact(body: ContactPayload) {
      return fetcher<MessageResponse>(url('/api/contacts'), { method: 'POST', body })
    },
  }
}

export type Api = ReturnType<typeof createApi>
