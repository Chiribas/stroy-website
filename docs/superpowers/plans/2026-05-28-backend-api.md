# Backend API Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Завершить backend публичного API сайта-визитки: модель данных с медиа, инициализация БД, доменные сервисы с пагинацией, санитайзинг HTML-контента, тонкие контроллеры и тесты.

**Architecture:** ASP.NET Core Web API (.NET 10), слои Core/Infrastructure/Api. Доступ к данным — тонкие доменные сервисы поверх `AppDbContext` (без generic-репозитория), фильтрация/пагинация в SQL. HTML-контент статей санитайзится по whitelist (теги + домены iframe). Контроллеры тонкие.

**Tech Stack:** .NET 10, ASP.NET Core, EF Core 10 (SQLite), HtmlSanitizer (Ganss.Xss), xUnit, WebApplicationFactory + EF InMemory.

**Состояние на старте (уже закоммичено):** solution `backend/backend.slnx`; проекты Api/Core/Infrastructure/tests; сущности `Article`, `ServicePrice`, `Callback`, `Contact`; `AppDbContext` с конфигурацией 4 таблиц; `Program.cs` с DbContext + контроллеры + Swagger. Пакеты EF Core Sqlite/Design 10.0.8 в Infrastructure.

**Тесты:** используем встроенные `Assert` из xUnit (без FluentAssertions — избегаем лицензионных нюансов v8).

---

## Файловая структура (создаётся/меняется этим планом)

```
backend/
├── src/
│   ├── Core/
│   │   ├── Entities/Article.cs            (modify: навигация на медиа)
│   │   ├── Entities/ArticleMedia.cs       (create)
│   │   ├── DTOs/PagedResult.cs            (create)
│   │   ├── DTOs/ArticleDto.cs             (create)
│   │   ├── DTOs/ArticleListItemDto.cs     (create)
│   │   ├── DTOs/CreateArticleDto.cs       (create)
│   │   ├── DTOs/UpdateArticleDto.cs       (create)
│   │   ├── DTOs/ServicePriceDto.cs        (create)
│   │   ├── DTOs/CallbackRequest.cs        (create)
│   │   ├── DTOs/ContactRequest.cs         (create)
│   │   └── Interfaces/
│   │       ├── IHtmlSanitizerService.cs   (create)
│   │       ├── IArticleService.cs         (create)
│   │       ├── IServicePriceService.cs    (create)
│   │       ├── ICallbackService.cs        (create)
│   │       └── IContactService.cs         (create)
│   ├── Infrastructure/
│   │   ├── Data/AppDbContext.cs           (modify: ArticleMedia)
│   │   ├── Migrations/                    (create via ef)
│   │   └── Services/
│   │       ├── HtmlSanitizerService.cs    (create)
│   │       ├── ArticleService.cs          (create)
│   │       ├── ServicePriceService.cs     (create)
│   │       ├── CallbackService.cs         (create)
│   │       └── ContactService.cs          (create)
│   └── Api/
│       ├── Program.cs                     (modify: DI + migrate + partial)
│       └── Controllers/
│           ├── ArticlesController.cs      (create)
│           ├── ServicesController.cs      (create)
│           ├── CallbacksController.cs     (create)
│           └── ContactsController.cs      (create)
└── tests/
    ├── Unit/
    │   ├── HtmlSanitizerServiceTests.cs   (create)
    │   └── ArticleServiceTests.cs         (create)
    └── Integration/
        ├── ApiTestFactory.cs              (create)
        ├── ArticlesEndpointTests.cs       (create)
        └── FormsEndpointTests.cs          (create)
```

---

## Phase 1: Data model & DB init

### Task 1: ArticleMedia entity + Article navigation

**Files:**
- Create: `backend/src/Core/Entities/ArticleMedia.cs`
- Modify: `backend/src/Core/Entities/Article.cs`
- Modify: `backend/src/Infrastructure/Data/AppDbContext.cs`

- [ ] **Step 1: Create ArticleMedia entity**

`backend/src/Core/Entities/ArticleMedia.cs`:
```csharp
namespace Core.Entities;

public class ArticleMedia
{
    public int Id { get; set; }
    public int ArticleId { get; set; }
    public Article Article { get; set; } = null!;
    public string Path { get; set; } = string.Empty;
    public string MediaType { get; set; } = "image";
    public string? Alt { get; set; }
    public int SortOrder { get; set; }
}
```

- [ ] **Step 2: Add navigation to Article**

Modify `backend/src/Core/Entities/Article.cs` — add collection property after `CreatedAt`:
```csharp
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<ArticleMedia> Media { get; set; } = new();
```

