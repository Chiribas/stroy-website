<script setup lang="ts">
const c = useContacts()
</script>

<template>
  <footer class="mt-16 border-t border-base bg-surface-2 text-ink">
    <div class="mx-auto grid max-w-6xl gap-6 px-4 py-10 md:grid-cols-3">
      <div>
        <div class="text-lg font-bold">{{ c.name }}</div>
        <p class="mt-2 text-sm text-muted">{{ c.tagline }}</p>
      </div>
      <div class="text-sm text-muted">
        <p>{{ c.address }}</p>
      </div>
      <div class="text-sm">
        <a :href="`tel:${c.phone}`" class="block text-ink hover:text-brand">{{ c.phone }}</a>
        <a :href="`mailto:${c.email}`" class="block text-ink hover:text-brand">{{ c.email }}</a>
      </div>
    </div>
  </footer>
</template>
