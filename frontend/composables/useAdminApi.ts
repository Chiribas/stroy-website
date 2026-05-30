import { createAdminApi } from '~/lib/adminApi'

export function useAdminApi() {
  const config = useRuntimeConfig()
  const auth = useAuth()
  const baseURL = config.public.apiClientBase

  // Перехватываем 401 (протух/невалиден токен): разлогиниваем и уводим на логин,
  // чтобы вместо пустых страниц юзер видел экран входа. Сам запрос логина пропускаем —
  // его 401 обрабатывает страница входа («Неверный логин или пароль»).
  const fetcher = async (url: string, opts?: Record<string, unknown>) => {
    try {
      return await ($fetch as any)(url, opts)
    } catch (e: any) {
      const status = e?.response?.status ?? e?.statusCode ?? e?.status
      if (status === 401 && !url.includes('/api/admin/auth')) {
        auth.logout()
        await navigateTo('/admin/login')
      }
      throw e
    }
  }

  return createAdminApi(fetcher as any, baseURL, auth.getToken)
}
