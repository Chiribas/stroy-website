<script setup lang="ts">
const c = useContacts()
</script>

<template>
  <section class="bg-gradient-to-br from-ink to-gray-700 text-white">
    <div class="mx-auto max-w-6xl px-4 py-24 text-center">
      <h1 class="text-4xl font-bold md:text-5xl">{{ c.name }}</h1>
      <p class="mx-auto mt-4 max-w-2xl text-lg text-gray-200">{{ c.tagline }}</p>
      <NuxtLink to="/contact" class="mt-8 inline-block rounded-card bg-brand px-6 py-3 font-medium hover:bg-brand-dark">
        Оставить заявку
      </NuxtLink>
    </div>
  </section>
</template>
