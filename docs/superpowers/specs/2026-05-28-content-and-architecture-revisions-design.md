# Правки архитектуры: контент, сервисный слой, деплой фронта

**Дата:** 2026-05-28
**Статус:** Approved
**Базовый документ:** дополняет и корректирует `DESIGN.md`

## Обзор

Документ фиксирует согласованные изменения к исходному `DESIGN.md` сайта-визитки
строительной компании. Затрагивает три области:

1. **Контент статей** — богатый контент (форматированный текст, фото, встроенное видео).
2. **Сервисный слой бэкенда** — отказ от generic-репозитория в пользу тонких доменных
   сервисов и эффективных запросов на уровне БД (фильтрация/пагинация в SQL).
3. **Деплой фронтенда** — SSG вместо постоянного Node-процесса; на проде остаётся два
   контейнера (backend + nginx).

База остаётся SQLite (1–2 админа наполняют контент ~раз в неделю — нагрузка минимальна).
Авторизация (один-два админа) пока не меняется.

---

## 1. Контент статей

### Хранение

- Поле `Article.Content` хранит **санитайзенный HTML** (а не плоский текст).
- Фотографии статьи выносятся в связанную сущность **`ArticleMedia`**:

```csharp
namespace Core.Entities;

public class ArticleMedia
{
    public int Id { get; set; }
    public int ArticleId { get; set; }
    public Article Article { get; set; } = null!;
    public string Path { get; set; } = string.Empty;   // путь к файлу в /uploads
    public string MediaType { get; set; } = "image";    // пока только image
    public string? Alt { get; set; }
    public int SortOrder { get; set; }
}
```

- `Article.ThumbnailPath` остаётся для превью в ленте портфолио.
- **Видео НЕ хранится на сервере.** Админ вставляет ссылку на ролик с хостинга
  (VK Video / Rutube / YouTube), которая превращается во встроенный `<iframe>` (embed)
  внутри HTML-контента.

### Редактор (админка)

- WYSIWYG-редактор на фронте — **Tiptap** (лёгкий, Vue-friendly, расширяемый).
- Возможности: форматирование (жирный/курсив/заголовки/списки), вставка изображения
  (загрузка через `POST /api/admin/media/upload`), вставка видео (вставка ссылки →
  безопасный embed).

### Безопасность (критично)

- Весь HTML из WYSIWYG прогоняется через **whitelist-санитайзер на бэке**
  (библиотека `HtmlSanitizer` для .NET) при сохранении и/или выводе.
- Разрешённый набор тегов/атрибутов ограничен. `<iframe>` допускается **только** с
  доверенных доменов: `vk.com` (видео), `rutube.ru`, `youtube.com/embed`.
- Цель — исключить XSS, который WYSIWYG открывает по умолчанию.

---

## 2. Сервисный слой бэкенда

### Отказ от generic-репозитория

- Удаляются `IRepository<T>` и `Repository<T>`.
- `AppDbContext` (EF Core) сам по себе реализует Unit of Work + репозиторий —
  дополнительная обёртка не нужна.

### Тонкие доменные сервисы

`Core` содержит интерфейсы, `Infrastructure` — реализации, работающие напрямую с `AppDbContext`:

- `IArticleService` / `ArticleService` — список (фильтр `IsPublished` + пагинация в БД),
  получение по slug, CRUD.
- `IServicePriceService` / `ServicePriceService` — цены, сортировка/группировка в запросе.
- `ICallbackService` / `CallbackService` — приём заявок «перезвоните мне».
- `IContactService` / `ContactService` — приём сообщений «написать нам».

Контроллеры становятся тонкими: вызывают сервис, маппят результат. EF-логики в
контроллерах нет.

### Эффективные запросы (фильтрация и пагинация в SQL)

Было (антипаттерн — тянем всё в память):

```csharp
var all = await _repo.GetAllAsync();
var one = all.FirstOrDefault(a => a.Slug == slug && a.IsPublished);
```

Станет (фильтр/поиск/проекция уходят в SQL):

```csharp
// список с пагинацией
var items = await _db.Articles
    .Where(a => a.IsPublished)
    .OrderByDescending(a => a.PublishedAt)
    .Skip((page - 1) * pageSize).Take(pageSize)
    .Select(a => new ArticleDto(/* ... */))
    .ToListAsync();

// по slug — одним запросом
var article = await _db.Articles
    .FirstOrDefaultAsync(a => a.Slug == slug && a.IsPublished);
```

### Изменения API

- `GET /api/articles?page=1&pageSize=12` отдаёт пагинированный результат:

```json
{ "items": [ /* ArticleDto[] */ ], "total": 0, "page": 1, "pageSize": 12 }
```

- Лента портфолио на фронте грузит данные постранично.
- Остальные эндпоинты из `DESIGN.md` сохраняются.

---

## 3. Деплой фронтенда (SSG)

### Режим сборки

- Nuxt в режиме **SSG** (`nuxt generate`) → статика (`.output/public`).
- Статику раздаёт **nginx**. Отдельного Node-процесса на проде нет.

### Контейнеры

Было (3): `frontend (Node :3000)` + `backend (.NET :8080)` + `nginx`.

Станет (2):

| Контейнер | Роль |
|-----------|------|
| `backend` | .NET Web API (:8080) |
| `nginx`   | раздаёт статику фронта, проксирует `/api/*` → backend, раздаёт `/uploads/*` |

Сборка фронта — multi-stage Docker: node собирает артефакт → копируется в nginx-образ.
На проде Node не запускается.

### Обновление контента при SSG

- Страницы портфолио/цен пререндерятся на билде (Nuxt краулит ссылки), интерактив
  (формы) работает через клиентский `$fetch` к `/api` в рантайме.
- Новый контент появляется после ребилда фронта. Реализуется скриптом/кнопкой деплоя;
  позже — вебхук «админ опубликовал → ребилд». Для наполнения ~раз в неделю достаточно.

### Migration path на SSR (оставляем дешёвым)

- Код страниц/компонентов/`useFetch` не зависит от режима рендеринга.
- При необходимости перехода на SSR: вернуть `frontend`-контейнер, сменить nitro-пресет
  и команду сборки (`nuxt generate` → `nuxt build`). Переписывания кода не требуется.

---

## Сводка изменений к DESIGN.md

| Область | Было | Стало |
|---------|------|-------|
| Контент статьи | `string Content` (текст) | санитайзенный HTML + `ArticleMedia` + видео-embed |
| Редактор | — | Tiptap WYSIWYG |
| Доступ к данным | `IRepository<T>` / `Repository<T>` | тонкие доменные сервисы поверх `AppDbContext` |
| Запросы | фильтр в памяти после `GetAllAsync()` | фильтр/пагинация/проекция в SQL |
| Список статей | без пагинации | `?page&pageSize`, ответ `{ items, total, page, pageSize }` |
| Фронт на проде | Node-контейнер (SSR) | SSG-статика за nginx |
| Контейнеры | 3 (frontend/backend/nginx) | 2 (backend/nginx) |

## Вне области (без изменений)

- База данных: SQLite.
- Авторизация: один-два админа (JWT), схема из `DESIGN.md` сохраняется.
- Tech stack: .NET 10, Nuxt 3, Tailwind, Docker, nginx.
