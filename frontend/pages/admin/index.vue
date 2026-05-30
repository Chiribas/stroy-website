<script setup lang="ts">
definePageMeta({ layout: 'admin', middleware: 'auth' })
const api = useAdminApi()
const articles = ref(0)
const newCallbacks = ref(0)
const newContacts = ref(0)

onMounted(async () => {
  const [a, cb, ct] = await Promise.all([api.listArticles(1, 1), api.listCallbacks(), api.listContacts()])
  articles.value = a.total
  newCallbacks.value = cb.filter(x => !x.isProcessed).length
  newContacts.value = ct.filter(x => !x.isProcessed).length
})
</script>

<template>
  <div>
    <h1 class="text-2xl font-bold mb-6">Дашборд</h1>
    <div class="grid grid-cols-3 gap-4">
      <div class="rounded border border-base bg-surface-2 p-4 shadow"><div class="text-3xl font-bold">{{ articles }}</div><div class="text-muted">Статей</div></div>
      <div class="rounded border border-base bg-surface-2 p-4 shadow"><div class="text-3xl font-bold">{{ newCallbacks }}</div><div class="text-muted">Новых звонков</div></div>
      <div class="rounded border border-base bg-surface-2 p-4 shadow"><div class="text-3xl font-bold">{{ newContacts }}</div><div class="text-muted">Новых сообщений</div></div>
    </div>
  </div>
</template>
