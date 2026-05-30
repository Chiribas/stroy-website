<script setup lang="ts">
import type { ArticleWrite } from '~/types/admin'
definePageMeta({ layout: 'admin', middleware: 'auth' })
const api = useAdminApi()
const mediaUrl = useMediaUrl()
const route = useRoute()
const id = Number(route.params.id)
const error = ref('')
const form = reactive<ArticleWrite>({
  title: '', slug: '', summary: '', content: '', thumbnailPath: '', isPublished: false, tags: '',
})

onMounted(async () => {
  const a = await api.getArticle(id)
  Object.assign(form, {
    title: a.title, slug: a.slug, summary: a.summary ?? '', content: a.content,
    thumbnailPath: a.thumbnailPath ?? '', isPublished: !!a.publishedAt, tags: a.tags ?? '',
  })
})

async function save() {
  error.value = ''
  try {
    await api.updateArticle(id, { ...form })
    await navigateTo('/admin/articles')
  } catch (e: any) {
    error.value = e?.statusCode === 409 ? 'Статья с таким slug уже существует' : 'Ошибка сохранения'
  }
}
</script>

<template>
  <div class="max-w-3xl">
    <h1 class="text-2xl font-bold mb-6">Редактирование статьи</h1>
    <div class="space-y-4">
      <input v-model="form.title" placeholder="Заголовок" class="w-full border rounded px-3 py-2" />
      <input v-model="form.slug" placeholder="slug-stati" class="w-full border rounded px-3 py-2" />
      <textarea v-model="form.summary" placeholder="Краткое описание" class="w-full border rounded px-3 py-2" />
      <input v-model="form.tags" placeholder="Теги через запятую (напр. foundation,remont)" class="w-full border rounded px-3 py-2" />
      <div>
        <div class="text-sm text-muted mb-1">Главная картинка (превью в портфолио)</div>
        <div class="flex items-center gap-3">
          <img v-if="form.thumbnailPath" :src="mediaUrl(form.thumbnailPath)" class="h-20 w-32 rounded border object-cover" />
          <div v-else class="h-20 w-32 rounded border border-base bg-surface flex items-center justify-center text-xs text-muted">нет фото</div>
          <MediaUploader label="Загрузить превью" @uploaded="(m) => form.thumbnailPath = m.thumbnailUrl" />
          <button v-if="form.thumbnailPath" type="button" class="text-red-600 text-sm" @click="form.thumbnailPath = ''">Убрать</button>
        </div>
      </div>
      <ArticleEditor v-model="form.content" :article-id="id" />
      <label class="flex items-center gap-2"><input v-model="form.isPublished" type="checkbox" /> Опубликовано</label>
      <p v-if="error" class="text-red-600">{{ error }}</p>
      <button class="bg-gray-900 text-white px-4 py-2 rounded" @click="save">Сохранить</button>
    </div>
  </div>
</template>