- [ ] **Step 3: Configure ArticleMedia in AppDbContext**

Modify `backend/src/Infrastructure/Data/AppDbContext.cs`:

Add DbSet after `Contacts`:
```csharp
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<ArticleMedia> ArticleMedia => Set<ArticleMedia>();
```

Add configuration inside `OnModelCreating`, after the `Article` block:
```csharp
        modelBuilder.Entity<ArticleMedia>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Path).IsRequired();
            entity.Property(e => e.MediaType).IsRequired();
            entity.HasOne(e => e.Article)
                .WithMany(a => a.Media)
                .HasForeignKey(e => e.ArticleId)
                .OnDelete(DeleteBehavior.Cascade);
        });
```

- [ ] **Step 4: Build to verify it compiles**

Run: `dotnet build backend/backend.slnx`
Expected: Build succeeded, 0 errors.

- [ ] **Step 5: Commit**

```bash
git add backend/src/Core/Entities/ArticleMedia.cs backend/src/Core/Entities/Article.cs backend/src/Infrastructure/Data/AppDbContext.cs
git commit -m "feat: add ArticleMedia entity and Article navigation"
```

### Task 2: EF migration + apply on startup

**Files:**
- Create: `backend/src/Infrastructure/Migrations/` (generated)
- Modify: `backend/src/Api/Program.cs`

- [ ] **Step 1: Install EF CLI tool (idempotent)**

Run:
```bash
dotnet tool install --global dotnet-ef --version 10.* || dotnet tool update --global dotnet-ef --version 10.*
```
Expected: tool installed/updated (if already present, update succeeds).

- [ ] **Step 2: Create initial migration**

Run from `backend/`:
```bash
dotnet ef migrations add InitialCreate -p src/Infrastructure -s src/Api -o Migrations
```
Expected: migration files generated under `backend/src/Infrastructure/Migrations/`.

- [ ] **Step 3: Apply migration on startup**

Modify `backend/src/Api/Program.cs` — after `var app = builder.Build();` add:
```csharp
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}
```

- [ ] **Step 4: Run API and verify DB created**

Run from `backend/src/Api`: `dotnet run` (let it start, then Ctrl+C)
Expected: starts without errors; `database.db` file created in the run directory.

- [ ] **Step 5: Commit**

```bash
git add backend/src/Infrastructure/Migrations/ backend/src/Api/Program.cs
git commit -m "feat: add initial EF migration and apply on startup"
```

---

## Phase 2: DTOs

### Task 3: Create DTOs

**Files:**
- Create: `backend/src/Core/DTOs/PagedResult.cs`
- Create: `backend/src/Core/DTOs/ArticleDto.cs`
- Create: `backend/src/Core/DTOs/ArticleListItemDto.cs`
- Create: `backend/src/Core/DTOs/CreateArticleDto.cs`
- Create: `backend/src/Core/DTOs/UpdateArticleDto.cs`
- Create: `backend/src/Core/DTOs/ServicePriceDto.cs`
- Create: `backend/src/Core/DTOs/CallbackRequest.cs`
- Create: `backend/src/Core/DTOs/ContactRequest.cs`

- [ ] **Step 1: PagedResult**

`backend/src/Core/DTOs/PagedResult.cs`:
```csharp
namespace Core.DTOs;

public record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
```

- [ ] **Step 2: ArticleDto + ArticleListItemDto**

`backend/src/Core/DTOs/ArticleDto.cs`:
```csharp
namespace Core.DTOs;

public record ArticleMediaDto(int Id, string Path, string MediaType, string? Alt, int SortOrder);

public record ArticleDto(
    int Id,
    string Title,
    string Slug,
    string? Summary,
    string Content,
    string? ThumbnailPath,
    DateTime PublishedAt,
    IReadOnlyList<ArticleMediaDto> Media
);
```

`backend/src/Core/DTOs/ArticleListItemDto.cs`:
```csharp
namespace Core.DTOs;

public record ArticleListItemDto(
    int Id,
    string Title,
    string Slug,
    string? Summary,
    string? ThumbnailPath,
    DateTime PublishedAt
);
```

- [ ] **Step 3: CreateArticleDto + UpdateArticleDto**

`backend/src/Core/DTOs/CreateArticleDto.cs`:
```csharp
using System.ComponentModel.DataAnnotations;

namespace Core.DTOs;

public class CreateArticleDto
{
    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^[a-z0-9-]+$", ErrorMessage = "Slug must contain only lowercase letters, numbers and hyphens")]
    public string Slug { get; set; } = string.Empty;

    public string? Summary { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;

    public string? ThumbnailPath { get; set; }
    public bool IsPublished { get; set; } = false;
}
```

