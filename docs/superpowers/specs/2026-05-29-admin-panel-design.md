# Админка: авторизация, CRUD, медиа, WYSIWYG — Design Document

**Дата:** 2026-05-29
**Статус:** Approved
**Базовые документы:** дополняет `DESIGN.md` и `docs/superpowers/specs/2026-05-28-content-and-architecture-revisions-design.md`

## Обзор

Админка для хозяев строительной компании: управление контентом сайта-визитки
(статьи/портфолио, цены), обработка заявок и сообщений, загрузка фото и
богатый WYSIWYG-редактор. Бэкенд (.NET 10) и фронт (Nuxt 3) пилятся в рамках
одного подпроекта.

**Текущее состояние:** на бэке только публичные контроллеры (Articles, Callbacks,
Contacts, Services), авторизации/админских CRUD/загрузки медиа нет. На фронте
только публичные страницы, Tiptap не подключён. Админка пилится с нуля.

**Решённые на брейншторме вопросы:**
- Учётки админов — таблица `Users` в БД (BCrypt-хеши), первый админ сидится из `.env`.
- Фронт-админка — client-only роуты в том же Nuxt-приложении (один деплой).
- Загружаемые фото — ресайз + генерация тумб через ImageSharp.
- JWT-токен на фронте — `localStorage`, без refresh (1–2 админа).
- Публикация статьи ставит `IsPublished` в БД; пересборка SSG-статики — задача фазы инфры.

**Вне области (YAGNI):** refresh-токены, роли/права (все админы равноправны),
редактор цен через Tiptap (цены — обычная форма), вебхук-ребилд статики.

---

## 1. Авторизация (бэкенд)

### Модель
Новая сущность `User`:

```csharp
namespace Core.Entities;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;  // BCrypt
    public DateTime CreatedAt { get; set; }
}
```

EF-конфигурация: `Username` уникален. Новая миграция.

### Сид первого админа
При старте приложения, если таблица `Users` пуста, создаётся админ из переменных
окружения `ADMIN_USERNAME` / `ADMIN_PASSWORD` (хеш через BCrypt). Если переменных
нет — сид пропускается с warning в лог. Идемпотентно (не пересоздаёт существующих).

### Сервис и эндпоинт
- `IAuthService` / `AuthService`: `Task<AuthResult?> AuthenticateAsync(login, password)` —
  ищет пользователя, сверяет BCrypt-хеш, при успехе генерирует JWT.
- JWT: подпись HMAC-SHA256 из `JWT_SECRET`, claim `sub`=username, срок жизни из
  конфига (`Jwt:ExpiresHours`, дефолт 8 ч).
- `POST /api/admin/auth` → `{ token, expiresAt }`; неверные креды → `401`.

### Защита
JWT-аутентификация регистрируется в `Program.cs` (`AddAuthentication().AddJwtBearer`).
Все админские контроллеры помечены `[Authorize]`. Запрос без/с невалидным токеном → `401`.

---

## 2. Админские эндпоинты (бэкенд)

Отдельный namespace `Api.Controllers.Admin` — защищённые контроллеры не смешиваются
с тонкими публичными. Все под `[Authorize]`.

### Articles
| Метод | Путь | Назначение |
|-------|------|------------|
| GET | `/api/admin/articles?page&pageSize` | все статьи (включая черновики), пагинация |
| GET | `/api/admin/articles/{id}` | одна статья по id (для редактирования) |
| POST | `/api/admin/articles` | создать |
| PUT | `/api/admin/articles/{id}` | обновить |
| DELETE | `/api/admin/articles/{id}` | удалить |

- HTML-контент (`Content`) прогоняется через существующий `HtmlSanitizerService`
  на создании и обновлении.
- Расширяем `IArticleService` админскими методами (`GetAllForAdminAsync`, `GetByIdAsync`,
  `CreateAsync`, `UpdateAsync`, `DeleteAsync`) — переиспользуем сервисный слой.

### Services (цены)
`GET / POST / PUT / DELETE /api/admin/services` — CRUD цен. Расширяем `IServicePriceService`.

### Callbacks / Contacts (входящие заявки)
- `GET /api/admin/callbacks` — список заявок «перезвоните».
- `PATCH /api/admin/callbacks/{id}` — пометить обработанной (поле `IsHandled`/статус).
- `GET /api/admin/contacts` — список сообщений «написать нам».
- `PATCH /api/admin/contacts/{id}` — пометить обработанным.
- При необходимости добавить поле статуса в сущности `Callback`/`Contact` + миграция.

### Media
`POST /api/admin/media/upload` (multipart/form-data):
1. Валидация типа (jpeg/png/webp) и размера (лимит, напр. 15 МБ).
2. ImageSharp: ресайз оригинала до макс. 1920px по большей стороне (если больше) +
   генерация тумбы (напр. 400px). Сохранение в `/uploads` под уникальными именами.
3. Привязка к статье (если передан `articleId`) → запись `ArticleMedia`.
4. Ответ: `{ url, thumbnailUrl, mediaId }`.
- Новый `IMediaService` / `MediaService` (Infrastructure). Путь `/uploads`
  конфигурируется (`Storage:UploadsPath`).

