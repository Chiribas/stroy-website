import type { ServicePrice } from '~/types/api'

// Группировка тысяч неразрывным пробелом (U+00A0), детерминированно, без зависимости от локали.
const group = (n: number) => n.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ' ')

export function formatPrice(p: ServicePrice): string {
  return `${group(p.price)} ₽`
}
