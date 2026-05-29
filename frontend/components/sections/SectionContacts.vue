<script setup lang="ts">
const c = useContacts()
</script>

<template>
  <section class="mx-auto max-w-6xl px-4 py-16">
    <h2 class="text-3xl font-bold text-ink">Контакты</h2>
    <div class="mt-6 space-y-2 text-ink">
      <p><a :href="`tel:${c.phone}`" class="text-brand">{{ c.phone }}</a></p>
      <p><a :href="`mailto:${c.email}`" class="text-brand">{{ c.email }}</a></p>
      <p>{{ c.address }}</p>
      <p class="text-muted">{{ c.schedule }}</p>
    </div>
  </section>
</template>
