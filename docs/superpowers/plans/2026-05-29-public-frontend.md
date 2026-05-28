# Public Frontend (Nuxt 3 SSG) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Реализовать публичный фронт сайта-визитки (лендинг, цены, портфолио, контакты, формы) на Nuxt 3 в режиме SSG, с архитектурой секций для дешёвой перекомпоновки.

**Architecture:** Nuxt 3 (статическая генерация). Секции — самодостаточные компоненты, лендинг собирается из массива `landingSections`. Доступ к API — через чистую фабрику `createApi(fetcher, baseURL)` (тестируется юнитами) + тонкий `useApi()`-обёртка под Nuxt. Формы — чистые SFC с пропом `onSubmit` (тестируются без Nuxt-рантайма). Дизайн-токены — в `tailwind.config` + `app.config.ts`.

**Tech Stack:** Nuxt 3, Vue 3, TypeScript, Tailwind CSS (`@nuxtjs/tailwindcss`), VueUse (`@vueuse/nuxt`), Vitest + `@vue/test-utils` + happy-dom.

**Дизайн-источник:** `docs/superpowers/specs/2026-05-29-public-frontend-design.md`.

**Контракт API (готовый бэкенд, dev-порт 8081):**
```
GET  /api/articles?page&pageSize → { items:[{id,title,slug,summary?,thumbnailPath?,publishedAt}], total, page, pageSize }
GET  /api/articles/{slug}        → { id,title,slug,summary?,content,thumbnailPath?,publishedAt, media:[{id,path,mediaType,alt?,sortOrder}] }
GET  /api/services/prices        → [{id,category,name,description?,priceFrom,priceTo?,unit?,sortOrder}]
POST /api/callbacks {phone,name?}          → {message}
POST /api/contacts  {name,phone,message}   → {message}
```

**baseURL (важно для SSG):** на билде fetch идёт server-side и требует абсолютный URL до бэка; формы — client-side в рантайме. `useApi()` выбирает базу по `import.meta.server`:
- server (build) → `runtimeConfig.public.apiBase` (dev `http://localhost:8081`, docker-build `http://backend:8080`)
- client (runtime) → `runtimeConfig.public.apiClientBase` (dev `http://localhost:8081`, прод `/api` через nginx)

---

## Файловая структура (создаётся этим планом)

```
frontend/
├── package.json                        (create)
├── nuxt.config.ts                      (create)
├── tailwind.config.ts                  (create)
├── app.config.ts                       (create: токены темы + контакты компании)
├── tsconfig.json                       (create — через nuxi prepare)
├── vitest.config.ts                    (create)
├── assets/css/main.css                 (create: tailwind директивы)
├── types/api.ts                        (create: DTO-типы)
├── lib/api.ts                          (create: чистая фабрика createApi + helpers)
├── lib/prices.ts                       (create: группировка/форматирование цен)
├── composables/useApi.ts               (create: Nuxt-обёртка над createApi)
├── composables/useContacts.ts          (create: контакты из app.config)
├── components/
│   ├── layout/AppHeader.vue            (create)
│   ├── layout/AppFooter.vue            (create)
│   ├── ui/BaseButton.vue               (create)
│   ├── ui/BaseInput.vue                (create)
│   ├── ui/PortfolioCard.vue            (create)
│   ├── ui/PriceTable.vue               (create)
│   ├── forms/CallbackForm.vue          (create)
│   ├── forms/ContactForm.vue           (create)
│   └── sections/
│       ├── SectionHero.vue             (create)
│       ├── SectionServicesTeaser.vue   (create)
│       ├── SectionPricesTeaser.vue     (create)
│       ├── SectionPortfolioTeaser.vue  (create)
│       ├── SectionAbout.vue            (create)
│       ├── SectionCta.vue              (create)
│       └── SectionContacts.vue         (create)
├── layouts/default.vue                 (create)
├── pages/
│   ├── index.vue                       (create: композиция секций)
│   ├── prices.vue                      (create)
│   ├── portfolio/index.vue             (create)
│   ├── portfolio/[slug].vue            (create)
│   └── contact.vue                     (create)
├── server/routes/sitemap.xml.ts        (create) — опционально, через prerender
└── tests/
    ├── unit/api.spec.ts                (create)
    ├── unit/prices.spec.ts             (create)
    ├── component/CallbackForm.spec.ts  (create)
    └── component/ContactForm.spec.ts   (create)
```

Все команды выполняются из каталога `frontend/` (если не указано иное). Шелл — bash (Windows), пути через `/`.

---

## Task 0: Скаффолд Nuxt-проекта и инструментов

**Files:**
- Create: `frontend/package.json`, `frontend/nuxt.config.ts`, `frontend/tailwind.config.ts`, `frontend/assets/css/main.css`, `frontend/vitest.config.ts`

- [ ] **Step 1: Инициализировать Nuxt-проект**

Из корня `stroy-website/`:
```bash
npx nuxi@latest init frontend --packageManager npm --gitInit false --no-install
```
Если каталог `frontend` уже существует и пуст — добавить флаг `--force`.

- [ ] **Step 2: Установить зависимости**

```bash
cd frontend
npm install
npm install -D @nuxtjs/tailwindcss @vueuse/nuxt
npm install -D vitest @vue/test-utils happy-dom @nuxt/test-utils
```

- [ ] **Step 3: Записать `nuxt.config.ts`**

```ts
export default defineNuxtConfig({
  compatibilityDate: '2025-01-01',
  devtools: { enabled: true },
  modules: ['@nuxtjs/tailwindcss', '@vueuse/nuxt'],
  css: ['~/assets/css/main.css'],
  devServer: { port: 3001 },
  nitro: {
    prerender: {
      crawlLinks: true,
      routes: ['/', '/prices', '/portfolio', '/contact'],
      failOnError: false,
    },
  },
  runtimeConfig: {
    public: {
      apiBase: 'http://localhost:8081',       // server-side / build
      apiClientBase: 'http://localhost:8081', // client-side runtime (прод: /api)
      siteUrl: 'http://localhost:3001',
    },
  },
})
```

- [ ] **Step 4: Записать `tailwind.config.ts` (дизайн-токены)**

