<script setup lang="ts">
import type { ArticleWrite } from '~/types/admin'
import { slugify } from '~/lib/slug'
definePageMeta({ layout: 'admin', middleware: 'auth' })
const api = useAdminApi()
const mediaUrl = useMediaUrl()
const error = ref('')
const form = reactive<ArticleWrite>({
  title: '', slug: '', summary: '', content: '', thumbnailPath: '', isPublished: false, tags: '',
})

// Авто-генерация slug из заголовка, пока юзер не правил slug руками.
const slugEdited = ref(false)
watch(() => form.title, (t) => { if (!slugEdited.value) form.slug = slugify(t) })

function describeError(e: any): string {
  if (e?.statusCode === 409) return 'Статья с таким slug уже существует'
  const errs = e?.data?.errors
  if (errs) return Object.values(errs).flat().join('; ')
  return e?.data?.title || 'Ошибка сохранения'
}

async function save() {
  error.value = ''
  try {
    const created = await api.createArticle({ ...form })
    await navigateTo(`/admin/articles/${created.id}`)
  } catch (e: any) {
    error.value = describeError(e)
  }
}
</script>

<template>
  <div class="max-w-3xl">
    <h1 class="text-2xl font-bold mb-6">Новая статья</h1>
    <div class="space-y-4">
      <label class="block">
        <span class="mb-1 block text-sm font-medium text-ink">Заголовок</span>
        <input v-model="form.title" placeholder="напр. Замена фундамента на сваи" class="w-full border rounded px-3 py-2" />
      </label>
      <label class="block">
        <span class="mb-1 block text-sm font-medium text-ink">Slug (адрес статьи — генерится из заголовка, только латиница)</span>
        <input v-model="form.slug" @input="slugEdited = true" placeholder="zamena-fundamenta-svai" class="w-full border rounded px-3 py-2" />
      </label>
      <label class="block">
        <span class="mb-1 block text-sm font-medium text-ink">Краткое описание (анонс в ленте)</span>
        <textarea v-model="form.summary" placeholder="1–2 предложения о статье" class="w-full border rounded px-3 py-2" />
      </label>
      <label class="block">
        <span class="mb-1 block text-sm font-medium text-ink">Теги (через запятую — связь с услугами/ценами)</span>
        <input v-model="form.tags" placeholder="foundation, remont" class="w-full border rounded px-3 py-2" />
      </label>
      <div>
        <div class="text-sm text-muted mb-1">Главная картинка (превью в портфолио)</div>
        <div class="flex items-center gap-3">
          <img v-if="form.thumbnailPath" :src="mediaUrl(form.thumbnailPath)" class="h-20 w-32 rounded border object-cover" />
          <div v-else class="h-20 w-32 rounded border border-base bg-surface flex items-center justify-center text-xs text-muted">нет фото</div>
          <MediaUploader label="Загрузить превью" @uploaded="(m) => form.thumbnailPath = m.thumbnailUrl" />
          <button v-if="form.thumbnailPath" type="button" class="text-red-600 text-sm" @click="form.thumbnailPath = ''">Убрать</button>
        </div>
      </div>
      <div>
        <span class="mb-1 block text-sm font-medium text-ink">Текст статьи</span>
        <ArticleEditor v-model="form.content" />
      </div>
      <label class="flex items-center gap-2"><input v-model="form.isPublished" type="checkbox" /> Опубликовать</label>
      <p v-if="error" class="text-red-600">{{ error }}</p>
      <button class="bg-gray-900 text-white px-4 py-2 rounded" @click="save">Сохранить</button>
    </div>
  </div>
</template>
