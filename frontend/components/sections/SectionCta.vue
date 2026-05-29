<script setup lang="ts">
const api = useApi()
</script>

<template>
  <section class="bg-brand/10">
    <div class="mx-auto max-w-3xl px-4 py-16 text-center">
      <h2 class="text-3xl font-bold text-ink">Перезвоните мне</h2>
      <p class="mt-2 text-muted">Оставьте телефон — перезвоним и проконсультируем.</p>
      <div class="mx-auto mt-6 max-w-md text-left">
        <CallbackForm :on-submit="api.sendCallback" />
      </div>
    </div>
  </section>
</template>
