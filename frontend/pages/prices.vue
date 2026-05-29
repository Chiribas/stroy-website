<script setup lang="ts">
import { groupByCategory } from '~/lib/prices'
const api = useApi()
const { data } = await useAsyncData('prices', () => api.getPrices())
const groups = computed(() => groupByCategory(data.value ?? []))

useSeoMeta({
  title: 'Цены на строительные работы',
  description: 'Актуальные цены на строительство, фасадные работы и ремонт.',
})
</script>

<template>
  <div class="mx-auto max-w-4xl px-4 py-12">
    <h1 class="text-3xl font-bold text-ink">Цены</h1>
    <PriceTable v-if="groups.length" :groups="groups" class="mt-8" />
    <p v-else class="mt-8 text-muted">Прайс уточняется. Свяжитесь с нами для расчёта.</p>
  </div>
</template>
