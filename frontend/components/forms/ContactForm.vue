<script setup lang="ts">
import { ref } from 'vue'
import type { ContactPayload, MessageResponse } from '~/types/api'
import BaseInput from '~/components/ui/BaseInput.vue'
import BaseButton from '~/components/ui/BaseButton.vue'

const props = defineProps<{ onSubmit: (p: ContactPayload) => Promise<MessageResponse> }>()

const name = ref('')
const phone = ref('')
const message = ref('')
const errors = ref<{ name?: string; phone?: string; message?: string }>({})
const success = ref('')
const failed = ref('')
const submitting = ref(false)

function validate() {
  const e: typeof errors.value = {}
  if (name.value.trim().length < 2) e.name = 'Укажите имя'
  if (!phone.value.trim()) e.phone = 'Укажите телефон'
  if (message.value.trim().length < 10) e.message = 'Сообщение слишком короткое'
  errors.value = e
  return Object.keys(e).length === 0
}

async function submit() {
  success.value = ''
  failed.value = ''
  if (!validate()) return
  submitting.value = true
  try {
    const res = await props.onSubmit({
      name: name.value.trim(),
      phone: phone.value.trim(),
      message: message.value.trim(),
    })
    success.value = res.message
    name.value = phone.value = message.value = ''
  } catch {
    failed.value = 'Не удалось отправить. Попробуйте позже.'
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <form class="space-y-4" @submit.prevent="submit">
    <BaseInput v-model="name" label="Имя" required :error="errors.name" />
    <BaseInput v-model="phone" label="Телефон" type="tel" required :error="errors.phone" />
    <BaseInput v-model="message" label="Сообщение" textarea required :error="errors.message" />
    <p v-if="success" class="text-green-600">{{ success }}</p>
    <p v-if="failed" class="text-red-600">{{ failed }}</p>
    <BaseButton type="submit" :disabled="submitting">
      {{ submitting ? 'Отправляем…' : 'Отправить' }}
    </BaseButton>
  </form>
</template>
