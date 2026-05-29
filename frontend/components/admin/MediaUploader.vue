<script setup lang="ts">
import type { MediaUploadResponse } from '~/types/admin'

const props = withDefaults(defineProps<{ articleId?: number; label?: string }>(), {
  label: 'Вставить фото',
})
const emit = defineEmits<{ uploaded: [media: MediaUploadResponse] }>()
const api = useAdminApi()
const uploading = ref(false)
const error = ref('')
// Unique id so the <label for> natively triggers THIS input (pages are client-only,
// so a random id can't cause hydration mismatch). Native label/for needs no JS and
// works in every browser, unlike a programmatic input.click().
const inputId = `media-upload-${Math.random().toString(36).slice(2, 9)}`

async function onChange(e: Event) {
  const el = e.target as HTMLInputElement
  const file = el.files?.[0]
  if (!file) return
  error.value = ''
  uploading.value = true
  try {
    const form = new FormData()
    form.append('file', file)
    if (props.articleId) form.append('articleId', String(props.articleId))
    const res = await api.uploadMedia(form)
    emit('uploaded', res)
  } catch {
    error.value = 'Не удалось загрузить файл'
  } finally {
    uploading.value = false
    el.value = '' // allow re-uploading the same file
  }
}
</script>

<template>
  <span class="inline-block align-middle">
    <label
      :for="inputId"
      class="cursor-pointer text-blue-600 hover:underline"
      :class="{ 'pointer-events-none opacity-50': uploading }"
    >
      {{ uploading ? 'Загрузка…' : label }}
    </label>
    <input :id="inputId" type="file" accept="image/*" class="sr-only" @change="onChange" />
    <span v-if="error" class="ml-2 text-sm text-red-600">{{ error }}</span>
  </span>
</template>