```ts
import type { Config } from 'tailwindcss'

export default <Partial<Config>>{
  content: [],
  theme: {
    extend: {
      colors: {
        brand: {
          DEFAULT: '#d97706', // amber-600, фирменный акцент
          dark: '#b45309',
          light: '#fbbf24',
        },
        ink: '#1f2937',
        muted: '#6b7280',
      },
      fontFamily: {
        sans: ['Inter', 'system-ui', 'sans-serif'],
      },
      borderRadius: {
        card: '0.75rem',
      },
    },
  },
}
```

- [ ] **Step 5: Записать `assets/css/main.css`**

```css
@tailwind base;
@tailwind components;
@tailwind utilities;
```

- [ ] **Step 6: Записать `vitest.config.ts`**

```ts
import { defineVitestConfig } from '@nuxt/test-utils/config'

export default defineVitestConfig({
  test: {
    environment: 'happy-dom',
    include: ['tests/**/*.spec.ts'],
  },
})
```

- [ ] **Step 7: Добавить test-скрипты в `package.json`**

В секцию `"scripts"` добавить:
```json
"test": "vitest run",
"test:watch": "vitest",
"generate": "nuxt generate"
```

- [ ] **Step 8: Подготовить типы и проверить dev-сборку**

```bash
npx nuxi prepare
npm run build
```
Expected: `nuxi prepare` создаёт `.nuxt/` и `tsconfig.json`; `npm run build` завершается без ошибок (приветственная страница Nuxt по умолчанию).

- [ ] **Step 9: Commit**

```bash
git add frontend/
git commit -m "feat(frontend): scaffold Nuxt 3 SSG project with Tailwind and Vitest"
```

---

## Task 1: DTO-типы API

**Files:**
- Create: `frontend/types/api.ts`

- [ ] **Step 1: Записать типы (зеркало DTO бэкенда)**

```ts
export interface ArticleListItem {
  id: number
  title: string
  slug: string
  summary?: string | null
  thumbnailPath?: string | null
  publishedAt: string
}

export interface ArticleMedia {
  id: number
  path: string
  mediaType: string
  alt?: string | null
  sortOrder: number
}

export interface Article {
  id: number
  title: string
  slug: string
  summary?: string | null
  content: string
  thumbnailPath?: string | null
  publishedAt: string
  media: ArticleMedia[]
}

export interface PagedResult<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
}

export interface ServicePrice {
  id: number
  category: string
  name: string
  description?: string | null
  priceFrom: number
  priceTo?: number | null
  unit?: string | null
  sortOrder: number
}

export interface CallbackPayload {
  phone: string
  name?: string
}

export interface ContactPayload {
  name: string
  phone: string
  message: string
}

export interface MessageResponse {
  message: string
}
```

- [ ] **Step 2: Commit**

```bash
git add frontend/types/api.ts
git commit -m "feat(frontend): add API DTO types"
```

---

## Task 2: Чистая API-фабрика `createApi` (TDD)

**Files:**
- Create: `frontend/lib/api.ts`
- Test: `frontend/tests/unit/api.spec.ts`

- [ ] **Step 1: Написать падающий тест**

```ts
import { describe, it, expect, vi } from 'vitest'
import { createApi } from '~/lib/api'

describe('createApi', () => {
  it('запрашивает статьи с page/pageSize в query и абсолютным baseURL', async () => {
    const fetcher = vi.fn().mockResolvedValue({ items: [], total: 0, page: 2, pageSize: 6 })
    const api = createApi(fetcher, 'http://api.test')

    const result = await api.getArticles(2, 6)

    expect(fetcher).toHaveBeenCalledWith('http://api.test/api/articles', {
      query: { page: 2, pageSize: 6 },
    })
    expect(result.page).toBe(2)
  })

  it('запрашивает статью по slug', async () => {
    const fetcher = vi.fn().mockResolvedValue({ slug: 'dom', title: 'Дом' })
    const api = createApi(fetcher, 'http://api.test')

    await api.getArticle('dom')

    expect(fetcher).toHaveBeenCalledWith('http://api.test/api/articles/dom')
  })

  it('постит callback методом POST с body', async () => {
    const fetcher = vi.fn().mockResolvedValue({ message: 'ок' })
    const api = createApi(fetcher, 'http://api.test')

    await api.sendCallback({ phone: '+79991234567' })

    expect(fetcher).toHaveBeenCalledWith('http://api.test/api/callbacks', {
      method: 'POST',
      body: { phone: '+79991234567' },
    })
  })
})
```

- [ ] **Step 2: Запустить тест — убедиться, что падает**

```bash
npm test -- tests/unit/api.spec.ts
```
Expected: FAIL — `createApi` не найден.

- [ ] **Step 3: Реализовать `lib/api.ts`**

```ts
import type {
  Article, ArticleListItem, PagedResult, ServicePrice,
  CallbackPayload, ContactPayload, MessageResponse,
} from '~/types/api'

type Fetcher = <T>(url: string, opts?: Record<string, unknown>) => Promise<T>

export function createApi(fetcher: Fetcher, baseURL: string) {
  const url = (path: string) => `${baseURL}${path}`
  return {
    getArticles(page = 1, pageSize = 12) {
      return fetcher<PagedResult<ArticleListItem>>(url('/api/articles'), {
        query: { page, pageSize },
      })
    },
    getArticle(slug: string) {
      return fetcher<Article>(url(`/api/articles/${slug}`))
    },
    getPrices() {
      return fetcher<ServicePrice[]>(url('/api/services/prices'))
    },
    sendCallback(body: CallbackPayload) {
      return fetcher<MessageResponse>(url('/api/callbacks'), { method: 'POST', body })
    },
    sendContact(body: ContactPayload) {
      return fetcher<MessageResponse>(url('/api/contacts'), { method: 'POST', body })
    },
  }
}

export type Api = ReturnType<typeof createApi>
```

- [ ] **Step 4: Запустить тест — убедиться, что проходит**

```bash
npm test -- tests/unit/api.spec.ts
```
Expected: PASS (3 теста).

- [ ] **Step 5: Commit**

```bash
git add frontend/lib/api.ts frontend/tests/unit/api.spec.ts
git commit -m "feat(frontend): add createApi factory with unit tests"
```

---

## Task 3: Композабл `useApi` (Nuxt-обёртка)

**Files:**
- Create: `frontend/composables/useApi.ts`

