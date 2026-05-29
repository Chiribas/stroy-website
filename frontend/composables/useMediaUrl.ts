// Строит URL медиа: абсолютные ссылки (http/https) отдаёт как есть,
// относительные пути дополняет базовым URL API (медиа раздаёт бэкенд/nginx).
export function useMediaUrl() {
  const config = useRuntimeConfig()
  return (path: string) =>
    /^https?:\/\//i.test(path) ? path : `${config.public.apiClientBase}${path}`
}
