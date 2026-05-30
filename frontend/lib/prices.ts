import type { ServicePrice } from '~/types/api'

// Группировка тысяч неразрывным пробелом (U+00A0), детерминированно, без зависимости от локали.
const group = (n: number) => n.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ' ')

export function formatPrice(p: ServicePrice): string {
  return `${group(p.price)} ₽`
}

// Срок: если введено голое число — трактуем как дни и склоняем («2» → «2 дня»,
// «7» → «7 дней»). Явные строки («1 неделя», «3-4 дня») отдаём как есть.
export function formatDuration(d: string): string {
  const s = d.trim()
  if (!/^\d+$/.test(s)) return s
  const n = Number(s)
  const mod100 = n % 100
  const mod10 = n % 10
  const word = mod100 >= 11 && mod100 <= 14 ? 'дней'
    : mod10 === 1 ? 'день'
    : mod10 >= 2 && mod10 <= 4 ? 'дня'
    : 'дней'
  return `${n} ${word}`
}