- [ ] **Step 1: Реализовать `useApi.ts`**

```ts
import { createApi } from '~/lib/api'

export function useApi() {
  const config = useRuntimeConfig()
  const baseURL = import.meta.server
    ? config.public.apiBase
    : config.public.apiClientBase
  return createApi($fetch as any, baseURL)
}
```

- [ ] **Step 2: Проверить типчек**

```bash
npx nuxi typecheck
```
Expected: без ошибок (использует автоимпорты Nuxt `useRuntimeConfig`, `$fetch`).

- [ ] **Step 3: Commit**

```bash
git add frontend/composables/useApi.ts
git commit -m "feat(frontend): add useApi composable wrapping createApi"
```

---

## Task 4: Группировка и форматирование цен (TDD)

**Files:**
- Create: `frontend/lib/prices.ts`
- Test: `frontend/tests/unit/prices.spec.ts`

- [ ] **Step 1: Написать падающий тест**

```ts
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
    expect(fasad.items.map(i => i.id)).toEqual([3, 1]) // по sortOrder
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
```

- [ ] **Step 2: Запустить тест — убедиться, что падает**

```bash
npm test -- tests/unit/prices.spec.ts
```
Expected: FAIL — модуль не найден.

- [ ] **Step 3: Реализовать `lib/prices.ts`**

```ts
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

const nbsp = (n: number) => n.toLocaleString('ru-RU').replace(/\s/g, ' ')

export function formatPrice(p: ServicePrice): string {
  const head = p.priceTo != null
    ? `от ${nbsp(p.priceFrom)} до ${nbsp(p.priceTo)} ₽`
    : `от ${nbsp(p.priceFrom)} ₽`
  return p.unit ? `${head} / ${p.unit}` : head
}
```

- [ ] **Step 4: Запустить тест — убедиться, что проходит**

```bash
npm test -- tests/unit/prices.spec.ts
```
Expected: PASS (4 теста).

- [ ] **Step 5: Commit**

```bash
git add frontend/lib/prices.ts frontend/tests/unit/prices.spec.ts
git commit -m "feat(frontend): add price grouping and formatting with tests"
```

---

## Task 5: UI-примитивы BaseButton и BaseInput

**Files:**
- Create: `frontend/components/ui/BaseButton.vue`, `frontend/components/ui/BaseInput.vue`

- [ ] **Step 1: Записать `BaseButton.vue`**

```vue
<script setup lang="ts">
defineProps<{ type?: 'button' | 'submit'; disabled?: boolean; variant?: 'primary' | 'ghost' }>()
</script>

<template>
  <button
    :type="type ?? 'button'"
    :disabled="disabled"
    class="inline-flex items-center justify-center rounded-card px-5 py-2.5 font-medium transition disabled:opacity-50 disabled:cursor-not-allowed"
    :class="(variant ?? 'primary') === 'primary'
      ? 'bg-brand text-white hover:bg-brand-dark'
      : 'border border-brand text-brand hover:bg-brand/10'"
  >
    <slot />
  </button>
</template>
```

- [ ] **Step 2: Записать `BaseInput.vue`**

```vue
<script setup lang="ts">
import { computed } from 'vue'

const props = defineProps<{
  modelValue: string
  label: string
  type?: string
  error?: string
  required?: boolean
  textarea?: boolean
}>()
const emit = defineEmits<{ 'update:modelValue': [string] }>()

const value = computed({
  get: () => props.modelValue,
  set: v => emit('update:modelValue', v),
})
</script>

<template>
  <label class="block">
    <span class="mb-1 block text-sm font-medium text-ink">
      {{ label }}<span v-if="required" class="text-brand"> *</span>
    </span>
    <textarea
      v-if="textarea"
      v-model="value"
      rows="4"
      class="w-full rounded-card border px-3 py-2 outline-none focus:border-brand"
      :class="error ? 'border-red-500' : 'border-gray-300'"
    />
    <input
      v-else
      v-model="value"
      :type="type ?? 'text'"
      class="w-full rounded-card border px-3 py-2 outline-none focus:border-brand"
      :class="error ? 'border-red-500' : 'border-gray-300'"
    />
    <span v-if="error" class="mt-1 block text-sm text-red-600">{{ error }}</span>
  </label>
</template>
```

- [ ] **Step 3: Commit**

```bash
git add frontend/components/ui/BaseButton.vue frontend/components/ui/BaseInput.vue
git commit -m "feat(frontend): add BaseButton and BaseInput primitives"
```

---

## Task 6: CallbackForm (TDD, чистый компонент)

Форма принимает асинхронный проп `onSubmit(payload)` — это развязывает её от Nuxt и делает тестируемой. Страница позже подставит `useApi().sendCallback`.

**Files:**
- Create: `frontend/components/forms/CallbackForm.vue`
- Test: `frontend/tests/component/CallbackForm.spec.ts`

- [ ] **Step 1: Написать падающий тест**

```ts
import { describe, it, expect, vi } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import CallbackForm from '~/components/forms/CallbackForm.vue'

const fill = (wrapper: any, sel: string, val: string) =>
  wrapper.find(sel).setValue(val)

describe('CallbackForm', () => {
  it('не вызывает onSubmit при пустом телефоне и показывает ошибку', async () => {
    const onSubmit = vi.fn()
    const wrapper = mount(CallbackForm, { props: { onSubmit } })

    await wrapper.find('form').trigger('submit.prevent')
    await flushPromises()

    expect(onSubmit).not.toHaveBeenCalled()
    expect(wrapper.text()).toContain('Укажите телефон')
  })

  it('вызывает onSubmit с телефоном и показывает success-сообщение', async () => {
    const onSubmit = vi.fn().mockResolvedValue({ message: 'Спасибо!' })
    const wrapper = mount(CallbackForm, { props: { onSubmit } })

    await fill(wrapper, 'input[type="tel"]', '+79991234567')
    await wrapper.find('form').trigger('submit.prevent')
    await flushPromises()

    expect(onSubmit).toHaveBeenCalledWith({ phone: '+79991234567', name: undefined })
    expect(wrapper.text()).toContain('Спасибо!')
  })

  it('показывает ошибку, если onSubmit бросает', async () => {
    const onSubmit = vi.fn().mockRejectedValue(new Error('network'))
    const wrapper = mount(CallbackForm, { props: { onSubmit } })

    await fill(wrapper, 'input[type="tel"]', '+79991234567')
    await wrapper.find('form').trigger('submit.prevent')
    await flushPromises()

    expect(wrapper.text()).toContain('Не удалось отправить')
  })
})
```

