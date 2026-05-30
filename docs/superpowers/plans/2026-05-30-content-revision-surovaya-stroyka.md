# Контент-ревизия «Суровая Стройка» — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Наполнить сайт реальным контентом «Суровая Стройка», добавить раздел Услуг с детальными страницами и связью статей по тегу, переработать раздел цен в «примеры работ», добавить тёмную тему и смягчить светлую.

**Architecture:** Весь смысловой контент — в БД через существующую админку (услуги — новая сущность, цены — переработанная `ServicePrice`, статьи — +теги). Бренд/контакты — в `app.config.ts`. Тема — через CSS-переменные (`:root` + `.dark`), завязанные на Tailwind-токены; переключатель в шапке, дефолт = системная, без вспышки. Картинки пока заглушки (фикс aspect-ratio), промпты — в отдельном doc.

**Tech Stack:** .NET 10 / EF Core / SQLite / xUnit (backend); Nuxt 3 / Vue 3 / Tailwind / TypeScript / Vitest (frontend). Спека: `docs/superpowers/specs/2026-05-30-content-revision-surovaya-stroyka-design.md`.

---

## ⚙️ Правила исполнения (важно)

- **Коммиты НЕ делаем** — юзер коммитит сам (проектное правило). Везде, где обычно `git commit`, **чекпойнт = прогон тестов** (ниже в каждой задаче).
- **БД с боевыми данными.** Все миграции — с сохранением данных. Сиды контента — **идемпотентные, только в пустую таблицу**.
- **Win-грабли:** перед `dotnet test` прибить залоченный dev-бэк: `taskkill //IM Api.exe //F` (если запущен). Frontend dev: `npm run dev -- --host 127.0.0.1`. curl к localhost — с `--noproxy '*'`.
- **Запуск тестов:**
  - Backend: `cd backend && dotnet test` (или с `--filter "FullyQualifiedName~<ClassName>"`).
  - Frontend: `cd frontend && npm run test` (vitest) и `npx nuxi typecheck`.

---

## Карта файлов

**Backend (`backend/`)**
- `src/Core/Entities/Article.cs` — +поле `Tags`
- `src/Core/Entities/Service.cs` — **новая** сущность услуги
- `src/Core/Entities/ServicePrice.cs` — переработка полей (пример работы)
- `src/Core/DTOs/ServiceDto.cs`, `CreateServiceDto.cs`, `UpdateServiceDto.cs`, `ServiceListItemDto.cs` — **новые**
- `src/Core/DTOs/ServicePriceDto.cs`, `CreateServicePriceDto.cs`, `UpdateServicePriceDto.cs` — новые поля
- `src/Core/DTOs/ArticleListItemDto.cs`, `ArticleDto.cs` — +`Tags`
- `src/Core/Interfaces/IServiceCatalogService.cs` — **новый**
- `src/Infrastructure/Services/ServiceCatalogService.cs` — **новый**
- `src/Infrastructure/Services/ServicePriceService.cs`, `ArticleService.cs` — правки
- `src/Infrastructure/Data/AppDbContext.cs` — +`Services` DbSet, конфиг
- `src/Infrastructure/Data/ContentSeeder.cs` — **новый** (идемпотентные сиды)
- `src/Api/Controllers/ServicesController.cs` — **переписать** под сущность Service
- `src/Api/Controllers/PricesController.cs` — **новый** (публичные цены, бывш. `/api/services/prices`)
- `src/Api/Controllers/Admin/AdminServicesController.cs` — **переписать** под Service
- `src/Api/Controllers/Admin/AdminPricesController.cs` — **новый** (бывш. admin services)
- `src/Api/Controllers/ArticlesController.cs` — +`?tag=`
- `src/Api/Program.cs` — DI + вызов `ContentSeeder`
- `tests/Unit/*`, `tests/Integration/*` — новые/обновлённые тесты

**Frontend (`frontend/`)**
- `app.config.ts` — бренд/контакты
- `assets/css/main.css` — CSS-переменные тем + prose-invert
- `tailwind.config.ts` — семантические токены на переменных
- `plugins/color-mode.client.ts` — **новый** (no-flash dark mode)
- `components/ui/ThemeToggle.vue` — **новый**
- `components/layout/AppHeader.vue`, `AppFooter.vue` — навигация/тема/футер
- `components/sections/SectionHero.vue`, `SectionAbout.vue`(→«Как работаем»), `SectionServicesTeaser.vue`, `SectionPricesTeaser.vue`, `SectionPortfolioTeaser.vue` — правки
- `components/ui/ServiceCard.vue`, `PriceExampleCard.vue`, `Icon.vue` — **новые**
- `components/ui/PriceTable.vue` — заменить на карточки примеров
- `pages/index.vue`, `pages/prices.vue`, `pages/portfolio/index.vue`, `pages/contact.vue` — копирайт
- `pages/services/index.vue`, `pages/services/[slug].vue` — **новые**
- `pages/admin/services.vue` — **новый** (CRUD услуг)
- `pages/admin/prices.vue` — переработка под новые поля
- `pages/admin/articles/new.vue`, `[id].vue` — +поле тегов
- `lib/api.ts`, `lib/adminApi.ts`, `types/api.ts`, `types/admin.ts`, `lib/prices.ts` — типы/методы
- `public/images/` + `placeholder.svg` — **новое**
- `docs/image-prompts.md` — **новый**

---

# Фаза A — Backend: модель данных и API

### Task A1: `Article.Tags` + фильтр `?tag=`

**Files:**
- Modify: `backend/src/Core/Entities/Article.cs`
- Modify: `backend/src/Core/DTOs/ArticleListItemDto.cs`, `backend/src/Core/DTOs/ArticleDto.cs`
- Modify: `backend/src/Core/Interfaces/IArticleService.cs`, `backend/src/Infrastructure/Services/ArticleService.cs`
- Modify: `backend/src/Api/Controllers/ArticlesController.cs`
- Test: `backend/tests/Unit/ArticleServiceTests.cs`

- [ ] **Step 1: Добавить поле в сущность**

В `Article.cs` после `public string Content`:
```csharp
    /// Теги через запятую (напр. "foundation,piles"). Связывают статью с услугами/ценами.
    public string? Tags { get; set; }
```

- [ ] **Step 2: Добавить `Tags` в DTO**

`ArticleListItemDto.cs`:
```csharp
public record ArticleListItemDto(
    int Id,
    string Title,
    string Slug,
    string? Summary,
    string? ThumbnailPath,
    DateTime? PublishedAt,
    string? Tags
);
```
`ArticleDto.cs` — добавить `string? Tags` перед `Media`:
```csharp
public record ArticleDto(
    int Id,
    string Title,
    string Slug,
    string? Summary,
    string Content,
    string? ThumbnailPath,
    DateTime? PublishedAt,
    string? Tags,
    IReadOnlyList<ArticleMediaDto> Media
);
```
Также добавить `Tags` в `CreateArticleDto.cs`/`UpdateArticleDto.cs`:
```csharp
    public string? Tags { get; set; }
```

- [ ] **Step 3: Обновить интерфейс и сервис**

`IArticleService.cs` — поменять сигнатуру публичного списка:
```csharp
    Task<PagedResult<ArticleListItemDto>> GetPublishedAsync(int page, int pageSize, string? tag = null);
```
В `ArticleService.cs`:
- В `GetPublishedAsync` добавить параметр и фильтр:
```csharp
    public async Task<PagedResult<ArticleListItemDto>> GetPublishedAsync(int page, int pageSize, string? tag = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 12;

        var query = _db.Articles.Where(a => a.IsPublished);
        if (!string.IsNullOrWhiteSpace(tag))
            query = query.Where(a => a.Tags != null &&
                ("," + a.Tags + ",").Contains("," + tag + ","));
        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(a => a.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new ArticleListItemDto(
                a.Id, a.Title, a.Slug, a.Summary, a.ThumbnailPath, a.PublishedAt, a.Tags))
            .ToListAsync();

        return new PagedResult<ArticleListItemDto>(items, total, page, pageSize);
    }
```
- В `GetAllForAdminAsync` — добавить `a.Tags` последним аргументом в `ArticleListItemDto(...)`.
- В `CreateAsync` — `Tags = dto.Tags,` в инициализатор `Article`.
- В `UpdateAsync` — `article.Tags = dto.Tags;`.
- В `ToDto` — добавить `a.Tags,` перед маппингом Media:
```csharp
    private static ArticleDto ToDto(Article a) => new(
        a.Id, a.Title, a.Slug, a.Summary, a.Content, a.ThumbnailPath, a.PublishedAt, a.Tags,
        a.Media.OrderBy(m => m.SortOrder)
            .Select(m => new ArticleMediaDto(m.Id, m.Path, m.MediaType, m.Alt, m.SortOrder))
            .ToList());
```

- [ ] **Step 4: Контроллер — параметр tag**

`ArticlesController.cs`:
```csharp
    [HttpGet]
    public async Task<ActionResult<PagedResult<ArticleListItemDto>>> Get(int page = 1, int pageSize = 12, string? tag = null)
        => Ok(await _service.GetPublishedAsync(page, pageSize, tag));
```

- [ ] **Step 5: Тест фильтра по тегу**

Добавить в `ArticleServiceTests.cs` (следовать существующему стилю создания контекста; если в файле есть хелпер `NewDb()`/`CreateService()` — переиспользовать его):
```csharp
[Fact]
public async Task GetPublishedAsync_FiltersByTag()
{
    await using var db = NewInMemoryDb();           // используйте имеющийся в файле способ
    var svc = new ArticleService(db, new PassthroughSanitizer());
    await svc.CreateAsync(new CreateArticleDto { Title="A", Slug="a", Content="x", IsPublished=true, Tags="foundation,piles" });
    await svc.CreateAsync(new CreateArticleDto { Title="B", Slug="b", Content="x", IsPublished=true, Tags="roofing" });

    var found = await svc.GetPublishedAsync(1, 12, "foundation");

    Assert.Single(found.Items);
    Assert.Equal("a", found.Items[0].Slug);
}
```
> Если в файле нет `NewInMemoryDb`/`PassthroughSanitizer` — посмотрите, как соседние тесты в этом файле строят `AppDbContext` и `IHtmlSanitizerService`, и используйте тот же приём (имена подставьте фактические).

- [ ] **Step 6: Миграция (аддитивная, безопасная)**

```bash
cd backend && taskkill //IM Api.exe //F 2>/dev/null; dotnet ef migrations add AddArticleTags -p src/Infrastructure -s src/Api
```
Проверить в сгенерированном файле миграции: только `AddColumn<string>(name: "Tags", table: "Articles", nullable: true)`. Если есть лишние операции — остановиться и разобраться.

- [ ] **Step 7: Чекпойнт — тесты**

Run: `cd backend && dotnet test`
Expected: всё зелёное, включая новый `GetPublishedAsync_FiltersByTag`. (Существующие тесты, конструирующие `ArticleListItemDto`/`ArticleDto` позиционно, поправить под новые поля, если упадут на компиляции.)

