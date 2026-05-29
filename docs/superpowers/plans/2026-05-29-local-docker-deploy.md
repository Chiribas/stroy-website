# Local Docker Deploy (SSR) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

> **ВАЖНО про коммиты:** в этом проекте коммитит ТОЛЬКО юзер сам (см. память `git-commits-user-owns`). НЕ делать `git commit`/`git add` от своего имени. Везде, где обычно был бы коммит — вместо него «контрольная проверка» (запустить тесты/сборку, убедиться что зелёно) и оставить изменения в рабочем дереве.

**Goal:** Собрать простой контейнерный деплой stroy-website (SSR-фронт + .NET API + nginx), запускаемый с локальной машины одной командой `docker compose up -d --build`, доступный по HTTP/IP.

**Architecture:** Три контейнера в одной docker-сети. Наружу торчит только nginx (:80): `/` → Nuxt SSR-сервер (node), `/api/` → .NET API (без переписывания пути), `/uploads/` → раздаётся nginx напрямую из общего volume. SQLite и загруженные файлы живут в named volumes. Клиент ходит относительными путями на тот же origin (`apiClientBase=''`), SSR-фетч — внутри сети (`apiBase=http://backend:8080`).

**Tech Stack:** Docker Compose, .NET 10 (`mcr.microsoft.com/dotnet/{sdk,aspnet}:10.0`), Node 22 (`node:22-alpine`, Nuxt 3 SSR), nginx (`nginx:alpine`).

**Спека:** `docs/superpowers/specs/2026-05-29-local-docker-deploy-design.md`

---

## File Structure

Создаём:
- `stroy-website/docker/Dockerfile.backend` — multi-stage сборка .NET API.
- `stroy-website/docker/Dockerfile.frontend` — multi-stage сборка Nuxt SSR.
- `stroy-website/docker/nginx.conf` — конфиг reverse-proxy + раздача uploads.
- `stroy-website/docker-compose.yml` — оркестрация трёх сервисов + volumes.
- `stroy-website/backend/.dockerignore` — исключить bin/obj/БД из контекста.
- `stroy-website/frontend/.dockerignore` — исключить node_modules/.output/dist.
- `stroy-website/.env` — реальные значения (gitignored, создаётся из примера).

Модифицируем:
- `stroy-website/backend/src/Api/Program.cs` — HttpsRedirection только в Dev + эндпоинт `/health`.
- `stroy-website/backend/tests/Integration/HealthEndpointTests.cs` — новый тест.
- `stroy-website/.env.example` — убрать фейковый `NUXT_PUBLIC_API_URL`, добавить реальные ключи.
- `stroy-website/DEPLOY.md` — секция «на будущее» (registry/CI/SSL/бэкап).

---

## Task 1: Бэкенд — /health + HttpsRedirection только в Dev

**Files:**
- Modify: `backend/src/Api/Program.cs:70` (HttpsRedirection), и добавить MapGet перед `:84` (MapControllers)
- Test: `backend/tests/Integration/HealthEndpointTests.cs` (create)

- [ ] **Step 1: Написать падающий тест**

Создать `backend/tests/Integration/HealthEndpointTests.cs`:

```csharp
using Xunit;
using System.Net;

namespace Integration;

public class HealthEndpointTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;
    public HealthEndpointTests(ApiTestFactory factory) => _factory = factory;

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task Health_IsAnonymous()
    {
        // без токена — всё равно 200 (health не за авторизацией)
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }
}
```

- [ ] **Step 2: Запустить тест — должен упасть (404)**

Run: `cd backend && dotnet test tests/Integration/Integration.csproj --filter "FullyQualifiedName~HealthEndpointTests"`
Expected: FAIL — `/health` отдаёт 404 NotFound, ассерт на OK не проходит.

- [ ] **Step 3: Добавить эндпоинт `/health`**

В `backend/src/Api/Program.cs` перед строкой `app.MapControllers();` (сейчас :84) добавить:

```csharp
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapControllers();
```

- [ ] **Step 4: Обернуть HttpsRedirection в Dev**