- [ ] **Step 2: Запустить тест — убедиться, что падает**

```bash
npm test -- tests/component/CallbackForm.spec.ts
```
Expected: FAIL — компонент не найден.

- [ ] **Step 3: Реализовать `CallbackForm.vue`**

```vue
<script setup lang="ts">
import { ref } from 'vue'
import type { CallbackPayload, MessageResponse } from '~/types/api'
import BaseInput from '~/components/ui/BaseInput.vue'
import BaseButton from '~/components/ui/BaseButton.vue'

const props = defineProps<{ onSubmit: (p: CallbackPayload) => Promise<MessageResponse> }>()

const phone = ref('')
const name = ref('')
const error = ref('')
const success = ref('')
const submitting = ref(false)

async function submit() {
  error.value = ''
  success.value = ''
  if (!phone.value.trim()) {
    error.value = 'Укажите телефон'
    return
  }
  submitting.value = true
  try {
    const res = await props.onSubmit({ phone: phone.value.trim(), name: name.value.trim() || undefined })
    success.value = res.message
    phone.value = ''
    name.value = ''
  } catch {
    error.value = 'Не удалось отправить. Попробуйте позже.'
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <form class="space-y-4" @submit.prevent="submit">
    <BaseInput v-model="name" label="Имя" />
    <BaseInput v-model="phone" label="Телефон" type="tel" required :error="error" />
    <p v-if="success" class="text-green-600">{{ success }}</p>
    <BaseButton type="submit" :disabled="submitting">
      {{ submitting ? 'Отправляем…' : 'Перезвоните мне' }}
    </BaseButton>
  </form>
</template>
```

- [ ] **Step 4: Запустить тест — убедиться, что проходит**

```bash
npm test -- tests/component/CallbackForm.spec.ts
```
Expected: PASS (3 теста).

- [ ] **Step 5: Commit**

```bash
git add frontend/components/forms/CallbackForm.vue frontend/tests/component/CallbackForm.spec.ts
git commit -m "feat(frontend): add CallbackForm with validation tests"
```

---

## Task 7: ContactForm (TDD, чистый компонент)

**Files:**
- Create: `frontend/components/forms/ContactForm.vue`
- Test: `frontend/tests/component/ContactForm.spec.ts`

- [ ] **Step 1: Написать падающий тест**

```ts
import { describe, it, expect, vi } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import ContactForm from '~/components/forms/ContactForm.vue'

describe('ContactForm', () => {
  it('валидирует имя (≥2), телефон и сообщение (≥10)', async () => {
    const onSubmit = vi.fn()
    const wrapper = mount(ContactForm, { props: { onSubmit } })

    await wrapper.find('form').trigger('submit.prevent')
    await flushPromises()

    expect(onSubmit).not.toHaveBeenCalled()
    expect(wrapper.text()).toContain('Укажите имя')
    expect(wrapper.text()).toContain('Укажите телефон')
    expect(wrapper.text()).toContain('Сообщение слишком короткое')
  })

  it('отправляет валидный payload и показывает success', async () => {
    const onSubmit = vi.fn().mockResolvedValue({ message: 'Принято!' })
    const wrapper = mount(ContactForm, { props: { onSubmit } })

    await wrapper.find('input[data-test="name"]').setValue('Иван')
    await wrapper.find('input[data-test="phone"]').setValue('+79991234567')
    await wrapper.find('textarea').setValue('Здравствуйте, нужен расчёт фасада дома.')
    await wrapper.find('form').trigger('submit.prevent')
    await flushPromises()

    expect(onSubmit).toHaveBeenCalledWith({
      name: 'Иван',
      phone: '+79991234567',
      message: 'Здравствуйте, нужен расчёт фасада дома.',
    })
    expect(wrapper.text()).toContain('Принято!')
  })
})
```

- [ ] **Step 2: Запустить тест — убедиться, что падает**

```bash
npm test -- tests/component/ContactForm.spec.ts
```
Expected: FAIL — компонент не найден.

- [ ] **Step 3: Реализовать `ContactForm.vue`**

```vue
<script setup lang="ts">
import { ref } from 'vue'
import type { ContactPayload, MessageResponse } from '~/types/api'
import BaseInput from '~/components/ui/BaseInput.vue'
import BaseButton from '~/components/ui/BaseButton.vue'

const props = defineProps<{ onSubmit: (p: ContactPayload) => Promise<MessageResponse> }>()

const name = ref('')
const phone = ref('')
const message = ref('')
const errors = ref<{ name?: string; phone?: string; message?: string }>({})
const success = ref('')
const failed = ref('')
const submitting = ref(false)

function validate() {
  const e: typeof errors.value = {}
  if (name.value.trim().length < 2) e.name = 'Укажите имя'
  if (!phone.value.trim()) e.phone = 'Укажите телефон'
  if (message.value.trim().length < 10) e.message = 'Сообщение слишком короткое'
  errors.value = e
  return Object.keys(e).length === 0
}

async function submit() {
  success.value = ''
  failed.value = ''
  if (!validate()) return
  submitting.value = true
  try {
    const res = await props.onSubmit({
      name: name.value.trim(),
      phone: phone.value.trim(),
      message: message.value.trim(),
    })
    success.value = res.message
    name.value = phone.value = message.value = ''
  } catch {
    failed.value = 'Не удалось отправить. Попробуйте позже.'
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <form class="space-y-4" @submit.prevent="submit">
    <BaseInput v-model="name" label="Имя" required :error="errors.name" data-test="name" />
    <BaseInput v-model="phone" label="Телефон" type="tel" required :error="errors.phone" data-test="phone" />
    <BaseInput v-model="message" label="Сообщение" textarea required :error="errors.message" />
    <p v-if="success" class="text-green-600">{{ success }}</p>
    <p v-if="failed" class="text-red-600">{{ failed }}</p>
    <BaseButton type="submit" :disabled="submitting">
      {{ submitting ? 'Отправляем…' : 'Отправить' }}
    </BaseButton>
  </form>
</template>
```

