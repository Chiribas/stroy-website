<script setup lang="ts">
const api = useApi()
const c = useContacts()

useSeoMeta({
  title: 'Контакты',
  description: `Свяжитесь с нами: ${c.phone}, ${c.email}`,
})
</script>

<template>
  <div class="mx-auto max-w-6xl px-4 py-12">
    <h1 class="text-3xl font-bold text-ink">Контакты</h1>
    <div class="mt-8 grid gap-12 md:grid-cols-2">
      <div class="space-y-3 text-ink">
        <p><a :href="`tel:${c.phone}`" class="text-lg text-brand">{{ c.phone }}</a></p>
        <p><a :href="`mailto:${c.email}`" class="text-brand">{{ c.email }}</a></p>
        <p>{{ c.address }}</p>
        <p class="text-muted">{{ c.schedule }}</p>
      </div>
      <div>
        <h2 class="text-xl font-semibold text-ink">Написать нам</h2>
        <div class="mt-4">
          <ContactForm :on-submit="api.sendContact" />
        </div>
      </div>
    </div>
  </div>
</template>
