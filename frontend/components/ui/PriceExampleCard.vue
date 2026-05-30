<script setup lang="ts">
import { Clock } from 'lucide-vue-next'
import type { ServicePrice } from '~/types/api'
import { formatPrice, formatDuration } from '~/lib/prices'
const props = defineProps<{ item: ServicePrice }>()
const mediaUrl = useMediaUrl()
// Реальное фото (из /uploads) — через mediaUrl; иначе локальный плейсхолдер (фронт-статика).
const photo = computed(() => props.item.photoPath ? mediaUrl(props.item.photoPath) : '/images/placeholder.svg')
</script>

<template>
  <div class="overflow-hidden rounded-card border border-base bg-surface-2">
    <div class="aspect-[4/3] w-full overflow-hidden bg-surface">
      <img :src="photo" :alt="item.title" class="h-full w-full object-cover" loading="lazy" />
    </div>
    <div class="p-5">
      <h3 class="text-lg font-semibold text-ink">{{ item.title }}</h3>
      <p v-if="item.description" class="mt-1 text-sm text-muted">{{ item.description }}</p>
      <div class="mt-3 flex items-center justify-between">
        <span class="font-bold text-brand">{{ formatPrice(item) }}</span>
        <span v-if="item.duration" class="flex items-center gap-1 text-sm text-muted" title="Срок выполнения">
          <Clock class="h-4 w-4" />{{ formatDuration(item.duration) }}
        </span>
      </div>
      <NuxtLink v-if="item.articleSlug" :to="`/portfolio/${item.articleSlug}`" class="mt-3 inline-block text-sm text-brand hover:underline">
        Подробнее →
      </NuxtLink>
    </div>
  </div>
</template>
