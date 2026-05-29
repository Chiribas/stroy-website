import { createAdminApi } from '~/lib/adminApi'

export function useAdminApi() {
  const config = useRuntimeConfig()
  const auth = useAuth()
  const baseURL = config.public.apiClientBase
  return createAdminApi($fetch as any, baseURL, auth.getToken)
}
