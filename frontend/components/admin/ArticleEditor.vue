<script setup lang="ts">
import { useEditor, EditorContent } from '@tiptap/vue-3'
import StarterKit from '@tiptap/starter-kit'
import Link from '@tiptap/extension-link'
import Image from '@tiptap/extension-image'
import { Iframe } from '~/lib/tiptap-iframe'
import { toEmbedUrl } from '~/lib/video'

const props = defineProps<{ modelValue: string; articleId?: number }>()
const emit = defineEmits<{ 'update:modelValue': [value: string] }>()

const editor = useEditor({
  content: props.modelValue,
  extensions: [StarterKit, Link.configure({ openOnClick: false }), Image, Iframe],
  onUpdate: ({ editor }) => emit('update:modelValue', editor.getHTML()),
})

// Content is often loaded asynchronously (edit page fetches the article after the
// editor mounts). Push external changes into the editor without re-emitting.
watch(() => props.modelValue, (value) => {
  if (editor.value && value !== editor.value.getHTML()) {
    editor.value.commands.setContent(value, { emitUpdate: false })
  }
})

function addVideo() {
  const input = window.prompt(
    'Ссылка на видео (VK / Rutube / YouTube) или код для вставки (iframe).\n' +
    'Для приватных видео ВК вставьте код из «Поделиться → Экспортировать».',
  )
  if (!input) return
  const src = toEmbedUrl(input)
  if (!src) {
    window.alert('Не удалось распознать ссылку. Поддерживаются YouTube, Rutube, VK Video.')
    return
  }
  // Insert as an iframe node; backend sanitizer enforces the trusted-host whitelist.
  editor.value?.chain().focus().insertContent({ type: 'iframe', attrs: { src } }).run()
}

function onMediaUploaded(media: { url: string }) {
  editor.value?.chain().focus().setImage({ src: media.url }).run()
}

onBeforeUnmount(() => editor.value?.destroy())
</script>

<template>
  <div class="border rounded bg-white">
    <div v-if="editor" class="flex flex-wrap gap-2 border-b p-2 text-sm">
      <button type="button" :class="{ 'font-bold': editor.isActive('bold') }" @click="editor.chain().focus().toggleBold().run()">Жирный</button>
      <button type="button" :class="{ 'italic': editor.isActive('italic') }" @click="editor.chain().focus().toggleItalic().run()">Курсив</button>
      <button type="button" @click="editor.chain().focus().toggleHeading({ level: 2 }).run()">H2</button>
      <button type="button" @click="editor.chain().focus().toggleHeading({ level: 3 }).run()">H3</button>
      <button type="button" @click="editor.chain().focus().toggleBulletList().run()">Список</button>
      <button type="button" @click="editor.chain().focus().toggleOrderedList().run()">Нумерация</button>
      <MediaUploader :article-id="articleId" @uploaded="onMediaUploaded" />
      <button type="button" @click="addVideo">Видео</button>
    </div>
    <EditorContent :editor="editor" class="prose max-w-none p-3 min-h-[300px]" />
  </div>
</template>