> Примечание: `data-test`/прочие атрибуты прокидываются на корневой `<label>` BaseInput, но `input[data-test="name"]` в тесте найдёт инпут только если атрибут на input. Поэтому в BaseInput добавить проброс: заменить в Task 5 не нужно — `data-test` попадёт на `<label>` (fallthrough). В тесте селектор `input[data-test="name"]` не сработает. **Исправление:** в этом шаге используем селекторы по label-тексту вместо data-test.

- [ ] **Step 3a: Скорректировать тест под реальные селекторы**

Заменить поиск инпутов в тесте на индексы (порядок: name, phone, textarea):
```ts
const inputs = wrapper.findAll('input')
await inputs[0].setValue('Иван')        // name
await inputs[1].setValue('+79991234567') // phone
await wrapper.find('textarea').setValue('Здравствуйте, нужен расчёт фасада дома.')
```
И убрать `data-test` из шаблона ContactForm (атрибуты не нужны).

- [ ] **Step 4: Запустить тест — убедиться, что проходит**

```bash
npm test -- tests/component/ContactForm.spec.ts
```
Expected: PASS (2 теста).

- [ ] **Step 5: Commit**

```bash
git add frontend/components/forms/ContactForm.vue frontend/tests/component/ContactForm.spec.ts
git commit -m "feat(frontend): add ContactForm with validation tests"
```

---

## Task 8: app.config.ts + useContacts

**Files:**
- Create: `frontend/app.config.ts`, `frontend/composables/useContacts.ts`

- [ ] **Step 1: Записать `app.config.ts`**

```ts
export default defineAppConfig({
  company: {
    name: 'СтройКомпания',
    tagline: 'Строим качественно и в срок',
    phone: '+7 (999) 123-45-67',
    email: 'info@stroycompany.ru',
    address: 'г. Москва, ул. Строителей, д. 1',
    schedule: 'Пн–Пт 9:00–18:00',
    social: {
      vk: '',
      telegram: '',
    },
  },
})
```

- [ ] **Step 2: Записать `composables/useContacts.ts`**

```ts
export function useContacts() {
  return useAppConfig().company
}
```

- [ ] **Step 3: Commit**

```bash
git add frontend/app.config.ts frontend/composables/useContacts.ts
git commit -m "feat(frontend): add company config and useContacts composable"
```

---

## Task 9: Layout (AppHeader, AppFooter, default layout)

**Files:**
- Create: `frontend/components/layout/AppHeader.vue`, `frontend/components/layout/AppFooter.vue`, `frontend/layouts/default.vue`

- [ ] **Step 1: Записать `AppHeader.vue`**

```vue
<script setup lang="ts">
const c = useContacts()
const links = [
  { to: '/', label: 'Главная' },
  { to: '/prices', label: 'Цены' },
  { to: '/portfolio', label: 'Портфолио' },
  { to: '/contact', label: 'Контакты' },
]
</script>

<template>
  <header class="border-b bg-white">
    <div class="mx-auto flex max-w-6xl items-center justify-between px-4 py-4">
      <NuxtLink to="/" class="text-xl font-bold text-brand">{{ c.name }}</NuxtLink>
      <nav class="hidden gap-6 md:flex">
        <NuxtLink v-for="l in links" :key="l.to" :to="l.to" class="text-ink hover:text-brand">
          {{ l.label }}
        </NuxtLink>
      </nav>
      <a :href="`tel:${c.phone}`" class="font-medium text-brand">{{ c.phone }}</a>
    </div>
  </header>
</template>
```

- [ ] **Step 2: Записать `AppFooter.vue`**

```vue
<script setup lang="ts">
const c = useContacts()
</script>

<template>
  <footer class="mt-16 border-t bg-ink text-white">
    <div class="mx-auto grid max-w-6xl gap-6 px-4 py-10 md:grid-cols-3">
      <div>
        <div class="text-lg font-bold">{{ c.name }}</div>
        <p class="mt-2 text-sm text-gray-300">{{ c.tagline }}</p>
      </div>
      <div class="text-sm text-gray-300">
        <p>{{ c.address }}</p>
        <p>{{ c.schedule }}</p>
      </div>
      <div class="text-sm">
        <a :href="`tel:${c.phone}`" class="block hover:text-brand-light">{{ c.phone }}</a>
        <a :href="`mailto:${c.email}`" class="block hover:text-brand-light">{{ c.email }}</a>
      </div>
    </div>
  </footer>
</template>
```

- [ ] **Step 3: Записать `layouts/default.vue`**

```vue
<template>
  <div class="flex min-h-screen flex-col">
    <AppHeader />
    <main class="flex-1">
      <slot />
    </main>
    <AppFooter />
  </div>
</template>
```

- [ ] **Step 4: Commit**

```bash
git add frontend/components/layout/ frontend/layouts/
git commit -m "feat(frontend): add header, footer and default layout"
```

---

## Task 10: Секции лендинга

Каждая секция — самодостаточный компонент с пропсами. Данные не тянут сами (кроме статики из app.config) — приходят сверху. Это и обеспечивает перекомпоновку.

**Files:**
- Create: `frontend/components/sections/SectionHero.vue`, `SectionServicesTeaser.vue`, `SectionPricesTeaser.vue`, `SectionPortfolioTeaser.vue`, `SectionAbout.vue`, `SectionCta.vue`, `SectionContacts.vue`

- [ ] **Step 1: `SectionHero.vue`**

```vue
<script setup lang="ts">
const c = useContacts()
</script>

<template>
  <section class="bg-gradient-to-br from-ink to-gray-700 text-white">
    <div class="mx-auto max-w-6xl px-4 py-24 text-center">
      <h1 class="text-4xl font-bold md:text-5xl">{{ c.name }}</h1>
      <p class="mx-auto mt-4 max-w-2xl text-lg text-gray-200">{{ c.tagline }}</p>
      <NuxtLink to="/contact" class="mt-8 inline-block rounded-card bg-brand px-6 py-3 font-medium hover:bg-brand-dark">
        Оставить заявку
      </NuxtLink>
    </div>
  </section>
</template>
```

- [ ] **Step 2: `SectionServicesTeaser.vue`**

