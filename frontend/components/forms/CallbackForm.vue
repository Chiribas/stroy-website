<script setup lang="ts">
import { ref } from 'vue'
import type { CallbackPayload, MessageResponse } from '~/types/api'
import BaseInput from '~/components/ui/BaseInput.vue'
import BaseButton from '~/components/ui/BaseButton.vue'

const props = defineProps<{ onSubmit: (p: CallbackPayload) => Promise<MessageResponse> }>()

const phone = ref('')
const name = ref('')
const error = ref('')
const success = ref('')
const submitting = ref(false)

async function submit() {
  error.value = ''
  success.value = ''
  if (!phone.value.trim()) {
    error.value = 'Укажите телефон'
    return
  }
  submitting.value = true
  try {
    const res = await props.onSubmit({ phone: phone.value.trim(), name: name.value.trim() || undefined })
    success.value = res.message
    phone.value = ''
    name.value = ''
  } catch {
    error.value = 'Не удалось отправить. Попробуйте позже.'
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <form class="space-y-4" @submit.prevent="submit">
    <BaseInput v-model="name" label="Имя" />
    <BaseInput v-model="phone" label="Телефон" type="tel" required :error="error" />
    <p v-if="success" class="text-green-600">{{ success }}</p>
    <BaseButton type="submit" :disabled="submitting">
      {{ submitting ? 'Отправляем…' : 'Перезвоните мне' }}
    </BaseButton>
  </form>
</template>
