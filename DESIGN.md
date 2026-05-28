# Сайт-визитка строительной компании — Design Document

**Дата:** 2025-05-28
**Статус:** Approved

## Обзор

Простой сайт-визитка для строительной компании с админкой для управления контентом.

**Требования:**
- Лендинг с описанием услуг
- Цены на типовые работы
- Портфолио (лента статей с фото/видео)
- Контакты
- Форма "Перезвоните мне"
- Форма "Написать нам"
- Админка для хозяев

**Ограничения:**
- Хостинг: Yandex VPS
- Стек: .NET 10 (backend), Nuxt 3 (frontend)
- База: SQLite (до 200 статей, тысячи фото)
- Один админ-пользователь

---

## Архитектура

```
┌─────────────────────────────────────────────────────────────┐
│                         Интернет                            │
└────────────────────────┬────────────────────────────────────┘
                         │ 80/443
                    ┌────▼────┐
                    │  nginx  │  (SSL termination, reverse proxy)
                    └────┬────┘
         ┌──────────────┼──────────────┐
         │              │              │
    ┌────▼────┐   ┌────▼────┐   ┌────▼────┐
    │frontend │   │backend  │   │  admin  │  (served by frontend)
    │ :3000   │   │ :8080   │   │         │
    └────┬────┘   └────┬────┘   └─────────┘
         │              │
         │         ┌────▼────┐
         │         │  SQLite │
         │         │database │
         │         └─────────┘
         │
    ┌────▼────┐
    │ static  │  (media files: photos, videos)
    │ files   │
    └─────────┘
```

**Контейнеры:**

| Контейнер | Порты | Роль |
|-----------|-------|------|
| `frontend` | 3000 (internal) | Nuxt 3, SSR/SSG |
| `backend` | 8080 (internal) | .NET Web API |
| `nginx` | 80/443 (external) | Reverse proxy, SSL |

> **Примечание для локальной разработки:** При локальной разработке `npm run dev` использует порт 3001, `dotnet run` — порт 8081. В docker-compose порты стандартные (изоляция контейнеров).

---

## Фронтенд (Nuxt 3)

**Структура:**

```
frontend/
├── pages/
│   ├── index.vue              ── Главная (лендинг)
│   ├── prices.vue             ── Цены на типовые работы
│   ├── portfolio/
│   │   ├── index.vue          ── Лента портфолио
│   │   └── [slug].vue         ── Детальная статья/проект
│   └── contact.vue            ── Контакты
│
├── components/
│   ├── Header.vue
│   ├── Footer.vue
│   ├── CallbackForm.vue       ── "Перезвоните мне"
│   ├── ContactForm.vue        ── "Написать нам"
│   └── PortfolioCard.vue
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

**Технологии:** Nuxt 3, Tailwind CSS, VueUse, TypeScript, Vitest

---

## Бэкенд (.NET 10)

**Структура:**

```
backend/
├── src/
│   ├── Api/                    ── Web API
│   ├── Core/                   ── Domain (Entities, DTOs, Interfaces)
│   └── Infrastructure/         ── Data (EF Core, SQLite, Repositories)
│
├── tests/
│   ├── Unit/                   ── Business logic tests
│   └── Integration/           ── API endpoint tests
│
└── backend.sln
```

**Технологии:** .NET 10, ASP.NET Core, EF Core, SQLite, Serilog, FluentValidation, xUnit

---

## Модель данных

**Таблицы:**

- `Articles` — статьи/портфолио
- `ServicePrices` — цены на услуги
- `Callbacks` — заявки "перезвоните"
- `Contacts` — сообщения "написать нам"

**API endpoints:**

```
GET    /api/articles                    ── список (публичные)
GET    /api/articles/{slug}             ── детальная

GET    /api/services/prices             ── цены

POST   /api/callbacks                   ── создать заявку
POST   /api/contacts                    ── создать сообщение

-- Админка (auth required)
GET    /api/admin/articles              ── все (вкл. черновики)
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

POST   /api/admin/media/upload
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
│   ├── frontend.Dockerfile
│   ├── backend.Dockerfile
│   └── nginx.conf
├── docker-compose.yml
├── docker-compose.prod.yml
├── .env.example
├── .env (gitignored)
├── README.md
├── DEPLOY.md
└── scripts/
    ├── backup.sh
    └── deploy.sh
```

**CI/CD:**
- GitHub Actions для тестов (на каждый push)
- GitHub Actions для деплоя (manual trigger)
- Auto-backup cron на VPS

---

## Технологический стек

| Слой | Технология |
|------|------------|
| Frontend | Nuxt 3, Vue 3, Tailwind CSS, TypeScript |
| Backend | .NET 10, ASP.NET Core, EF Core |
| Database | SQLite |
| Testing | Vitest (frontend), xUnit (backend) |
| Deployment | Docker, Docker Compose, nginx |
| CI/CD | GitHub Actions |

---

## Следующие шаги

1. Создание implementation plan (writing-plans skill)
2. Генерация scafffolding проектов
3. Реализация core features
4. Настройка CI/CD
5. Деплой на Yandex VPS