В `backend/src/Api/Program.cs` заменить строку `:70`:

```csharp
app.UseHttpsRedirection();
```

на:

```csharp
// За nginx работаем по чистому HTTP — редирект на https только в локальной dev-разработке.
if (app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
```

- [ ] **Step 5: Запустить тест — должен пройти**

Run: `cd backend && dotnet test tests/Integration/Integration.csproj --filter "FullyQualifiedName~HealthEndpointTests"`
Expected: PASS (2 теста зелёные).

- [ ] **Step 6: Прогнать весь бэкенд — ничего не сломалось**

Run: `cd backend && dotnet test`
Expected: PASS — Unit (36) + Integration (11: было 9 + 2 новых) зелёные.

- [ ] **Step 7: Контрольная проверка (НЕ коммит)**

Изменения остаются в рабочем дереве. Файлы: `Program.cs`, `HealthEndpointTests.cs`. Коммитит юзер сам.

---

## Task 2: .dockerignore для backend и frontend

**Files:**
- Create: `backend/.dockerignore`
- Create: `frontend/.dockerignore`

- [ ] **Step 1: Создать `backend/.dockerignore`**

```
bin/
obj/
**/bin/
**/obj/
*.db
*.db-shm
*.db-wal
src/Api/uploads/
src/Api/database.db
```

- [ ] **Step 2: Создать `frontend/.dockerignore`**

```
node_modules/
.output/
.nuxt/
dist/
.env
*.log
```

- [ ] **Step 3: Контрольная проверка**

Run: `cat backend/.dockerignore frontend/.dockerignore`
Expected: оба файла существуют с указанным содержимым.

---

## Task 3: Dockerfile.backend

**Files:**
- Create: `docker/Dockerfile.backend`

Контекст сборки этого образа — папка `backend/` (задаётся в compose, Task 6). Пути в `COPY` — относительно `backend/`.

- [ ] **Step 1: Создать `docker/Dockerfile.backend`**

```dockerfile
# ---- build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Сначала только csproj — для кеша restore
COPY src/Api/Api.csproj src/Api/
COPY src/Core/Core.csproj src/Core/
COPY src/Infrastructure/Infrastructure.csproj src/Infrastructure/
RUN dotnet restore src/Api/Api.csproj

# Затем исходники и публикация
COPY src/ src/
RUN dotnet publish src/Api/Api.csproj -c Release -o /app/publish

# ---- runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# curl нужен для healthcheck
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "Api.dll"]
```

- [ ] **Step 2: Проверить сборку образа**

Run: `cd stroy-website && docker build -f docker/Dockerfile.backend -t stroy-backend ./backend`
Expected: образ собирается успешно, последняя строка `naming to docker.io/library/stroy-backend`.

- [ ] **Step 3: Дымовой запуск (опционально, проверка что стартует)**

Run:
```bash
docker run --rm -d --name stroy-be-smoke \
  -e JWT_SECRET=smoke-secret-key-at-least-32-bytes-long! \
  -e ADMIN_USERNAME=admin -e ADMIN_PASSWORD=secret123 \
  -e "ConnectionStrings__DefaultConnection=Data Source=/tmp/test.db" \
  -p 8090:8080 stroy-backend
sleep 4 && curl -fsS http://localhost:8090/health && docker rm -f stroy-be-smoke
```
Expected: `{"status":"healthy"}`, контейнер удаляется.

---

## Task 4: Dockerfile.frontend (Nuxt SSR)

**Files:**
- Create: `docker/Dockerfile.frontend`

Контекст сборки — папка `frontend/`. Сборка `nuxt build` (НЕ generate) → рантайм запускает `.output/server/index.mjs`.

> **NB (выявлено при исполнении):** образ `node:24-alpine` (npm 11), т.к. dev-окружение — node 24/npm 11; на `node:22` (npm 10) `npm ci` падал на рассинхроне lock (`Missing: crossws@0.4.5`). Перед сборкой lock был пересинхронизирован локально (`npm install` во `frontend/`) — `package-lock.json` изменён, коммитит юзер.