---

### Task A2: Новая сущность `Service` + DbContext + миграция

**Files:**
- Create: `backend/src/Core/Entities/Service.cs`
- Modify: `backend/src/Infrastructure/Data/AppDbContext.cs`
- Test: (миграция проверяется в Step 4)

- [ ] **Step 1: Сущность**

Create `Service.cs`:
```csharp
namespace Core.Entities;

public class Service
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string? IconName { get; set; }          // имя lucide-иконки, напр. "home"
    public string Content { get; set; } = string.Empty; // санитайзенный HTML детальной страницы
    public string? Tag { get; set; }               // тег для подбора статей "Из практики"
    public int SortOrder { get; set; }
    public bool IsPublished { get; set; }
}
```

- [ ] **Step 2: DbContext**

В `AppDbContext.cs` добавить DbSet:
```csharp
    public DbSet<Service> Services => Set<Service>();
```
И в `OnModelCreating`:
```csharp
        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.Property(e => e.Title).IsRequired();
            entity.Property(e => e.Slug).IsRequired();
            entity.Property(e => e.Content).IsRequired();
        });
```

- [ ] **Step 3: Миграция**

```bash
cd backend && taskkill //IM Api.exe //F 2>/dev/null; dotnet ef migrations add AddServices -p src/Infrastructure -s src/Api
```
Проверить: `CreateTable("Services", ...)` + уникальный индекс по Slug.

- [ ] **Step 4: Чекпойнт — сборка**

Run: `cd backend && dotnet build`
Expected: успешно.

---

### Task A3: DTO + сервис услуг + санитайзинг

**Files:**
- Create: `backend/src/Core/DTOs/ServiceDto.cs`, `ServiceListItemDto.cs`, `CreateServiceDto.cs`, `UpdateServiceDto.cs`
- Create: `backend/src/Core/Interfaces/IServiceCatalogService.cs`
- Create: `backend/src/Infrastructure/Services/ServiceCatalogService.cs`
- Test: `backend/tests/Unit/ServiceCatalogServiceTests.cs`

- [ ] **Step 1: DTO**

`ServiceListItemDto.cs`:
```csharp
namespace Core.DTOs;

public record ServiceListItemDto(
    int Id, string Title, string Slug, string? ShortDescription, string? IconName, int SortOrder);
```
`ServiceDto.cs`:
```csharp
namespace Core.DTOs;

public record ServiceDto(
    int Id, string Title, string Slug, string? ShortDescription, string? IconName,
    string Content, string? Tag, int SortOrder, bool IsPublished);
```
`CreateServiceDto.cs`:
```csharp
using System.ComponentModel.DataAnnotations;

namespace Core.DTOs;

public class CreateServiceDto
{
    [Required] public string Title { get; set; } = string.Empty;
    [Required]
    [RegularExpression(@"^[a-z0-9-]+$", ErrorMessage = "Slug must contain only lowercase letters, numbers and hyphens")]
    public string Slug { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string? IconName { get; set; }
    [Required] public string Content { get; set; } = string.Empty;
    public string? Tag { get; set; }
    public int SortOrder { get; set; }
    public bool IsPublished { get; set; } = true;
}
```
`UpdateServiceDto.cs` — то же, что Create, но `Slug` без `[Required]` (nullable), как у `UpdateArticleDto`:
```csharp
using System.ComponentModel.DataAnnotations;

namespace Core.DTOs;

public class UpdateServiceDto
{
    [Required] public string Title { get; set; } = string.Empty;
    [RegularExpression(@"^[a-z0-9-]+$", ErrorMessage = "Slug must contain only lowercase letters, numbers and hyphens")]
    public string? Slug { get; set; }
    public string? ShortDescription { get; set; }
    public string? IconName { get; set; }
    [Required] public string Content { get; set; } = string.Empty;
    public string? Tag { get; set; }
    public int SortOrder { get; set; }
    public bool IsPublished { get; set; }
}
```

- [ ] **Step 2: Интерфейс**

`IServiceCatalogService.cs`:
```csharp
using Core.DTOs;

namespace Core.Interfaces;

public interface IServiceCatalogService
{
    Task<IReadOnlyList<ServiceListItemDto>> GetPublishedAsync();
    Task<ServiceDto?> GetBySlugAsync(string slug);
    Task<IReadOnlyList<ServiceDto>> GetAllForAdminAsync();
    Task<ServiceDto?> GetByIdAsync(int id);
    Task<ServiceDto> CreateAsync(CreateServiceDto dto);
    Task<ServiceDto?> UpdateAsync(int id, UpdateServiceDto dto);
    Task<bool> DeleteAsync(int id);
}
```

- [ ] **Step 3: Реализация (паттерн как `ArticleService`, с санитайзингом `Content` и проверкой дубликата slug)**

`ServiceCatalogService.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Core.DTOs;
using Core.Entities;
using Core.Exceptions;
using Core.Interfaces;
using Infrastructure.Data;

namespace Infrastructure.Services;

public class ServiceCatalogService : IServiceCatalogService
{
    private readonly AppDbContext _db;
    private readonly IHtmlSanitizerService _sanitizer;

    public ServiceCatalogService(AppDbContext db, IHtmlSanitizerService sanitizer)
    {
        _db = db;
        _sanitizer = sanitizer;
    }

    public async Task<IReadOnlyList<ServiceListItemDto>> GetPublishedAsync() =>
        await _db.Services.Where(s => s.IsPublished)
            .OrderBy(s => s.SortOrder)
            .Select(s => new ServiceListItemDto(s.Id, s.Title, s.Slug, s.ShortDescription, s.IconName, s.SortOrder))
            .ToListAsync();

    public async Task<ServiceDto?> GetBySlugAsync(string slug)
    {
        var s = await _db.Services.FirstOrDefaultAsync(x => x.Slug == slug && x.IsPublished);
        return s is null ? null : ToDto(s);
    }

    public async Task<IReadOnlyList<ServiceDto>> GetAllForAdminAsync() =>
        await _db.Services.OrderBy(s => s.SortOrder)
            .Select(s => ToDtoExpr(s)).ToListAsync();

    public async Task<ServiceDto?> GetByIdAsync(int id)
    {
        var s = await _db.Services.FindAsync(id);
        return s is null ? null : ToDto(s);
    }

    public async Task<ServiceDto> CreateAsync(CreateServiceDto dto)
    {
        if (await _db.Services.AnyAsync(s => s.Slug == dto.Slug))
            throw new DuplicateSlugException(dto.Slug);
        var s = new Service
        {
            Title = dto.Title, Slug = dto.Slug, ShortDescription = dto.ShortDescription,
            IconName = dto.IconName, Content = _sanitizer.Sanitize(dto.Content),
            Tag = dto.Tag, SortOrder = dto.SortOrder, IsPublished = dto.IsPublished,
        };
        _db.Services.Add(s);
        await _db.SaveChangesAsync();
        return ToDto(s);
    }

    public async Task<ServiceDto?> UpdateAsync(int id, UpdateServiceDto dto)
    {
        var s = await _db.Services.FindAsync(id);
        if (s is null) return null;
        if (!string.IsNullOrEmpty(dto.Slug) && dto.Slug != s.Slug
            && await _db.Services.AnyAsync(x => x.Slug == dto.Slug && x.Id != id))
            throw new DuplicateSlugException(dto.Slug);
        s.Title = dto.Title;
        if (!string.IsNullOrEmpty(dto.Slug)) s.Slug = dto.Slug;
        s.ShortDescription = dto.ShortDescription;
        s.IconName = dto.IconName;
        s.Content = _sanitizer.Sanitize(dto.Content);
        s.Tag = dto.Tag;
        s.SortOrder = dto.SortOrder;
        s.IsPublished = dto.IsPublished;
        await _db.SaveChangesAsync();
        return ToDto(s);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var s = await _db.Services.FindAsync(id);
        if (s is null) return false;
        _db.Services.Remove(s);
        await _db.SaveChangesAsync();
        return true;
    }

    private static ServiceDto ToDto(Service s) => new(
        s.Id, s.Title, s.Slug, s.ShortDescription, s.IconName, s.Content, s.Tag, s.SortOrder, s.IsPublished);

    private static ServiceDto ToDtoExpr(Service s) => new(
        s.Id, s.Title, s.Slug, s.ShortDescription, s.IconName, s.Content, s.Tag, s.SortOrder, s.IsPublished);
}
```

- [ ] **Step 4: Тест**

`ServiceCatalogServiceTests.cs` (использовать тот же приём построения `AppDbContext`+sanitizer, что и в `ArticleServiceTests.cs`):
```csharp
using Core.DTOs;
using Core.Exceptions;
using Infrastructure.Services;
using Xunit;

public class ServiceCatalogServiceTests
{
    [Fact]
    public async Task Create_Then_GetBySlug_ReturnsPublished()
    {
        await using var db = TestDb.NewInMemory();            // подставьте фактический хелпер
        var svc = new ServiceCatalogService(db, new PassthroughSanitizer());
        await svc.CreateAsync(new CreateServiceDto { Title="Фундамент", Slug="fundament", Content="<p>x</p>", IsPublished=true, Tag="foundation" });

        var dto = await svc.GetBySlugAsync("fundament");

        Assert.NotNull(dto);
        Assert.Equal("foundation", dto!.Tag);
    }

    [Fact]
    public async Task Create_DuplicateSlug_Throws()
    {
        await using var db = TestDb.NewInMemory();
        var svc = new ServiceCatalogService(db, new PassthroughSanitizer());
        await svc.CreateAsync(new CreateServiceDto { Title="A", Slug="dup", Content="x", IsPublished=true });

        await Assert.ThrowsAsync<DuplicateSlugException>(() =>
            svc.CreateAsync(new CreateServiceDto { Title="B", Slug="dup", Content="x", IsPublished=true }));
    }
}
```

- [ ] **Step 5: Чекпойнт — тесты**

Run: `cd backend && dotnet test --filter "FullyQualifiedName~ServiceCatalogServiceTests"`
Expected: 2 теста зелёные.

---

### Task A4: Контроллеры услуг (public + admin) + DI

**Files:**
- Modify (переписать): `backend/src/Api/Controllers/ServicesController.cs`
- Create: `backend/src/Api/Controllers/PricesController.cs`
- Modify (переписать): `backend/src/Api/Controllers/Admin/AdminServicesController.cs`
- Create: `backend/src/Api/Controllers/Admin/AdminPricesController.cs`
- Modify: `backend/src/Api/Program.cs`
- Test: `backend/tests/Integration/ServicesEndpointTests.cs`

- [ ] **Step 1: Публичный `ServicesController` под сущность Service**

