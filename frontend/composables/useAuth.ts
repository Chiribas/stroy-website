import { computed, ref, type Ref } from 'vue'

const TOKEN_KEY = 'admin_token'

export function createAuthStore(tokenRef?: Ref<string | null>) {
  const token = tokenRef ?? ref<string | null>(
    typeof localStorage !== 'undefined' ? localStorage.getItem(TOKEN_KEY) : null,
  )
  const isAuthenticated = computed(() => !!token.value)

  function setToken(value: string) {
    token.value = value
    if (typeof localStorage !== 'undefined') localStorage.setItem(TOKEN_KEY, value)
  }
  function logout() {
    token.value = null
    if (typeof localStorage !== 'undefined') localStorage.removeItem(TOKEN_KEY)
  }
  function getToken() {
    return token.value
  }
  return { token, isAuthenticated, setToken, logout, getToken }
}

export function useAuth() {
  const token = useState<string | null>('admin_token', () =>
    typeof localStorage !== 'undefined' ? localStorage.getItem(TOKEN_KEY) : null,
  )
  return createAuthStore(token)
}
