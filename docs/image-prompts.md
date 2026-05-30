# Промпты для картинок сайта «Суровая Стройка»

Картинки сейчас **не сгенерированы** — везде заглушка `frontend/public/images/placeholder.svg`.
Когда захочешь добавить настоящие фото: сгенерируй по промптам ниже и положи файл с
нужным именем в `frontend/public/images/`. Размеры/пропорции компоненты держат сами
(`aspect-ratio` + `object-fit: cover`), так что обработка не нужна — любой исходник впишется.

**Общий стиль (добавляй к каждому промпту):** реалистичное фото, дневной свет,
средняя полоса России / Тульская область, без людей крупным планом, без текста на
картинке, приглушённая холодная палитра (сланцево-серый), тёплый акцент оранжевого
инструмента/спецодежды. Photorealistic, professional.

---

## hero.jpg — фон геро-экрана (16:9, ~1920×1080)
> Wide cinematic photo of a rural construction site in central Russia: a timber-frame
> private house under construction on screw piles, overcast cool daylight, slate-grey
> tones, a hint of warm orange tooling, no people, no text, photorealistic, professional.

*Слот пока не используется в разметке (геро — градиент). Если решишь подложить фото —
добавь `bg-[url(/images/hero.jpg)] bg-cover` в `components/sections/SectionHero.vue`.*

## service-stroitelstvo.jpg — услуга «Строительство» (4:3, ~1200×900)
> Newly built wooden private house with a small gazebo in a Russian countryside yard,
> clear structure, fresh timber, cool daylight, no people, no text.

## service-remont-pristroyki.jpg — «Ремонт и пристройки» (4:3)
> A wooden veranda / extension being attached to a dacha house, fresh framing and
> sandwich panels, work in progress, cool daylight, no people, no text.

## service-zamena-fundamenta.jpg — «Замена фундамента» (4:3)
> Close-up of galvanized screw piles and steel channel beams supporting a small dacha
> house lifted slightly off soft soil, hydraulic jacks nearby, cool daylight, no text.

## service-peredvizhka.jpg — «Передвижка построек» (4:3)
> A small utility cabin (bytovka) lifted on jacks and rollers, prepared for relocation
> in a rural yard, cool daylight, no people, no text.

## service-pod-klyuch.jpg — «Под ключ» (4:3)
> Construction materials — sandwich panels, timber, fasteners — neatly delivered and
> stacked at a building site near a truck, cool daylight, no people, no text.

## placeholder.svg — заглушка (уже сделана)
Серый прямоугольник с подписью «Суровая Стройка». Используется в карточках примеров
работ, у которых ещё не загружено фото.

---

### Как привязать фото услуги к карточке (на будущее)
Сейчас карточка услуги (`components/ui/ServiceCard.vue`) показывает лайн-иконку
(`iconName`). Если захочешь фото вместо/поверх иконки — добавь в сущность `Service`
поле под путь картинки и выводи `<img>` со слотом `aspect-[4/3] object-cover`, либо
клади файл `service-<slug>.jpg` и маппь по slug в компоненте.
