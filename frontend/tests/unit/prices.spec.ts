import { describe, it, expect } from 'vitest'
import { formatPrice } from '~/lib/prices'
import type { ServicePrice } from '~/types/api'

const p = (over: Partial<ServicePrice>): ServicePrice => ({
  id: 1, title: 'Работа', photoPath: null, description: null,
  price: 100, duration: null, articleSlug: null, tag: null, sortOrder: 0, ...over,
})

describe('formatPrice', () => {
  it('форматирует цену с разделителем тысяч и ₽', () => {
    expect(formatPrice(p({ price: 340000 }))).toBe('340 000 ₽')
  })
  it('малые суммы без разделителя', () => {
    expect(formatPrice(p({ price: 500 }))).toBe('500 ₽')
  })
  it('миллионы', () => {
    expect(formatPrice(p({ price: 1250000 }))).toBe('1 250 000 ₽')
  })
})