Переписать `ServicesController.cs`:
```csharp
using Microsoft.AspNetCore.Mvc;
using Core.DTOs;
using Core.Interfaces;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServicesController : ControllerBase
{
    private readonly IServiceCatalogService _service;
    public ServicesController(IServiceCatalogService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ServiceListItemDto>>> Get()
        => Ok(await _service.GetPublishedAsync());

    [HttpGet("{slug}")]
    public async Task<ActionResult<ServiceDto>> GetBySlug(string slug)
    {
        var dto = await _service.GetBySlugAsync(slug);
        return dto is null ? NotFound() : Ok(dto);
    }
}
```

- [ ] **Step 2: Публичный `PricesController` (бывш. `/api/services/prices` → `/api/prices`)**

Create `PricesController.cs`:
```csharp
using Microsoft.AspNetCore.Mvc;
using Core.DTOs;
using Core.Interfaces;

namespace Api.Controllers;

[ApiController]
[Route("api/prices")]
public class PricesController : ControllerBase
{
    private readonly IServicePriceService _service;
    public PricesController(IServicePriceService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ServicePriceDto>>> Get()
        => Ok(await _service.GetAllAsync());
}
```

- [ ] **Step 3: Admin-контроллеры**

Переписать `Admin/AdminServicesController.cs` под `IServiceCatalogService`:
```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Core.DTOs;
using Core.Exceptions;
using Core.Interfaces;

namespace Api.Controllers.Admin;

[ApiController]
[Authorize]
[Route("api/admin/services")]
public class AdminServicesController : ControllerBase
{
    private readonly IServiceCatalogService _service;
    public AdminServicesController(IServiceCatalogService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ServiceDto>>> GetAll()
        => Ok(await _service.GetAllForAdminAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ServiceDto>> GetById(int id)
    {
        var dto = await _service.GetByIdAsync(id);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<ServiceDto>> Create(CreateServiceDto dto)
    {
        try
        {
            var created = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (DuplicateSlugException) { return Conflict(new { message = "Slug уже существует" }); }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ServiceDto>> Update(int id, UpdateServiceDto dto)
    {
        try
        {
            var updated = await _service.UpdateAsync(id, dto);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (DuplicateSlugException) { return Conflict(new { message = "Slug уже существует" }); }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
        => await _service.DeleteAsync(id) ? NoContent() : NotFound();
}
```
Create `Admin/AdminPricesController.cs` — это бывший admin services (CRUD цен) на новом маршруте:
```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Core.DTOs;
using Core.Interfaces;

namespace Api.Controllers.Admin;

[ApiController]
[Authorize]
[Route("api/admin/prices")]
public class AdminPricesController : ControllerBase
{
    private readonly IServicePriceService _service;
    public AdminPricesController(IServicePriceService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ServicePriceDto>>> GetAll()
        => Ok(await _service.GetAllAsync());

    [HttpPost]
    public async Task<ActionResult<ServicePriceDto>> Create(CreateServicePriceDto dto)
    {
        var created = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetAll), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ServicePriceDto>> Update(int id, UpdateServicePriceDto dto)
    {
        var updated = await _service.UpdateAsync(id, dto);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
        => await _service.DeleteAsync(id) ? NoContent() : NotFound();
}
```

- [ ] **Step 4: DI-регистрация**

В `Program.cs` после строки регистрации `IServicePriceService` добавить:
```csharp
builder.Services.AddScoped<Core.Interfaces.IServiceCatalogService, Infrastructure.Services.ServiceCatalogService>();
```

- [ ] **Step 5: Интеграционный тест**

`ServicesEndpointTests.cs` (по образцу `ArticlesEndpointTests.cs` — тот же `WebApplicationFactory<Program>`/фикстура, что используется в проекте):
```csharp
using System.Net;
using System.Net.Http.Json;
using Core.DTOs;
using Xunit;

public class ServicesEndpointTests : IClassFixture<ApiFactory>   // подставьте фактическую фикстуру
{
    private readonly HttpClient _client;
    public ServicesEndpointTests(ApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task GetServices_ReturnsOk()
    {
        var res = await _client.GetAsync("/api/services");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task GetService_Unknown_Returns404()
    {
        var res = await _client.GetAsync("/api/services/no-such-slug");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task GetPrices_ReturnsOk()
    {
        var res = await _client.GetAsync("/api/prices");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }
}
```

- [ ] **Step 6: Чекпойнт — тесты**

Run: `cd backend && dotnet test`
Expected: всё зелёное. (Если есть старый тест, дёргавший `/api/services/prices` — обновить на `/api/prices`.)

---

### Task A5: Переработка `ServicePrice` в «пример работы»

**Files:**
- Modify: `backend/src/Core/Entities/ServicePrice.cs`
- Modify: `backend/src/Infrastructure/Data/AppDbContext.cs`
- Modify: `backend/src/Core/DTOs/ServicePriceDto.cs`, `CreateServicePriceDto.cs`, `UpdateServicePriceDto.cs`
- Modify: `backend/src/Infrastructure/Services/ServicePriceService.cs`
- Test: `backend/tests/Unit/ServicePriceServiceTests.cs`

- [ ] **Step 1: Сущность (rename + новые поля)**

`ServicePrice.cs`:
```csharp
namespace Core.Entities;

public class ServicePrice
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;   // было Name
    public string? PhotoPath { get; set; }
    public string? Description { get; set; }
    public int Price { get; set; }                       // было PriceFrom (рубли, итог)
    public string? Duration { get; set; }                // напр. "2 дня"
    public string? ArticleSlug { get; set; }             // ссылка на статью с подробностями
    public string? Tag { get; set; }
    public int SortOrder { get; set; }
}
```

- [ ] **Step 2: DbContext-конфиг**

В `AppDbContext.cs` заменить блок `ServicePrice`:
```csharp
        modelBuilder.Entity<ServicePrice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired();
        });
```

- [ ] **Step 3: DTO**

`ServicePriceDto.cs`:
```csharp
namespace Core.DTOs;

public record ServicePriceDto(
    int Id, string Title, string? PhotoPath, string? Description,
    int Price, string? Duration, string? ArticleSlug, string? Tag, int SortOrder);
```
`CreateServicePriceDto.cs`:
```csharp
using System.ComponentModel.DataAnnotations;

namespace Core.DTOs;

public class CreateServicePriceDto
{
    [Required] public string Title { get; set; } = string.Empty;
    public string? PhotoPath { get; set; }
    public string? Description { get; set; }
    public int Price { get; set; }
    public string? Duration { get; set; }
    public string? ArticleSlug { get; set; }
    public string? Tag { get; set; }
    public int SortOrder { get; set; }
}
```
`UpdateServicePriceDto.cs` — идентично Create (та же форма).

- [ ] **Step 4: Сервис**

`ServicePriceService.cs` — заменить маппинги:
```csharp
    public async Task<IReadOnlyList<ServicePriceDto>> GetAllAsync()
    {
        return await _db.ServicePrices
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Price)
            .Select(p => new ServicePriceDto(
                p.Id, p.Title, p.PhotoPath, p.Description, p.Price, p.Duration, p.ArticleSlug, p.Tag, p.SortOrder))
            .ToListAsync();
    }

    public async Task<ServicePriceDto> CreateAsync(CreateServicePriceDto dto)
    {
        var entity = new ServicePrice
        {
            Title = dto.Title, PhotoPath = dto.PhotoPath, Description = dto.Description,
            Price = dto.Price, Duration = dto.Duration, ArticleSlug = dto.ArticleSlug,
            Tag = dto.Tag, SortOrder = dto.SortOrder,
        };
        _db.ServicePrices.Add(entity);
        await _db.SaveChangesAsync();
        return ToDto(entity);
    }

    public async Task<ServicePriceDto?> UpdateAsync(int id, UpdateServicePriceDto dto)
    {
        var entity = await _db.ServicePrices.FindAsync(id);
        if (entity is null) return null;
        entity.Title = dto.Title; entity.PhotoPath = dto.PhotoPath; entity.Description = dto.Description;
        entity.Price = dto.Price; entity.Duration = dto.Duration; entity.ArticleSlug = dto.ArticleSlug;
        entity.Tag = dto.Tag; entity.SortOrder = dto.SortOrder;
        await _db.SaveChangesAsync();
        return ToDto(entity);
    }

    private static ServicePriceDto ToDto(ServicePrice p) => new(
        p.Id, p.Title, p.PhotoPath, p.Description, p.Price, p.Duration, p.ArticleSlug, p.Tag, p.SortOrder);
```
(`DeleteAsync` без изменений.)

- [ ] **Step 5: Миграция с сохранением данных**

```bash
cd backend && taskkill //IM Api.exe //F 2>/dev/null; dotnet ef migrations add ReworkServicePrice -p src/Infrastructure -s src/Api
```
**Проверить сгенерированную миграцию:** EF для SQLite делает rebuild таблицы. Убедиться, что присутствуют `RenameColumn(name: "Name", newName: "Title")` и `RenameColumn(name: "PriceFrom", newName: "Price")` (чтобы боевые данные перенеслись), а `Category`/`Unit`/`PriceTo` — `DropColumn`. Если EF сгенерировал Drop+Add вместо Rename (данные потеряются) — **вручную заменить** на `migrationBuilder.RenameColumn(...)` для `Name→Title` и `PriceFrom→Price` в методе `Up`, и обратные — в `Down`.

- [ ] **Step 6: Обновить unit-тесты цен**

В `ServicePriceServiceTests.cs` заменить обращения `Category/Name/PriceFrom/PriceTo/Unit` на новые поля. Пример теста:
```csharp
[Fact]
public async Task Create_Then_GetAll_MapsNewFields()
{
    await using var db = TestDb.NewInMemory();
    var svc = new ServicePriceService(db);
    await svc.CreateAsync(new CreateServicePriceDto {
        Title="Замена фундамента 4×5", Description="сваи+швеллеры", Price=340000,
        Duration="2 дня", ArticleSlug="zamena-fundamenta-svai", Tag="foundation", SortOrder=1 });

    var all = await svc.GetAllAsync();

    Assert.Single(all);
    Assert.Equal(340000, all[0].Price);
    Assert.Equal("2 дня", all[0].Duration);
}
```

- [ ] **Step 7: Чекпойнт — тесты**

Run: `cd backend && dotnet test`
Expected: всё зелёное.

---

### Task A6: Идемпотентные сиды контента

**Files:**
- Create: `backend/src/Infrastructure/Data/ContentSeeder.cs`
- Modify: `backend/src/Api/Program.cs`
- Test: `backend/tests/Unit/ContentSeederTests.cs`

- [ ] **Step 1: Сидер**

