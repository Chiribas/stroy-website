<script setup lang="ts">
definePageMeta({ layout: 'admin' })
const api = useAdminApi()
const auth = useAuth()
const username = ref('')
const password = ref('')
const error = ref('')
const loading = ref(false)

async function submit() {
  error.value = ''
  loading.value = true
  try {
    const res = await api.login({ username: username.value, password: password.value })
    auth.setToken(res.token)
    await navigateTo('/admin')
  } catch {
    error.value = 'Неверный логин или пароль'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="max-w-sm mx-auto mt-24 bg-white p-6 rounded shadow">
    <h1 class="text-xl font-bold mb-4">Вход в админку</h1>
    <form class="space-y-3" @submit.prevent="submit">
      <input v-model="username" placeholder="Логин" class="w-full border rounded px-3 py-2" />
      <input v-model="password" type="password" placeholder="Пароль" class="w-full border rounded px-3 py-2" />
      <p v-if="error" class="text-red-600 text-sm">{{ error }}</p>
      <button :disabled="loading" class="w-full bg-gray-900 text-white py-2 rounded disabled:opacity-50">
        {{ loading ? 'Вход…' : 'Войти' }}
      </button>
    </form>
  </div>
</template>
