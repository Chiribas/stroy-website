import type { ServicePrice } from '~/types/api'

export interface PriceGroup {
  category: string
  items: ServicePrice[]
}

export function groupByCategory(prices: ServicePrice[]): PriceGroup[] {
  const order: string[] = []
  const map = new Map<string, ServicePrice[]>()
  for (const pr of prices) {
    if (!map.has(pr.category)) {
      map.set(pr.category, [])
      order.push(pr.category)
    }
    map.get(pr.category)!.push(pr)
  }
  return order.map(category => ({
    category,
    items: map.get(category)!.slice().sort((a, b) => a.sortOrder - b.sortOrder),
  }))
}

// Группировка тысяч неразрывным пробелом (U+00A0), детерминированно, без зависимости от локали.
const group = (n: number) => n.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ' ')

export function formatPrice(p: ServicePrice): string {
  const head = p.priceTo != null
    ? `от ${group(p.priceFrom)} до ${group(p.priceTo)} ₽`
    : `от ${group(p.priceFrom)} ₽`
  return p.unit ? `${head} / ${p.unit}` : head
}