`ContentSeeder.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Core.Entities;

namespace Infrastructure.Data;

public static class ContentSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await SeedServicesAsync(db);
        await SeedPriceExamplesAsync(db);
        await SeedArticlesAsync(db);
    }

    private static async Task SeedServicesAsync(AppDbContext db)
    {
        if (await db.Services.AnyAsync()) return;   // только в пустую таблицу
        db.Services.AddRange(
            new Service { Title="Строительство", Slug="stroitelstvo", IconName="home", SortOrder=1, IsPublished=true,
                ShortDescription="Дома, беседки, сараи и подобное — по нашему или вашему проекту.",
                Content="<p>Строим частные дома, беседки, сараи и хозпостройки. Можем работать по вашему проекту или спроектировать сами.</p>", Tag="stroitelstvo" },
            new Service { Title="Ремонт и пристройки", Slug="remont-pristroyki", IconName="hammer", SortOrder=2, IsPublished=true,
                ShortDescription="Веранды, пристройки, расширение, фасады.",
                Content="<p>Ремонт частных построек, веранды и пристройки, расширение, фасадные работы.</p>", Tag="remont" },
            new Service { Title="Замена фундамента", Slug="zamena-fundamenta", IconName="layers", SortOrder=3, IsPublished=true,
                ShortDescription="Подведём новый надёжный фундамент под существующую постройку.",
                Content="<p>Меняем фундамент под существующими постройками.</p>", Tag="foundation" },
            new Service { Title="Передвижка построек", Slug="peredvizhka", IconName="move", SortOrder=4, IsPublished=true,
                ShortDescription="Аккуратно: бытовки и небольшие строения.",
                Content="<p>Передвигаем небольшие строения и бытовки.</p>", Tag="peredvizhka" },
            new Service { Title="Под ключ", Slug="pod-klyuch", IconName="key", SortOrder=5, IsPublished=true,
                ShortDescription="Закупим материалы по своей цене и привезём.",
                Content="<p>Берём закупку и доставку материалов на себя — по своей цене.</p>", Tag="pod-klyuch" });
        await db.SaveChangesAsync();
    }

    private static async Task SeedPriceExamplesAsync(AppDbContext db)
    {
        if (await db.ServicePrices.AnyAsync()) return;   // боевые цены не трогаем
        db.ServicePrices.AddRange(
            new ServicePrice { Title="Замена фундамента дачного дома 4×5 с верандой", SortOrder=1, Price=340000, Duration="2 дня",
                Description="С бетонных блоков на винтовые сваи и швеллеры.", ArticleSlug="zamena-fundamenta-svai", Tag="foundation" },
            new ServicePrice { Title="Утеплённая каркасная пристройка 3×7", SortOrder=2, Price=750000, Duration="1 неделя",
                Description="Сэндвич-панели, 2 окна.", Tag="remont" });
        await db.SaveChangesAsync();
    }

    private static async Task SeedArticlesAsync(AppDbContext db)
    {
        if (await db.Articles.AnyAsync()) return;   // боевые статьи не трогаем
        db.Articles.Add(new Article {
            Title="Замена фундамента на винтовые сваи и швеллеры",
            Slug="zamena-fundamenta-svai",
            Summary="Как заменили фундамент дачного дома 4×5 с бетонных блоков на сваи и швеллеры.",
            Content="<p>Дачный дом 4×5 с верандой стоял на бетонных блоках. Предложили винтовые сваи по периметру и швеллеры. Сделали за два дня.</p>",
            Tags="foundation", IsPublished=true, PublishedAt=DateTime.UtcNow });
        await db.SaveChangesAsync();
    }
}
```

- [ ] **Step 2: Вызов в Program.cs**

В `Program.cs` в блоке `using (var scope = ...)` после `await AdminSeeder.SeedAsync(...)` добавить:
```csharp
    await ContentSeeder.SeedAsync(db);
```

- [ ] **Step 3: Тест идемпотентности**

`ContentSeederTests.cs`:
```csharp
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class ContentSeederTests
{
    [Fact]
    public async Task Seed_IsIdempotent_NoDuplicateServices()
    {
        await using var db = TestDb.NewInMemory();   // подставьте фактический хелпер
        await ContentSeeder.SeedAsync(db);
        await ContentSeeder.SeedAsync(db);           // второй прогон не дублирует

        Assert.Equal(5, await db.Services.CountAsync());
    }

    [Fact]
    public async Task Seed_SkipsPricesWhenNotEmpty()
    {
        await using var db = TestDb.NewInMemory();
        db.ServicePrices.Add(new Core.Entities.ServicePrice { Title="боевая", Price=1 });
        await db.SaveChangesAsync();

        await ContentSeeder.SeedAsync(db);

        Assert.Equal(1, await db.ServicePrices.CountAsync());   // боевые не тронуты, демо не добавлено
    }
}
```

- [ ] **Step 4: Чекпойнт — тесты**

Run: `cd backend && dotnet test`
Expected: всё зелёное.

---

# Фаза B — Тема (светлая + тёмная)

### Task B1: CSS-переменные + Tailwind-токены

**Files:**
- Modify: `frontend/assets/css/main.css`
- Modify: `frontend/tailwind.config.ts`

- [ ] **Step 1: Переменные тем в main.css**

Заменить начало `main.css` (после `@tailwind` директив добавить):
```css
@layer base {
  :root {
    --surface: 248 250 252;     /* #f8fafc */
    --surface-2: 255 255 255;   /* #ffffff */
    --base: 226 232 240;        /* #e2e8f0 */
    --ink: 31 41 55;            /* #1f2937 */
    --muted: 100 116 139;       /* #64748b */
    --brand: 217 119 6;         /* #d97706 */
    --brand-contrast: 255 255 255;
  }
  .dark {
    --surface: 15 23 42;        /* #0f172a */
    --surface-2: 30 41 59;      /* #1e293b */
    --base: 51 65 85;           /* #334155 */
    --ink: 241 245 249;         /* #f1f5f9 */
    --muted: 148 163 184;       /* #94a3b8 */
    --brand: 245 158 11;        /* #f59e0b */
    --brand-contrast: 15 23 42;
  }
  body { background-color: rgb(var(--surface)); color: rgb(var(--ink)); }
}
```
(значения в формате `R G B` — чтобы Tailwind мог применять прозрачность через `/`.)

- [ ] **Step 2: Tailwind-токены на переменных**

`tailwind.config.ts` — заменить блок `colors` и добавить `darkMode`:
```ts
import type { Config } from 'tailwindcss'
import typography from '@tailwindcss/typography'

const withOpacity = (v: string) => `rgb(var(${v}) / <alpha-value>)`

export default <Partial<Config>>{
  darkMode: 'class',
  content: [],
  theme: {
    extend: {
      colors: {
        surface: withOpacity('--surface'),
        'surface-2': withOpacity('--surface-2'),
        base: withOpacity('--base'),
        ink: withOpacity('--ink'),
        muted: withOpacity('--muted'),
        brand: {
          DEFAULT: withOpacity('--brand'),
          contrast: withOpacity('--brand-contrast'),
        },
      },
      fontFamily: { sans: ['Inter', 'system-ui', 'sans-serif'] },
      borderRadius: { card: '0.75rem' },
    },
  },
  plugins: [typography],
}
```
> NB: `border-base` теперь даёт цвет границы из переменной. Класс `border` без цвета у Tailwind по умолчанию `currentColor` — в задачах фронта меняем `border` → `border border-base` там, где нужна тематическая граница.

- [ ] **Step 3: Чекпойнт — typecheck**

Run: `cd frontend && npx nuxi typecheck`
Expected: 0 ошибок. (Визуально проверим в Z-фазе.)

---

### Task B2: No-flash переключатель тёмной темы

**Files:**
- Create: `frontend/plugins/color-mode.client.ts`
- Create: `frontend/app.vue` head-скрипт ИЛИ `nuxt.config.ts` `app.head` (см. Step 2)
- Create: `frontend/components/ui/ThemeToggle.vue`
- Test: `frontend/tests/component/ThemeToggle.spec.ts`

- [ ] **Step 1: Плагин чтения/применения темы**

`plugins/color-mode.client.ts`:
```ts
export default defineNuxtPlugin(() => {
  const apply = (dark: boolean) => document.documentElement.classList.toggle('dark', dark)
  const stored = localStorage.getItem('theme')
  const system = window.matchMedia('(prefers-color-scheme: dark)').matches
  apply(stored ? stored === 'dark' : system)
})
```

- [ ] **Step 2: Инлайн-скрипт против вспышки (до гидрации)**

В `nuxt.config.ts` добавить в конфиг `app.head.script` (или создать `app.vue` c `<Head>`). Вариант через nuxt.config:
```ts
  app: {
    head: {
      script: [{
        innerHTML: `(function(){try{var t=localStorage.getItem('theme');var d=t?t==='dark':matchMedia('(prefers-color-scheme:dark)').matches;document.documentElement.classList.toggle('dark',d);}catch(e){}})();`,
        tagPosition: 'head',
      }],
    },
  },
```
(добавить рядом с существующими ключами `nuxt.config.ts`, не ломая их.)

- [ ] **Step 3: Компонент-переключатель**

`components/ui/ThemeToggle.vue`:
```vue
<script setup lang="ts">
const isDark = ref(false)
onMounted(() => { isDark.value = document.documentElement.classList.contains('dark') })
function toggle() {
  isDark.value = !isDark.value
  document.documentElement.classList.toggle('dark', isDark.value)
  localStorage.setItem('theme', isDark.value ? 'dark' : 'light')
}
</script>

<template>
  <button
    type="button"
    class="rounded-card border border-base px-2 py-1 text-ink hover:text-brand"
    :aria-label="isDark ? 'Светлая тема' : 'Тёмная тема'"
    @click="toggle"
  >
    {{ isDark ? '☀' : '☾' }}
  </button>
</template>
```

- [ ] **Step 4: Тест компонента**

`tests/component/ThemeToggle.spec.ts` (по образцу существующих spec в `tests/component/`):
```ts
import { mountSuspended } from '@nuxt/test-utils/runtime'
import { describe, it, expect, beforeEach } from 'vitest'
import ThemeToggle from '~/components/ui/ThemeToggle.vue'

describe('ThemeToggle', () => {
  beforeEach(() => { localStorage.clear(); document.documentElement.classList.remove('dark') })

  it('toggles dark class and persists', async () => {
    const wrapper = await mountSuspended(ThemeToggle)
    await wrapper.find('button').trigger('click')
    expect(document.documentElement.classList.contains('dark')).toBe(true)
    expect(localStorage.getItem('theme')).toBe('dark')
  })
})
```
> Если в проекте компонентные тесты используют `mount` из `@vue/test-utils`, а не `mountSuspended` — повторить тамошний паттерн.

- [ ] **Step 5: Чекпойнт**

Run: `cd frontend && npm run test -- ThemeToggle && npx nuxi typecheck`
Expected: тест зелёный, typecheck чист.

---

### Task B3: Заменить хардкод цветов на токены

