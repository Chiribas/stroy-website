<script setup lang="ts">
import { computed } from 'vue'
import type { ArticleListItem } from '~/types/api'
const props = defineProps<{ article: ArticleListItem }>()
const mediaUrl = useMediaUrl()
const img = computed(() =>
  props.article.thumbnailPath ? mediaUrl(props.article.thumbnailPath) : null,
)
</script>

<template>
  <NuxtLink :to="`/portfolio/${article.slug}`" class="group block overflow-hidden rounded-card border border-base bg-surface-2">
    <div class="aspect-video bg-surface">
      <img v-if="img" :src="img" :alt="article.title" class="h-full w-full object-cover transition group-hover:scale-105" />
    </div>
    <div class="p-4">
      <h3 class="font-semibold text-ink group-hover:text-brand">{{ article.title }}</h3>
      <p v-if="article.summary" class="mt-1 line-clamp-2 text-sm text-muted">{{ article.summary }}</p>
    </div>
  </NuxtLink>
</template>