---

## 3. Чистка долгов бэка (делаем в этой фазе)

- `Article.PublishedAt` → **nullable** (`DateTime?`). Убираем sentinel `0001-01-01`;
  черновик = `null`. Правка сущности, конфигурации, миграция, публичные запросы
  (фильтр `IsPublished` уже есть; сортировка по `PublishedAt` учитывает null).
- Дубликат `slug` при create/update → доменная ошибка → **409 Conflict** (не 500
  от unique-constraint). Проверка наличия slug перед сохранением.
- Валидация slug при `Update` (формат + непустой), как при create.
- Авторизация на Create/Update/Delete закрыта `[Authorize]` (см. раздел 1).

---

## 4. Фронт-админка (Nuxt 3, client-only)

### Режим рендеринга
- `nuxt.config.ts` route rules: `/admin/**` → `ssr: false` (client-rendered SPA-острова).
- Исключение `/admin/**` из prerender (`nitro.prerender.ignore`) и из sitemap.

### Авторизация на фронте
- `composables/useAuth.ts`: хранит JWT в `localStorage`, методы `login/logout`,
  `isAuthenticated`, `token`.
- `useApi` добавляет заголовок `Authorization: Bearer <token>` для админских вызовов;
  на `401` — сброс токена и редирект на логин.
- `middleware/auth.ts` (на `/admin/**`, кроме `/admin/login`): нет токена → редирект на логин.

### Страницы
| Путь | Назначение |
|------|------------|
| `/admin/login` | форма входа |
| `/admin` | дашборд (счётчики: статьи, новые заявки) |
| `/admin/articles` | список статей + черновики, кнопки edit/delete/new |
| `/admin/articles/new` | создание статьи (редактор) |
| `/admin/articles/[id]` | редактирование статьи (редактор) |
| `/admin/prices` | CRUD цен (обычная таблица-форма) |
| `/admin/inbox` | заявки «перезвоните» + сообщения «написать нам», отметка обработанных |

### WYSIWYG-редактор
`components/admin/ArticleEditor.vue` на **Tiptap**:
- Форматирование: жирный, курсив, заголовки (H2/H3), списки (ul/ol), ссылки.
- Вставка изображения: загрузка через `POST /api/admin/media/upload` → вставка `<img>` с URL.
- Вставка видео: ввод ссылки (vk/rutube/youtube) → безопасный `<iframe>`-embed.
  Финальная подчистка — бэковый `HtmlSanitizerService` по whitelist доменов.
- На выходе — HTML-строка в поле `Content` статьи.

### Публичный рендер контента
Подключить `@tailwindcss/typography`; `v-html` контента статьи оборачивается в `prose`
(закрытая недоделка из реализации публичного фронта).

---

## 5. Тестирование

**Бэкенд (xUnit):**
- `AuthService`: BCrypt verify (успех/провал), генерация/валидность JWT, expiry.
- Сид админа: создаёт при пустой таблице, пропускает при наличии.
- Admin CRUD (integration): 401 без токена, 200 с токеном, 409 на дубль slug,
  валидация slug при update.
- `MediaService`: ресайз большого изображения, генерация тумбы, отказ на неверный тип/размер.

**Фронт (Vitest):**
- `useAuth`: сохранение/чтение/сброс токена.
- `middleware/auth`: редирект без токена.
- `ArticleEditor`: базовый компонентный тест (рендер, эмит контента).

---

## 6. Технологии (добавляемое)

| Слой | Добавляем |
|------|-----------|
| Backend auth | `Microsoft.AspNetCore.Authentication.JwtBearer`, `BCrypt.Net-Next` |
| Backend media | `SixLabors.ImageSharp` |
| Frontend | `@tiptap/vue-3` (+ starter-kit, нужные расширения), `@tailwindcss/typography` |

---

## 7. Конфигурация (новые переменные)

| Переменная | Назначение |
|------------|------------|
| `ADMIN_USERNAME` / `ADMIN_PASSWORD` | сид первого админа |
| `JWT_SECRET` | подпись JWT |
| `Jwt:ExpiresHours` | срок жизни токена (дефолт 8) |
| `Storage:UploadsPath` | путь к каталогу `/uploads` |

Добавить в `.env.example`.

---

## Сводка изменений к DESIGN.md

| Область | Было | Стало |
|---------|------|-------|
| Users | — | сущность `User` + BCrypt + сид из env |
| Auth | схема описана, не реализована | JWT-bearer, `POST /api/admin/auth`, `[Authorize]` |
| Admin API | описано в DESIGN | реализуется (Articles/Services/Callbacks/Contacts/Media) |
| `Article.PublishedAt` | sentinel-дата | nullable |
| Дубль slug | 500 | 409 Conflict |
| Media | описано | ImageSharp ресайз + тумбы, `IMediaService` |
| Фронт-админка | заглушка в структуре | client-only роуты, Tiptap, страницы admin |
| Публичный prose | плоский v-html | `@tailwindcss/typography` |
