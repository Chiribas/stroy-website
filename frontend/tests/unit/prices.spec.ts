import { describe, it, expect } from 'vitest'
import { groupByCategory, formatPrice } from '~/lib/prices'
import type { ServicePrice } from '~/types/api'

const p = (over: Partial<ServicePrice>): ServicePrice => ({
  id: 1, category: 'Общее', name: 'Работа', description: null,
  priceFrom: 100, priceTo: null, unit: 'м²', sortOrder: 0, ...over,
})

describe('groupByCategory', () => {
  it('группирует по категории и сортирует по sortOrder', () => {
    const groups = groupByCategory([
      p({ id: 1, category: 'Фасад', sortOrder: 2 }),
      p({ id: 2, category: 'Кровля', sortOrder: 0 }),
      p({ id: 3, category: 'Фасад', sortOrder: 1 }),
    ])
    expect(groups.map(g => g.category)).toEqual(['Фасад', 'Кровля'])
    const fasad = groups.find(g => g.category === 'Фасад')!
    expect(fasad.items.map(i => i.id)).toEqual([3, 1])
  })
})

describe('formatPrice', () => {
  it('диапазон, когда есть priceTo', () => {
    expect(formatPrice(p({ priceFrom: 1000, priceTo: 2000, unit: 'м²' })))
      .toBe('от 1 000 до 2 000 ₽ / м²')
  })
  it('от X, когда priceTo нет', () => {
    expect(formatPrice(p({ priceFrom: 500, priceTo: null, unit: 'шт' })))
      .toBe('от 500 ₽ / шт')
  })
  it('без unit — без суффикса', () => {
    expect(formatPrice(p({ priceFrom: 500, priceTo: null, unit: null })))
      .toBe('от 500 ₽')
  })
})
