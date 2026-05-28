# Сайт-визитка строительной компании — Design Document

**Дата:** 2026-05-28
**Статус:** Approved
**Ревизии:** учитывает `docs/superpowers/specs/2026-05-28-content-and-architecture-revisions-design.md`

## Обзор

Простой сайт-визитка для строительной компании с админкой для управления контентом.

**Требования:**
- Лендинг с описанием услуг
- Цены на типовые работы
- Портфолио (лента статей с фото/видео)
- Контакты
- Форма "Перезвоните мне"
- Форма "Написать нам"
- Админка для хозяев (богатый контент: форматированный текст, фото, встроенное видео)

**Ограничения:**
- Хостинг: Yandex VPS
- Стек: .NET 10 (backend), Nuxt 3 (frontend, SSG)
- База: SQLite (до 200 статей, тысячи фото; 1–2 админа наполняют ~раз в неделю)
- Один-два админ-пользователя

---

## Архитектура

```
┌─────────────────────────────────────────────────────────────┐
│                         Интернет                            │
└────────────────────────┬────────────────────────────────────┘
                         │ 80/443
                    ┌────▼────┐
                    │  nginx  │  SSL termination, статика фронта,
                    └────┬────┘  reverse proxy на /api, раздача /uploads
              ┌──────────┴──────────┐
              │                     │
        статика фронта         ┌────▼────┐
        (SSG, .output)         │backend  │  .NET Web API
                               │ :8080   │
                               └────┬────┘
                                    │
                               ┌────▼────┐
                               │  SQLite │
                               │database │
                               └─────────┘
```

**Контейнеры (2):**

| Контейнер | Порты | Роль |
|-----------|-------|------|
| `backend` | 8080 (internal) | .NET Web API |
| `nginx` | 80/443 (external) | Раздача статики фронта (SSG), reverse proxy на `/api`, раздача `/uploads` |

Отдельного Node-контейнера на проде **нет** — фронт собирается в статику (`nuxt generate`)
и раздаётся nginx. Node нужен только на этапе сборки.

> **Примечание для локальной разработки:** `npm run dev` использует порт 3001,
> `dotnet run` — порт 8081. В docker-compose порты стандартные (изоляция контейнеров).

> **Migration path на SSR:** код фронта не зависит от режима рендеринга. При
> необходимости перехода на SSR — вернуть `frontend`-контейнер (Node), сменить
> nitro-пресет и команду сборки (`nuxt generate` → `nuxt build`). Переписывания кода нет.

---

## Фронтенд (Nuxt 3, SSG)

**Структура:**

```
frontend/
├── pages/
│   ├── index.vue              ── Главная (лендинг)
│   ├── prices.vue             ── Цены на типовые работы
│   ├── portfolio/
│   │   ├── index.vue          ── Лента портфолио (пагинация)
│   │   └── [slug].vue         ── Детальная статья/проект
│   ├── contact.vue            ── Контакты
│   └── admin/                 ── Админка (WYSIWYG-редактор, авторизация)
│
├── components/
│   ├── Header.vue
│   ├── Footer.vue
│   ├── CallbackForm.vue       ── "Перезвоните мне"
│   ├── ContactForm.vue        ── "Написать нам"
│   ├── PortfolioCard.vue
│   └── admin/
│       └── ArticleEditor.vue  ── WYSIWYG (Tiptap)
│
├── composables/
│   ├── useApi.ts
│   └── useForms.ts
│
├── tests/
│   ├── unit/
│   │   └── composables/
│   └── component/
│       ├── CallbackForm.spec.ts
│       └── ContactForm.spec.ts
```

**Технологии:** Nuxt 3 (SSG), Tailwind CSS, VueUse, TypeScript, Tiptap (WYSIWYG), Vitest

---

## Бэкенд (.NET 10)

**Структура:**

```
backend/
├── src/
│   ├── Api/                    ── Web API (тонкие контроллеры)
│   ├── Core/                   ── Domain (Entities, DTOs, Interfaces сервисов)
│   └── Infrastructure/         ── Data (EF Core, SQLite) + реализации сервисов
│
├── tests/
│   ├── Unit/                   ── Business logic tests
│   └── Integration/           ── API endpoint tests
│
└── backend.sln
```

