<script setup lang="ts">
import type { ServiceListItem } from '~/types/api'
defineProps<{ title?: string; services: ServiceListItem[] }>()
</script>

<template>
  <section class="mx-auto max-w-6xl px-4 py-16">
    <div class="flex items-center justify-between">
      <h2 class="text-3xl font-bold text-ink">{{ title ?? 'Услуги' }}</h2>
      <NuxtLink to="/services" class="text-brand hover:underline">Все услуги →</NuxtLink>
    </div>
    <div v-if="services.length" class="mt-8 grid gap-6 md:grid-cols-3">
      <ServiceCard v-for="s in services" :key="s.id" :service="s" />
    </div>
    <p v-else class="mt-8 text-muted">Список услуг скоро появится.</p>
  </section>
</template>
