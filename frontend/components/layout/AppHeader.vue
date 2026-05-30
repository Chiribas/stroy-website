<script setup lang="ts">
const c = useContacts()
const links = [
  { to: '/', label: 'Главная' },
  { to: '/services', label: 'Услуги' },
  { to: '/portfolio', label: 'Из практики' },
  { to: '/prices', label: 'Примеры работ и цен' },
  { to: '/contact', label: 'Контакты' },
]
</script>

<template>
  <header class="border-b border-base bg-surface-2">
    <div class="mx-auto flex max-w-6xl items-center justify-between px-4 py-4">
      <NuxtLink to="/" class="text-xl font-bold text-brand">{{ c.name }}</NuxtLink>
      <nav class="hidden gap-6 md:flex">
        <NuxtLink v-for="l in links" :key="l.to" :to="l.to" class="text-ink hover:text-brand">
          {{ l.label }}
        </NuxtLink>
      </nav>
      <div class="flex items-center gap-3">
        <a :href="`tel:${c.phone}`" class="hidden font-medium text-brand sm:inline">{{ c.phone }}</a>
        <ThemeToggle />
      </div>
    </div>
  </header>
</template>
