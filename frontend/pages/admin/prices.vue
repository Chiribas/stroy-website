<script setup lang="ts">
import type { ServicePrice } from '~/types/api'
import type { ServicePriceWrite, MediaUploadResponse } from '~/types/admin'
definePageMeta({ layout: 'admin', middleware: 'auth' })
const api = useAdminApi()
const mediaUrl = useMediaUrl()
const items = ref<ServicePrice[]>([])
const editingId = ref<number | null>(null)
const blank = (): ServicePriceWrite => ({ title: '', photoPath: null, description: '', price: 0, duration: '', articleSlug: '', tag: '', sortOrder: 0 })
const draft = reactive<ServicePriceWrite>(blank())

async function load() { items.value = await api.listPrices() }
onMounted(load)

function edit(p: ServicePrice) {
  editingId.value = p.id
  Object.assign(draft, {
    title: p.title, photoPath: p.photoPath ?? null, description: p.description ?? '',
    price: p.price, duration: p.duration ?? '', articleSlug: p.articleSlug ?? '',
    tag: p.tag ?? '', sortOrder: p.sortOrder,
  })
}
function reset() { editingId.value = null; Object.assign(draft, blank()) }
function onPhoto(m: MediaUploadResponse) { draft.photoPath = m.thumbnailUrl ?? m.url }

async function save() {
  if (editingId.value) await api.updatePrice(editingId.value, { ...draft })
  else await api.createPrice({ ...draft })
  reset(); await load()
}
async function remove(id: number) {
  if (!confirm('Удалить пример?')) return
  await api.deletePrice(id); await load()
}
</script>

<template>
  <div class="max-w-4xl">
    <h1 class="text-2xl font-bold mb-6">Примеры работ и цен</h1>
    <table class="w-full bg-surface-2 rounded shadow mb-6">
      <thead><tr class="text-left border-b border-base"><th class="p-2">Порядок</th><th class="p-2">Название</th><th class="p-2">Цена</th><th class="p-2">Срок</th><th></th></tr></thead>
      <tbody>
        <tr v-for="p in items" :key="p.id" class="border-b border-base">
          <td class="p-2">{{ p.sortOrder }}</td><td class="p-2">{{ p.title }}</td>
          <td class="p-2">{{ p.price }}</td><td class="p-2">{{ p.duration }}</td>
          <td class="p-2 text-right space-x-3">
            <button class="text-blue-600" @click="edit(p)">Изм.</button>
            <button class="text-red-600" @click="remove(p.id)">Удалить</button>
          </td>
        </tr>
      </tbody>
    </table>

    <div class="bg-surface-2 p-4 rounded shadow space-y-3">
      <h2 class="font-semibold">{{ editingId ? 'Редактировать' : 'Добавить' }} пример</h2>
      <label class="block">
        <span class="mb-1 block text-sm font-medium text-ink">Название работы</span>
        <input v-model="draft.title" placeholder="напр. Замена фундамента дачного дома 4×5" class="border rounded px-2 py-1 w-full" />
      </label>
      <label class="block">
        <span class="mb-1 block text-sm font-medium text-ink">Короткое описание</span>
        <input v-model="draft.description" placeholder="напр. С бетонных блоков на винтовые сваи и швеллеры" class="border rounded px-2 py-1 w-full" />
      </label>
      <div class="grid grid-cols-3 gap-3">
        <label class="block">
          <span class="mb-1 block text-sm font-medium text-ink">Цена, ₽</span>
          <input v-model.number="draft.price" type="number" placeholder="340000" class="border rounded px-2 py-1 w-full" />
        </label>
        <label class="block">
          <span class="mb-1 block text-sm font-medium text-ink">Срок</span>
          <input v-model="draft.duration" placeholder="напр. 2 дня или 1 неделя" class="border rounded px-2 py-1 w-full" />
        </label>
        <label class="block">
          <span class="mb-1 block text-sm font-medium text-ink">Порядок</span>
          <input v-model.number="draft.sortOrder" type="number" placeholder="0" class="border rounded px-2 py-1 w-full" />
        </label>
        <label class="block">
          <span class="mb-1 block text-sm font-medium text-ink">Ссылка на статью (slug, опц.)</span>
          <input v-model="draft.articleSlug" placeholder="zamena-fundamenta-svai" class="border rounded px-2 py-1 w-full" />
        </label>
        <label class="block">
          <span class="mb-1 block text-sm font-medium text-ink">Тег (опц.)</span>
          <input v-model="draft.tag" placeholder="foundation" class="border rounded px-2 py-1 w-full" />
        </label>
      </div>
      <div>
        <span class="mb-1 block text-sm font-medium text-ink">Фото</span>
        <div class="flex items-center gap-3">
          <img v-if="draft.photoPath" :src="mediaUrl(draft.photoPath)" alt="" class="h-16 w-16 rounded object-cover" />
          <MediaUploader label="Загрузить фото" @uploaded="onPhoto" />
          <button v-if="draft.photoPath" type="button" class="text-sm text-red-600" @click="draft.photoPath = null">Убрать</button>
        </div>
      </div>
      <div class="flex gap-3">
        <button class="bg-gray-900 text-white py-2 px-4 rounded" @click="save">Сохранить</button>
        <button v-if="editingId" type="button" class="py-2 px-4 rounded border" @click="reset">Отмена</button>
      </div>
    </div>
  </div>
</template>
