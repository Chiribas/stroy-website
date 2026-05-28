# Сайт-визитка строительной компании

Простой сайт с админкой для строительной компании.

## Что внутри

- Лендинг с описанием услуг
- Цены на типовые работы
- Портфолио (статьи с фото/видео)
- Контакты с формами обратной связи
- Админка для управления контентом

## Стек

- **Frontend:** Nuxt 3 (Vue 3, Tailwind CSS)
- **Backend:** .NET 10 (ASP.NET Core, EF Core)
- **Database:** SQLite
- **Deployment:** Docker Compose + nginx

## Структура проекта

```
stroy-website/
├── frontend/       ── Nuxt 3 приложение
├── backend/        ── .NET solution
├── docker/         ── Dockerfile и nginx конфиг
├── scripts/        ── Скрипты для деплоя и бэкапа
├── .env.example    ── Шаблон переменных окружения
├── .env            ── Твои переменные (gitignored)
├── DESIGN.md       ── Дизайн документ
├── README.md       ── Этот файл
└── DEPLOY.md       ── Инструкция по деплою
```

## Быстрый старт (локально)

1. **Клонировать репо**
   ```bash
   git clone <repo-url>
   cd stroy-website
   ```

2. **Настроить переменные**
   ```bash
   cp .env.example .env
   # Отредактировать .env
   ```

3. **Запустить**
   ```bash
   docker compose up
   ```

4. **Открыть**
   - Frontend: http://localhost:3000
   - Backend API: http://localhost:8080
   - Админка: http://localhost:3000/admin

## Разработка

**Frontend:**
```bash
cd frontend
npm install
npm run dev
```

**Backend:**
```bash
cd backend
dotnet restore
dotnet run
```

## Деплой на сервер

См. [DEPLOY.md](./DEPLOY.md)

## Тесты

**Frontend:**
```bash
cd frontend
npm run test
```

**Backend:**
```bash
cd backend
dotnet test
```