```vue
<script setup lang="ts">
defineProps<{ title?: string }>()
const services = [
  { icon: '🏠', title: 'Строительство', text: 'Дома, коттеджи, пристройки под ключ.' },
  { icon: '🧱', title: 'Фасадные работы', text: 'Утепление, отделка, вентфасады.' },
  { icon: '🛠️', title: 'Ремонт', text: 'Капитальный и косметический ремонт.' },
]
</script>

<template>
  <section class="mx-auto max-w-6xl px-4 py-16">
    <h2 class="text-3xl font-bold text-ink">{{ title ?? 'Услуги' }}</h2>
    <div class="mt-8 grid gap-6 md:grid-cols-3">
      <div v-for="s in services" :key="s.title" class="rounded-card border p-6">
        <div class="text-3xl">{{ s.icon }}</div>
        <h3 class="mt-3 text-lg font-semibold text-ink">{{ s.title }}</h3>
        <p class="mt-1 text-muted">{{ s.text }}</p>
      </div>
    </div>
  </section>
</template>
```

- [ ] **Step 3: `SectionPricesTeaser.vue`**

```vue
<template>
  <section class="bg-gray-50">
    <div class="mx-auto max-w-6xl px-4 py-16 text-center">
      <h2 class="text-3xl font-bold text-ink">Прозрачные цены</h2>
      <p class="mt-3 text-muted">Фиксируем стоимость в договоре. Без скрытых доплат.</p>
      <NuxtLink to="/prices" class="mt-6 inline-block rounded-card border border-brand px-6 py-3 font-medium text-brand hover:bg-brand/10">
        Смотреть прайс
      </NuxtLink>
    </div>
  </section>
</template>
```

- [ ] **Step 4: `SectionPortfolioTeaser.vue`**

```vue
<script setup lang="ts">
import type { ArticleListItem } from '~/types/api'
defineProps<{ items: ArticleListItem[] }>()
</script>

<template>
  <section class="mx-auto max-w-6xl px-4 py-16">
    <div class="flex items-center justify-between">
      <h2 class="text-3xl font-bold text-ink">Наши работы</h2>
      <NuxtLink to="/portfolio" class="text-brand hover:underline">Все проекты →</NuxtLink>
    </div>
    <div v-if="items.length" class="mt-8 grid gap-6 md:grid-cols-3">
      <PortfolioCard v-for="a in items" :key="a.id" :article="a" />
    </div>
    <p v-else class="mt-8 text-muted">Скоро здесь появятся наши проекты.</p>
  </section>
</template>
```

- [ ] **Step 5: `SectionAbout.vue`**

```vue
<template>
  <section class="mx-auto max-w-6xl px-4 py-16">
    <h2 class="text-3xl font-bold text-ink">О компании</h2>
    <div class="mt-6 grid gap-8 md:grid-cols-3">
      <div><div class="text-3xl font-bold text-brand">10+</div><p class="text-muted">лет на рынке</p></div>
      <div><div class="text-3xl font-bold text-brand">200+</div><p class="text-muted">завершённых объектов</p></div>
      <div><div class="text-3xl font-bold text-brand">5 лет</div><p class="text-muted">гарантии на работы</p></div>
    </div>
  </section>
</template>
```

- [ ] **Step 6: `SectionCta.vue`** (форма обратного звонка)

```vue
<script setup lang="ts">
const api = useApi()
</script>

<template>
  <section class="bg-brand/10">
    <div class="mx-auto max-w-3xl px-4 py-16 text-center">
      <h2 class="text-3xl font-bold text-ink">Перезвоните мне</h2>
      <p class="mt-2 text-muted">Оставьте телефон — перезвоним и проконсультируем.</p>
      <div class="mx-auto mt-6 max-w-md text-left">
        <CallbackForm :on-submit="api.sendCallback" />
      </div>
    </div>
  </section>
</template>
```

- [ ] **Step 7: `SectionContacts.vue`**

```vue
<script setup lang="ts">
const c = useContacts()
</script>

<template>
  <section class="mx-auto max-w-6xl px-4 py-16">
    <h2 class="text-3xl font-bold text-ink">Контакты</h2>
    <div class="mt-6 space-y-2 text-ink">
      <p><a :href="`tel:${c.phone}`" class="text-brand">{{ c.phone }}</a></p>
      <p><a :href="`mailto:${c.email}`" class="text-brand">{{ c.email }}</a></p>
      <p>{{ c.address }}</p>
      <p class="text-muted">{{ c.schedule }}</p>
    </div>
  </section>
</template>
```

- [ ] **Step 8: Commit**

```bash
git add frontend/components/sections/
git commit -m "feat(frontend): add landing section components"
```

---

## Task 11: PortfolioCard

**Files:**
- Create: `frontend/components/ui/PortfolioCard.vue`

- [ ] **Step 1: Записать `PortfolioCard.vue`**

```vue
<script setup lang="ts">
import type { ArticleListItem } from '~/types/api'
const props = defineProps<{ article: ArticleListItem }>()
const config = useRuntimeConfig()
const img = computed(() =>
  props.article.thumbnailPath
    ? `${config.public.apiClientBase}${props.article.thumbnailPath}`
    : null,
)
</script>

<template>
  <NuxtLink :to="`/portfolio/${article.slug}`" class="group block overflow-hidden rounded-card border">
    <div class="aspect-video bg-gray-100">
      <img v-if="img" :src="img" :alt="article.title" class="h-full w-full object-cover transition group-hover:scale-105" />
    </div>
    <div class="p-4">
      <h3 class="font-semibold text-ink group-hover:text-brand">{{ article.title }}</h3>
      <p v-if="article.summary" class="mt-1 line-clamp-2 text-sm text-muted">{{ article.summary }}</p>
    </div>
  </NuxtLink>
</template>
```

- [ ] **Step 2: Commit**

```bash
git add frontend/components/ui/PortfolioCard.vue
git commit -m "feat(frontend): add PortfolioCard component"
```

---

## Task 12: Главная страница (композиция секций)

**Files:**
- Create: `frontend/pages/index.vue`

- [ ] **Step 1: Записать `pages/index.vue`**

Лендинг тянет данные для тизера портфолио и собирает секции по порядку. Порядок секций здесь — единственное место для перекомпоновки.

