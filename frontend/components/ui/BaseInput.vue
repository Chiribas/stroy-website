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
      class="w-full rounded-card border bg-surface-2 px-3 py-2 text-ink outline-none focus:border-brand"
      :class="error ? 'border-red-500' : 'border-base'"
    />
    <input
      v-else
      v-model="value"
      :type="type ?? 'text'"
      class="w-full rounded-card border bg-surface-2 px-3 py-2 text-ink outline-none focus:border-brand"
      :class="error ? 'border-red-500' : 'border-base'"
    />
    <span v-if="error" class="mt-1 block text-sm text-red-600">{{ error }}</span>
  </label>
</template>
