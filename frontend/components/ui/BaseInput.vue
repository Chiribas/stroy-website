<script setup lang="ts">
import { computed } from 'vue'

const props = defineProps<{
  modelValue: string
  label: string
  type?: string
  error?: string
  required?: boolean
  textarea?: boolean
}>()
const emit = defineEmits<{ 'update:modelValue': [string] }>()

const value = computed({
  get: () => props.modelValue,
  set: v => emit('update:modelValue', v),
})
</script>

<template>
  <label class="block">
    <span class="mb-1 block text-sm font-medium text-ink">
      {{ label }}<span v-if="required" class="text-brand"> *</span>
    </span>
    <textarea
      v-if="textarea"
      v-model="value"
      rows="4"
      class="w-full rounded-card border px-3 py-2 outline-none focus:border-brand"
      :class="error ? 'border-red-500' : 'border-gray-300'"
    />
    <input
      v-else
      v-model="value"
      :type="type ?? 'text'"
      class="w-full rounded-card border px-3 py-2 outline-none focus:border-brand"
      :class="error ? 'border-red-500' : 'border-gray-300'"
    />
    <span v-if="error" class="mt-1 block text-sm text-red-600">{{ error }}</span>
  </label>
</template>