- [ ] **Step 1: Создать `docker/Dockerfile.frontend`**

```dockerfile
# ---- build stage ----
FROM node:24-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

# ---- runtime stage ----
FROM node:22-alpine AS runtime
WORKDIR /app

COPY --from=build /app/.output ./.output

ENV NITRO_HOST=0.0.0.0
ENV NITRO_PORT=3000
EXPOSE 3000

CMD ["node", ".output/server/index.mjs"]
```

- [ ] **Step 2: Проверить сборку образа**

Run: `cd stroy-website && docker build -f docker/Dockerfile.frontend -t stroy-frontend ./frontend`
Expected: образ собирается, `npm run build` (nuxt build) проходит, генерится `.output/server`.

- [ ] **Step 3: Дымовой запуск**

Run:
```bash
docker run --rm -d --name stroy-fe-smoke -p 3010:3000 stroy-frontend
sleep 5 && curl -fsS -o /dev/null -w "%{http_code}\n" http://localhost:3010/ && docker rm -f stroy-fe-smoke
```
Expected: `200` (SSR отдаёт HTML; запросы к API упадут без бэка — это норм для дымового теста, главное что сервер поднялся).

---

## Task 5: nginx.conf

**Files:**
- Create: `docker/nginx.conf`

- [ ] **Step 1: Создать `docker/nginx.conf`**

```nginx
server {
    listen 80;
    server_name _;

    # Загрузка картинок через /api/admin/media/upload может быть крупной
    client_max_body_size 25m;

    # Раздача загруженных файлов напрямую из общего volume (без удара по бэку).
    # URL в контенте статей хранятся как /uploads/xxx.webp.
    location /uploads/ {
        alias /var/www/uploads/;
        access_log off;
        expires 7d;
        try_files $uri =404;
    }

    # API — проксируем как есть, роуты уже под /api/...
    location /api/ {
        proxy_pass http://backend:8080;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # Всё остальное — на Nuxt SSR-сервер
    location / {
        proxy_pass http://frontend:3000;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

- [ ] **Step 2: Контрольная проверка синтаксиса (после Task 6, в составе стека)**

Синтаксис проверится при старте контейнера nginx в Task 7. Отдельно сейчас файл просто создан.

---

## Task 6: docker-compose.yml + .env.example + .env

**Files:**
- Create: `docker-compose.yml`
- Modify: `.env.example`
- Create: `.env`

- [ ] **Step 1: Создать `docker-compose.yml`**

```yaml
services:
  backend:
    build:
      context: ./backend
      dockerfile: ../docker/Dockerfile.backend
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      JWT_SECRET: ${JWT_SECRET}
      ADMIN_USERNAME: ${ADMIN_USERNAME}
      ADMIN_PASSWORD: ${ADMIN_PASSWORD}
      ConnectionStrings__DefaultConnection: "Data Source=/app/data/database.db"
      Storage__UploadsPath: "/app/uploads"
    volumes:
      - db-data:/app/data
      - uploads:/app/uploads
    healthcheck:
      test: ["CMD", "curl", "-fsS", "http://localhost:8080/health"]
      interval: 10s
      timeout: 3s
      retries: 5
      start_period: 15s
    restart: unless-stopped

  frontend:
    build:
      context: ./frontend
      dockerfile: ../docker/Dockerfile.frontend
    environment:
      NUXT_PUBLIC_API_BASE: "http://backend:8080"
      NUXT_PUBLIC_API_CLIENT_BASE: ""
      NUXT_PUBLIC_SITE_URL: ${SITE_URL}
    depends_on:
      backend:
        condition: service_healthy
    restart: unless-stopped

  nginx:
    image: nginx:alpine
    ports:
      - "80:80"
    volumes:
      - ./docker/nginx.conf:/etc/nginx/conf.d/default.conf:ro
      - uploads:/var/www/uploads:ro
    depends_on:
      - frontend
      - backend
    restart: unless-stopped

volumes:
  db-data:
  uploads:
```

- [ ] **Step 2: Переписать `.env.example`**

Заменить всё содержимое `.env.example` на:

```env
# Admin (сидится в БД при первом старте бэкенда)
ADMIN_USERNAME=admin
ADMIN_PASSWORD=change_me_to_secure_password

# JWT signing secret (минимум 32 символа)
JWT_SECRET=change-me-to-a-long-random-string-32+chars

# Публичный URL сайта (для sitemap/canonical). Для локального демо — IP машины.
SITE_URL=http://localhost

# --- Справочно (значения заданы в docker-compose.yml, менять не нужно) ---
# Фронт: NUXT_PUBLIC_API_BASE=http://backend:8080  (SSR-фетч внутри docker-сети)
#        NUXT_PUBLIC_API_CLIENT_BASE=              (пусто → клиент шлёт /api и /uploads на тот же origin через nginx)
# Бэк:   ConnectionStrings__DefaultConnection=Data Source=/app/data/database.db
#        Storage__UploadsPath=/app/uploads
```

- [ ] **Step 3: Создать рабочий `.env` из примера**

Run:
```bash
cd stroy-website && cp .env.example .env
```
Затем вручную отредактировать `.env`: задать надёжный `ADMIN_PASSWORD`, случайный `JWT_SECRET` (≥32 символа), и `SITE_URL=http://<IP_машины>` (или оставить `http://localhost` если ходишь локально).

Проверка `.env` в gitignore:
Run: `grep -n "^\.env$\|^\.env\b" .gitignore`
Expected: `.env` присутствует в `.gitignore` (не закоммитится). Если нет — добавить строку `.env`.

---

## Task 7: Поднять стек и проверить end-to-end

**Files:** нет (только запуск и проверка)

- [ ] **Step 1: Собрать и поднять**

Run: `cd stroy-website && docker compose up -d --build`
Expected: три сервиса собираются и стартуют. `backend` доходит до `healthy`, после чего стартует `frontend`, затем `nginx`.

- [ ] **Step 2: Проверить статус контейнеров**

Run: `docker compose ps`
Expected: `backend` (healthy), `frontend` (up), `nginx` (up). Порт nginx — `0.0.0.0:80->80/tcp`.

- [ ] **Step 3: Лендинг отдаётся (SSR)**

Run: `curl -fsS http://localhost/ | head -20`
Expected: HTML страницы (видны реальные тексты секций, не пустой каркас).

- [ ] **Step 4: API работает через nginx**

Run: `curl -fsS http://localhost/api/services/prices`
Expected: JSON-массив цен (200).

- [ ] **Step 5: Браузерная проверка живого контента**

В обычном браузере (НЕ Playwright — он глотает file-диалоги, см. память):
1. Открыть `http://localhost/` — лендинг.
2. `http://localhost/admin/login` — войти под `ADMIN_USERNAME`/`ADMIN_PASSWORD` из `.env`.
3. Создать статью с заголовком, текстом и **главной картинкой** (загрузить файл).
4. Опубликовать.
5. Открыть `http://localhost/portfolio` — статья **видна сразу** (SSR, без пересборки).
6. Открыть статью — картинка `/uploads/*.webp` грузится (200, отдаёт nginx).

Expected: все пункты проходят, в консоли браузера нет ошибок.

- [ ] **Step 6: Персистентность данных**

Run:
```bash
cd stroy-website && docker compose down && docker compose up -d
```
Затем снова открыть `http://localhost/portfolio`.
Expected: созданная статья и картинка на месте (volumes `db-data` и `uploads` сохранились).

- [ ] **Step 7: Доступ по IP (если показываешь с другой машины в сети)**

Run (на машине-хосте): узнать IP (`ipconfig` / `ip a`), затем с другого устройства открыть `http://<IP>/`.
Expected: сайт открывается. (Если `SITE_URL` важен для ссылок — он должен быть `http://<IP>`.)

- [ ] **Step 8: Контрольная проверка — остановить стек**

Run: `cd stroy-website && docker compose down`
Expected: контейнеры остановлены, volumes сохранены (данные не теряются).

---

## Task 8: Секция «на будущее» в DEPLOY.md