`backend/src/Core/DTOs/UpdateArticleDto.cs`:
```csharp
using System.ComponentModel.DataAnnotations;

namespace Core.DTOs;

public class UpdateArticleDto
{
    [Required]
    public string Title { get; set; } = string.Empty;

    [RegularExpression(@"^[a-z0-9-]+$", ErrorMessage = "Slug must contain only lowercase letters, numbers and hyphens")]
    public string? Slug { get; set; }

    public string? Summary { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;

    public string? ThumbnailPath { get; set; }
    public bool IsPublished { get; set; }
}
```

- [ ] **Step 4: ServicePriceDto**

`backend/src/Core/DTOs/ServicePriceDto.cs`:
```csharp
namespace Core.DTOs;

public record ServicePriceDto(
    int Id,
    string Category,
    string Name,
    string? Description,
    int PriceFrom,
    int? PriceTo,
    string? Unit,
    int SortOrder
);
```

- [ ] **Step 5: CallbackRequest + ContactRequest**

`backend/src/Core/DTOs/CallbackRequest.cs`:
```csharp
using System.ComponentModel.DataAnnotations;

namespace Core.DTOs;

public class CallbackRequest
{
    [Required(ErrorMessage = "Phone is required")]
    [Phone(ErrorMessage = "Invalid phone format")]
    public string Phone { get; set; } = string.Empty;

    public string? Name { get; set; }
}
```

`backend/src/Core/DTOs/ContactRequest.cs`:
```csharp
using System.ComponentModel.DataAnnotations;

namespace Core.DTOs;

public class ContactRequest
{
    [Required(ErrorMessage = "Name is required")]
    [MinLength(2, ErrorMessage = "Name is too short")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone is required")]
    [Phone(ErrorMessage = "Invalid phone format")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Message is required")]
    [MinLength(10, ErrorMessage = "Message is too short")]
    public string Message { get; set; } = string.Empty;
}
```

- [ ] **Step 6: Build**

Run: `dotnet build backend/backend.slnx`
Expected: Build succeeded.

- [ ] **Step 7: Commit**

```bash
git add backend/src/Core/DTOs/
git commit -m "feat: add DTOs with paging and article media"
```

---

## Phase 3: HTML sanitizer (TDD)

### Task 4: HtmlSanitizerService

**Files:**
- Create: `backend/src/Core/Interfaces/IHtmlSanitizerService.cs`
- Create: `backend/src/Infrastructure/Services/HtmlSanitizerService.cs`
- Test: `backend/tests/Unit/HtmlSanitizerServiceTests.cs`
- Modify: `backend/src/Infrastructure/Infrastructure.csproj`

- [ ] **Step 1: Add HtmlSanitizer package**

Run from `backend/src/Infrastructure`:
```bash
dotnet add package HtmlSanitizer
```
Expected: package added to `Infrastructure.csproj`.

- [ ] **Step 2: Create interface**

`backend/src/Core/Interfaces/IHtmlSanitizerService.cs`:
```csharp
namespace Core.Interfaces;

public interface IHtmlSanitizerService
{
    string Sanitize(string html);
}
```

- [ ] **Step 3: Add Core reference to Unit test project + write failing test**

Run from `backend/tests/Unit`:
```bash
dotnet add reference ../../src/Infrastructure/Infrastructure.csproj
```

`backend/tests/Unit/HtmlSanitizerServiceTests.cs`:
```csharp
using Xunit;
using Core.Interfaces;
using Infrastructure.Services;

namespace Unit;

public class HtmlSanitizerServiceTests
{
    private readonly IHtmlSanitizerService _sut = new HtmlSanitizerService();

    [Fact]
    public void Sanitize_RemovesScriptTags()
    {
        var result = _sut.Sanitize("<p>hi</p><script>alert('x')</script>");
        Assert.DoesNotContain("script", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<p>hi</p>", result);
    }

    [Fact]
    public void Sanitize_KeepsBasicFormatting()
    {
        var result = _sut.Sanitize("<p><strong>bold</strong> and <em>italic</em></p>");
        Assert.Contains("<strong>bold</strong>", result);
        Assert.Contains("<em>italic</em>", result);
    }

    [Fact]
    public void Sanitize_KeepsIframeFromTrustedDomain()
    {
        var html = "<iframe src=\"https://www.youtube.com/embed/abc123\"></iframe>";
        var result = _sut.Sanitize(html);
        Assert.Contains("youtube.com/embed/abc123", result);
    }

    [Fact]
    public void Sanitize_RemovesIframeFromUntrustedDomain()
    {
        var html = "<iframe src=\"https://evil.example.com/x\"></iframe>";
        var result = _sut.Sanitize(html);
        Assert.DoesNotContain("evil.example.com", result);
    }
}
```