```vue
<script setup lang="ts">
const api = useApi()
const { data } = await useAsyncData('home-portfolio', () => api.getArticles(1, 3))

const c = useContacts()
useSeoMeta({
  title: `${c.name} — ${c.tagline}`,
  description: 'Строительство, фасадные работы и ремонт под ключ. Прозрачные цены, гарантия 5 лет.',
  ogTitle: c.name,
  ogDescription: c.tagline,
})
</script>

<template>
  <div>
    <SectionHero />
    <SectionServicesTeaser />
    <SectionPricesTeaser />
    <SectionPortfolioTeaser :items="data?.items ?? []" />
    <SectionAbout />
    <SectionCta />
  </div>
</template>
```

- [ ] **Step 2: Проверить dev-рендер**

Запустить бэкенд (`cd backend/src/Api && dotnet run`, порт 8081), затем:
```bash
cd frontend && npm run dev
```
Открыть http://localhost:3001 — лендинг рендерится, тизер портфолио показывает до 3 карточек или заглушку «Скоро…».

- [ ] **Step 3: Commit**

```bash
git add frontend/pages/index.vue
git commit -m "feat(frontend): add landing page composed of sections"
```

---

## Task 13: PriceTable + страница цен

**Files:**
- Create: `frontend/components/ui/PriceTable.vue`, `frontend/pages/prices.vue`

- [ ] **Step 1: Записать `PriceTable.vue`**

```vue
<script setup lang="ts">
import type { PriceGroup } from '~/lib/prices'
import { formatPrice } from '~/lib/prices'
defineProps<{ groups: PriceGroup[] }>()
</script>

<template>
  <div class="space-y-10">
    <div v-for="g in groups" :key="g.category">
      <h2 class="text-2xl font-bold text-ink">{{ g.category }}</h2>
      <table class="mt-4 w-full border-collapse text-left">
        <tbody>
          <tr v-for="item in g.items" :key="item.id" class="border-b">
            <td class="py-3">
              <div class="font-medium text-ink">{{ item.name }}</div>
              <div v-if="item.description" class="text-sm text-muted">{{ item.description }}</div>
            </td>
            <td class="py-3 text-right font-medium text-brand whitespace-nowrap">{{ formatPrice(item) }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>
```

- [ ] **Step 2: Записать `pages/prices.vue`**

```vue
<script setup lang="ts">
import { groupByCategory } from '~/lib/prices'
const api = useApi()
const { data } = await useAsyncData('prices', () => api.getPrices())
const groups = computed(() => groupByCategory(data.value ?? []))

useSeoMeta({
  title: 'Цены на строительные работы',
  description: 'Актуальные цены на строительство, фасадные работы и ремонт.',
})
</script>

<template>
  <div class="mx-auto max-w-4xl px-4 py-12">
    <h1 class="text-3xl font-bold text-ink">Цены</h1>
    <PriceTable v-if="groups.length" :groups="groups" class="mt-8" />
    <p v-else class="mt-8 text-muted">Прайс уточняется. Свяжитесь с нами для расчёта.</p>
  </div>
</template>
```

- [ ] **Step 3: Commit**

```bash
git add frontend/components/ui/PriceTable.vue frontend/pages/prices.vue
git commit -m "feat(frontend): add prices page with grouped price table"
```

---

## Task 14: Лента портфолио с пагинацией

**Files:**
- Create: `frontend/pages/portfolio/index.vue`

- [ ] **Step 1: Записать `pages/portfolio/index.vue`**

Пагинация через query-параметр `?page=`. Для SSG все страницы попадут в пререндер за счёт `crawlLinks` (ссылки пагинации внизу).

```vue
<script setup lang="ts">
const api = useApi()
const route = useRoute()
const page = computed(() => Math.max(1, Number(route.query.page) || 1))
const pageSize = 12

const { data } = await useAsyncData(
  () => `portfolio-${page.value}`,
  () => api.getArticles(page.value, pageSize),
  { watch: [page] },
)

const totalPages = computed(() =>
  data.value ? Math.max(1, Math.ceil(data.value.total / data.value.pageSize)) : 1,
)

useSeoMeta({
  title: 'Портфолио — наши работы',
  description: 'Реализованные проекты: строительство, фасады, ремонт.',
})
</script>

<template>
  <div class="mx-auto max-w-6xl px-4 py-12">
    <h1 class="text-3xl font-bold text-ink">Портфолио</h1>

    <div v-if="data && data.items.length" class="mt-8 grid gap-6 md:grid-cols-3">
      <PortfolioCard v-for="a in data.items" :key="a.id" :article="a" />
    </div>
    <p v-else class="mt-8 text-muted">Скоро здесь появятся наши проекты.</p>

    <nav v-if="totalPages > 1" class="mt-10 flex justify-center gap-2">
      <NuxtLink
        v-for="p in totalPages"
        :key="p"
        :to="p === 1 ? '/portfolio' : `/portfolio?page=${p}`"
        class="rounded-card border px-4 py-2"
        :class="p === page ? 'bg-brand text-white' : 'text-ink hover:border-brand'"
      >
        {{ p }}
      </NuxtLink>
    </nav>
  </div>
</template>
```

- [ ] **Step 2: Commit**

```bash
git add frontend/pages/portfolio/index.vue
git commit -m "feat(frontend): add portfolio list page with pagination"
```

---

## Task 15: Детальная страница статьи

**Files:**
- Create: `frontend/pages/portfolio/[slug].vue`

- [ ] **Step 1: Записать `pages/portfolio/[slug].vue`**

Контент — санитайзенный сервером HTML, рендерим через `v-html`. Медиа-галерея из `article.media`. На 404 — `createError`.

