# Простой локальный деплой через Docker (SSR)

**Дата:** 2026-05-29
**Статус:** дизайн утверждён, готов к написанию плана

## Цель

Собрать минимальную, но рабочую контейнерную сборку, которую можно запустить
с локальной машины одной командой (`docker compose up -d --build`) и ходить на
сайт **по IP, по HTTP** — «посмотреть/показать». Без домена, SSL и CI/CD.

Образ намеренно делаем расширяемым: позже доработаем под хостинг (registry,
GitHub Actions, certbot/SSL). Инструкции на будущее оставляем рядом в `DEPLOY.md`.

### Ключевое решение: живой фронт (SSR), не SSG

Фронт запускаем как **Node SSR-сервер** (`nuxt build` → `node .output/server/index.mjs`),
а не статикой (`nuxt generate`). Причины:

- Контент из админки (статьи, цены) должен появляться на публичном сайте
  **сразу**, без пересборки образа. SSG отдавал бы застывший снимок.
- В Docker SSR **проще**: у SSG пре-рендер на этапе сборки требует, чтобы бэкенд
  был доступен во время `build` (он фетчит данные) — лишняя оркестрация. У SSR
  этой зависимости нет, страницы рендерятся по запросу.

SSG-вариант остаётся задокументированным на будущее (для статик-хостинга).

## Архитектура

Три контейнера в `docker-compose.yml`. Наружу торчит **только nginx на :80**.

```
        :80 (единственный внешний порт)
          │
       ┌──▼─── nginx ──────────────────────────┐
       │  /         → frontend:3000  (Nuxt SSR) │
       │  /api/     → backend:8080   (как есть) │
       │  /uploads/ → раздаёт из volume uploads │
       └────────────────────────────────────────┘
            │                    │
        frontend             backend  ──┬── volume: db-data  (/app/data, sqlite)
       (node SSR)           (.NET API)  └── volume: uploads  (/app/uploads) ← общий с nginx
```

### Маршрутизация в nginx

- `location /` → `proxy_pass http://frontend:3000;` (Nuxt SSR, с проксированием
  заголовков и поддержкой ws для HMR не нужно — это prod-сборка).
- `location /api/` → `proxy_pass http://backend:8080;` **без переписывания пути** —
  все контроллеры уже под `/api/...` (`/api/articles`, `/api/admin/auth`,
  `/api/admin/media/upload` и т.д.).
- `location /uploads/` → раздаётся напрямую из общего volume `uploads`
  (nginx читает файлы, которые пишет бэкенд). Бэкенд НЕ дёргается на каждую
  картинку.

### Разрешение prod-заметки «/uploads vs /api/uploads»

В коде уже заложено разделение базовых URL:

- `apiBase` — серверная сторона (SSR-фетч внутри docker-сети) = `http://backend:8080`.
- `apiClientBase` — клиентская сторона = **пустая строка** → запросы идут
  относительными путями (`/api/...`, `/uploads/...`) на тот же origin через nginx.

`useMediaUrl` клеит `${apiClientBase}${path}`, где `path = /uploads/xxx.webp`.
С пустым `apiClientBase` это относительный `/uploads/xxx.webp`, который раздаёт
nginx из volume. **Итог: uploads — относительный путь под `/uploads`, отдельно
от `/api`; nginx раздаёт его сам.** Никакого `/api/uploads` не вводим.

## Компоненты

### backend (контейнер)

- Multi-stage Dockerfile: `mcr.microsoft.com/dotnet/sdk:10.0` (restore+publish) →
  `mcr.microsoft.com/dotnet/aspnet:10.0` (runtime).
- Слушает `http://+:8080` (`ASPNETCORE_URLS`).
- Миграции накатываются на старте (`db.Database.Migrate()` уже есть), админ
  сидится из env (`AdminSeeder` уже есть).
- Volumes: `db-data` → `/app/data` (sqlite-файл), `uploads` → `/app/uploads`.
- Env: `ASPNETCORE_ENVIRONMENT=Production`, `JWT_SECRET`, `ADMIN_USERNAME`,
  `ADMIN_PASSWORD`, `ConnectionStrings__DefaultConnection=Data Source=/app/data/database.db`,
  `Storage__UploadsPath=/app/uploads`.

### frontend (контейнер)

- Multi-stage Dockerfile: `node:22-alpine` (npm ci + `nuxt build`) → рантайм
  `node:22-alpine` с `.output`, `CMD ["node", ".output/server/index.mjs"]`.
- Слушает `0.0.0.0:3000` (`NITRO_HOST=0.0.0.0`, `NITRO_PORT=3000`).
- Env: `NUXT_PUBLIC_API_BASE=http://backend:8080`, `NUXT_PUBLIC_API_CLIENT_BASE=`
  (пусто), `NUXT_PUBLIC_SITE_URL=http://<ip>` (для sitemap/canonical; для демо
  можно оставить дефолт или подставить IP).

### nginx (контейнер)

- `nginx:alpine` + кастомный конфиг.
- Volume `uploads` смонтирован **read-only** для раздачи картинок.
- Публикует порт `80:80`.

## Правки в коде

1. **`backend/src/Api/Program.cs`** — `app.UseHttpsRedirection()` обернуть в
   `if (app.Environment.IsDevelopment())`. За nginx по чистому HTTP он не нужен
   и создаёт предупреждения/возможные редиректы.
2. **`backend/src/Api/Program.cs`** — добавить лёгкий эндпоинт `GET /health`
   (возвращает 200), чтобы compose-healthcheck мог дождаться готовности бэка
   перед стартом фронта (без гонок при первом запросе SSR).
3. **`.env.example`** — почистить: убрать несуществующий `NUXT_PUBLIC_API_URL`,
   добавить реальные `NUXT_PUBLIC_API_BASE` / `NUXT_PUBLIC_API_CLIENT_BASE`.
   Привести ключи к тем, что реально читает код.

## Файлы, которые появятся

- `docker/Dockerfile.backend`
- `docker/Dockerfile.frontend`
- `docker/nginx.conf`
- `docker-compose.yml` (в корне `stroy-website/`)
- обновлённый `.env.example`
- секция «на будущее» в `DEPLOY.md`

## Что НЕ делаем сейчас (оставляем инструкции в DEPLOY.md)

- SSL / Let's Encrypt / домен.
- Push образов в container registry.
- GitHub Actions автосборка и авто-деплой по SSH.
- Скрипты бэкапа и cron.

Для этих пунктов в `DEPLOY.md` оставляем понятные заготовки/шаги, чтобы потом
доработать без археологии.

## Проверка (критерии готовности)

1. `docker compose up -d --build` поднимается без ошибок, все 3 контейнера `healthy`/`up`.
2. `curl http://localhost/` отдаёт HTML лендинга (SSR-рендер, не пустой каркас).
3. `curl http://localhost/api/services/prices` отдаёт JSON.
4. Логин в `/admin/login` под кредами из `.env`, создание статьи с картинкой,
   публикация → статья **сразу** видна на `/portfolio`, картинка `/uploads/*.webp`
   грузится.
5. Обработка callback/contact в инбоксе работает.
6. `docker compose down && docker compose up -d` → данные (БД, картинки) на месте
   (персистятся в volumes).
7. Доступ по IP с другой машины в той же сети: `http://<ip>/` открывается.
