<script setup lang="ts">
defineProps<{ title?: string }>()
const services = [
  { icon: '🏠', title: 'Строительство', text: 'Дома, коттеджи, пристройки под ключ.' },
  { icon: '🧱', title: 'Фасадные работы', text: 'Утепление, отделка, вентфасады.' },
  { icon: '🛠️', title: 'Ремонт', text: 'Капитальный и косметический ремонт.' },
]
</script>

<template>
  <section class="mx-auto max-w-6xl px-4 py-16">
    <h2 class="text-3xl font-bold text-ink">{{ title ?? 'Услуги' }}</h2>
    <div class="mt-8 grid gap-6 md:grid-cols-3">
      <div v-for="s in services" :key="s.title" class="rounded-card border p-6">
        <div class="text-3xl">{{ s.icon }}</div>
        <h3 class="mt-3 text-lg font-semibold text-ink">{{ s.title }}</h3>
        <p class="mt-1 text-muted">{{ s.text }}</p>
      </div>
    </div>
  </section>
</template>
