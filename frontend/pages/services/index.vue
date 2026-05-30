<script setup lang="ts">
const api = useApi()
const { data } = await useAsyncData('services', () => api.getServices())
useSeoMeta({
  title: 'Услуги — Суровая Стройка',
  description: 'Строительство, ремонт и пристройки, замена фундамента, передвижка построек, под ключ.',
})
</script>

<template>
  <div class="mx-auto max-w-6xl px-4 py-12">
    <h1 class="text-3xl font-bold text-ink">Услуги</h1>
    <div v-if="data && data.length" class="mt-8 grid gap-6 md:grid-cols-3">
      <ServiceCard v-for="s in data" :key="s.id" :service="s" />
    </div>
    <p v-else class="mt-8 text-muted">Список услуг скоро появится.</p>
  </div>
</template>