**Files:**
- Modify: `frontend/components/sections/SectionHero.vue`, `SectionServicesTeaser.vue`, `SectionPricesTeaser.vue`, `SectionPortfolioTeaser.vue`, `SectionAbout.vue`, `SectionCta.vue`
- Modify: `frontend/components/layout/AppHeader.vue`, `AppFooter.vue`
- Modify: `frontend/components/ui/BaseButton.vue`, `BaseInput.vue`, `PortfolioCard.vue`

- [ ] **Step 1: Найти хардкод**

Run: `cd frontend && grep -rn "bg-white\|bg-gray-50\|bg-gray-700\|text-gray-200\|text-gray-300\|border-b bg-white\|from-ink\|to-gray" components/ pages/`
Expected: список мест с захардкоженными цветами.

- [ ] **Step 2: Заменить по правилам**

Применить замены во всех найденных местах:
- `bg-white` (карточки/шапка) → `bg-surface-2`
- `bg-gray-50` (секции-подложки) → `bg-surface`
- одиночный `border` для карточек → `border border-base`
- `text-gray-200/300` (на тёмном геро) — оставить как есть (геро тёмный в обеих темах)
- `text-white` на кнопке brand → `text-brand-contrast`
- В `AppHeader.vue`: `border-b bg-white` → `border-b border-base bg-surface-2`
- В `AppFooter.vue`: `bg-ink text-white` оставить (футер тёмный в обеих темах допустим) ИЛИ → `bg-surface-2 text-ink border-t border-base` для единообразия; **выбрать `bg-surface-2 ... border-t border-base`**.
- Контентные `prose` (страницы статьи/услуги) → добавить `dark:prose-invert`.

- [ ] **Step 3: Чекпойнт**

Run: `cd frontend && npx nuxi typecheck && npm run test`
Expected: всё зелёное (вёрстку смотрим в Z).

---

# Фаза C — Frontend: контент и страницы

### Task C1: Бренд и контакты (`app.config.ts`)

**Files:**
- Modify: `frontend/app.config.ts`

- [ ] **Step 1: Заполнить бренд**

`app.config.ts`:
```ts
export default defineAppConfig({
  company: {
    name: 'Суровая Стройка',
    tagline: 'Подопрём, поднимем, построим',
    phone: '+7 (999) 123-45-67',          // заглушка до реальных данных
    email: 'info@surovaya-stroyka.ru',     // заглушка
    address: 'г. Тула и Тульская область',
    social: { vk: '', telegram: '' },
  },
})
```
(поле `schedule` удалено.)

- [ ] **Step 2: Чекпойнт — typecheck**

Run: `cd frontend && npx nuxi typecheck`
Expected: если где-то использовался `c.schedule`, словить ошибку и убрать использование (см. Task C3).

---

### Task C2: Навигация + переключатель темы в шапке

**Files:**
- Modify: `frontend/components/layout/AppHeader.vue`

- [ ] **Step 1: Обновить ссылки и вставить ThemeToggle**

`AppHeader.vue`:
```vue
<script setup lang="ts">
const c = useContacts()
const links = [
  { to: '/', label: 'Главная' },
  { to: '/services', label: 'Услуги' },
  { to: '/portfolio', label: 'Из практики' },
  { to: '/prices', label: 'Примеры работ и цен' },
  { to: '/contact', label: 'Контакты' },
]
</script>

<template>
  <header class="border-b border-base bg-surface-2">
    <div class="mx-auto flex max-w-6xl items-center justify-between px-4 py-4">
      <NuxtLink to="/" class="text-xl font-bold text-brand">{{ c.name }}</NuxtLink>
      <nav class="hidden gap-6 md:flex">
        <NuxtLink v-for="l in links" :key="l.to" :to="l.to" class="text-ink hover:text-brand">
          {{ l.label }}
        </NuxtLink>
      </nav>
      <div class="flex items-center gap-3">
        <a :href="`tel:${c.phone}`" class="font-medium text-brand">{{ c.phone }}</a>
        <ThemeToggle />
      </div>
    </div>
  </header>
</template>
```

- [ ] **Step 2: Чекпойнт**

Run: `cd frontend && npx nuxi typecheck`
Expected: чисто.

---

### Task C3: Футер (убрать часы работы)

**Files:**
- Modify: `frontend/components/layout/AppFooter.vue`

- [ ] **Step 1: Убрать `schedule`, применить токены**

`AppFooter.vue`:
```vue
<script setup lang="ts">
const c = useContacts()
</script>

<template>
  <footer class="mt-16 border-t border-base bg-surface-2 text-ink">
    <div class="mx-auto grid max-w-6xl gap-6 px-4 py-10 md:grid-cols-3">
      <div>
        <div class="text-lg font-bold">{{ c.name }}</div>
        <p class="mt-2 text-sm text-muted">{{ c.tagline }}</p>
      </div>
      <div class="text-sm text-muted">
        <p>{{ c.address }}</p>
      </div>
      <div class="text-sm">
        <a :href="`tel:${c.phone}`" class="block text-ink hover:text-brand">{{ c.phone }}</a>
        <a :href="`mailto:${c.email}`" class="block text-ink hover:text-brand">{{ c.email }}</a>
      </div>
    </div>
  </footer>
</template>
```

- [ ] **Step 2: Чекпойнт**

Run: `cd frontend && npx nuxi typecheck`
Expected: чисто (ссылок на `schedule` больше нет).

---

### Task C4: Геро + SEO-копирайт

**Files:**
- Modify: `frontend/components/sections/SectionHero.vue`
- Modify: `frontend/pages/index.vue`

- [ ] **Step 1: Геро (кнопка с контрастным текстом)**

`SectionHero.vue`:
```vue
<script setup lang="ts">
const c = useContacts()
</script>

<template>
  <section class="bg-gradient-to-br from-slate-900 to-slate-700 text-white">
    <div class="mx-auto max-w-6xl px-4 py-24 text-center">
      <h1 class="text-4xl font-bold md:text-5xl">{{ c.name }}</h1>
      <p class="mx-auto mt-4 max-w-2xl text-lg text-slate-200">{{ c.tagline }}</p>
      <NuxtLink to="/contact" class="mt-8 inline-block rounded-card bg-brand px-6 py-3 font-medium text-brand-contrast hover:opacity-90">
        Оставить заявку
      </NuxtLink>
    </div>
  </section>
</template>
```

- [ ] **Step 2: SEO без выдуманных обещаний**

В `index.vue` заменить `useSeoMeta`:
```ts
useSeoMeta({
  title: `${c.name} — ${c.tagline}`,
  description: 'Строительство, ремонт, замена фундамента и передвижка построек в Туле и области. Подскажем цену заранее, всё обсудим до начала.',
  ogTitle: c.name,
  ogDescription: c.tagline,
})
```

- [ ] **Step 3: Чекпойнт**

Run: `cd frontend && npx nuxi typecheck`
Expected: чисто.

---

### Task C5: Блок «Как работаем» (вместо фейк-статистики и «прозрачных цен»)

**Files:**
- Modify: `frontend/components/sections/SectionAbout.vue` (переименовать смысл в «Как работаем»)
- Modify: `frontend/components/sections/SectionPricesTeaser.vue`
- Create: `frontend/components/ui/Icon.vue` (универсальная lucide-иконка — см. Task E2; до неё можно использовать заглушку-эмодзи, заменив в E2)

- [ ] **Step 1: SectionAbout → «Как работаем»**

`SectionAbout.vue`:
```vue
<script setup lang="ts">
const items = [
  { icon: 'badge-ruble', title: 'Примерная стоимость сразу', text: 'По фото и описанию подскажем порядок цены ещё до выезда.' },
  { icon: 'handshake', title: 'Всё обсудим заранее', text: 'Согласуем объём и стоимость до начала, по ходу работ честно корректируем.' },
  { icon: 'hammer', title: 'Полный цикл работ', text: 'Строительство, ремонт, замена фундамента, передвижка построек.' },
  { icon: 'truck', title: 'Материалы — наша забота', text: 'Под ключ: закупим и привезём по своей цене.' },
]
</script>

<template>
  <section class="bg-surface">
    <div class="mx-auto max-w-6xl px-4 py-16">
      <h2 class="text-3xl font-bold text-ink">Как работаем</h2>
      <div class="mt-8 grid gap-6 md:grid-cols-2 lg:grid-cols-4">
        <div v-for="i in items" :key="i.title" class="rounded-card border border-base bg-surface-2 p-6">
          <Icon :name="i.icon" class="h-7 w-7 text-brand" />
          <h3 class="mt-3 text-lg font-semibold text-ink">{{ i.title }}</h3>
          <p class="mt-1 text-muted">{{ i.text }}</p>
        </div>
      </div>
    </div>
  </section>
</template>
```

- [ ] **Step 2: Убрать «Прозрачные цены», поправить teaser цен**

`SectionPricesTeaser.vue`:
```vue
<template>
  <section class="bg-surface-2 border-y border-base">
    <div class="mx-auto max-w-6xl px-4 py-16 text-center">
      <h2 class="text-3xl font-bold text-ink">Примеры работ и цен</h2>
      <p class="mt-3 text-muted">Посмотрите реальные работы с ценами и сроками.</p>
      <NuxtLink to="/prices" class="mt-6 inline-block rounded-card border border-brand px-6 py-3 font-medium text-brand hover:bg-brand/10">
        Смотреть примеры
      </NuxtLink>
    </div>
  </section>
</template>
```

- [ ] **Step 3: Чекпойнт**

Run: `cd frontend && npx nuxi typecheck`
Expected: чисто (после Task E2, где появится `Icon.vue`; если выполняется раньше — временно заменить `<Icon .../>` на эмодзи, вернуть в E2).

---

### Task C6: Услуги — типы/API, teaser, список, детальная

**Files:**
- Modify: `frontend/types/api.ts`, `frontend/lib/api.ts`
- Modify: `frontend/components/sections/SectionServicesTeaser.vue`
- Create: `frontend/components/ui/ServiceCard.vue`
- Create: `frontend/pages/services/index.vue`, `frontend/pages/services/[slug].vue`

- [ ] **Step 1: Типы услуг + метод API + tag-фильтр + getPrices→/api/prices**

В `types/api.ts` добавить:
```ts
export interface ServiceListItem {
  id: number
  title: string
  slug: string
  shortDescription?: string | null
  iconName?: string | null
  sortOrder: number
}

export interface ServiceDetail {
  id: number
  title: string
  slug: string
  shortDescription?: string | null
  iconName?: string | null
  content: string
  tag?: string | null
  sortOrder: number
  isPublished: boolean
}
```
В `types/api.ts` обновить `ArticleListItem` и `ServicePrice`:
```ts
export interface ArticleListItem {
  id: number
  title: string
  slug: string
  summary?: string | null
  thumbnailPath?: string | null
  publishedAt: string
  tags?: string | null
}

export interface ServicePrice {
  id: number
  title: string
  photoPath?: string | null
  description?: string | null
  price: number
  duration?: string | null
  articleSlug?: string | null
  tag?: string | null
  sortOrder: number
}
```
В `lib/api.ts` — добавить методы и поправить `getPrices`/`getArticles`:
```ts
import type {
  Article, ArticleListItem, PagedResult, ServicePrice,
  ServiceListItem, ServiceDetail,
  CallbackPayload, ContactPayload, MessageResponse,
} from '~/types/api'
// ...
    getArticles(page = 1, pageSize = 12, tag?: string) {
      return fetcher<PagedResult<ArticleListItem>>(url('/api/articles'), {
        query: { page, pageSize, ...(tag ? { tag } : {}) },
      })
    },
    getServices() {
      return fetcher<ServiceListItem[]>(url('/api/services'))
    },
    getService(slug: string) {
      return fetcher<ServiceDetail>(url(`/api/services/${slug}`))
    },
    getPrices() {
      return fetcher<ServicePrice[]>(url('/api/prices'))
    },
```

