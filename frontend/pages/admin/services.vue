<script setup lang="ts">
import type { AdminService, ServiceWrite } from '~/types/admin'
definePageMeta({ layout: 'admin', middleware: 'auth' })
const api = useAdminApi()
const items = ref<AdminService[]>([])
const editingId = ref<number | null>(null)
const blank = (): ServiceWrite => ({ title: '', slug: '', shortDescription: '', iconName: '', content: '', tag: '', sortOrder: 0, isPublished: true })
const draft = reactive<ServiceWrite>(blank())
const error = ref('')

async function load() { items.value = await api.listServices() }
onMounted(load)

function edit(s: AdminService) {
  editingId.value = s.id
  Object.assign(draft, {
    title: s.title, slug: s.slug, shortDescription: s.shortDescription ?? '', iconName: s.iconName ?? '',
    content: s.content, tag: s.tag ?? '', sortOrder: s.sortOrder, isPublished: s.isPublished,
  })
}
function reset() { editingId.value = null; error.value = ''; Object.assign(draft, blank()) }

async function save() {
  error.value = ''
  try {
    if (editingId.value) await api.updateService(editingId.value, { ...draft })
    else await api.createService({ ...draft })
    reset(); await load()
  } catch (e: any) {
    error.value = e?.statusCode === 409 ? 'Услуга с таким slug уже существует' : 'Ошибка сохранения'
  }
}
async function remove(id: number) {
  if (!confirm('Удалить услугу?')) return
  await api.deleteService(id); await load()
}
</script>

<template>
  <div class="max-w-4xl">
    <h1 class="text-2xl font-bold mb-6">Услуги</h1>
    <table class="w-full bg-surface-2 rounded shadow mb-6">
      <thead><tr class="text-left border-b border-base"><th class="p-2">Порядок</th><th class="p-2">Название</th><th class="p-2">Slug</th><th class="p-2">Тег</th><th></th></tr></thead>
      <tbody>
        <tr v-for="s in items" :key="s.id" class="border-b border-base">
          <td class="p-2">{{ s.sortOrder }}</td><td class="p-2">{{ s.title }}</td>
          <td class="p-2">{{ s.slug }}</td><td class="p-2">{{ s.tag }}</td>
          <td class="p-2 text-right space-x-3">
            <button class="text-blue-600" @click="edit(s)">Изм.</button>
            <button class="text-red-600" @click="remove(s.id)">Удалить</button>
          </td>
        </tr>
      </tbody>
    </table>

    <div class="bg-surface-2 p-4 rounded shadow space-y-3">
      <h2 class="font-semibold">{{ editingId ? 'Редактировать' : 'Добавить' }} услугу</h2>
      <div class="grid grid-cols-2 gap-3">
        <input v-model="draft.title" placeholder="Название" class="border rounded px-2 py-1" />
        <input v-model="draft.slug" placeholder="slug (a-z0-9-)" class="border rounded px-2 py-1" />
        <input v-model="draft.iconName" placeholder="иконка (home/hammer/layers/move/key/truck)" class="border rounded px-2 py-1" />
        <input v-model="draft.tag" placeholder="тег для статей" class="border rounded px-2 py-1" />
        <input v-model.number="draft.sortOrder" type="number" placeholder="Порядок" class="border rounded px-2 py-1" />
        <label class="flex items-center gap-2"><input v-model="draft.isPublished" type="checkbox" /> Опубликовано</label>
      </div>
      <input v-model="draft.shortDescription" placeholder="Короткое описание" class="border rounded px-2 py-1 w-full" />
      <ArticleEditor v-model="draft.content" />
      <p v-if="error" class="text-red-600">{{ error }}</p>
      <div class="flex gap-3">
        <button class="bg-gray-900 text-white py-2 px-4 rounded" @click="save">Сохранить</button>
        <button v-if="editingId" type="button" class="py-2 px-4 rounded border" @click="reset">Отмена</button>
      </div>
    </div>
  </div>
</template>