```vue
<script setup lang="ts">
const api = useApi()
const route = useRoute()
const config = useRuntimeConfig()
const slug = route.params.slug as string

const { data: article } = await useAsyncData(`article-${slug}`, () => api.getArticle(slug))

if (!article.value) {
  throw createError({ statusCode: 404, statusMessage: 'Статья не найдена', fatal: true })
}

const mediaUrl = (path: string) => `${config.public.apiClientBase}${path}`
const sortedMedia = computed(() =>
  [...(article.value?.media ?? [])].sort((a, b) => a.sortOrder - b.sortOrder),
)

useSeoMeta({
  title: () => article.value?.title ?? 'Проект',
  description: () => article.value?.summary ?? '',
  ogTitle: () => article.value?.title ?? '',
  ogImage: () => (article.value?.thumbnailPath ? mediaUrl(article.value.thumbnailPath) : undefined),
})
</script>

<template>
  <article v-if="article" class="mx-auto max-w-3xl px-4 py-12">
    <NuxtLink to="/portfolio" class="text-brand hover:underline">← Все проекты</NuxtLink>
    <h1 class="mt-4 text-3xl font-bold text-ink">{{ article.title }}</h1>
    <p v-if="article.summary" class="mt-2 text-lg text-muted">{{ article.summary }}</p>

    <!-- Контент санитайзится на бэкенде (HtmlSanitizer), v-html безопасен -->
    <div class="prose mt-8 max-w-none" v-html="article.content" />

    <div v-if="sortedMedia.length" class="mt-10 grid gap-4 sm:grid-cols-2">
      <img
        v-for="m in sortedMedia"
        :key="m.id"
        :src="mediaUrl(m.path)"
        :alt="m.alt ?? article.title"
        class="w-full rounded-card object-cover"
      />
    </div>
  </article>
</template>
```

- [ ] **Step 2: Commit**

```bash
git add frontend/pages/portfolio/[slug].vue
git commit -m "feat(frontend): add article detail page with HTML content and media"
```

---

## Task 16: Страница контактов

**Files:**
- Create: `frontend/pages/contact.vue`

- [ ] **Step 1: Записать `pages/contact.vue`**

```vue
<script setup lang="ts">
const api = useApi()
const c = useContacts()

useSeoMeta({
  title: 'Контакты',
  description: `Свяжитесь с нами: ${c.phone}, ${c.email}`,
})
</script>

<template>
  <div class="mx-auto max-w-6xl px-4 py-12">
    <h1 class="text-3xl font-bold text-ink">Контакты</h1>
    <div class="mt-8 grid gap-12 md:grid-cols-2">
      <div class="space-y-3 text-ink">
        <p><a :href="`tel:${c.phone}`" class="text-lg text-brand">{{ c.phone }}</a></p>
        <p><a :href="`mailto:${c.email}`" class="text-brand">{{ c.email }}</a></p>
        <p>{{ c.address }}</p>
        <p class="text-muted">{{ c.schedule }}</p>
      </div>
      <div>
        <h2 class="text-xl font-semibold text-ink">Написать нам</h2>
        <div class="mt-4">
          <ContactForm :on-submit="api.sendContact" />
        </div>
      </div>
    </div>
  </div>
</template>
```

- [ ] **Step 2: Commit**

```bash
git add frontend/pages/contact.vue
git commit -m "feat(frontend): add contact page with contact form"
```

---

## Task 17: SEO — robots.txt и sitemap

**Files:**
- Create: `frontend/public/robots.txt`
- Modify: `frontend/nuxt.config.ts` (добавить `site` метаданные, опц. модуль sitemap)

- [ ] **Step 1: Установить модуль sitemap**

```bash
cd frontend
npm install -D @nuxtjs/sitemap
```

- [ ] **Step 2: Подключить модуль и задать site.url в `nuxt.config.ts`**

В массив `modules` добавить `'@nuxtjs/sitemap'`. Добавить на верхний уровень конфига:
```ts
  site: { url: process.env.NUXT_PUBLIC_SITE_URL || 'http://localhost:3001' },
```
Модуль автоматически соберёт `sitemap.xml` из пререндеренных маршрутов на билде.

- [ ] **Step 3: Записать `public/robots.txt`**

```
User-agent: *
Allow: /
Sitemap: /sitemap.xml
```

- [ ] **Step 4: Проверить генерацию**

```bash
npm run generate
```
Expected: в `.output/public/` присутствуют `sitemap.xml`, `robots.txt`, `index.html`, `prices/index.html`, `portfolio/index.html`, `contact/index.html`. (Детальные статьи попадут, если бэкенд доступен на билде.)

- [ ] **Step 5: Commit**

```bash
git add frontend/public/robots.txt frontend/nuxt.config.ts frontend/package.json
git commit -m "feat(frontend): add sitemap and robots.txt for SEO"
```

---

## Task 18: Полная проверка сборки и тестов

**Files:** нет (верификация)

- [ ] **Step 1: Прогнать все тесты**

```bash
cd frontend && npm test
```
Expected: PASS — api.spec (3), prices.spec (4), CallbackForm.spec (3), ContactForm.spec (2). Итого 12.

- [ ] **Step 2: Типчек**

```bash
npx nuxi typecheck
```
Expected: без ошибок.

- [ ] **Step 3: Статическая генерация с запущенным бэкендом**

В одном терминале: `cd backend/src/Api && dotnet run` (порт 8081).
В другом:
```bash
cd frontend && npm run generate
```
Expected: сборка успешна, статика в `.output/public/`, статьи портфолио пререндерены.

- [ ] **Step 4: Превью статики**

```bash
npx serve frontend/.output/public
```
Открыть в браузере, проверить: все 4 страницы, навигация, карточки портфолио, детальная статья, отправка обеих форм (формы бьют в рантайме — нужен запущенный бэкенд + CORS/прокси; в проде через nginx `/api`).

- [ ] **Step 5: Финальный commit**

```bash
git add -A frontend/
git commit -m "chore(frontend): verify build, tests and static generation"
```

---

## Заметки по интеграции (не входят в этот план)

- **CORS для dev:** формы в dev бьют на `http://localhost:8081` с origin `http://localhost:3001` — на бэкенде нужен CORS для dev-origin (или vite-прокси на `/api`). Уточнить в плане инфры/деплоя.
- **Прод baseURL:** при `npm run generate` в docker задать `NUXT_PUBLIC_API_BASE=http://backend:8080` (server-side билд), `NUXT_PUBLIC_API_CLIENT_BASE=/api` (рантайм через nginx), `NUXT_PUBLIC_SITE_URL=https://<домен>`.
- **Ребилд статики при новом контенте** — скрипт/вебхук в плане инфры.
- **`prose`-стили** для `v-html` контента — подключить `@tailwindcss/typography` (можно добавить при первом наполнении реальным контентом).
```
