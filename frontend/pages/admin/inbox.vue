<script setup lang="ts">
import type { Callback, Contact } from '~/types/admin'
definePageMeta({ layout: 'admin', middleware: 'auth' })
const api = useAdminApi()
const callbacks = ref<Callback[]>([])
const contacts = ref<Contact[]>([])

async function load() {
  [callbacks.value, contacts.value] = await Promise.all([api.listCallbacks(), api.listContacts()])
}
onMounted(load)

async function toggleCallback(c: Callback) {
  await api.setCallbackProcessed(c.id, !c.isProcessed)
  await load()
}
async function toggleContact(c: Contact) {
  await api.setContactProcessed(c.id, !c.isProcessed)
  await load()
}
</script>

<template>
  <div class="max-w-4xl space-y-8">
    <section>
      <h1 class="text-2xl font-bold mb-4">Звонки</h1>
      <div v-for="c in callbacks" :key="c.id" class="bg-surface-2 p-3 rounded shadow mb-2 flex justify-between"
           :class="{ 'opacity-50': c.isProcessed }">
        <div>{{ c.phone }} <span v-if="c.name" class="text-gray-500">— {{ c.name }}</span></div>
        <button class="text-blue-600" @click="toggleCallback(c)">{{ c.isProcessed ? 'Вернуть' : 'Обработано' }}</button>
      </div>
    </section>
    <section>
      <h2 class="text-2xl font-bold mb-4">Сообщения</h2>
      <div v-for="c in contacts" :key="c.id" class="bg-surface-2 p-3 rounded shadow mb-2"
           :class="{ 'opacity-50': c.isProcessed }">
        <div class="flex justify-between">
          <div class="font-medium">{{ c.name }} — {{ c.phone }}</div>
          <button class="text-blue-600" @click="toggleContact(c)">{{ c.isProcessed ? 'Вернуть' : 'Обработано' }}</button>
        </div>
        <p class="text-muted mt-1">{{ c.message }}</p>
      </div>
    </section>
  </div>
</template>
