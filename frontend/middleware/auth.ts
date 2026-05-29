export default defineNuxtRouteMiddleware((to) => {
  if (to.path === '/admin/login') return
  const auth = useAuth()
  if (!auth.isAuthenticated.value) {
    return navigateTo('/admin/login')
  }
})