- [ ] **Step 4: Run tests to verify they fail**

Run: `dotnet test backend/tests/Unit --filter HtmlSanitizerServiceTests`
Expected: FAIL — `HtmlSanitizerService` does not exist.

- [ ] **Step 5: Implement HtmlSanitizerService**

`backend/src/Infrastructure/Services/HtmlSanitizerService.cs`:
```csharp
using Ganss.Xss;
using Core.Interfaces;

namespace Infrastructure.Services;

public class HtmlSanitizerService : IHtmlSanitizerService
{
    private static readonly string[] AllowedIframeHosts =
    {
        "www.youtube.com", "youtube.com",
        "rutube.ru", "vk.com", "vkvideo.ru"
    };

    private readonly HtmlSanitizer _sanitizer;

    public HtmlSanitizerService()
    {
        _sanitizer = new HtmlSanitizer();
        _sanitizer.AllowedTags.Clear();
        foreach (var tag in new[]
        {
            "p", "br", "strong", "em", "u", "h2", "h3",
            "ul", "ol", "li", "a", "img", "iframe",
            "blockquote", "figure", "figcaption"
        })
        {
            _sanitizer.AllowedTags.Add(tag);
        }

        _sanitizer.AllowedAttributes.Clear();
        foreach (var attr in new[]
        {
            "href", "src", "alt", "class", "title",
            "allowfullscreen", "frameborder", "width", "height"
        })
        {
            _sanitizer.AllowedAttributes.Add(attr);
        }

        _sanitizer.AllowedSchemes.Add("https");

        _sanitizer.PostProcessNode += (sender, e) =>
        {
            if (e.Node is AngleSharp.Html.Dom.IHtmlInlineFrameElement iframe)
            {
                var src = iframe.GetAttribute("src");
                if (!IsTrustedIframe(src))
                    iframe.Remove();
            }
        };
    }

    public string Sanitize(string html) => _sanitizer.Sanitize(html ?? string.Empty);

    private static bool IsTrustedIframe(string? src)
    {
        if (string.IsNullOrWhiteSpace(src)) return false;
        if (!Uri.TryCreate(src, UriKind.Absolute, out var uri)) return false;
        if (uri.Scheme != Uri.UriSchemeHttps) return false;
        return AllowedIframeHosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase);
    }
}
```

- [ ] **Step 6: Run tests to verify they pass**

Run: `dotnet test backend/tests/Unit --filter HtmlSanitizerServiceTests`
Expected: PASS (4 passed).

- [ ] **Step 7: Commit**

```bash
git add backend/src/Core/Interfaces/IHtmlSanitizerService.cs backend/src/Infrastructure/Services/HtmlSanitizerService.cs backend/tests/Unit/HtmlSanitizerServiceTests.cs backend/src/Infrastructure/Infrastructure.csproj backend/tests/Unit/Unit.csproj
git commit -m "feat: add HTML sanitizer with iframe domain whitelist"
```

---

## Phase 4: Domain services (TDD)

### Task 5: IArticleService + ArticleService

**Files:**
- Create: `backend/src/Core/Interfaces/IArticleService.cs`
- Create: `backend/src/Infrastructure/Services/ArticleService.cs`
- Test: `backend/tests/Unit/ArticleServiceTests.cs`

- [ ] **Step 1: Create interface**

`backend/src/Core/Interfaces/IArticleService.cs`:
```csharp
using Core.DTOs;

namespace Core.Interfaces;

public interface IArticleService
{
    Task<PagedResult<ArticleListItemDto>> GetPublishedAsync(int page, int pageSize);
    Task<ArticleDto?> GetBySlugAsync(string slug);
    Task<ArticleDto> CreateAsync(CreateArticleDto dto);
    Task<ArticleDto?> UpdateAsync(int id, UpdateArticleDto dto);
    Task<bool> DeleteAsync(int id);
}
```

- [ ] **Step 2: Add EF InMemory to Unit tests + write failing tests**

Run from `backend/tests/Unit`:
```bash
dotnet add package Microsoft.EntityFrameworkCore.InMemory
```