**Files:**
- Modify: `DEPLOY.md` (добавить секцию в конец)

- [ ] **Step 1: Добавить в конец `DEPLOY.md`**

```markdown
---

## Локальный демо-деплой (этап 1, реализован)

Простой запуск с локальной машины для показа (HTTP, по IP, без SSL/домена):

```bash
cp .env.example .env   # отредактировать ADMIN_PASSWORD, JWT_SECRET, SITE_URL
docker compose up -d --build
# сайт: http://localhost/  (или http://<IP>/)
```

Фронт работает в режиме **SSR** (Nuxt node-сервер) — контент из админки виден на
сайте сразу, без пересборки.

**Обновить после правок кода:**
```bash
docker compose up -d --build
```

**Логи / статус:**
```bash
docker compose logs -f
docker compose ps
```

## На будущее (этап 2 — хостинг)

Заготовки для боевого деплоя, когда дойдут руки:

1. **Container registry** — собирать образы в CI и пушить (GHCR/Docker Hub),
   на сервере `docker compose pull` вместо `--build`. Добавить `image:` в compose.
2. **GitHub Actions автосборка** — workflow `.github/workflows/deploy.yml`:
   билд образов → push в registry → SSH на сервер → `docker compose pull && up -d`.
   Секреты: `SSH_PRIVATE_KEY`, `SSH_HOST`, `SSH_USER`, креды registry.
3. **SSL / домен** — добавить сервис certbot или внешний reverse-proxy (Caddy/Traefik),
   nginx-локейшн на 443, редирект 80→443. Поправить `SITE_URL` на `https://<домен>`.
4. **Бэкап** — `scripts/backup.sh` (dump SQLite + tar uploads) по cron (см. примеры выше).
5. **SSG-вариант фронта** — если публичные страницы захочется статикой (быстрее,
   дешевле хостинг): `nuxt generate` вместо SSR, nginx раздаёт `.output/public`,
   пересборка фронта при изменении контента. Сборка потребует доступности бэка
   во время билда (пре-рендер фетчит данные).
```

- [ ] **Step 2: Контрольная проверка**

Run: `tail -40 DEPLOY.md`
Expected: новые секции на месте, markdown корректный.

---

## Self-Review (выполнено при написании плана)

**Покрытие спеки:**
- Архитектура (3 контейнера, nginx :80) → Task 5, 6. ✅
- Маршрутизация `/`, `/api/`, `/uploads/` → Task 5 (nginx.conf). ✅
- Разрешение prod-заметки uploads (`apiClientBase=''`, nginx из volume) → Task 5 + Task 6 (env). ✅
- backend контейнер (Dockerfile, миграции на старте, volumes, env) → Task 3, 6. ✅
- frontend SSR контейнер (`nuxt build` → node server, env) → Task 4, 6. ✅
- nginx контейнер (volume uploads ro, порт 80) → Task 5, 6. ✅
- Правка `UseHttpsRedirection`→Dev → Task 1. ✅
- Эндпоинт `/health` + healthcheck → Task 1, 6. ✅
- Чистка `.env.example` (убрать `NUXT_PUBLIC_API_URL`) → Task 6. ✅
- Заметки на будущее в DEPLOY.md → Task 8. ✅
- Критерии проверки (compose up, curl, браузер, персистентность, IP) → Task 7. ✅

**Плейсхолдеры:** нет TODO/TBD; весь код приведён целиком. `<IP_машины>`/`<IP>` — намеренные значения, которые подставляет юзер.

**Согласованность:** имена сервисов (`backend`/`frontend`/`nginx`), порты (8080/3000/80), volumes (`db-data`/`uploads`), env-ключи (`NUXT_PUBLIC_API_BASE`/`NUXT_PUBLIC_API_CLIENT_BASE`/`Storage__UploadsPath`/`ConnectionStrings__DefaultConnection`) совпадают между Dockerfile, compose, nginx и кодом. nginx раздаёт `/uploads/` из `/var/www/uploads` (compose монтирует volume `uploads` туда ro; backend пишет в него же через `/app/uploads`). ✅