**Доступ к данным:** без generic-репозитория. Тонкие доменные сервисы работают
напрямую с `AppDbContext` (EF Core сам реализует Unit of Work + репозиторий).
Фильтрация, поиск и пагинация выполняются на уровне БД (`IQueryable` → SQL),
не в памяти.

- `IArticleService` / `ArticleService`
- `IServicePriceService` / `ServicePriceService`
- `ICallbackService` / `CallbackService`
- `IContactService` / `ContactService`

**Технологии:** .NET 10, ASP.NET Core, EF Core, SQLite, Serilog, FluentValidation,
HtmlSanitizer (санитайзинг WYSIWYG-контента), xUnit

---

## Модель данных

**Таблицы:**

- `Articles` — статьи/портфолио. Поле `Content` хранит **санитайзенный HTML**
  (форматированный текст + встроенные видео через `<iframe>` с доверенных доменов).
- `ArticleMedia` — фотографии статьи (связь с `Articles`): `Id`, `ArticleId`,
  `Path`, `MediaType` (`image`), `Alt`, `SortOrder`.
- `ServicePrices` — цены на услуги
- `Callbacks` — заявки "перезвоните"
- `Contacts` — сообщения "написать нам"

**Контент и медиа:**
- Текст/форматирование — HTML из WYSIWYG (Tiptap), санитайзится на бэке по whitelist.
- Фото — загрузка на сервер (`/uploads`), запись в `ArticleMedia`.
- Видео — **не хранится на сервере**; embed (`<iframe>`) только с доверенных доменов:
  `vk.com`, `rutube.ru`, `youtube.com/embed`.

**API endpoints:**

```
GET    /api/articles?page=1&pageSize=12  ── список (публичные, пагинация)
                                            ответ: { items, total, page, pageSize }
GET    /api/articles/{slug}             ── детальная

GET    /api/services/prices             ── цены

POST   /api/callbacks                   ── создать заявку
POST   /api/contacts                    ── создать сообщение

-- Админка (auth required)
GET    /api/admin/articles              ── все (вкл. черновики, пагинация)
POST   /api/admin/articles
PUT    /api/admin/articles/{id}
DELETE /api/admin/articles/{id}

GET    /api/admin/services
POST   /api/admin/services
PUT    /api/admin/services/{id}
DELETE /api/admin/services/{id}

GET    /api/admin/callbacks
PATCH  /api/admin/callbacks/{id}

GET    /api/admin/contacts
PATCH  /api/admin/contacts/{id}

POST   /api/admin/media/upload          ── загрузка фото
POST   /api/admin/auth
```

---

## Деплой

**Файловая структура:**

```
stroy-website/
├── frontend/
├── backend/
├── docker/
│   ├── backend.Dockerfile
│   ├── frontend.Dockerfile     ── multi-stage: node собирает статику → копия в nginx
│   └── nginx.conf
├── docker-compose.yml
├── docker-compose.prod.yml
├── .env.example
├── .env (gitignored)
├── README.md
├── DEPLOY.md
└── scripts/
    ├── backup.sh
    ├── deploy.sh
    └── rebuild-frontend.sh     ── пересборка статики при обновлении контента
```

**Обновление контента (SSG):**
- Страницы портфолио/цен пререндерятся на билде; интерактив (формы) — клиентский
  `$fetch` к `/api` в рантайме.
- Новый контент появляется после ребилда фронта (скрипт/кнопка деплоя; позже —
  вебхук «админ опубликовал → ребилд»). Для наполнения ~раз в неделю достаточно.

**CI/CD:**
- GitHub Actions для тестов (на каждый push)
- GitHub Actions для деплоя (manual trigger)
- Auto-backup cron на VPS

---

## Технологический стек

| Слой | Технология |
|------|------------|
| Frontend | Nuxt 3 (SSG), Vue 3, Tailwind CSS, TypeScript, Tiptap |
| Backend | .NET 10, ASP.NET Core, EF Core, HtmlSanitizer |
| Database | SQLite |
| Testing | Vitest (frontend), xUnit (backend) |
| Deployment | Docker, Docker Compose, nginx (раздача статики + reverse proxy) |
| CI/CD | GitHub Actions |

---

## Следующие шаги

1. Обновление implementation plan под новый дизайн (writing-plans skill)
2. Реализация core features (сервисы, контроллеры, контент)
3. Админка с WYSIWYG-редактором
4. Настройка CI/CD
5. Деплой на Yandex VPS
