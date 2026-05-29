import { describe, it, expect, beforeEach } from 'vitest'
import { ref } from 'vue'
import { createAuthStore } from '~/composables/useAuth'

describe('auth store', () => {
  beforeEach(() => localStorage.clear())

  it('starts unauthenticated', () => {
    const auth = createAuthStore(ref<string | null>(null))
    expect(auth.isAuthenticated.value).toBe(false)
  })

  it('stores token on setToken and reports authenticated', () => {
    const auth = createAuthStore(ref<string | null>(null))
    auth.setToken('abc.def.ghi')
    expect(auth.token.value).toBe('abc.def.ghi')
    expect(auth.isAuthenticated.value).toBe(true)
    expect(localStorage.getItem('admin_token')).toBe('abc.def.ghi')
  })

  it('clears token on logout', () => {
    const auth = createAuthStore(ref<string | null>(null))
    auth.setToken('abc')
    auth.logout()
    expect(auth.token.value).toBeNull()
    expect(localStorage.getItem('admin_token')).toBeNull()
  })
})
