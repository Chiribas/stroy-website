<script setup lang="ts">
const isDark = ref(false)
onMounted(() => { isDark.value = document.documentElement.classList.contains('dark') })
function toggle() {
  isDark.value = !isDark.value
  document.documentElement.classList.toggle('dark', isDark.value)
  localStorage.setItem('theme', isDark.value ? 'dark' : 'light')
}
</script>

<template>
  <button
    type="button"
    class="rounded-card border border-base px-2 py-1 text-ink hover:text-brand"
    :aria-label="isDark ? 'Светлая тема' : 'Тёмная тема'"
    @click="toggle"
  >
    {{ isDark ? '☀' : '☾' }}
  </button>
</template>