`backend/tests/Unit/ArticleServiceTests.cs`:
```csharp
using Xunit;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;
using Infrastructure.Services;
using Core.DTOs;
using Core.Interfaces;

namespace Unit;

public class ArticleServiceTests
{
    private static AppDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static IArticleService NewSut(AppDbContext db) =>
        new ArticleService(db, new HtmlSanitizerService());

    [Fact]
    public async Task GetPublishedAsync_ReturnsOnlyPublished_Paged()
    {
        using var db = NewDb();
        var sut = NewSut(db);
        await sut.CreateAsync(new CreateArticleDto { Title = "A", Slug = "a", Content = "<p>a</p>", IsPublished = true });
        await sut.CreateAsync(new CreateArticleDto { Title = "B", Slug = "b", Content = "<p>b</p>", IsPublished = false });

        var result = await sut.GetPublishedAsync(page: 1, pageSize: 10);

        Assert.Equal(1, result.Total);
        Assert.Single(result.Items);
        Assert.Equal("a", result.Items[0].Slug);
    }

    [Fact]
    public async Task CreateAsync_SanitizesContent()
    {
        using var db = NewDb();
        var sut = NewSut(db);

        var created = await sut.CreateAsync(new CreateArticleDto
        {
            Title = "X", Slug = "x",
            Content = "<p>ok</p><script>alert(1)</script>",
            IsPublished = true
        });

        Assert.DoesNotContain("script", created.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetBySlugAsync_ReturnsNull_ForUnpublished()
    {
        using var db = NewDb();
        var sut = NewSut(db);
        await sut.CreateAsync(new CreateArticleDto { Title = "D", Slug = "d", Content = "<p>d</p>", IsPublished = false });

        var result = await sut.GetBySlugAsync("d");

        Assert.Null(result);
    }
}
```

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test backend/tests/Unit --filter ArticleServiceTests`
Expected: FAIL — `ArticleService` does not exist.

- [ ] **Step 4: Implement ArticleService**

`backend/src/Infrastructure/Services/ArticleService.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Core.DTOs;
using Core.Entities;
using Core.Interfaces;
using Infrastructure.Data;

namespace Infrastructure.Services;

public class ArticleService : IArticleService
{
    private readonly AppDbContext _db;
    private readonly IHtmlSanitizerService _sanitizer;

    public ArticleService(AppDbContext db, IHtmlSanitizerService sanitizer)
    {
        _db = db;
        _sanitizer = sanitizer;
    }

    public async Task<PagedResult<ArticleListItemDto>> GetPublishedAsync(int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 12;

        var query = _db.Articles.Where(a => a.IsPublished);
        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(a => a.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new ArticleListItemDto(
                a.Id, a.Title, a.Slug, a.Summary, a.ThumbnailPath, a.PublishedAt))
            .ToListAsync();

        return new PagedResult<ArticleListItemDto>(items, total, page, pageSize);
    }

    public async Task<ArticleDto?> GetBySlugAsync(string slug)
    {
        var article = await _db.Articles
            .Include(a => a.Media)
            .FirstOrDefaultAsync(a => a.Slug == slug && a.IsPublished);

        return article is null ? null : ToDto(article);
    }

    public async Task<ArticleDto> CreateAsync(CreateArticleDto dto)
    {
        var article = new Article
        {
            Title = dto.Title,
            Slug = dto.Slug,
            Summary = dto.Summary,
            Content = _sanitizer.Sanitize(dto.Content),
            ThumbnailPath = dto.ThumbnailPath,
            IsPublished = dto.IsPublished,
            PublishedAt = dto.IsPublished ? DateTime.UtcNow : default
        };

        _db.Articles.Add(article);
        await _db.SaveChangesAsync();
        return ToDto(article);
    }

    public async Task<ArticleDto?> UpdateAsync(int id, UpdateArticleDto dto)
    {
        var article = await _db.Articles.Include(a => a.Media).FirstOrDefaultAsync(a => a.Id == id);
        if (article is null) return null;

        article.Title = dto.Title;
        if (!string.IsNullOrEmpty(dto.Slug)) article.Slug = dto.Slug;
        article.Summary = dto.Summary;
        article.Content = _sanitizer.Sanitize(dto.Content);
        article.ThumbnailPath = dto.ThumbnailPath;
        article.IsPublished = dto.IsPublished;
        if (dto.IsPublished && article.PublishedAt == default)
            article.PublishedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return ToDto(article);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var article = await _db.Articles.FindAsync(id);
        if (article is null) return false;
        _db.Articles.Remove(article);
        await _db.SaveChangesAsync();
        return true;
    }

