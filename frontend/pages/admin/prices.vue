<script setup lang="ts">
import type { ServicePrice } from '~/types/api'
import type { ServicePriceWrite } from '~/types/admin'
definePageMeta({ layout: 'admin', middleware: 'auth' })
const api = useAdminApi()
const items = ref<ServicePrice[]>([])
const draft = reactive<ServicePriceWrite>({
  category: '', name: '', description: '', priceFrom: 0, priceTo: null, unit: '', sortOrder: 0,
})

async function load() { items.value = await api.listPrices() }
onMounted(load)

async function add() {
  await api.createPrice({ ...draft })
  Object.assign(draft, { category: '', name: '', description: '', priceFrom: 0, priceTo: null, unit: '', sortOrder: 0 })
  await load()
}
async function remove(id: number) {
  if (!confirm('Удалить позицию?')) return
  await api.deletePrice(id)
  await load()
}
</script>

<template>
  <div class="max-w-4xl">
    <h1 class="text-2xl font-bold mb-6">Цены</h1>
    <table class="w-full bg-white rounded shadow mb-6">
      <thead><tr class="text-left border-b"><th class="p-2">Категория</th><th class="p-2">Услуга</th><th class="p-2">От</th><th class="p-2">До</th><th class="p-2">Ед.</th><th></th></tr></thead>
      <tbody>
        <tr v-for="p in items" :key="p.id" class="border-b">
          <td class="p-2">{{ p.category }}</td><td class="p-2">{{ p.name }}</td>
          <td class="p-2">{{ p.priceFrom }}</td><td class="p-2">{{ p.priceTo ?? '—' }}</td><td class="p-2">{{ p.unit }}</td>
          <td class="p-2 text-right"><button class="text-red-600" @click="remove(p.id)">Удалить</button></td>
        </tr>
      </tbody>
    </table>
    <div class="bg-white p-4 rounded shadow grid grid-cols-3 gap-3">
      <input v-model="draft.category" placeholder="Категория" class="border rounded px-2 py-1" />
      <input v-model="draft.name" placeholder="Услуга" class="border rounded px-2 py-1" />
      <input v-model="draft.unit" placeholder="Ед. изм." class="border rounded px-2 py-1" />
      <input v-model.number="draft.priceFrom" type="number" placeholder="Цена от" class="border rounded px-2 py-1" />
      <input v-model.number="draft.priceTo" type="number" placeholder="Цена до" class="border rounded px-2 py-1" />
      <input v-model.number="draft.sortOrder" type="number" placeholder="Порядок" class="border rounded px-2 py-1" />
      <button class="col-span-3 bg-gray-900 text-white py-2 rounded" @click="add">Добавить</button>
    </div>
  </div>
</template>
