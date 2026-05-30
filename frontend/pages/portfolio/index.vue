<script setup lang="ts">
const api = useApi()
const route = useRoute()
const page = computed(() => Math.max(1, Number(route.query.page) || 1))
const pageSize = 12

const { data } = await useAsyncData(
  () => `portfolio-${page.value}`,
  () => api.getArticles(page.value, pageSize),
  { watch: [page] },
)

const totalPages = computed(() =>
  data.value ? Math.max(1, Math.ceil(data.value.total / data.value.pageSize)) : 1,
)

useSeoMeta({
  title: 'Из практики — Суровая Стройка',
  description: 'Проделанные работы и полезные решения: фундаменты, пристройки, передвижка построек.',
})
</script>

<template>
  <div class="mx-auto max-w-6xl px-4 py-12">
    <h1 class="text-3xl font-bold text-ink">Из практики</h1>

    <div v-if="data && data.items.length" class="mt-8 grid gap-6 md:grid-cols-3">
      <PortfolioCard v-for="a in data.items" :key="a.id" :article="a" />
    </div>
    <p v-else class="mt-8 text-muted">Скоро здесь появятся материалы.</p>

    <nav v-if="totalPages > 1" class="mt-10 flex justify-center gap-2">
      <NuxtLink
        v-for="p in totalPages"
        :key="p"
        :to="p === 1 ? '/portfolio' : `/portfolio?page=${p}`"
        class="rounded-card border border-base px-4 py-2"
        :class="p === page ? 'bg-brand text-brand-contrast' : 'text-ink hover:border-brand'"
      >
        {{ p }}
      </NuxtLink>
    </nav>
  </div>
</template>
