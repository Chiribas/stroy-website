<script setup lang="ts">
const api = useApi()
const route = useRoute()
const mediaUrl = useMediaUrl()
const slug = route.params.slug as string

const { data: article } = await useAsyncData(`article-${slug}`, () => api.getArticle(slug))

if (!article.value) {
  throw createError({ statusCode: 404, statusMessage: 'Статья не найдена', fatal: true })
}
const sortedMedia = computed(() =>
  [...(article.value?.media ?? [])].sort((a, b) => a.sortOrder - b.sortOrder),
)

useSeoMeta({
  title: () => article.value?.title ?? 'Проект',
  description: () => article.value?.summary ?? '',
  ogTitle: () => article.value?.title ?? '',
  ogImage: () => (article.value?.thumbnailPath ? mediaUrl(article.value.thumbnailPath) : undefined),
})
</script>

<template>
  <article v-if="article" class="mx-auto max-w-3xl px-4 py-12">
    <NuxtLink to="/portfolio" class="text-brand hover:underline">← Все проекты</NuxtLink>
    <h1 class="mt-4 text-3xl font-bold text-ink">{{ article.title }}</h1>
    <p v-if="article.summary" class="mt-2 text-lg text-muted">{{ article.summary }}</p>

    <!-- Контент санитайзится на бэкенде (HtmlSanitizer), v-html безопасен -->
    <div class="prose mt-8 max-w-none" v-html="article.content" />

    <div v-if="sortedMedia.length" class="mt-10 grid gap-4 sm:grid-cols-2">
      <img
        v-for="m in sortedMedia"
        :key="m.id"
        :src="mediaUrl(m.path)"
        :alt="m.alt ?? article.title"
        class="w-full rounded-card object-cover"
      />
    </div>
  </article>
</template>