    private static ArticleDto ToDto(Article a) => new(
        a.Id, a.Title, a.Slug, a.Summary, a.Content, a.ThumbnailPath, a.PublishedAt,
        a.Media.OrderBy(m => m.SortOrder)
            .Select(m => new ArticleMediaDto(m.Id, m.Path, m.MediaType, m.Alt, m.SortOrder))
            .ToList());
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test backend/tests/Unit --filter ArticleServiceTests`
Expected: PASS (3 passed).

- [ ] **Step 6: Commit**

```bash
git add backend/src/Core/Interfaces/IArticleService.cs backend/src/Infrastructure/Services/ArticleService.cs backend/tests/Unit/ArticleServiceTests.cs backend/tests/Unit/Unit.csproj
git commit -m "feat: add ArticleService with paging and sanitizing"
```

### Task 6: ServicePriceService

**Files:**
- Create: `backend/src/Core/Interfaces/IServicePriceService.cs`
- Create: `backend/src/Infrastructure/Services/ServicePriceService.cs`

- [ ] **Step 1: Create interface**

`backend/src/Core/Interfaces/IServicePriceService.cs`:
```csharp
using Core.DTOs;

namespace Core.Interfaces;

public interface IServicePriceService
{
    Task<IReadOnlyList<ServicePriceDto>> GetAllAsync();
}
```

- [ ] **Step 2: Implement service**

`backend/src/Infrastructure/Services/ServicePriceService.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Core.DTOs;
using Core.Interfaces;
using Infrastructure.Data;

namespace Infrastructure.Services;

public class ServicePriceService : IServicePriceService
{
    private readonly AppDbContext _db;

    public ServicePriceService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ServicePriceDto>> GetAllAsync()
    {
        return await _db.ServicePrices
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.PriceFrom)
            .Select(p => new ServicePriceDto(
                p.Id, p.Category, p.Name, p.Description, p.PriceFrom, p.PriceTo, p.Unit, p.SortOrder))
            .ToListAsync();
    }
}
```

- [ ] **Step 3: Build**

Run: `dotnet build backend/backend.slnx`
Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add backend/src/Core/Interfaces/IServicePriceService.cs backend/src/Infrastructure/Services/ServicePriceService.cs
git commit -m "feat: add ServicePriceService"
```

### Task 7: CallbackService + ContactService

**Files:**
- Create: `backend/src/Core/Interfaces/ICallbackService.cs`
- Create: `backend/src/Core/Interfaces/IContactService.cs`
- Create: `backend/src/Infrastructure/Services/CallbackService.cs`
- Create: `backend/src/Infrastructure/Services/ContactService.cs`

- [ ] **Step 1: Create interfaces**

`backend/src/Core/Interfaces/ICallbackService.cs`:
```csharp
using Core.DTOs;

namespace Core.Interfaces;

public interface ICallbackService
{
    Task CreateAsync(CallbackRequest request);
}
```

`backend/src/Core/Interfaces/IContactService.cs`:
```csharp
using Core.DTOs;

namespace Core.Interfaces;

public interface IContactService
{
    Task CreateAsync(ContactRequest request);
}
```

- [ ] **Step 2: Implement CallbackService**

`backend/src/Infrastructure/Services/CallbackService.cs`:
```csharp
using Core.DTOs;
using Core.Entities;
using Core.Interfaces;
using Infrastructure.Data;

namespace Infrastructure.Services;

public class CallbackService : ICallbackService
{
    private readonly AppDbContext _db;

    public CallbackService(AppDbContext db) => _db = db;

    public async Task CreateAsync(CallbackRequest request)
    {
        _db.Callbacks.Add(new Callback { Phone = request.Phone, Name = request.Name });
        await _db.SaveChangesAsync();
    }
}
```

- [ ] **Step 3: Implement ContactService**

`backend/src/Infrastructure/Services/ContactService.cs`:
```csharp
using Core.DTOs;
using Core.Entities;
using Core.Interfaces;
using Infrastructure.Data;

namespace Infrastructure.Services;

public class ContactService : IContactService
{
    private readonly AppDbContext _db;

    public ContactService(AppDbContext db) => _db = db;

    public async Task CreateAsync(ContactRequest request)
    {
        _db.Contacts.Add(new Contact
        {
            Name = request.Name,
            Phone = request.Phone,
            Message = request.Message
        });
        await _db.SaveChangesAsync();
    }
}
```

- [ ] **Step 4: Build**

Run: `dotnet build backend/backend.slnx`
Expected: Build succeeded.

- [ ] **Step 5: Commit**

