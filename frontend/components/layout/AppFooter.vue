<script setup lang="ts">
const c = useContacts()
</script>

<template>
  <footer class="mt-16 border-t bg-ink text-white">
    <div class="mx-auto grid max-w-6xl gap-6 px-4 py-10 md:grid-cols-3">
      <div>
        <div class="text-lg font-bold">{{ c.name }}</div>
        <p class="mt-2 text-sm text-gray-300">{{ c.tagline }}</p>
      </div>
      <div class="text-sm text-gray-300">
        <p>{{ c.address }}</p>
        <p>{{ c.schedule }}</p>
      </div>
      <div class="text-sm">
        <a :href="`tel:${c.phone}`" class="block hover:text-brand-light">{{ c.phone }}</a>
        <a :href="`mailto:${c.email}`" class="block hover:text-brand-light">{{ c.email }}</a>
      </div>
    </div>
  </footer>
</template>
