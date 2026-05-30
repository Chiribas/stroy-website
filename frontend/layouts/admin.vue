<script setup lang="ts">
const auth = useAuth()
const route = useRoute()
function logout() {
  auth.logout()
  navigateTo('/admin/login')
}
const nav = [
  { to: '/admin', label: 'Дашборд' },
  { to: '/admin/articles', label: 'Статьи' },
  { to: '/admin/services', label: 'Услуги' },
  { to: '/admin/prices', label: 'Примеры и цены' },
  { to: '/admin/inbox', label: 'Заявки' },
]
</script>

<template>
  <div class="min-h-screen flex">
    <aside v-if="route.path !== '/admin/login'" class="w-56 bg-gray-900 text-gray-100 p-4 space-y-2">
      <div class="text-lg font-bold mb-4">Админка</div>
      <NuxtLink v-for="n in nav" :key="n.to" :to="n.to" class="block px-3 py-2 rounded hover:bg-gray-700">
        {{ n.label }}
      </NuxtLink>
      <button class="mt-6 text-sm text-gray-400 hover:text-white" @click="logout">Выйти</button>
    </aside>
    <main class="flex-1 bg-surface p-6 text-ink">
      <div class="mb-4 flex justify-end"><ThemeToggle /></div>
      <slot />
    </main>
  </div>
</template>
