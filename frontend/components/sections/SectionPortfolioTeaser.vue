<script setup lang="ts">
import type { ArticleListItem } from '~/types/api'
defineProps<{ items: ArticleListItem[] }>()
</script>

<template>
  <section class="mx-auto max-w-6xl px-4 py-16">
    <div class="flex items-center justify-between">
      <h2 class="text-3xl font-bold text-ink">Из практики</h2>
      <NuxtLink to="/portfolio" class="text-brand hover:underline">Все материалы →</NuxtLink>
    </div>
    <div v-if="items.length" class="mt-8 grid gap-6 md:grid-cols-3">
      <PortfolioCard v-for="a in items" :key="a.id" :article="a" />
    </div>
    <p v-else class="mt-8 text-muted">Скоро здесь появятся материалы.</p>
  </section>
</template>