- [ ] **Step 2: ServiceCard**

`components/ui/ServiceCard.vue`:
```vue
<script setup lang="ts">
import type { ServiceListItem } from '~/types/api'
defineProps<{ service: ServiceListItem }>()
</script>

<template>
  <NuxtLink :to="`/services/${service.slug}`" class="block rounded-card border border-base bg-surface-2 p-6 transition hover:border-brand">
    <Icon v-if="service.iconName" :name="service.iconName" class="h-8 w-8 text-brand" />
    <h3 class="mt-3 text-lg font-semibold text-ink">{{ service.title }}</h3>
    <p v-if="service.shortDescription" class="mt-1 text-muted">{{ service.shortDescription }}</p>
  </NuxtLink>
</template>
```

- [ ] **Step 3: Teaser услуг на главной (из API)**

`SectionServicesTeaser.vue`:
```vue
<script setup lang="ts">
import type { ServiceListItem } from '~/types/api'
defineProps<{ title?: string; services: ServiceListItem[] }>()
</script>

<template>
  <section class="mx-auto max-w-6xl px-4 py-16">
    <div class="flex items-center justify-between">
      <h2 class="text-3xl font-bold text-ink">{{ title ?? 'Услуги' }}</h2>
      <NuxtLink to="/services" class="text-brand hover:underline">Все услуги →</NuxtLink>
    </div>
    <div class="mt-8 grid gap-6 md:grid-cols-3">
      <ServiceCard v-for="s in services" :key="s.id" :service="s" />
    </div>
  </section>
</template>
```
И в `pages/index.vue` подгрузить услуги и передать:
```ts
const { data: services } = await useAsyncData('home-services', () => api.getServices())
```
```vue
    <SectionServicesTeaser :services="services ?? []" />
```

- [ ] **Step 4: Страница списка услуг**

`pages/services/index.vue`:
```vue
<script setup lang="ts">
const api = useApi()
const { data } = await useAsyncData('services', () => api.getServices())
useSeoMeta({ title: 'Услуги — Суровая Стройка', description: 'Строительство, ремонт, замена фундамента, передвижка построек, под ключ.' })
</script>

<template>
  <div class="mx-auto max-w-6xl px-4 py-12">
    <h1 class="text-3xl font-bold text-ink">Услуги</h1>
    <div v-if="data && data.length" class="mt-8 grid gap-6 md:grid-cols-3">
      <ServiceCard v-for="s in data" :key="s.id" :service="s" />
    </div>
    <p v-else class="mt-8 text-muted">Список услуг скоро появится.</p>
  </div>
</template>
```

- [ ] **Step 5: Детальная услуга + статьи по тегу**

`pages/services/[slug].vue`:
```vue
<script setup lang="ts">
const api = useApi()
const route = useRoute()
const slug = computed(() => String(route.params.slug))

const { data: service } = await useAsyncData(() => `service-${slug.value}`, () => api.getService(slug.value))
if (!service.value) throw createError({ statusCode: 404, statusMessage: 'Услуга не найдена' })

const { data: related } = await useAsyncData(
  () => `service-articles-${slug.value}`,
  () => service.value?.tag ? api.getArticles(1, 6, service.value.tag) : Promise.resolve(null),
)

useSeoMeta({
  title: () => `${service.value?.title} — Суровая Стройка`,
  description: () => service.value?.shortDescription ?? '',
})
</script>

<template>
  <div class="mx-auto max-w-3xl px-4 py-12">
    <h1 class="text-3xl font-bold text-ink">{{ service?.title }}</h1>
    <p v-if="service?.shortDescription" class="mt-2 text-muted">{{ service.shortDescription }}</p>
    <!-- eslint-disable-next-line vue/no-v-html -->
    <article class="prose dark:prose-invert mt-6 max-w-none" v-html="service?.content" />

    <section v-if="related && related.items.length" class="mt-12">
      <h2 class="text-2xl font-bold text-ink">Из практики по теме</h2>
      <div class="mt-6 grid gap-6 md:grid-cols-3">
        <PortfolioCard v-for="a in related.items" :key="a.id" :article="a" />
      </div>
    </section>

    <div class="mt-12">
      <NuxtLink to="/contact" class="inline-block rounded-card bg-brand px-6 py-3 font-medium text-brand-contrast hover:opacity-90">
        Обсудить задачу
      </NuxtLink>
    </div>
  </div>
</template>
```

- [ ] **Step 6: Чекпойнт**

Run: `cd frontend && npx nuxi typecheck && npm run test`
Expected: чисто/зелёное. (Тесты, использующие `getArticles`/`getPrices` сигнатуры — поправить, если упадут.)

---

### Task C7: Цены → примеры работ

**Files:**
- Modify: `frontend/lib/prices.ts`
- Create: `frontend/components/ui/PriceExampleCard.vue`
- Delete/replace usage: `frontend/components/ui/PriceTable.vue`
- Modify: `frontend/pages/prices.vue`
- Test: `frontend/tests/unit/prices.spec.ts` (если есть — обновить)

- [ ] **Step 1: lib/prices — форматирование цены, без групп/единиц**

Заменить `lib/prices.ts` целиком:
```ts
import type { ServicePrice } from '~/types/api'

// Группировка тысяч неразрывным пробелом (U+00A0), без зависимости от локали.
const group = (n: number) => n.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ' ')

export function formatPrice(p: ServicePrice): string {
  return `${group(p.price)} ₽`
}
```
> Удалить экспорт `groupByCategory`/`PriceGroup` (больше не используются).

- [ ] **Step 2: PriceExampleCard**

`components/ui/PriceExampleCard.vue`:
```vue
<script setup lang="ts">
import type { ServicePrice } from '~/types/api'
import { formatPrice } from '~/lib/prices'
const props = defineProps<{ item: ServicePrice }>()
const photo = useMediaUrl(() => props.item.photoPath ?? '/images/placeholder.svg')
</script>

<template>
  <div class="overflow-hidden rounded-card border border-base bg-surface-2">
    <div class="aspect-[4/3] w-full overflow-hidden bg-surface">
      <img :src="photo" :alt="item.title" class="h-full w-full object-cover" loading="lazy" />
    </div>
    <div class="p-5">
      <h3 class="text-lg font-semibold text-ink">{{ item.title }}</h3>
      <p v-if="item.description" class="mt-1 text-sm text-muted">{{ item.description }}</p>
      <div class="mt-3 flex items-center justify-between">
        <span class="font-bold text-brand">{{ formatPrice(item) }}</span>
        <span v-if="item.duration" class="text-sm text-muted">{{ item.duration }}</span>
      </div>
      <NuxtLink v-if="item.articleSlug" :to="`/portfolio/${item.articleSlug}`" class="mt-3 inline-block text-sm text-brand hover:underline">
        Подробнее →
      </NuxtLink>
    </div>
  </div>
</template>
```
> `useMediaUrl` — существующий composable. Проверить его сигнатуру: если он принимает строку, а не геттер — использовать `computed(() => useMediaUrl(props.item.photoPath ?? '/images/placeholder.svg'))` соответствующим образом. Плейсхолдер `/images/...` — локальная статика (не через apiClientBase): если `useMediaUrl` префиксит относительные пути api-базой, для плейсхолдера задать абсолютный публичный путь напрямую (`/images/placeholder.svg`) минуя `useMediaUrl`.

- [ ] **Step 3: Страница цен**

`pages/prices.vue`:
```vue
<script setup lang="ts">
const api = useApi()
const { data } = await useAsyncData('prices', () => api.getPrices())

useSeoMeta({
  title: 'Примеры работ и цен — Суровая Стройка',
  description: 'Реальные примеры выполненных работ с ценами и сроками.',
})
</script>

<template>
  <div class="mx-auto max-w-5xl px-4 py-12">
    <h1 class="text-3xl font-bold text-ink">Примеры работ и цен</h1>
    <p class="mt-3 text-muted">Точную цену назовём после обсуждения задачи. Ниже — реальные примеры.</p>
    <div v-if="data && data.length" class="mt-8 grid gap-6 md:grid-cols-2 lg:grid-cols-3">
      <PriceExampleCard v-for="p in data" :key="p.id" :item="p" />
    </div>
    <p v-else class="mt-8 text-muted">Примеры скоро появятся. Свяжитесь с нами для расчёта.</p>
  </div>
</template>
```

- [ ] **Step 4: Удалить PriceTable**

Удалить `components/ui/PriceTable.vue` (заменён карточками). Проверить, что нигде не импортируется:
Run: `cd frontend && grep -rn "PriceTable" components/ pages/`
Expected: пусто.

- [ ] **Step 5: Чекпойнт**

Run: `cd frontend && npx nuxi typecheck && npm run test`
Expected: чисто/зелёное. (Если был `tests/unit/prices.spec.ts` с `groupByCategory`/`formatPrice(unit)` — переписать под новый `formatPrice`.)

---

### Task C8: Портфолио → «Из практики» (лейблы)

**Files:**
- Modify: `frontend/pages/portfolio/index.vue`
- Modify: `frontend/components/sections/SectionPortfolioTeaser.vue`

- [ ] **Step 1: Переименовать заголовки/SEO**

`pages/portfolio/index.vue` — заменить:
```vue
    <h1 class="text-3xl font-bold text-ink">Из практики</h1>
```
```ts
useSeoMeta({
  title: 'Из практики — Суровая Стройка',
  description: 'Проделанные работы и полезные решения: фундаменты, пристройки, передвижка построек.',
})
```
Пустое состояние: `Скоро здесь появятся материалы.`

- [ ] **Step 2: Teaser**

`SectionPortfolioTeaser.vue` — заменить заголовок/ссылку:
```vue
      <h2 class="text-3xl font-bold text-ink">Из практики</h2>
      <NuxtLink to="/portfolio" class="text-brand hover:underline">Все материалы →</NuxtLink>
```
Пустое состояние: `Скоро здесь появятся материалы.`

- [ ] **Step 3: Чекпойнт**

Run: `cd frontend && npx nuxi typecheck`
Expected: чисто.

---

### Task C9: Контакты — копирайт

