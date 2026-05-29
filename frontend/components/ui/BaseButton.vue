<script setup lang="ts">
defineProps<{ type?: 'button' | 'submit'; disabled?: boolean; variant?: 'primary' | 'ghost' }>()
</script>

<template>
  <button
    :type="type ?? 'button'"
    :disabled="disabled"
    class="inline-flex items-center justify-center rounded-card px-5 py-2.5 font-medium transition disabled:opacity-50 disabled:cursor-not-allowed"
    :class="(variant ?? 'primary') === 'primary'
      ? 'bg-brand text-white hover:bg-brand-dark'
      : 'border border-brand text-brand hover:bg-brand/10'"
  >
    <slot />
  </button>
</template>
