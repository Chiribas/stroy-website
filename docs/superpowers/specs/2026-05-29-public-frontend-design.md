# Публичный фронтенд (Nuxt 3 SSG) — Design Document

**Дата:** 2026-05-29
**Статус:** Approved
**Контекст:** часть декомпозированного плана stroy-website. Базовый дизайн — `DESIGN.md`.
Бэкенд API реализован (`docs/superpowers/plans/2026-05-28-backend-api.md`).

## Обзор

Публичный (read-only + формы) фронт сайта-визитки строительной компании.
Стандартный брошюрный UI, собранный быстро, но с архитектурой, позволяющей
**перекомпоновать лендинг и сменить визуальный стиль без переписывания кода**.

**В скоупе:** лендинг, цены, портфолио (лента + детальная), контакты, формы
«Перезвоните мне» и «Написать нам». Рендеринг — SSG (`nuxt generate`).

**Вне скоупа (отдельные планы):** админка, Tiptap/WYSIWYG, JWT-авторизация,
загрузка медиа, инфра/деплой (Docker/nginx/CI/CD).

## Ключевой принцип: дешёвая перекомпоновка

Три уровня развязки:

1. **Секции — самодостаточные компоненты.** `Hero`, `ServicesTeaser`,
   `PricesTeaser`, `PortfolioTeaser`, `AboutBlock`, `CtaBlock`, `ContactsBlock`.
   Каждая получает данные через пропсы и ничего не знает об окружении.
2. **Страница = упорядоченный список секций.** Лендинг собирается из массива
   `landingSections` (порядок + пропсы), а не из хардкода в шаблоне.
   Переставить/убрать/добавить блок = правка одного списка.
3. **Дизайн-токены в одном месте** — палитра, радиусы, шрифты в `tailwind.config`
   + `app.config.ts`. Рестайл бренда = смена токенов, компоненты не трогаем.

## Структура

```
frontend/
├── app.config.ts              ── токены темы, контакты компании (телефон/адрес/соцсети)
├── nuxt.config.ts             ── SSG-пресет, runtimeConfig.public.apiBase, modules
├── tailwind.config.ts         ── дизайн-токены (цвета/шрифты/радиусы)
├── pages/
│   ├── index.vue              ── лендинг (композиция секций из landingSections)
│   ├── prices.vue             ── цены (группировка по Category)
│   ├── portfolio/
│   │   ├── index.vue          ── лента портфолио (пагинация)
│   │   └── [slug].vue         ── детальная статья (рендер санитайзенного HTML + медиа)
│   └── contact.vue            ── контакты + формы
├── components/
│   ├── layout/   Header.vue, Footer.vue
│   ├── sections/ Hero.vue, ServicesTeaser.vue, PricesTeaser.vue,
│   │             PortfolioTeaser.vue, AboutBlock.vue, CtaBlock.vue, ContactsBlock.vue
│   ├── ui/       PortfolioCard.vue, PriceTable.vue, BaseInput.vue, BaseButton.vue
│   └── forms/    CallbackForm.vue, ContactForm.vue
├── composables/
│   ├── useApi.ts              ── типизированные обёртки + DTO-типы
│   └── useContacts.ts         ── контакты компании из app.config
├── config/
│   └── landing.ts             ── landingSections (порядок + пропсы секций лендинга)
└── tests/
    ├── unit/composables/useApi.spec.ts
    └── component/CallbackForm.spec.ts, ContactForm.spec.ts
```

## Контракт API (готовый бэкенд)

```
GET  /api/articles?page=1&pageSize=12 → PagedResult { items[], total, page, pageSize }
                                         item: { id, title, slug, summary?, thumbnailPath?, publishedAt }
GET  /api/articles/{slug}             → { id, title, slug, summary?, content (HTML),
                                          thumbnailPath?, publishedAt, media[] }
                                         media: { id, path, mediaType, alt?, sortOrder }
GET  /api/services/prices             → ServicePriceDto[]
                                         { id, category, name, description?, priceFrom,
                                           priceTo?, unit?, sortOrder }
POST /api/callbacks  { phone, name? }           → { message }
POST /api/contacts   { name, phone, message }   → { message }
```

## Данные и рендеринг (SSG)

- **Пререндер на билде** (`useAsyncData` + `$fetch`): главная, цены, портфолио-лента,
  детальные статьи. Бэкенд должен быть доступен в момент `nuxt generate`.
- **Список slug'ов** для `portfolio/[slug]` — динамически в prerender-хуке
  (краулинг ссылок с `portfolio/index`); явный fallback на пустую ленту, если API
  недоступен на билде (страница соберётся, контент догрузится клиентом).
- **Формы** — клиентский `$fetch` в рантайме к `/api`, без пререндера.
- **Источник baseURL** — единственный: `runtimeConfig.public.apiBase`
  (dev → `http://localhost:8081`, прод → `/api` через nginx).
- **Контент статьи** — санитайзенный сервером HTML рендерится через `v-html`
  (доверяем санитайзингу бэка); whitelist-видео уже внутри `content`.

## Цены

`GET /api/services/prices` отдаёт плоский список. Группировка по `Category`
и сортировка по `SortOrder` — на фронте (`PriceTable` принимает уже сгруппированные
данные). Формат цены: `priceTo` есть → «от X до Y ₽», иначе «от X ₽» + `unit`.

## Формы

Валидация на фронте дублирует ограничения бэка (минимальный фидбэк до отправки):

- **CallbackForm:** `phone` обязателен (формат телефона), `name` опционален.
- **ContactForm:** `name` ≥ 2 символов, `phone` (формат), `message` ≥ 10 символов.

Состояния: idle → submitting → success (показываем `message` из ответа) / error
(сетевая ошибка или 400 с ValidationProblemDetails). Кнопка дизейблится на submit.

## SEO

- `useSeoMeta` + Open Graph на каждой странице; для статей — title/description/og:image
  из данных статьи.
- Генерация `sitemap.xml` и `robots.txt` на билде (модуль или prerender-хук).
- Канонические URL из `app.config` (базовый домен).

## Тестирование (Vitest, TDD)

- **unit:** `useApi` — формирование query (page/pageSize), типизация ответа,
  обработка ошибок.
- **component:** `CallbackForm`, `ContactForm` — валидация полей, блокировка на
  submit, отображение success/error, корректный payload.

## Технологии

Nuxt 3 (SSG-пресет), Vue 3, TypeScript, Tailwind CSS, VueUse, Vitest.
(Tiptap — НЕ здесь, он в плане админки.)

## Открытые вопросы / отложено

- Отдельная страница «О компании» — пока блок `AboutBlock` на лендинге; можно
  вынести в страницу позже (архитектура секций это позволяет).
- Вебхук «админ опубликовал → ребилд статики» — в плане инфры.
