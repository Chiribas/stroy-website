export default defineNuxtConfig({
  compatibilityDate: '2025-01-01',
  devtools: { enabled: true },
  modules: ['@nuxtjs/tailwindcss', '@vueuse/nuxt', '@nuxtjs/sitemap'],
  components: [{ path: '~/components', pathPrefix: false }],
  css: ['~/assets/css/main.css'],
  site: { url: process.env.NUXT_PUBLIC_SITE_URL || 'http://localhost:3001' },
  devServer: { port: 3001 },
  routeRules: {
    '/admin/**': { ssr: false },
  },
  sitemap: {
    exclude: ['/admin/**'],
  },
  nitro: {
    // Dev only: proxy same-origin /uploads to the backend so relative image URLs
    // (stored in article content) resolve in dev exactly as they do behind nginx in prod.
    devProxy: {
      '/uploads': { target: 'http://localhost:8081/uploads', changeOrigin: true },
    },
    // НЕ пре-рендерим публичные страницы: работаем в режиме живого SSR (node-сервер),
    // чтобы контент из админки появлялся сразу, без пересборки образа.
    // (Раньше здесь был prerender.routes — он запекал статику на этапе build,
    //  из-за чего /portfolio показывал пустой снимок времён сборки.)
  },
  runtimeConfig: {
    public: {
      apiBase: 'http://localhost:8081',
      apiClientBase: 'http://localhost:8081',
      siteUrl: 'http://localhost:3001',
    },
  },
})