**Files:**
- Modify: `frontend/pages/contact.vue`

- [ ] **Step 1: Проверить и поправить тексты**

Открыть `pages/contact.vue`. Убедиться, что выводится `c.address` (= «г. Тула и Тульская область») и НЕ выводится `c.schedule`/часы работы. Если есть упоминание города Москва/часов/гарантий — убрать. Заголовок/описание привести к бренду «Суровая Стройка».

- [ ] **Step 2: Чекпойнт**

Run: `cd frontend && npx nuxi typecheck && npm run test`
Expected: чисто/зелёное.

---

# Фаза D — Админка

### Task D1: Типы/API админки (услуги + цены-примеры + теги)

**Files:**
- Modify: `frontend/types/admin.ts`, `frontend/lib/adminApi.ts`

- [ ] **Step 1: Типы**

В `types/admin.ts`:
- Добавить `tags` в `AdminArticle` и `ArticleWrite`:
```ts
// в AdminArticle:
  tags?: string | null
// в ArticleWrite:
  tags?: string | null
```
- Заменить `ServicePriceWrite`:
```ts
export interface ServicePriceWrite {
  title: string
  photoPath?: string | null
  description?: string | null
  price: number
  duration?: string | null
  articleSlug?: string | null
  tag?: string | null
  sortOrder: number
}
```
- Добавить типы услуг:
```ts
export interface ServiceWrite {
  title: string
  slug: string
  shortDescription?: string | null
  iconName?: string | null
  content: string
  tag?: string | null
  sortOrder: number
  isPublished: boolean
}
export interface AdminService extends ServiceWrite { id: number }
```

- [ ] **Step 2: adminApi — методы цен (новый маршрут) + услуг**

В `lib/adminApi.ts`:
- Заменить методы цен на маршрут `/api/admin/prices`:
```ts
    listPrices() {
      return fetcher<ServicePrice[]>(url('/api/admin/prices'), { headers: auth() })
    },
    createPrice(body: ServicePriceWrite) {
      return fetcher<ServicePrice>(url('/api/admin/prices'), { method: 'POST', body, headers: auth() })
    },
    updatePrice(id: number, body: ServicePriceWrite) {
      return fetcher<ServicePrice>(url(`/api/admin/prices/${id}`), { method: 'PUT', body, headers: auth() })
    },
    deletePrice(id: number) {
      return fetcher<void>(url(`/api/admin/prices/${id}`), { method: 'DELETE', headers: auth() })
    },
```
- Добавить методы услуг (импортировать `ServiceWrite, AdminService` из `~/types/admin`):
```ts
    listServices() {
      return fetcher<AdminService[]>(url('/api/admin/services'), { headers: auth() })
    },
    getService(id: number) {
      return fetcher<AdminService>(url(`/api/admin/services/${id}`), { headers: auth() })
    },
    createService(body: ServiceWrite) {
      return fetcher<AdminService>(url('/api/admin/services'), { method: 'POST', body, headers: auth() })
    },
    updateService(id: number, body: ServiceWrite) {
      return fetcher<AdminService>(url(`/api/admin/services/${id}`), { method: 'PUT', body, headers: auth() })
    },
    deleteService(id: number) {
      return fetcher<void>(url(`/api/admin/services/${id}`), { method: 'DELETE', headers: auth() })
    },
```

- [ ] **Step 2: Чекпойнт**

Run: `cd frontend && npx nuxi typecheck`
Expected: ошибки только в местах, которые правим в D2/D3/D4 (страницы) — их закроем там.

---

### Task D2: Админ-страница услуг (CRUD)

**Files:**
- Create: `frontend/pages/admin/services.vue`
- Modify: `frontend/pages/admin/index.vue` (ссылка в меню админки, если есть навигация)

- [ ] **Step 1: Страница CRUD услуг**

`pages/admin/services.vue` (паттерн как `admin/prices.vue`; WYSIWYG для Content переиспользует `ArticleEditor`):
```vue
<script setup lang="ts">
import type { AdminService, ServiceWrite } from '~/types/admin'
definePageMeta({ layout: 'admin', middleware: 'auth' })
const api = useAdminApi()
const items = ref<AdminService[]>([])
const editingId = ref<number | null>(null)
const blank = (): ServiceWrite => ({ title:'', slug:'', shortDescription:'', iconName:'', content:'', tag:'', sortOrder:0, isPublished:true })
const draft = reactive<ServiceWrite>(blank())

async function load() { items.value = await api.listServices() }
onMounted(load)

function edit(s: AdminService) { editingId.value = s.id; Object.assign(draft, s) }
function reset() { editingId.value = null; Object.assign(draft, blank()) }

async function save() {
  if (editingId.value) await api.updateService(editingId.value, { ...draft })
  else await api.createService({ ...draft })
  reset(); await load()
}
async function remove(id: number) {
  if (!confirm('Удалить услугу?')) return
  await api.deleteService(id); await load()
}
</script>

<template>
  <div class="max-w-4xl">
    <h1 class="text-2xl font-bold mb-6">Услуги</h1>
    <table class="w-full bg-white rounded shadow mb-6">
      <thead><tr class="text-left border-b"><th class="p-2">Порядок</th><th class="p-2">Название</th><th class="p-2">Slug</th><th class="p-2">Тег</th><th></th></tr></thead>
      <tbody>
        <tr v-for="s in items" :key="s.id" class="border-b">
          <td class="p-2">{{ s.sortOrder }}</td><td class="p-2">{{ s.title }}</td>
          <td class="p-2">{{ s.slug }}</td><td class="p-2">{{ s.tag }}</td>
          <td class="p-2 text-right space-x-3">
            <button class="text-blue-600" @click="edit(s)">Изм.</button>
            <button class="text-red-600" @click="remove(s.id)">Удалить</button>
          </td>
        </tr>
      </tbody>
    </table>

    <div class="bg-white p-4 rounded shadow space-y-3">
      <h2 class="font-semibold">{{ editingId ? 'Редактировать' : 'Добавить' }} услугу</h2>
      <div class="grid grid-cols-2 gap-3">
        <input v-model="draft.title" placeholder="Название" class="border rounded px-2 py-1" />
        <input v-model="draft.slug" placeholder="slug (a-z0-9-)" class="border rounded px-2 py-1" />
        <input v-model="draft.iconName" placeholder="иконка (напр. home)" class="border rounded px-2 py-1" />
        <input v-model="draft.tag" placeholder="тег для статей" class="border rounded px-2 py-1" />
        <input v-model.number="draft.sortOrder" type="number" placeholder="Порядок" class="border rounded px-2 py-1" />
        <label class="flex items-center gap-2"><input v-model="draft.isPublished" type="checkbox" /> Опубликовано</label>
      </div>
      <input v-model="draft.shortDescription" placeholder="Короткое описание" class="border rounded px-2 py-1 w-full" />
      <ArticleEditor v-model="draft.content" />
      <div class="flex gap-3">
        <button class="bg-gray-900 text-white py-2 px-4 rounded" @click="save">Сохранить</button>
        <button v-if="editingId" class="py-2 px-4 rounded border" @click="reset">Отмена</button>
      </div>
    </div>
  </div>
</template>
```
> Если у `ArticleEditor` другой проп/событие, чем `v-model` — посмотреть его сигнатуру и подставить (он уже использует `modelValue` + watch, см. проектную заметку).

- [ ] **Step 2: Ссылка в навигации админки**

Если в `pages/admin/index.vue` или `layouts/admin.vue` есть список ссылок — добавить `{ to:'/admin/services', label:'Услуги' }`.

- [ ] **Step 3: Чекпойнт**

Run: `cd frontend && npx nuxi typecheck`
Expected: чисто.

---

### Task D3: Админ-страница цен под новые поля

**Files:**
- Modify: `frontend/pages/admin/prices.vue`

- [ ] **Step 1: Переписать форму под пример работы**

`pages/admin/prices.vue`:
```vue
<script setup lang="ts">
import type { ServicePrice } from '~/types/api'
import type { ServicePriceWrite } from '~/types/admin'
import type { MediaUploadResponse } from '~/types/admin'
definePageMeta({ layout: 'admin', middleware: 'auth' })
const api = useAdminApi()
const items = ref<ServicePrice[]>([])
const editingId = ref<number | null>(null)
const blank = (): ServicePriceWrite => ({ title:'', photoPath:null, description:'', price:0, duration:'', articleSlug:'', tag:'', sortOrder:0 })
const draft = reactive<ServicePriceWrite>(blank())

async function load() { items.value = await api.listPrices() }
onMounted(load)

function edit(p: ServicePrice) { editingId.value = p.id; Object.assign(draft, p) }
function reset() { editingId.value = null; Object.assign(draft, blank()) }
function onPhoto(m: MediaUploadResponse) { draft.photoPath = m.thumbnailUrl ?? m.url }

async function save() {
  if (editingId.value) await api.updatePrice(editingId.value, { ...draft })
  else await api.createPrice({ ...draft })
  reset(); await load()
}
async function remove(id: number) {
  if (!confirm('Удалить пример?')) return
  await api.deletePrice(id); await load()
}
</script>

<template>
  <div class="max-w-4xl">
    <h1 class="text-2xl font-bold mb-6">Примеры работ и цен</h1>
    <table class="w-full bg-white rounded shadow mb-6">
      <thead><tr class="text-left border-b"><th class="p-2">Порядок</th><th class="p-2">Название</th><th class="p-2">Цена</th><th class="p-2">Срок</th><th></th></tr></thead>
      <tbody>
        <tr v-for="p in items" :key="p.id" class="border-b">
          <td class="p-2">{{ p.sortOrder }}</td><td class="p-2">{{ p.title }}</td>
          <td class="p-2">{{ p.price }}</td><td class="p-2">{{ p.duration }}</td>
          <td class="p-2 text-right space-x-3">
            <button class="text-blue-600" @click="edit(p)">Изм.</button>
            <button class="text-red-600" @click="remove(p.id)">Удалить</button>
          </td>
        </tr>
      </tbody>
    </table>

    <div class="bg-white p-4 rounded shadow space-y-3">
      <h2 class="font-semibold">{{ editingId ? 'Редактировать' : 'Добавить' }} пример</h2>
      <input v-model="draft.title" placeholder="Название работы" class="border rounded px-2 py-1 w-full" />
      <input v-model="draft.description" placeholder="Короткое описание" class="border rounded px-2 py-1 w-full" />
      <div class="grid grid-cols-3 gap-3">
        <input v-model.number="draft.price" type="number" placeholder="Цена, ₽" class="border rounded px-2 py-1" />
        <input v-model="draft.duration" placeholder="Срок (напр. 2 дня)" class="border rounded px-2 py-1" />
        <input v-model.number="draft.sortOrder" type="number" placeholder="Порядок" class="border rounded px-2 py-1" />
        <input v-model="draft.articleSlug" placeholder="slug статьи (опц.)" class="border rounded px-2 py-1" />
        <input v-model="draft.tag" placeholder="тег (опц.)" class="border rounded px-2 py-1" />
      </div>
      <div class="flex items-center gap-3">
        <img v-if="draft.photoPath" :src="useMediaUrl(draft.photoPath)" alt="" class="h-16 w-16 rounded object-cover" />
        <MediaUploader label="Загрузить фото" @uploaded="onPhoto" />
        <button v-if="draft.photoPath" class="text-sm text-red-600" @click="draft.photoPath = null">Убрать</button>
      </div>
      <div class="flex gap-3">
        <button class="bg-gray-900 text-white py-2 px-4 rounded" @click="save">Сохранить</button>
        <button v-if="editingId" class="py-2 px-4 rounded border" @click="reset">Отмена</button>
      </div>
    </div>
  </div>
</template>
```
> Проверить фактический API `MediaUploader` (событие `uploaded` с `MediaUploadResponse` и проп `label` — согласно проектной заметке) и `useMediaUrl` (строковый аргумент). Подставить как в `admin/articles/new.vue`.

