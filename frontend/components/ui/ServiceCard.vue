<script setup lang="ts">
import type { ServiceListItem } from '~/types/api'
defineProps<{ service: ServiceListItem }>()
</script>

<template>
  <NuxtLink :to="`/services/${service.slug}`" class="block rounded-card border border-base bg-surface-2 p-6 transition hover:border-brand">
    <Icon v-if="service.iconName" :name="service.iconName" class="h-8 w-8 text-brand" />
    <h3 class="mt-3 text-lg font-semibold text-ink">{{ service.title }}</h3>
    <p v-if="service.shortDescription" class="mt-1 text-muted">{{ service.shortDescription }}</p>
  </NuxtLink>
</template>
