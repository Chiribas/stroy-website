<script setup lang="ts">
import type { ArticleListItem, PagedResult } from '~/types/api'
definePageMeta({ layout: 'admin', middleware: 'auth' })
const api = useAdminApi()
const data = ref<PagedResult<ArticleListItem> | null>(null)

async function load() { data.value = await api.listArticles(1, 50) }
onMounted(load)

async function remove(id: number) {
  if (!confirm('Удалить статью?')) return
  await api.deleteArticle(id)
  await load()
}
</script>

<template>
  <div>
    <div class="flex justify-between items-center mb-6">
      <h1 class="text-2xl font-bold">Статьи</h1>
      <NuxtLink to="/admin/articles/new" class="bg-gray-900 text-white px-4 py-2 rounded">Новая статья</NuxtLink>
    </div>
    <table class="w-full bg-white rounded shadow">
      <thead><tr class="text-left border-b"><th class="p-3">Заголовок</th><th class="p-3">Статус</th><th class="p-3"></th></tr></thead>
      <tbody>
        <tr v-for="a in data?.items ?? []" :key="a.id" class="border-b">
          <td class="p-3">{{ a.title }}</td>
          <td class="p-3">
            <span :class="a.publishedAt ? 'text-green-600' : 'text-gray-400'">
              {{ a.publishedAt ? 'Опубликовано' : 'Черновик' }}
            </span>
          </td>
          <td class="p-3 text-right space-x-3">
            <NuxtLink :to="`/admin/articles/${a.id}`" class="text-blue-600">Изменить</NuxtLink>
            <button class="text-red-600" @click="remove(a.id)">Удалить</button>
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>
