import type {
  LoginPayload, AuthResponse, AdminArticle, ArticleWrite,
  ServicePriceWrite, Callback, Contact, MediaUploadResponse,
} from '~/types/admin'
import type { ArticleListItem, PagedResult, ServicePrice } from '~/types/api'

type Fetcher = <T>(url: string, opts?: Record<string, unknown>) => Promise<T>

export function createAdminApi(fetcher: Fetcher, baseURL: string, getToken: () => string | null) {
  const url = (path: string) => `${baseURL}${path}`
  const auth = () => {
    const t = getToken()
    return t ? { Authorization: `Bearer ${t}` } : {}
  }
  return {
    login(body: LoginPayload) {
      return fetcher<AuthResponse>(url('/api/admin/auth'), { method: 'POST', body })
    },
    listArticles(page = 1, pageSize = 20) {
      return fetcher<PagedResult<ArticleListItem>>(url('/api/admin/articles'), {
        query: { page, pageSize }, headers: auth(),
      })
    },
    getArticle(id: number) {
      return fetcher<AdminArticle>(url(`/api/admin/articles/${id}`), { headers: auth() })
    },
    createArticle(body: ArticleWrite) {
      return fetcher<AdminArticle>(url('/api/admin/articles'), { method: 'POST', body, headers: auth() })
    },
    updateArticle(id: number, body: ArticleWrite) {
      return fetcher<AdminArticle>(url(`/api/admin/articles/${id}`), { method: 'PUT', body, headers: auth() })
    },
    deleteArticle(id: number) {
      return fetcher<void>(url(`/api/admin/articles/${id}`), { method: 'DELETE', headers: auth() })
    },
    listPrices() {
      return fetcher<ServicePrice[]>(url('/api/admin/services'), { headers: auth() })
    },
    createPrice(body: ServicePriceWrite) {
      return fetcher<ServicePrice>(url('/api/admin/services'), { method: 'POST', body, headers: auth() })
    },
    updatePrice(id: number, body: ServicePriceWrite) {
      return fetcher<ServicePrice>(url(`/api/admin/services/${id}`), { method: 'PUT', body, headers: auth() })
    },
    deletePrice(id: number) {
      return fetcher<void>(url(`/api/admin/services/${id}`), { method: 'DELETE', headers: auth() })
    },
    listCallbacks() {
      return fetcher<Callback[]>(url('/api/admin/callbacks'), { headers: auth() })
    },
    setCallbackProcessed(id: number, isProcessed: boolean) {
      return fetcher<void>(url(`/api/admin/callbacks/${id}`), { method: 'PATCH', body: { isProcessed }, headers: auth() })
    },
    listContacts() {
      return fetcher<Contact[]>(url('/api/admin/contacts'), { headers: auth() })
    },
    setContactProcessed(id: number, isProcessed: boolean) {
      return fetcher<void>(url(`/api/admin/contacts/${id}`), { method: 'PATCH', body: { isProcessed }, headers: auth() })
    },
    uploadMedia(form: FormData) {
      return fetcher<MediaUploadResponse>(url('/api/admin/media/upload'), {
        method: 'POST', body: form, headers: auth(),
      })
    },
  }
}

export type AdminApi = ReturnType<typeof createAdminApi>
