<script setup lang="ts">
const api = useApi()
const { data } = await useAsyncData('prices', () => api.getPrices())

useSeoMeta({
  title: 'Примеры работ и цен — Суровая Стройка',
  description: 'Реальные примеры выполненных работ с ценами и сроками.',
})
</script>

<template>
  <div class="mx-auto max-w-5xl px-4 py-12">
    <h1 class="text-3xl font-bold text-ink">Примеры работ и цен</h1>
    <p class="mt-3 text-muted">Точную цену назовём после обсуждения задачи. Ниже — реальные примеры.</p>
    <div v-if="data && data.length" class="mt-8 grid gap-6 md:grid-cols-2 lg:grid-cols-3">
      <PriceExampleCard v-for="p in data" :key="p.id" :item="p" />
    </div>
    <p v-else class="mt-8 text-muted">Примеры скоро появятся. Свяжитесь с нами для расчёта.</p>
  </div>
</template>