- [ ] **Step 2: Чекпойнт**

Run: `cd frontend && npx nuxi typecheck`
Expected: чисто.

---

### Task D4: Поле тегов в редакторе статей

**Files:**
- Modify: `frontend/pages/admin/articles/new.vue`, `frontend/pages/admin/articles/[id].vue`

- [ ] **Step 1: Добавить поле tags в форму**

В обеих страницах: в реактивную форму статьи добавить `tags: ''` (для `[id].vue` — заполнять из загруженной статьи `form.tags = data.tags ?? ''`). В шаблон рядом с прочими полями добавить:
```vue
    <input v-model="form.tags" placeholder="Теги через запятую (напр. foundation,remont)" class="border rounded px-2 py-1 w-full" />
```
Убедиться, что `tags` уходит в `createArticle`/`updateArticle` (тип `ArticleWrite` уже содержит `tags`).

- [ ] **Step 2: Чекпойнт**

Run: `cd frontend && npx nuxi typecheck && npm run test`
Expected: чисто/зелёное.

---

# Фаза E — Иконки, картинки, промпты

### Task E1: Папка картинок + плейсхолдер

**Files:**
- Create: `frontend/public/images/placeholder.svg`
- Create: `frontend/public/images/README.md`

- [ ] **Step 1: Плейсхолдер**

`frontend/public/images/placeholder.svg`:
```svg
<svg xmlns="http://www.w3.org/2000/svg" width="800" height="600" viewBox="0 0 800 600">
  <rect width="800" height="600" fill="#e2e8f0"/>
  <text x="50%" y="50%" font-family="sans-serif" font-size="28" fill="#64748b" text-anchor="middle" dominant-baseline="middle">Суровая Стройка</text>
</svg>
```

- [ ] **Step 2: README-подсказка (механизм замены)**

`frontend/public/images/README.md`:
```markdown
# Картинки сайта

Все статичные картинки сайта лежат здесь и доступны по `/images/<имя>`.
Чтобы заменить — положи файл с тем же именем. Размеры/пропорции компоненты
держат сами (`aspect-ratio` + `object-fit: cover`), поэтому обработка не нужна —
любая исходная картинка впишется в слот.

Рекомендованные пропорции и промпты для генерации — в `docs/image-prompts.md`.

Слоты:
- `placeholder.svg` — заглушка для примеров работ без фото
- (опционально позже) `hero.jpg`, `service-stroitelstvo.jpg`, ... — см. image-prompts.md
```

- [ ] **Step 3: Чекпойнт**

Run: `cd frontend && ls public/images`
Expected: `placeholder.svg`, `README.md`.

---

### Task E2: Лайн-иконки (Icon.vue)

**Files:**
- Create: `frontend/components/ui/Icon.vue`
- Modify: `frontend/package.json` (зависимость иконок)

- [ ] **Step 1: Поставить пакет иконок**

Run: `cd frontend && npm install lucide-vue-next`
Expected: пакет добавлен в dependencies (затронет package-lock — юзер коммитит сам).

- [ ] **Step 2: Универсальный компонент Icon**

`components/ui/Icon.vue` (маппинг строкового имени → компонент lucide):
```vue
<script setup lang="ts">
import { Home, Hammer, Layers, Move, Key, Truck, Handshake, BadgeRussianRuble, Wrench } from 'lucide-vue-next'
const props = defineProps<{ name: string }>()
const map: Record<string, any> = {
  home: Home, hammer: Hammer, layers: Layers, move: Move, key: Key,
  truck: Truck, handshake: Handshake, 'badge-ruble': BadgeRussianRuble, wrench: Wrench,
}
const cmp = computed(() => map[props.name] ?? Wrench)
</script>

<template>
  <component :is="cmp" />
</template>
```
> Имена компонентов сверить с экспортами установленной версии `lucide-vue-next` (напр. `BadgeRussianRuble` может называться иначе — заменить на доступный, напр. `Banknote`/`Wallet`). Иконки в сидере (`IconName`) использовать из ключей `map`.

- [ ] **Step 3: Вернуть `<Icon>` в секции (если в C5/C6 ставили эмодзи-заглушку)**

Убедиться, что `SectionAbout.vue`, `ServiceCard.vue` используют `<Icon :name="..." />`.

- [ ] **Step 4: Чекпойнт**

Run: `cd frontend && npx nuxi typecheck && npm run test`
Expected: чисто/зелёное.

---

### Task E3: Документ с промптами

**Files:**
- Create: `docs/image-prompts.md`

- [ ] **Step 1: Промпты по слотам**

`docs/image-prompts.md`:
```markdown
# Промпты для картинок сайта «Суровая Стройка»

Стиль общий: реалистичное фото, дневной свет, Тульская область/средняя полоса России,
без людей крупным планом, без текста на картинке, приглушённая холодная палитра
(сланцево-серый, тёплый акцент оранжевого инструмента). Формат — см. слот.

Куда класть: `frontend/public/images/<имя>`. Замена = тот же файл, компонент впишет сам.

## hero.jpg (16:9, ~1920×1080)
"Wide cinematic photo of a rural construction site in central Russia, wooden frame house
under construction on screw piles, overcast cool daylight, slate-grey tones, no people,
no text, realistic, professional."

## service-stroitelstvo.jpg (4:3, ~1200×900)
"Newly built wooden private house with a gazebo in a Russian countryside yard, ..."

## service-remont-pristroyki.jpg (4:3)
"Wooden veranda extension being attached to a dacha house, fresh framing, ..."

## service-zamena-fundamenta.jpg (4:3)
"Close-up of screw piles and steel channel beams supporting a small dacha house, soil ground, ..."

## service-peredvizhka.jpg (4:3)
"Small utility cabin (bytovka) being lifted with jacks for relocation, ..."

## service-pod-klyuch.jpg (4:3)
"Construction materials (sandwich panels, timber) delivered and stacked at a building site, ..."

## placeholder.svg — уже сделан (заглушка)
```
(каждый промпт дополнить деталями по аналогии с hero.)

- [ ] **Step 2: Чекпойнт**

Run: `ls docs/image-prompts.md`
Expected: файл существует.

---

# Фаза Z — Сквозная проверка

### Task Z1: Сборка, тесты, браузер

- [ ] **Step 1: Бэкенд — полный прогон**

Run: `cd backend && taskkill //IM Api.exe //F 2>/dev/null; dotnet test`
Expected: все Unit + Integration зелёные.

- [ ] **Step 2: Фронт — тесты и типы**

Run: `cd frontend && npm run test && npx nuxi typecheck`
Expected: зелёное, 0 ошибок типов.

- [ ] **Step 3: Запуск и визуальная проверка**

Поднять бэк (`cd backend/src/Api && ASPNETCORE_ENVIRONMENT=Development dotnet run --urls http://localhost:8081`) и фронт (`cd frontend && npm run dev -- --host 127.0.0.1`). В браузере проверить:
  - Шапка: бренд «Суровая Стройка», меню (Главная/Услуги/Из практики/Примеры работ и цен/Контакты), переключатель темы работает без вспышки, выбор сохраняется после F5.
  - Светлая тема — фон мягкий (не чисто-белый); тёмная — графит, текст читаемый, акцент оранжевый.
  - Услуги: сетка из 5 карточек с лайн-иконками → детальная открывается, статьи «по теме» подтягиваются по тегу.
  - Примеры работ и цен: карточки с фото/плейсхолдером, ценой, сроком, ссылкой на статью.
  - Футер: «г. Тула и Тульская область», без часов работы.
  - Нет фейк-статистики (10+ лет/200/5 лет) и обещаний «гарантия 5 лет».
  - Админка: услуги CRUD, цены под новые поля, поле тегов в статье.
Expected: всё по списку. Баги — чинить через superpowers:systematic-debugging.

> Напоминание: для ручной проверки загрузки фото юзер открывает СВОЙ браузер (не Playwright — он глотает нативные file-диалоги).

---

## Self-Review (выполнено при написании плана)

- **Покрытие спеки:** §1 бренд → C1/C4; §2 «Как работаем» → C5; §3 IA/навигация → C2/C6/C7/C8; §4.1 Service → A2/A3/A4; §4.2 теги → A1; §4.3 ServicePrice → A5; §4.4 сиды → A6; §5 тема → B1/B2/B3; §6 иконки/картинки → E1/E2/E3; §7 «гибкий механизм» → всё через админку + README-подсказки. ✅
- **Конфликт `/api/services/prices`:** решён переносом цен на `/api/prices` (A4) + правкой `lib/api.ts` (C6) и `adminApi.ts` (D1). ✅
- **Типы консистентны:** `ServicePrice` (новые поля) согласован между бэк-DTO (A5), `types/api.ts` (C6), `types/admin.ts` (D1), компонентами (C7), админкой (D3). `Service`/`ServiceListItem`/`ServiceDetail` согласованы A3↔C6. `tags` сквозной A1↔C6↔D1↔D4. ✅
- **Плейсхолдеры:** тестовые хелперы (`TestDb.NewInMemory`, `PassthroughSanitizer`, `ApiFactory`) помечены как «подставить фактические из соседних тестов» — это не дыры, а привязка к существующему коду, который исполнитель видит. lucide-имена помечены «сверить с версией пакета». ✅

---

## Execution Handoff

**Plan complete and saved to `docs/superpowers/plans/2026-05-30-content-revision-surovaya-stroyka.md`. Two execution options:**

**1. Subagent-Driven (recommended)** — свежий субагент на задачу, ревью между задачами, быстрая итерация.
> ⚠️ Проектная заметка: на этом репо субагенты глючили с релеем результата (писали «отклонено», хотя файлы создавались, иногда без нужных пакетов). Если повторится — переходим на inline.

**2. Inline Execution** — выполняю задачи в этой сессии (executing-plans), батчами с чекпойнтами.

**Какой подход выбираешь?**
