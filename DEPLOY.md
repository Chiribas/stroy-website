# Деплой на Yandex VPS

Пошаговая инструкция для развёртывания сайта на Yandex Cloud VPS.

## 1. Создание VPS

1. Зайди в [Yandex Cloud Console](https://console.cloud.yandex.ru/)
2. Создай новый instance:
   - **Platform:** Linux
   - **Image:** Ubuntu 22.04 LTS
   - **vCPU:** 2 (минимум)
   - **RAM:** 2 GB (минимум)
   - **Disk:** 20 GB SSD

3. Включи статический IP

4. SSH ключи:
   ```bash
   ssh-keygen -t ed25519
   # Добавь публичный ключ в Yandex Cloud при создании instance
   ```

## 2. Первичная настройка сервера

**Подключись:**
```bash
ssh ubuntu@<your-ip>
```

**Обнови систему:**
```bash
sudo apt update && sudo apt upgrade -y
```

**Установи Docker:**
```bash
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh
sudo usermod -aG docker ubuntu
```

**Установи Docker Compose:**
```bash
sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
sudo chmod +x /usr/local/bin/docker-compose
```

**Перезайди (для групп docker):**
```bash
exit
ssh ubuntu@<your-ip>
```

## 3. Развертывание проекта

**Создай директорию:**
```bash
sudo mkdir -p /opt/stroy-website
sudo chown ubuntu:ubuntu /opt/stroy-website
cd /opt/stroy-website
```

**Скопируй код:**
```bash
# Вариант A: git clone
git clone <your-repo-url> .

# Вариант B: архив (если git не настроен)
# scp -r stroy-website/* ubuntu@<ip>:/opt/stroy-website/
```

**Настрой .env:**
```bash
cp .env.example .env
nano .env
```

Заполни:
```env
# Admin
ADMIN_USERNAME=admin
ADMIN_PASSWORD=сложный_пароль_сюда

# Domain
DOMAIN=stroycompany.ru

# API
NUXT_PUBLIC_API_URL=http://backend:8080
```

**Запусти:**
```bash
docker compose up -d
```

## 4. Настройка SSL (Let's Encrypt)

**Установи certbot:**
```bash
sudo apt install certbot python3-certbot-nginx -y
```

**Получи сертификат:**
```bash
sudo certbot certonly --nginx -d stroycompany.ru -d www.stroycompany.ru
```

**Сертификаты будут в:**
```
/etc/letsencrypt/live/stroycompany.ru/fullchain.pem
/etc/letsencrypt/live/stroycompany.ru/privkey.pem
```

**Обнови docker-compose.yml** (раскомментируй SSL volumes)

**Перезапусти nginx:**
```bash
docker compose restart nginx
```

## 5. Авто-бэкап

**Создай скрипт бэкапа:**
```bash
sudo nano /opt/stroy-website/scripts/backup.sh
```

```bash
#!/bin/bash
BACKUP_DIR="/backup"
DATE=$(date +%Y%m%d_%H%M%S)

mkdir -p $BACKUP_DIR

# Бэкап базы
docker compose exec -T backend cp /app/data/database.db - > $BACKUP_DIR/db_$DATE.db

# Бэкап медиа
docker compose exec -T backend tar czf - /app/uploads > $BACKUP_DIR/uploads_$DATE.tar.gz

# Удали старые (30 дней)
find $BACKUP_DIR -name "db_*.db" -mtime +30 -delete
find $BACKUP_DIR -name "uploads_*.tar.gz" -mtime +30 -delete
```

**Сделай executable:**
```bash
chmod +x /opt/stroy-website/scripts/backup.sh
```

**Добавь в cron (каждую ночь в 2:00):**
```bash
crontab -e
```

```
0 2 * * * /opt/stroy-website/scripts/backup.sh
```

## 6. GitHub Actions Auto-Deploy

**Создай Secret в GitHub:**
- `SSH_PRIVATE_KEY` — твой приватный ключ
- `SSH_HOST` — IP сервера
- `SSH_USER` — ubuntu

**Деплой кнопкой:**
1. Заходи в GitHub → Actions
2. Выбери "Deploy to Production"
3. Нажми "Run workflow"

## Обновление сайта

**Вручную:**
```bash
cd /opt/stroy-website
git pull
docker compose pull
docker compose up -d --build
docker image prune -f
```

**Автоматически (GitHub Action):**
Просто пуши в main → запускай workflow вручную.

## Мониторинг

**Логи:**
```bash
docker compose logs -f          # все сервисы
docker compose logs -f backend # только backend
```

**Статус:**
```bash
docker compose ps
```

**Healthcheck:**
```bash
curl http://localhost:8080/health
```

## Проблемы?

**Контейнер не стартует:**
```bash
docker compose logs backend
docker compose ps
```

**SSL не обновляется:**
```bash
sudo certbot renew
docker compose restart nginx
```

**База数据的:**
```bash
docker compose exec backend sqlite3 /app/data/database.db
```

---

## Локальный демо-деплой (этап 1, реализован)

Простой запуск с локальной машины для показа (HTTP, по IP, без SSL/домена):

```bash
cp .env.example .env   # отредактировать ADMIN_PASSWORD, JWT_SECRET, SITE_URL
docker compose up -d --build
# сайт: http://localhost/  (или http://<IP>/)
```

Фронт работает в режиме **SSR** (Nuxt node-сервер) — контент из админки виден на
сайте сразу, без пересборки. (Пре-рендер публичных страниц намеренно отключён в
`nuxt.config.ts`: иначе они запекались бы статикой на этапе сборки и не показывали
новый контент.)

**Где лежат данные (важно для переноса):**
БД и загруженные картинки хранятся через **bind-mount** прямо в папке проекта:
- `./data/database.db` — база (SQLite)
- `./uploads/` — загруженные изображения (`.webp`)

Эти папки в `.gitignore` (в гит не коммитятся), но **переезжают вместе с папкой проекта**.

**Перенести на другую машину «как есть» (со всеми статьями и картинками):**
```bash
# 1. Скопировать ВСЮ папку stroy-website (включая ./data и ./uploads) на целевую машину
#    (например: rsync -av stroy-website/ user@host:/opt/stroy-website/)
# 2. На целевой машине:
docker compose up -d --build
```
Данные уже внутри папки — ничего отдельно переносить не надо.

**Обновить после правок кода (данные не теряются):**
```bash
docker compose up -d --build
```

**Логи / статус:**
```bash
docker compose logs -f
docker compose ps
```

**Сбросить данные (чистый старт демо):**
```bash
docker compose down
rm -rf ./data/* ./uploads/*     # bind-mount: данные удаляются вручную
docker compose up -d            # БД пересоздастся, админ пересеется из .env
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
