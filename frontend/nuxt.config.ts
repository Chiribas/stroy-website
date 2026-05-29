export default defineNuxtConfig({
  compatibilityDate: '2025-01-01',
  devtools: { enabled: true },
  modules: ['@nuxtjs/tailwindcss', '@vueuse/nuxt', '@nuxtjs/sitemap'],
  components: [{ path: '~/components', pathPrefix: false }],
  css: ['~/assets/css/main.css'],
  site: { url: process.env.NUXT_PUBLIC_SITE_URL || 'http://localhost:3001' },
  devServer: { port: 3001 },
  nitro: {
    prerender: {
      crawlLinks: true,
      routes: ['/', '/prices', '/portfolio', '/contact'],
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
