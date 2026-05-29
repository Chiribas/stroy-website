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
    prerender: {
      crawlLinks: true,
      routes: ['/', '/prices', '/portfolio', '/contact'],
      ignore: ['/admin'],
      failOnError: false,
    },
  },
  runtimeConfig: {
    public: {
      apiBase: 'http://localhost:8081',
      apiClientBase: 'http://localhost:8081',
      siteUrl: 'http://localhost:3001',
    },
  },
})
