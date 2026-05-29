import { createApi } from '~/lib/api'

export function useApi() {
  const config = useRuntimeConfig()
  const baseURL = import.meta.server
    ? config.public.apiBase
    : config.public.apiClientBase
  return createApi($fetch as any, baseURL)
}
