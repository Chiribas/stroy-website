<script setup lang="ts">
const api = useApi()
const route = useRoute()
const slug = computed(() => String(route.params.slug))

const { data: service } = await useAsyncData(() => `service-${slug.value}`, () => api.getService(slug.value))
if (!service.value) throw createError({ statusCode: 404, statusMessage: 'Услуга не найдена' })

const { data: related } = await useAsyncData(
  () => `service-articles-${slug.value}`,
  () => service.value?.tag ? api.getArticles(1, 6, service.value.tag) : Promise.resolve(null),
)

useSeoMeta({
  title: () => `${service.value?.title} — Суровая Стройка`,
  description: () => service.value?.shortDescription ?? '',
})
</script>

<template>
  <div class="mx-auto max-w-3xl px-4 py-12">
    <h1 class="text-3xl font-bold text-ink">{{ service?.title }}</h1>
    <p v-if="service?.shortDescription" class="mt-2 text-muted">{{ service.shortDescription }}</p>
    <!-- eslint-disable-next-line vue/no-v-html -->
    <article class="prose dark:prose-invert mt-6 max-w-none" v-html="service?.content" />

    <section v-if="related && related.items.length" class="mt-12">
      <h2 class="text-2xl font-bold text-ink">Из практики по теме</h2>
      <div class="mt-6 grid gap-6 md:grid-cols-3">
        <PortfolioCard v-for="a in related.items" :key="a.id" :article="a" />
      </div>
    </section>

    <div class="mt-12">
      <NuxtLink to="/contact" class="inline-block rounded-card bg-brand px-6 py-3 font-medium text-brand-contrast hover:opacity-90">
        Обсудить задачу
      </NuxtLink>
    </div>
  </div>
</template>
