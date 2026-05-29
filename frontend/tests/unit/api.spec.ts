import { describe, it, expect, vi } from 'vitest'
import { createApi } from '~/lib/api'

describe('createApi', () => {
  it('запрашивает статьи с page/pageSize в query и абсолютным baseURL', async () => {
    const fetcher = vi.fn().mockResolvedValue({ items: [], total: 0, page: 2, pageSize: 6 })
    const api = createApi(fetcher, 'http://api.test')

    const result = await api.getArticles(2, 6)

    expect(fetcher).toHaveBeenCalledWith('http://api.test/api/articles', {
      query: { page: 2, pageSize: 6 },
    })
    expect(result.page).toBe(2)
  })

  it('запрашивает статью по slug', async () => {
    const fetcher = vi.fn().mockResolvedValue({ slug: 'dom', title: 'Дом' })
    const api = createApi(fetcher, 'http://api.test')

    await api.getArticle('dom')

    expect(fetcher).toHaveBeenCalledWith('http://api.test/api/articles/dom')
  })

  it('постит callback методом POST с body', async () => {
    const fetcher = vi.fn().mockResolvedValue({ message: 'ок' })
    const api = createApi(fetcher, 'http://api.test')

    await api.sendCallback({ phone: '+79991234567' })

    expect(fetcher).toHaveBeenCalledWith('http://api.test/api/callbacks', {
      method: 'POST',
      body: { phone: '+79991234567' },
    })
  })
})
