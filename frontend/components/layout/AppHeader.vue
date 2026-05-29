<script setup lang="ts">
const c = useContacts()
const links = [
  { to: '/', label: 'Главная' },
  { to: '/prices', label: 'Цены' },
  { to: '/portfolio', label: 'Портфолио' },
  { to: '/contact', label: 'Контакты' },
]
</script>

<template>
  <header class="border-b bg-white">
    <div class="mx-auto flex max-w-6xl items-center justify-between px-4 py-4">
      <NuxtLink to="/" class="text-xl font-bold text-brand">{{ c.name }}</NuxtLink>
      <nav class="hidden gap-6 md:flex">
        <NuxtLink v-for="l in links" :key="l.to" :to="l.to" class="text-ink hover:text-brand">
          {{ l.label }}
        </NuxtLink>
      </nav>
      <a :href="`tel:${c.phone}`" class="font-medium text-brand">{{ c.phone }}</a>
    </div>
  </header>
</template>