```bash
git add backend/src/Core/Interfaces/ICallbackService.cs backend/src/Core/Interfaces/IContactService.cs backend/src/Infrastructure/Services/CallbackService.cs backend/src/Infrastructure/Services/ContactService.cs
git commit -m "feat: add Callback and Contact services"
```

### Task 8: Register services in DI

**Files:**
- Modify: `backend/src/Api/Program.cs`

- [ ] **Step 1: Register services**

Modify `backend/src/Api/Program.cs` — after `builder.Services.AddDbContext(...)` block, before `AddControllers`:
```csharp
builder.Services.AddSingleton<Core.Interfaces.IHtmlSanitizerService, Infrastructure.Services.HtmlSanitizerService>();
builder.Services.AddScoped<Core.Interfaces.IArticleService, Infrastructure.Services.ArticleService>();
builder.Services.AddScoped<Core.Interfaces.IServicePriceService, Infrastructure.Services.ServicePriceService>();
builder.Services.AddScoped<Core.Interfaces.ICallbackService, Infrastructure.Services.CallbackService>();
builder.Services.AddScoped<Core.Interfaces.IContactService, Infrastructure.Services.ContactService>();
```

- [ ] **Step 2: Build**

Run: `dotnet build backend/backend.slnx`
Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add backend/src/Api/Program.cs
git commit -m "feat: register domain services in DI"
```

---

## Phase 5: Controllers

### Task 9: ArticlesController

**Files:**
- Create: `backend/src/Api/Controllers/ArticlesController.cs`

- [ ] **Step 1: Create controller**

`backend/src/Api/Controllers/ArticlesController.cs`:
```csharp
using Microsoft.AspNetCore.Mvc;
using Core.DTOs;
using Core.Interfaces;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ArticlesController : ControllerBase
{
    private readonly IArticleService _service;

    public ArticlesController(IArticleService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<PagedResult<ArticleListItemDto>>> Get(int page = 1, int pageSize = 12)
        => Ok(await _service.GetPublishedAsync(page, pageSize));

    [HttpGet("{slug}")]
    public async Task<ActionResult<ArticleDto>> GetBySlug(string slug)
    {
        var article = await _service.GetBySlugAsync(slug);
        return article is null ? NotFound() : Ok(article);
    }
}
```

- [ ] **Step 2: Build**

Run: `dotnet build backend/backend.slnx`
Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add backend/src/Api/Controllers/ArticlesController.cs
git commit -m "feat: add Articles controller (public)"
```

### Task 10: ServicesController

**Files:**
- Create: `backend/src/Api/Controllers/ServicesController.cs`

- [ ] **Step 1: Create controller**

`backend/src/Api/Controllers/ServicesController.cs`:
```csharp
using Microsoft.AspNetCore.Mvc;
using Core.DTOs;
using Core.Interfaces;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServicesController : ControllerBase
{
    private readonly IServicePriceService _service;

    public ServicesController(IServicePriceService service) => _service = service;

    [HttpGet("prices")]
    public async Task<ActionResult<IReadOnlyList<ServicePriceDto>>> GetPrices()
        => Ok(await _service.GetAllAsync());
}
```

- [ ] **Step 2: Build**

Run: `dotnet build backend/backend.slnx`
Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add backend/src/Api/Controllers/ServicesController.cs
git commit -m "feat: add Services controller"
```

### Task 11: CallbacksController + ContactsController

**Files:**
- Create: `backend/src/Api/Controllers/CallbacksController.cs`
- Create: `backend/src/Api/Controllers/ContactsController.cs`

- [ ] **Step 1: Create CallbacksController**

`backend/src/Api/Controllers/CallbacksController.cs`:
```csharp
using Microsoft.AspNetCore.Mvc;
using Core.DTOs;
using Core.Interfaces;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CallbacksController : ControllerBase
{
    private readonly ICallbackService _service;

    public CallbacksController(ICallbackService service) => _service = service;

    [HttpPost]
    public async Task<ActionResult> Create(CallbackRequest request)
    {
        await _service.CreateAsync(request);
        return Ok(new { message = "Спасибо! Мы перезвоним вам в ближайшее время." });
    }
}
```

- [ ] **Step 2: Create ContactsController**

`backend/src/Api/Controllers/ContactsController.cs`:
```csharp
using Microsoft.AspNetCore.Mvc;
using Core.DTOs;
using Core.Interfaces;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContactsController : ControllerBase
{
    private readonly IContactService _service;

    public ContactsController(IContactService service) => _service = service;

    [HttpPost]
    public async Task<ActionResult> Create(ContactRequest request)
    {
        await _service.CreateAsync(request);
        return Ok(new { message = "Спасибо за сообщение! Мы ответим вам в ближайшее время." });
    }
}
```

- [ ] **Step 3: Build**

Run: `dotnet build backend/backend.slnx`
Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add backend/src/Api/Controllers/CallbacksController.cs backend/src/Api/Controllers/ContactsController.cs
git commit -m "feat: add Callbacks and Contacts controllers"
```

---

## Phase 6: Integration tests

### Task 12: Integration test infrastructure + tests

**Files:**
- Modify: `backend/src/Api/Program.cs` (expose Program as partial)
- Modify: `backend/tests/Integration/Integration.csproj`
- Create: `backend/tests/Integration/ApiTestFactory.cs`
- Create: `backend/tests/Integration/ArticlesEndpointTests.cs`
- Create: `backend/tests/Integration/FormsEndpointTests.cs`

- [ ] **Step 1: Expose Program partial class**

Modify `backend/src/Api/Program.cs` — add at the very end of the file:
```csharp
app.Run();

public partial class Program { }
```

- [ ] **Step 2: Add test packages + Api reference**

Run from `backend/tests/Integration`:
```bash
dotnet add reference ../../src/Api/Api.csproj
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package Microsoft.EntityFrameworkCore.InMemory
```

- [ ] **Step 3: Create ApiTestFactory (overrides DB with InMemory)**

`backend/tests/Integration/ApiTestFactory.cs`:
```csharp
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;

namespace Integration;

public class ApiTestFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("IntegrationTestDb"));
        });
    }
}
```

- [ ] **Step 4: Guard startup Migrate() against non-relational provider**

Modify `backend/src/Api/Program.cs` — replace the migrate block from Task 2 with a provider check (InMemory has no migrations):
```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.IsRelational())
        db.Database.Migrate();
}
```

- [ ] **Step 5: Write articles endpoint test**

`backend/tests/Integration/ArticlesEndpointTests.cs`:
```csharp
using Xunit;
using System.Net;
using System.Net.Http.Json;
using Core.DTOs;

namespace Integration;

public class ArticlesEndpointTests : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client;

    public ArticlesEndpointTests(ApiTestFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task GetArticles_WhenEmpty_ReturnsEmptyPagedResult()
    {
        var response = await _client.GetAsync("/api/articles");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var paged = await response.Content.ReadFromJsonAsync<PagedResult<ArticleListItemDto>>();
        Assert.NotNull(paged);
        Assert.Equal(0, paged!.Total);
        Assert.Empty(paged.Items);
    }

    [Fact]
    public async Task GetArticleBySlug_WhenMissing_Returns404()
    {
        var response = await _client.GetAsync("/api/articles/does-not-exist");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
```

- [ ] **Step 6: Write forms endpoint test**

`backend/tests/Integration/FormsEndpointTests.cs`:
```csharp
using Xunit;
using System.Net;
using System.Net.Http.Json;
using Core.DTOs;

namespace Integration;

public class FormsEndpointTests : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client;

    public FormsEndpointTests(ApiTestFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task PostCallback_WithValidPhone_ReturnsOk()
    {
        var response = await _client.PostAsJsonAsync("/api/callbacks",
            new CallbackRequest { Phone = "+79991234567", Name = "Иван" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PostContact_WithMissingMessage_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/contacts",
            new { name = "Иван", phone = "+79991234567" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
```

- [ ] **Step 7: Run all backend tests**

Run: `dotnet test backend/backend.slnx`
Expected: PASS (all unit + integration tests green).

- [ ] **Step 8: Commit**

```bash
git add backend/src/Api/Program.cs backend/tests/Integration/
git commit -m "test: add API integration tests with InMemory DB"
```

---

## Definition of Done

- `dotnet build backend/backend.slnx` — success.
- `dotnet test backend/backend.slnx` — all green.
- `dotnet run` (Api) — стартует, создаёт `database.db`, Swagger доступен в Development.
- Эндпоинты работают: `GET /api/articles?page&pageSize` (paged), `GET /api/articles/{slug}`,
  `GET /api/services/prices`, `POST /api/callbacks`, `POST /api/contacts`.
- HTML статей санитайзится, iframe только с доверенных доменов.

## Out of scope (отдельные планы)

- Админка: JWT-авторизация, admin CRUD-эндпоинты, `POST /api/admin/media/upload`.
- Публичный фронт (Nuxt SSG).
- Админ-фронт (Tiptap WYSIWYG).
- Docker (2 контейнера), nginx, CI/CD, SSG-ребилд.
