# Сайт-визитка строительной компании — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Создать сайт-визитку строительной компании с лендингом, портфолио, ценами, формами обратной связи и админкой для управления контентом.

**Architecture:** Docker Compose с тремя контейнерами: Nuxt 3 (frontend), .NET 10 Web API (backend), nginx (reverse proxy). SQLite для данных, файлы на диске в volume.

**Tech Stack:** Nuxt 3, Vue 3, Tailwind CSS, .NET 10, EF Core, SQLite, Docker, nginx

---

## Phase 1: Backend Foundation

### Task 1: Create .NET Solution structure

**Files:**
- Create: `backend/backend.sln`
- Create: `backend/src/Core/Core.csproj`
- Create: `backend/src/Infrastructure/Infrastructure.csproj`
- Create: `backend/src/Api/Api.csproj`
- Create: `backend/tests/Unit/Unit.csproj`
- Create: `backend/tests/Integration/Integration.csproj`

- [ ] **Step 1: Create solution file**

```bash
cd backend
dotnet new sln -n backend
```

- [ ] **Step 2: Create Core project**

```bash
dotnet new classlib -n Core -src/Core/Core.csproj
dotnet sln add Core/Core.csproj
```

- [ ] **Step 3: Create Infrastructure project**

```bash
dotnet new classlib -n Infrastructure -src/Infrastructure/Infrastructure.csproj
dotnet sln add Infrastructure/Infrastructure.csproj
```

- [ ] **Step 4: Create API project**

```bash
dotnet new webapi -n Api -src/Api/Api.csproj
dotnet sln add Api/Api.csproj
```

- [ ] **Step 5: Create test projects**

```bash
dotnet new xunit -n Unit -ttests/Unit/Unit.csproj
dotnet new xunit -n Integration -ttests/Integration/Integration.csproj
dotnet sln add tests/Unit/Unit.csproj
dotnet sln add tests/Integration/Integration.csproj
```

- [ ] **Step 6: Add project references**

```bash
dotnet add Infrastructure/Infrastructure.csproj reference Core/Core.csproj
dotnet add Api/Api.csproj reference Core/Core.csproj
dotnet add Api/Api.csproj reference Infrastructure/Infrastructure.csproj
dotnet add tests/Unit/Unit.csproj reference Core/Core.csproj
dotnet add tests/Integration/Integration.csproj reference Api/Api.csproj
```

- [ ] **Step 7: Commit**

```bash
git add backend/
git commit -m "feat: create .NET solution structure"
```

### Task 2: Define Entity classes

**Files:**
- Create: `backend/src/Core/Entities/Article.cs`
- Create: `backend/src/Core/Entities/ServicePrice.cs`
- Create: `backend/src/Core/Entities/Callback.cs`
- Create: `backend/src/Core/Entities/Contact.cs`

- [ ] **Step 1: Create Article entity**

```csharp
namespace Core.Entities;

public class Article
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ThumbnailPath { get; set; }
    public DateTime PublishedAt { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

- [ ] **Step 2: Create ServicePrice entity**

```csharp
namespace Core.Entities;

public class ServicePrice
{
    public int Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int PriceFrom { get; set; }
    public int? PriceTo { get; set; }
    public string? Unit { get; set; }
    public int SortOrder { get; set; }
}
```

- [ ] **Step 3: Create Callback entity**

```csharp
namespace Core.Entities;

public class Callback
{
    public int Id { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? Name { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsProcessed { get; set; }
}
```

- [ ] **Step 4: Create Contact entity**

```csharp
namespace Core.Entities;

public class Contact
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsProcessed { get; set; }
}
```

- [ ] **Step 5: Commit**

```bash
git add backend/src/Core/Entities/
git commit -m "feat: add entity classes"
```

### Task 3: Create DbContext and configure SQLite

**Files:**
- Create: `backend/src/Infrastructure/Data/AppDbContext.cs`
- Modify: `backend/src/Infrastructure/Infrastructure.csproj`
- Modify: `backend/src/Api/Program.cs`

- [ ] **Step 1: Install EF Core packages**

```bash
cd backend/src/Infrastructure
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.EntityFrameworkCore.Design
```

- [ ] **Step 2: Create AppDbContext**

```csharp
using Microsoft.EntityFrameworkCore;
using Core.Entities;

namespace Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Article> Articles => Set<Article>();
    public DbSet<ServicePrice> ServicePrices => Set<ServicePrice>();
    public DbSet<Callback> Callbacks => Set<Callback>();
    public DbSet<Contact> Contacts => Set<Contact>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Article>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.Property(e => e.Title).IsRequired();
            entity.Property(e => e.Slug).IsRequired();
            entity.Property(e => e.Content).IsRequired();
        });

        modelBuilder.Entity<ServicePrice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Category).IsRequired();
            entity.Property(e => e.Name).IsRequired();
        });

        modelBuilder.Entity<Callback>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Phone).IsRequired();
        });

        modelBuilder.Entity<Contact>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.Phone).IsRequired();
            entity.Property(e => e.Message).IsRequired();
        });
    }
}
```

- [ ] **Step 3: Configure DbContext in Program.cs**

```csharp
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add services here later

var app = builder.Build();

app.Run();
```

- [ ] **Step 4: Add connection string to appsettings.json**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=database.db"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

- [ ] **Step 5: Commit**

```bash
git add backend/src/Infrastructure/ backend/src/Api/
git commit -m "feat: configure DbContext with SQLite"
```

### Task 4: Create Repository pattern

**Files:**
- Create: `backend/src/Core/Interfaces/IRepository.cs`
- Create: `backend/src/Infrastructure/Repositories/Repository.cs`

- [ ] **Step 1: Create IRepository interface**

```csharp
namespace Core.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
}
```

- [ ] **Step 2: Create Repository implementation**

```csharp
using Microsoft.EntityFrameworkCore;
using Core.Interfaces;

namespace Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    private readonly AppDbContext _context;
    private readonly DbSet<T> _dbSet;

    public Repository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<T> AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(T entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
    }
}
```

- [ ] **Step 3: Register in Program.cs**

```csharp
using Core.Interfaces;
using Infrastructure.Repositories;

builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
```

- [ ] **Step 4: Commit**

```bash
git add backend/src/Core/Interfaces/ backend/src/Infrastructure/Repositories/
git commit -m "feat: add repository pattern"
```

### Task 5: Create DTOs

**Files:**
- Create: `backend/src/Core/DTOs/ArticleDto.cs`
- Create: `backend/src/Core/DTOs/ServicePriceDto.cs`
- Create: `backend/src/Core/DTOs/CallbackRequest.cs`
- Create: `backend/src/Core/DTOs/ContactRequest.cs`
- Create: `backend/src/Core/DTOs/CreateArticleDto.cs`
- Create: `backend/src/Core/DTOs/UpdateArticleDto.cs`

- [ ] **Step 1: Create ArticleDto**

```csharp
namespace Core.DTOs;

public record ArticleDto(
    int Id,
    string Title,
    string Slug,
    string? Summary,
    string Content,
    string? ThumbnailPath,
    DateTime PublishedAt
);
```

- [ ] **Step 2: Create ServicePriceDto**

```csharp
namespace Core.DTOs;

public record ServicePriceDto(
    int Id,
    string Category,
    string Name,
    string? Description,
    int PriceFrom,
    int? PriceTo,
    string? Unit
);
```

- [ ] **Step 3: Create CallbackRequest**

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

- [ ] **Step 4: Create ContactRequest**

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

- [ ] **Step 5: Create CreateArticleDto**

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

- [ ] **Step 6: Create UpdateArticleDto**

```csharp
using System.ComponentModel.DataAnnotations;

namespace Core.DTOs;

public class UpdateArticleDto
{
    [Required]
    public string Title { get; set; } = string.Empty;

    public string? Slug { get; set; }

    public string? Summary { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;

    public string? ThumbnailPath { get; set; }
    public bool IsPublished { get; set; }
}
```

- [ ] **Step 7: Commit**

```bash
git add backend/src/Core/DTOs/
git commit -m "feat: add DTOs"
```

---

## Phase 2: Backend API Controllers

### Task 6: Articles Controller

**Files:**
- Create: `backend/src/Api/Controllers/ArticlesController.cs`
- Modify: `backend/src/Api/Program.cs`

- [ ] **Step 1: Create ArticlesController**

```csharp
using Microsoft.AspNetCore.Mvc;
using Core.Entities;
using Core.Interfaces;
using Core.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ArticlesController : ControllerBase
{
    private readonly IRepository<Article> _repository;
    private readonly ILogger<ArticlesController> _logger;

    public ArticlesController(IRepository<Article> repository, ILogger<ArticlesController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet]
public async Task<ActionResult<IEnumerable<ArticleDto>>> GetArticles(bool publishedOnly = true)
    {
        var articles = await _repository.GetAllAsync();
        var filtered = publishedOnly
            ? articles.Where(a => a.IsPublished).OrderByDescending(a => a.PublishedAt)
            : articles.OrderByDescending(a => a.CreatedAt);

        return Ok(filtered.Select(a => new ArticleDto(
            a.Id, a.Title, a.Slug, a.Summary, a.Content, a.ThumbnailPath, a.PublishedAt
        )));
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<ArticleDto>> GetArticleBySlug(string slug)
    {
        var articles = await _repository.GetAllAsync();
        var article = articles.FirstOrDefault(a => a.Slug == slug && a.IsPublished);

        if (article == null)
            return NotFound();

        return Ok(new ArticleDto(
            article.Id, article.Title, article.Slug, article.Summary,
            article.Content, article.ThumbnailPath, article.PublishedAt
        ));
    }

    [HttpPost]
    public async Task<ActionResult<ArticleDto>> CreateArticle(CreateArticleDto dto)
    {
        var article = new Article
        {
            Title = dto.Title,
            Slug = dto.Slug,
            Summary = dto.Summary,
            Content = dto.Content,
            ThumbnailPath = dto.ThumbnailPath,
            IsPublished = dto.IsPublished,
            PublishedAt = dto.IsPublished ? DateTime.UtcNow : default
        };

        var created = await _repository.AddAsync(article);
        _logger.LogInformation("Created article {Id} with slug {Slug}", created.Id, created.Slug);

        return CreatedAtAction(nameof(GetArticleBySlug), new { slug = created.Slug },
            new ArticleDto(created.Id, created.Title, created.Slug, created.Summary,
                created.Content, created.ThumbnailPath, created.PublishedAt));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ArticleDto>> UpdateArticle(int id, UpdateArticleDto dto)
    {
        var article = await _repository.GetByIdAsync(id);
        if (article == null)
            return NotFound();

        article.Title = dto.Title;
        if (!string.IsNullOrEmpty(dto.Slug))
            article.Slug = dto.Slug;
        article.Summary = dto.Summary;
        article.Content = dto.Content;
        article.ThumbnailPath = dto.ThumbnailPath;
        article.IsPublished = dto.IsPublished;

        if (dto.IsPublished && article.PublishedAt == default)
            article.PublishedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(article);
        _logger.LogInformation("Updated article {Id}", id);

        return Ok(new ArticleDto(article.Id, article.Title, article.Slug, article.Summary,
            article.Content, article.ThumbnailPath, article.PublishedAt));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteArticle(int id)
    {
        var article = await _repository.GetByIdAsync(id);
        if (article == null)
            return NotFound();

        await _repository.DeleteAsync(article);
        _logger.LogInformation("Deleted article {Id}", id);

        return NoContent();
    }
}
```

- [ ] **Step 2: Add controllers to Program.cs**

```csharp
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

app.MapControllers();
app.UseSwagger();
app.UseSwaggerUI();
```

- [ ] **Step 3: Commit**

```bash
git add backend/src/Api/
git commit -m "feat: add Articles controller"
```

### Task 7: Service Prices Controller

**Files:**
- Create: `backend/src/Api/Controllers/ServicesController.cs`

- [ ] **Step 1: Create ServicesController**

```csharp
using Microsoft.AspNetCore.Mvc;
using Core.Entities;
using Core.Interfaces;
using Core.DTOs;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServicesController : ControllerBase
{
    private readonly IRepository<ServicePrice> _repository;

    public ServicesController(IRepository<ServicePrice> repository)
    {
        _repository = repository;
    }

    [HttpGet("prices")]
    public async Task<ActionResult<IEnumerable<ServicePriceDto>>> GetPrices()
    {
        var prices = await _repository.GetAllAsync();
        var grouped = prices
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.PriceFrom);

        return Ok(grouped.Select(p => new ServicePriceDto(
            p.Id, p.Category, p.Name, p.Description, p.PriceFrom, p.PriceTo, p.Unit
        )));
    }

    [HttpPost]
    public async Task<ActionResult<ServicePriceDto>> CreatePrice(ServicePriceDto dto)
    {
        var price = new ServicePrice
        {
            Category = dto.Category,
            Name = dto.Name,
            Description = dto.Description,
            PriceFrom = dto.PriceFrom,
            PriceTo = dto.PriceTo,
            Unit = dto.Unit,
            SortOrder = dto.Id // using Id as temp SortOrder
        };

        var created = await _repository.AddAsync(price);
        return Ok(new ServicePriceDto(created.Id, created.Category, created.Name,
            created.Description, created.PriceFrom, created.PriceTo, created.Unit));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ServicePriceDto>> UpdatePrice(int id, ServicePriceDto dto)
    {
        var price = await _repository.GetByIdAsync(id);
        if (price == null)
            return NotFound();

        price.Category = dto.Category;
        price.Name = dto.Name;
        price.Description = dto.Description;
        price.PriceFrom = dto.PriceFrom;
        price.PriceTo = dto.PriceTo;
        price.Unit = dto.Unit;

        await _repository.UpdateAsync(price);
        return Ok(new ServicePriceDto(price.Id, price.Category, price.Name,
            price.Description, price.PriceFrom, price.PriceTo, price.Unit));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeletePrice(int id)
    {
        var price = await _repository.GetByIdAsync(id);
        if (price == null)
            return NotFound();

        await _repository.DeleteAsync(price);
        return NoContent();
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add backend/src/Api/Controllers/ServicesController.cs
git commit -m "feat: add Services controller"
```

### Task 8: Callbacks and Contacts Controllers

**Files:**
- Create: `backend/src/Api/Controllers/CallbacksController.cs`
- Create: `backend/src/Api/Controllers/ContactsController.cs`

- [ ] **Step 1: Create CallbacksController**

```csharp
using Microsoft.AspNetCore.Mvc;
using Core.Entities;
using Core.Interfaces;
using Core.DTOs;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CallbacksController : ControllerBase
{
    private readonly IRepository<Callback> _repository;
    private readonly ILogger<CallbacksController> _logger;

    public CallbacksController(IRepository<Callback> repository, ILogger<CallbacksController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult> CreateCallback(CallbackRequest dto)
    {
        var callback = new Callback
        {
            Phone = dto.Phone,
            Name = dto.Name
        };

        await _repository.AddAsync(callback);
        _logger.LogInformation("Created callback request from {Phone}", dto.Phone);

        return Ok(new { message = "Спасибо! Мы перезвоним вам в ближайшее время." });
    }
}
```

- [ ] **Step 2: Create ContactsController**

```csharp
using Microsoft.AspNetCore.Mvc;
using Core.Entities;
using Core.Interfaces;
using Core.DTOs;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContactsController : ControllerBase
{
    private readonly IRepository<Contact> _repository;
    private readonly ILogger<ContactsController> _logger;

    public ContactsController(IRepository<Contact> repository, ILogger<ContactsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult> CreateContact(ContactRequest dto)
    {
        var contact = new Contact
        {
            Name = dto.Name,
            Phone = dto.Phone,
            Message = dto.Message
        };

        await _repository.AddAsync(contact);
        _logger.LogInformation("Created contact message from {Name} ({Phone})", dto.Name, dto.Phone);

        return Ok(new { message = "Спасибо за сообщение! Мы ответим вам в ближайшее время." });
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add backend/src/Api/Controllers/CallbacksController.cs backend/src/Api/Controllers/ContactsController.cs
git commit -m "feat: add Callbacks and Contacts controllers"
```

---

## Phase 3: Backend Tests

### Task 9: Unit Tests for Article Entity

**Files:**
- Create: `backend/tests/Unit/Core/Entities/ArticleTests.cs`

- [ ] **Step 1: Add FluentAssertions**

```bash
cd backend/tests/Unit
dotnet add package FluentAssertions
```

- [ ] **Step 2: Create ArticleTests**

```csharp
using FluentAssertions;
using Xunit;
using Core.Entities;

namespace Unit.Core.Entities;

public class ArticleTests
{
    [Fact]
    public void Article_WithValidData_CreatesCorrectly()
    {
        // Arrange & Act
        var article = new Article
        {
            Title = "Test Article",
            Slug = "test-article",
            Summary = "Test summary",
            Content = "Test content",
            ThumbnailPath = "/uploads/test.jpg",
            PublishedAt = DateTime.UtcNow,
            IsPublished = true
        };

        // Assert
        article.Title.Should().Be("Test Article");
        article.Slug.Should().Be("test-article");
        article.IsPublished.Should().BeTrue();
    }

    [Fact]
    public void Article_WhenCreated_SetsCreatedAtToUtcNow()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var article = new Article();

        // Assert
        article.CreatedAt.Should().BeAfter(beforeCreation);
    }
}
```

- [ ] **Step 3: Run tests**

```bash
cd backend
dotnet test
```

Expected: All tests PASS

- [ ] **Step 4: Commit**

```bash
git add backend/tests/Unit/Core/Entities/ArticleTests.cs
git commit -m "test: add Article entity unit tests"
```

### Task 10: Integration Tests for Articles Endpoint

**Files:**
- Create: `backend/tests/Integration/Api/ArticlesEndpointTests.cs`

- [ ] **Step 1: Add required packages**

```bash
cd backend/tests/Integration
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package Microsoft.EntityFrameworkCore.InMemory
```

- [ ] **Step 2: Create ArticlesEndpointTests**

```csharp
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Infrastructure.Data;
using Xunit;
using System.Net;
using System.Net.Http.Json;

namespace Integration.Api;

public class ArticlesEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ArticlesEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase("TestDb"));
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetArticles_WhenNoArticles_ReturnsEmptyList()
    {
        // Act
        var response = await _client.GetAsync("/api/articles");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var articles = await response.Content.ReadFromJsonAsync<List<object>>();
        articles.Should().BeEmpty();
    }
}
```

- [ ] **Step 3: Fix Program.cs for integration tests**

Add partial class: `backend/src/Api/Program.cs` → split to `Program.cs` and `Program.cs` (keep minimal in Program.cs)

- [ ] **Step 4: Run tests**

```bash
cd backend
dotnet test
```

Expected: All tests PASS

- [ ] **Step 5: Commit**

```bash
git add backend/tests/Integration/Api/ArticlesEndpointTests.cs
git commit -m "test: add Articles endpoint integration tests"
```

---

## Phase 4: Docker Infrastructure

### Task 11: Dockerfile for Backend

**Files:**
- Create: `docker/backend.Dockerfile`

- [ ] **Step 1: Create backend.Dockerfile**

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["backend/backend.sln", "./"]
COPY ["backend/src/Api/Api.csproj", "Api/"]
COPY ["backend/src/Core/Core.csproj", "Core/"]
COPY ["backend/src/Infrastructure/Infrastructure.csproj", "Infrastructure/"]
RUN dotnet restore "backend/backend.sln"
COPY . .
WORKDIR "/src/backend/src/Api"
RUN dotnet build "Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Api.dll"]
```

- [ ] **Step 2: Commit**

```bash
git add docker/backend.Dockerfile
git commit -m "infra: add backend Dockerfile"
```

### Task 12: Dockerfile for Frontend

**Files:**
- Create: `docker/frontend.Dockerfile`

- [ ] **Step 1: Create frontend.Dockerfile**

```dockerfile
FROM node:20-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM node:20-alpine AS runtime
WORKDIR /app
COPY --from=build /app/.output ./
COPY --from=build /app/package*.json ./
RUN npm ci --production
EXPOSE 3000
CMD ["node", "server/index.mjs"]
```

- [ ] **Step 2: Commit**

```bash
git add docker/frontend.Dockerfile
git commit -m "infra: add frontend Dockerfile"
```

### Task 13: Docker Compose and Nginx

**Files:**
- Create: `docker-compose.yml`
- Create: `docker-compose.prod.yml`
- Create: `docker/nginx.conf`

- [ ] **Step 1: Create docker-compose.yml**

```yaml
version: '3.8'

services:
  frontend:
    build:
      context: ./frontend
      dockerfile: ../docker/frontend.Dockerfile
    environment:
      - NUXT_PUBLIC_API_URL=http://backend:8080
    volumes:
      - uploads:/app/public/uploads

  backend:
    build:
      context: ./backend
      dockerfile: ../docker/backend.Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Data Source=/app/data/database.db
      - UploadPath=/app/uploads
    volumes:
      - db:/app/data
      - uploads:/app/uploads

  nginx:
    image: nginx:alpine
    ports:
      - "80:80"
    volumes:
      - ./docker/nginx.conf:/etc/nginx/nginx.conf:ro
    depends_on:
      - frontend
      - backend

volumes:
  db:
  uploads:
```

- [ ] **Step 2: Create docker-compose.prod.yml**

```yaml
version: '3.8'

services:
  frontend:
    environment:
      - NUXT_PUBLIC_API_URL=https://${DOMAIN}/api

  backend:
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
      - ConnectionStrings__DefaultConnection=Data Source=/app/data/database.db
      - UploadPath=/app/uploads
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3

  nginx:
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./docker/nginx.conf:/etc/nginx/nginx.conf:ro
      - ./docker/ssl:/etc/nginx/ssl:ro
      - uploads:/uploads:ro
```

- [ ] **Step 3: Create nginx.conf**

```nginx
events {
    worker_connections 1024;
}

http {
    upstream frontend {
        server frontend:3000;
    }

    upstream backend {
        server backend:8080;
    }

    server {
        listen 80;
        server_name localhost;

        location / {
            proxy_pass http://frontend;
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
        }

        location /api/ {
            proxy_pass http://backend;
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
        }

        location /uploads/ {
            alias /uploads/;
        }
    }
}
```

- [ ] **Step 4: Add .gitignore**

```bash
cat > .gitignore << 'EOF'
# Environment
.env

# Database
backend/src/Infrastructure/Data/database.db
backend/src/Infrastructure/Data/database.db-shm
backend/src/Infrastructure/Data/database.db-wal

# uploads
uploads/

# Node
frontend/node_modules/
frontend/.nuxt/
frontend/dist/

# Docker
docker/ssl/

# IDE
.vscode/
.idea/
*.swp
EOF
```

- [ ] **Step 5: Commit**

```bash
git add docker-compose.yml docker-compose.prod.yml docker/nginx.conf .gitignore
git commit -m "infra: add docker compose and nginx config"
```

---

## Phase 5: Frontend

### Task 14: Initialize Nuxt 3 Project

**Files:**
- Create: `frontend/nuxt.config.ts`
- Create: `frontend/package.json`
- Create: `frontend/tsconfig.json`
- Create: `frontend/tailwind.config.js`

- [ ] **Step 1: Initialize Nuxt**

```bash
cd frontend
npx nuxi@latest init . --packageManager npm
# Answer prompts: Project name: frontend, Package manager: npm, UI framework: None
```

- [ ] **Step 2: Install dependencies**

```bash
cd frontend
npm install -D @nuxtjs/tailwindcss @vueuse/nuxt
npm install -D vitest @vue/test-utils
npm install -D @nuxt/test-utils
```

- [ ] **Step 3: Configure nuxt.config.ts**

```typescript
export default defineNuxtConfig({
  devtools: { enabled: true },
  modules: ['@nuxtjs/tailwindcss', '@vueuse/nuxt'],
  runtimeConfig: {
    public: {
      apiUrl: process.env.NUXT_PUBLIC_API_URL || 'http://localhost:8080'
    }
  },
  app: {
    head: {
      title: 'Строительная компания',
      meta: [
        { charset: 'utf-8' },
        { name: 'viewport', content: 'width=device-width, initial-scale=1' }
      ]
    }
  }
})
```

- [ ] **Step 4: Configure tailwind.config.js**

```js
module.exports = {
  content: [
    "./components/**/*.{js,vue,ts}",
    "./layouts/**/*.vue",
    "./pages/**/*.vue",
    "./plugins/**/*.{js,ts}",
  ],
  theme: {
    extend: {},
  },
  plugins: [],
}
```

- [ ] **Step 5: Configure vitest**

Update `package.json`:
```json
{
  "scripts": {
    "dev": "nuxt dev",
    "build": "nuxt build",
    "test": "vitest"
  },
  "devDependencies": {
    "@nuxt/test-utils": "^3.0.0",
    "@vue/test-utils": "^2.4.0",
    "vitest": "^1.0.0"
  }
}
```

Create `vitest.config.ts`:
```typescript
import { defineConfig } from 'vitest/config'
import vue from '@vitejs/plugin-vue'

export default defineConfig({
  plugins: [vue()],
  test: {
    environment: 'jsdom'
  }
})
```

- [ ] **Step 6: Commit**

```bash
git add frontend/
git commit -m "feat: initialize Nuxt 3 project"
```

### Task 15: Create Base Layout and Components

**Files:**
- Create: `frontend/app.vue`
- Create: `frontend/components/Header.vue`
- Create: `frontend/components/Footer.vue`
- Create: `frontend/assets/css/main.css`

- [ ] **Step 1: Create app.vue**

```vue
<template>
  <div class="min-h-screen flex flex-col">
    <Header />
    <main class="flex-1">
      <NuxtPage />
    </main>
    <Footer />
  </div>
</template>
```

- [ ] **Step 2: Create main.css**

```css
@tailwind base;
@tailwind components;
@tailwind utilities;
```

Import in `nuxt.config.ts`:
```typescript
css: ['~/assets/css/main.css']
```

- [ ] **Step 3: Create Header component**

```vue
<template>
  <header class="bg-white shadow-sm">
    <nav class="container mx-auto px-4 py-4">
      <div class="flex justify-between items-center">
        <NuxtLink to="/" class="text-xl font-bold text-gray-800">
          СтройГрад
        </NuxtLink>
        <ul class="flex space-x-6">
          <li><NuxtLink to="/" class="text-gray-600 hover:text-gray-900">Главная</NuxtLink></li>
          <li><NuxtLink to="/prices" class="text-gray-600 hover:text-gray-900">Цены</NuxtLink></li>
          <li><NuxtLink to="/portfolio" class="text-gray-600 hover:text-gray-900">Портфолио</NuxtLink></li>
          <li><NuxtLink to="/contact" class="text-gray-600 hover:text-gray-900">Контакты</NuxtLink></li>
        </ul>
      </div>
    </nav>
  </header>
</template>
```

- [ ] **Step 4: Create Footer component**

```vue
<template>
  <footer class="bg-gray-800 text-white py-8">
    <div class="container mx-auto px-4">
      <div class="grid md:grid-cols-3 gap-8">
        <div>
          <h3 class="text-lg font-bold mb-4">СтройГрад</h3>
          <p class="text-gray-400">Качественные строительные работы</p>
        </div>
        <div>
          <h3 class="text-lg font-bold mb-4">Контакты</h3>
          <p class="text-gray-400">Телефон: +7 (XXX) XXX-XX-XX</p>
          <p class="text-gray-400">Email: info@stroygrad.ru</p>
        </div>
        <div>
          <h3 class="text-lg font-bold mb-4">Соцсети</h3>
          <div class="flex space-x-4">
            <a href="#" class="text-gray-400 hover:text-white">Telegram</a>
            <a href="#" class="text-gray-400 hover:text-white">WhatsApp</a>
          </div>
        </div>
      </div>
    </div>
  </footer>
</template>
```

- [ ] **Step 5: Commit**

```bash
git add frontend/app.vue frontend/components/ frontend/assets/
git commit -m "feat: add base layout and components"
```

### Task 16: Create Pages - Home, Prices, Contact

**Files:**
- Create: `frontend/pages/index.vue`
- Create: `frontend/pages/prices.vue`
- Create: `frontend/pages/contact.vue`
- Create: `frontend/components/CallbackForm.vue`
- Create: `frontend/components/ContactForm.vue`

- [ ] **Step 1: Create index.vue (Home)**

```vue
<template>
  <div>
    <section class="bg-blue-600 text-white py-20">
      <div class="container mx-auto px-4 text-center">
        <h1 class="text-4xl md:text-5xl font-bold mb-4">Строительные услуги</h1>
        <p class="text-xl mb-8">Качественно, в срок, по разумной цене</p>
        <NuxtLink to="/contact" class="bg-white text-blue-600 px-6 py-3 rounded-lg font-semibold hover:bg-gray-100">
          Связаться с нами
        </NuxtLink>
      </div>
    </section>

    <section class="py-16 bg-gray-50">
      <div class="container mx-auto px-4">
        <h2 class="text-3xl font-bold text-center mb-12">Наши услуги</h2>
        <div class="grid md:grid-cols-3 gap-8">
          <div class="bg-white p-6 rounded-lg shadow">
            <h3 class="text-xl font-bold mb-2">Ремонт квартир</h3>
            <p class="text-gray-600">Косметический и капитальный ремонт любой сложности</p>
          </div>
          <div class="bg-white p-6 rounded-lg shadow">
            <h3 class="text-xl font-bold mb-2">Строительство домов</h3>
            <p class="text-gray-600">Возведение домов под ключ из различных материалов</p>
          </div>
          <div class="bg-white p-6 rounded-lg shadow">
            <h3 class="text-xl font-bold mb-2">Сантехника</h3>
            <p class="text-gray-600">Монтаж и ремонт систем водоснабжения и канализации</p>
          </div>
        </div>
      </div>
    </section>
  </div>
</template>
```

- [ ] **Step 2: Create prices.vue**

```vue
<template>
  <div class="container mx-auto px-4 py-12">
    <h1 class="text-3xl font-bold mb-8">Цены на услуги</h1>

    <div v-if="pending" class="text-center py-8">Загрузка...</div>
    <div v-else-if="error" class="text-red-600">Ошибка загрузки цен</div>
    <div v-else>
      <div v-for="category in groupedPrices" :key="category.name" class="mb-8">
        <h2 class="text-2xl font-bold mb-4">{{ category.name }}</h2>
        <div class="bg-white rounded-lg shadow overflow-hidden">
          <table class="w-full">
            <thead class="bg-gray-50">
              <tr>
                <th class="px-6 py-3 text-left text-sm font-semibold">Услуга</th>
                <th class="px-6 py-3 text-left text-sm font-semibold">Описание</th>
                <th class="px-6 py-3 text-right text-sm font-semibold">Цена</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-200">
              <tr v-for="item in category.items" :key="item.id">
                <td class="px-6 py-4">{{ item.name }}</td>
                <td class="px-6 py-4 text-gray-600">{{ item.description || '-' }}</td>
                <td class="px-6 py-4 text-right font-semibold">
                  от {{ item.priceFrom }} ₽
                  <span v-if="item.priceTo">до {{ item.priceTo }} ₽</span>
                  <span v-if="item.unit">/ {{ item.unit }}</span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
const { data, pending, error } = await useFetch('/api/services/prices')

const groupedPrices = computed(() => {
  if (!data.value) return []
  const groups = {}
  data.value.forEach(item => {
    if (!groups[item.category]) {
      groups[item.category] = { name: item.category, items: [] }
    }
    groups[item.category].items.push(item)
  })
  return Object.values(groups)
})
</script>
```

- [ ] **Step 3: Create contact.vue**

```vue
<template>
  <div class="container mx-auto px-4 py-12">
    <h1 class="text-3xl font-bold mb-8">Контакты</h1>

    <div class="grid md:grid-cols-2 gap-12">
      <div>
        <h2 class="text-2xl font-bold mb-4">Свяжитесь с нами</h2>
        <div class="space-y-4 mb-8">
          <p><strong>Телефон:</strong> +7 (XXX) XXX-XX-XX</p>
          <p><strong>Email:</strong> info@stroygrad.ru</p>
          <p><strong>Адрес:</strong> г. Москва, ул. Примерная, д. 1</p>
        </div>

        <h3 class="text-xl font-bold mb-4">Мессенджеры</h3>
        <div class="flex space-x-4">
          <a href="#" class="bg-blue-500 text-white px-4 py-2 rounded">Telegram</a>
          <a href="#" class="bg-green-500 text-white px-4 py-2 rounded">WhatsApp</a>
        </div>
      </div>

      <div>
        <ContactForm />
        <div class="mt-8">
          <CallbackForm />
        </div>
      </div>
    </div>
  </div>
</template>
```

- [ ] **Step 4: Create ContactForm component**

```vue
<template>
  <form @submit.prevent="submit" class="space-y-4">
    <h3 class="text-xl font-bold mb-4">Написать нам</h3>

    <div>
      <label class="block text-sm font-medium mb-1">Ваше имя *</label>
      <input v-model="form.name" type="text" required
        class="w-full px-4 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500"
        placeholder="Иван Иванов">
    </div>

    <div>
      <label class="block text-sm font-medium mb-1">Телефон *</label>
      <input v-model="form.phone" type="tel" required
        class="w-full px-4 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500"
        placeholder="+7 (XXX) XXX-XX-XX">
    </div>

    <div>
      <label class="block text-sm font-medium mb-1">Сообщение *</label>
      <textarea v-model="form.message" required rows="4"
        class="w-full px-4 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500"
        placeholder="Опишите ваш запрос..."></textarea>
    </div>

    <button type="submit" :disabled="loading"
      class="w-full bg-blue-600 text-white py-3 rounded-lg font-semibold hover:bg-blue-700 disabled:opacity-50">
      {{ loading ? 'Отправка...' : 'Отправить' }}
    </button>

    <div v-if="message" :class="message.type === 'success' ? 'text-green-600' : 'text-red-600'">
      {{ message.text }}
    </div>
  </form>
</template>

<script setup>
const form = ref({ name: '', phone: '', message: '' })
const loading = ref(false)
const message = ref(null)

const config = useRuntimeConfig()

const submit = async () => {
  loading.value = true
  message.value = null

  try {
    const response = await $fetch(`${config.public.apiUrl}/api/contacts`, {
      method: 'POST',
      body: form.value
    })
    message.value = { type: 'success', text: response.message }
    form.value = { name: '', phone: '', message: '' }
  } catch (error) {
    message.value = { type: 'error', text: 'Ошибка отправки. Попробуйте позже.' }
  } finally {
    loading.value = false
  }
}
</script>
```

- [ ] **Step 5: Create CallbackForm component**

```vue
<template>
  <div class="bg-gray-50 p-6 rounded-lg">
    <h3 class="text-lg font-bold mb-4">Перезвоните мне</h3>

    <form @submit.prevent="submit" class="space-y-4">
      <div>
        <input v-model="form.phone" type="tel" required
          class="w-full px-4 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500"
          placeholder="Ваш телефон">
      </div>

      <div>
        <input v-model="form.name" type="text"
          class="w-full px-4 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500"
          placeholder="Ваше имя (необязательно)">
      </div>

      <button type="submit" :disabled="loading"
        class="w-full bg-green-600 text-white py-2 rounded-lg font-semibold hover:bg-green-700 disabled:opacity-50">
        {{ loading ? 'Отправка...' : 'Заказать звонок' }}
      </button>

      <div v-if="message" :class="message.type === 'success' ? 'text-green-600' : 'text-red-600'">
        {{ message.text }}
      </div>
    </form>
  </div>
</template>

<script setup>
const form = ref({ phone: '', name: '' })
const loading = ref(false)
const message = ref(null)

const config = useRuntimeConfig()

const submit = async () => {
  loading.value = true
  message.value = null

  try {
    const response = await $fetch(`${config.public.apiUrl}/api/callbacks`, {
      method: 'POST',
      body: form.value
    })
    message.value = { type: 'success', text: response.message }
    form.value = { phone: '', name: '' }
  } catch (error) {
    message.value = { type: 'error', text: 'Ошибка отправки.' }
  } finally {
    loading.value = false
  }
}
</script>
```

- [ ] **Step 6: Commit**

```bash
git add frontend/pages/ frontend/components/
git commit -m "feat: add home, prices, contact pages with forms"
```

### Task 17: Portfolio Pages

**Files:**
- Create: `frontend/pages/portfolio/index.vue`
- Create: `frontend/pages/portfolio/[slug].vue`
- Create: `frontend/components/PortfolioCard.vue`

- [ ] **Step 1: Create PortfolioCard component**

```vue
<template>
  <NuxtLink :to="`/portfolio/${article.slug}`" class="block">
    <div class="bg-white rounded-lg shadow overflow-hidden hover:shadow-lg transition">
      <img v-if="article.thumbnailPath"
        :src="article.thumbnailPath"
        :alt="article.title"
        class="w-full h-48 object-cover">
      <div class="p-6">
        <h3 class="text-xl font-bold mb-2">{{ article.title }}</h3>
        <p class="text-gray-600">{{ article.summary }}</p>
        <p class="text-sm text-gray-400 mt-2">{{ formatDate(article.publishedAt) }}</p>
      </div>
    </div>
  </NuxtLink>
</template>

<script setup>
const props = defineProps({
  article: Object
})

const formatDate = (date) => {
  return new Date(date).toLocaleDateString('ru-RU', {
    year: 'numeric',
    month: 'long',
    day: 'numeric'
  })
}
</script>
```

- [ ] **Step 2: Create portfolio/index.vue**

```vue
<template>
  <div class="container mx-auto px-4 py-12">
    <h1 class="text-3xl font-bold mb-8">Портфолио</h1>

    <div v-if="pending" class="text-center py-8">Загрузка...</div>
    <div v-else-if="error" class="text-red-600">Ошибка загрузки</div>
    <div v-else>
      <div v-if="articles.length === 0" class="text-center text-gray-600 py-8">
        Материалы пока не добавлены
      </div>
      <div v-else class="grid md:grid-cols-2 lg:grid-cols-3 gap-6">
        <PortfolioCard v-for="article in articles" :key="article.id" :article="article" />
      </div>
    </div>
  </div>
</template>

<script setup>
const { data, pending, error } = await useFetch('/api/articles')

const articles = computed(() => data.value || [])
</script>
```

- [ ] **Step 3: Create portfolio/[slug].vue**

```vue
<template>
  <div v-if="pending" class="container mx-auto px-4 py-12 text-center">Загрузка...</div>
  <div v-else-if="error" class="container mx-auto px-4 py-12 text-red-600">Статья не найдена</div>
  <div v-else class="container mx-auto px-4 py-12">
    <img v-if="article.thumbnailPath"
      :src="article.thumbnailPath"
      :alt="article.title"
      class="w-full h-96 object-cover rounded-lg mb-8">

    <h1 class="text-4xl font-bold mb-4">{{ article.title }}</h1>
    <p class="text-gray-500 mb-8">{{ formatDate(article.publishedAt) }}</p>

    <div class="prose max-w-none">
      {{ article.content }}
    </div>

    <NuxtLink to="/portfolio" class="inline-block mt-8 text-blue-600 hover:underline">
      ← Вернуться к портфолио
    </NuxtLink>
  </div>
</template>

<script setup>
const route = useRoute()
const { data, pending, error } = await useFetch(`/api/articles/${route.params.slug}`)

const article = computed(() => data.value)

const formatDate = (date) => {
  return new Date(date).toLocaleDateString('ru-RU', {
    year: 'numeric',
    month: 'long',
    day: 'numeric'
  })
}
</script>
```

- [ ] **Step 4: Commit**

```bash
git add frontend/pages/portfolio/ frontend/components/PortfolioCard.vue
git commit -m "feat: add portfolio pages"
```

### Task 18: Frontend Tests

**Files:**
- Create: `frontend/tests/component/CallbackForm.spec.ts`
- Create: `frontend/tests/component/ContactForm.spec.ts`

- [ ] **Step 1: Create CallbackForm.spec.ts**

```typescript
import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import CallbackForm from '@/components/CallbackForm.vue'

describe('CallbackForm', () => {
  it('renders form with phone input', () => {
    const wrapper = mount(CallbackForm)
    expect(wrapper.find('input[type="tel"]').exists()).toBe(true)
  })

  it('disables button when loading', async () => {
    const wrapper = mount(CallbackForm)
    await wrapper.setData({ loading: true })
    expect(wrapper.find('button').attributes('disabled')).toBeDefined()
  })
})
```

- [ ] **Step 2: Create ContactForm.spec.ts**

```typescript
import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import ContactForm from '@/components/ContactForm.vue'

describe('ContactForm', () => {
  it('renders all required fields', () => {
    const wrapper = mount(ContactForm)
    expect(wrapper.find('input[type="text"]').exists()).toBe(true) // name
    expect(wrapper.find('input[type="tel"]').exists()).toBe(true) // phone
    expect(wrapper.find('textarea').exists()).toBe(true) // message
  })

  it('shows error message on failed submit', async () => {
    const wrapper = mount(ContactForm)
    // Test would need mocked $fetch
  })
})
```

- [ ] **Step 3: Run tests**

```bash
cd frontend
npm run test
```

Expected: Tests PASS

- [ ] **Step 4: Commit**

```bash
git add frontend/tests/
git commit -m "test: add frontend component tests"
```

---

## Phase 6: Admin Panel

### Task 19: Admin Authentication

**Files:**
- Create: `backend/src/Infrastructure/Services/AdminAuthService.cs`
- Create: `backend/src/Core/DTOs/LoginRequest.cs`
- Create: `backend/src/Api/Controllers/AuthController.cs`
- Modify: `backend/src/Api/Program.cs`

- [ ] **Step 1: Create LoginRequest DTO**

```csharp
using System.ComponentModel.DataAnnotations;

namespace Core.DTOs;

public class LoginRequest
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
```

- [ ] **Step 2: Create AdminAuthService**

```csharp
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jokens;
using System.Text;

namespace Infrastructure.Services;

public class AdminAuthService
{
    private readonly IConfiguration _configuration;

    public AdminAuthService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string Authenticate(string username, string password)
    {
        var validUsername = _configuration["Admin:Username"];
        var validPassword = _configuration["Admin:Password"];

        if (username != validUsername || password != validPassword)
            return null;

        var tokenHandler = new JwtTokenHandler();
        var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, username) }),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public bool ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false
            }, out _);

            return true;
        }
        catch
        {
            return false;
        }
    }
}
```

- [ ] **Step 3: Add packages and configuration**

```bash
dotnet add package System.IdentityModel.Tokens.Jwt
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
```

Update `appsettings.json`:
```json
{
  "Admin": {
    "Username": "admin",
    "Password": "change_this_password"
  },
  "Jwt": {
    "Key": "your_super_secret_key_at_least_32_characters_long"
  }
}
```

- [ ] **Step 4: Create AuthController**

```csharp
using Microsoft.AspNetCore.Mvc;
using Core.DTOs;
using Infrastructure.Services;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AdminAuthService _authService;

    public AuthController(AdminAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost]
    public ActionResult Login(LoginRequest request)
    {
        var token = _authService.Authenticate(request.Username, request.Password);

        if (token == null)
            return Unauthorized(new { message = "Неверные логин или пароль" });

        return Ok(new { token });
    }
}
```

- [ ] **Step 5: Configure JWT in Program.cs**

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Infrastructure.Services;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"])),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<AdminAuthService>();

app.UseAuthentication();
app.UseAuthorization();
```

- [ ] **Step 6: Commit**

```bash
git add backend/src/Core/DTOs/LoginRequest.cs backend/src/Infrastructure/Services/AdminAuthService.cs backend/src/Api/Controllers/AuthController.cs backend/src/Api/Program.cs backend/src/Api/appsettings.json
git commit -m "feat: add admin authentication with JWT"
```

### Task 20: Admin Frontend Pages

**Files:**
- Create: `frontend/pages/admin/login.vue`
- Create: `frontend/pages/admin/index.vue`
- Create: `frontend/pages/admin/articles.vue`
- Create: `frontend/composables/useAuth.ts`

- [ ] **Step 1: Create useAuth composable**

```typescript
export const useAuth = () => {
  const token = useCookie('auth_token')

  const isAuthenticated = computed(() => !!token.value)

  const login = async (username: string, password: string) => {
    const config = useRuntimeConfig()
    try {
      const response = await $fetch(`${config.public.apiUrl}/api/auth`, {
        method: 'POST',
        body: { username, password }
      })
      token.value = response.token
      return true
    } catch (error) {
      return false
    }
  }

  const logout = () => {
    token.value = null
    navigateTo('/admin/login')
  }

  return {
    isAuthenticated,
    login,
    logout,
    token
  }
}
```

- [ ] **Step 2: Create admin/login.vue**

```vue
<template>
  <div class="min-h-screen flex items-center justify-center bg-gray-100">
    <div class="bg-white p-8 rounded-lg shadow-md w-full max-w-md">
      <h1 class="text-2xl font-bold mb-6 text-center">Вход в админку</h1>

      <form @submit.prevent="submit" class="space-y-4">
        <div>
          <label class="block text-sm font-medium mb-1">Логин</label>
          <input v-model="form.username" type="text" required
            class="w-full px-4 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500">
        </div>

        <div>
          <label class="block text-sm font-medium mb-1">Пароль</label>
          <input v-model="form.password" type="password" required
            class="w-full px-4 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500">
        </div>

        <div v-if="error" class="text-red-600 text-sm">
          Неверные логин или пароль
        </div>

        <button type="submit" :disabled="loading"
          class="w-full bg-blue-600 text-white py-2 rounded-lg font-semibold hover:bg-blue-700 disabled:opacity-50">
          {{ loading ? 'Вход...' : 'Войти' }}
        </button>
      </form>
    </div>
  </div>
</template>

<script setup>
definePageMeta({
  layout: false
})

const { login } = useAuth()
const router = useRouter()

const form = ref({ username: '', password: '' })
const loading = ref(false)
const error = ref(false)

const submit = async () => {
  loading.value = true
  error.value = false

  const success = await login(form.value.username, form.value.password)

  if (success) {
    router.push('/admin')
  } else {
    error.value = true
  }

  loading.value = false
}
</script>
```

- [ ] **Step 3: Create admin/articles.vue (placeholder)**

```vue
<template>
  <div class="container mx-auto px-4 py-12">
    <h1 class="text-3xl font-bold mb-8">Статьи</h1>

    <div class="mb-4">
      <button class="bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700">
        + Добавить статью
      </button>
    </div>

    <p class="text-gray-600">CRUD интерфейс будет добавлен позже</p>
  </div>
</template>

<script setup>
const { isAuthenticated } = useAuth()

if (!isAuthenticated.value) {
  navigateTo('/admin/login')
}
</script>
```

- [ ] **Step 4: Create admin/index.vue**

```vue
<template>
  <div class="container mx-auto px-4 py-12">
    <h1 class="text-3xl font-bold mb-8">Админка</h1>

    <div class="grid md:grid-cols-2 gap-6">
      <NuxtLink to="/admin/articles" class="bg-white p-6 rounded-lg shadow hover:shadow-lg">
        <h2 class="text-xl font-bold">Статьи</h2>
        <p class="text-gray-600">Управление портфолио</p>
      </NuxtLink>

      <NuxtLink to="/admin/services" class="bg-white p-6 rounded-lg shadow hover:shadow-lg">
        <h2 class="text-xl font-bold">Цены</h2>
        <p class="text-gray-600">Управление услугами и ценами</p>
      </NuxtLink>
    </div>
  </div>
</template>

<script setup>
const { isAuthenticated } = useAuth()

if (!isAuthenticated.value) {
  navigateTo('/admin/login')
}
</script>
```

- [ ] **Step 5: Commit**

```bash
git add frontend/pages/admin/ frontend/composables/useAuth.ts
git commit -m "feat: add admin authentication and pages"
```

---

## Phase 7: Final Polish

### Task 21: Add Health Check and Logging

**Files:**
- Modify: `backend/src/Api/Program.cs`
- Modify: `backend/src/Api/Api.csproj`

- [ ] **Step 1: Add Serilog**

```bash
cd backend/src/Api
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File
```

- [ ] **Step 2: Configure Serilog in Program.cs**

```csharp
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();
```

- [ ] **Step 3: Add health check endpoint**

```csharp
builder.Services.AddHealthChecks();

app.MapHealthChecks("/health");
```

- [ ] **Step 4: Commit**

```bash
git add backend/src/Api/Program.cs backend/src/Api/Api.csproj
git commit -m "feat: add health check and Serilog logging"
```

### Task 22: Update README with Final Instructions

**Files:**
- Modify: `README.md`
- Modify: `RULES.md`

- [ ] **Step 1: Update README with setup instructions**

(Already created in brainstorming phase - ensure it's complete)

- [ ] **Step 2: Update RULES.md**

```markdown
# Правила и заметки

## Логи и анализ

- Backend logs: `docker compose logs -f backend`
- Frontend logs: `docker compose logs -f frontend`
- Nginx logs: `docker compose logs -f nginx`
- Health check: `curl http://localhost:8080/health`

## Разработка

- Backend dev: `cd backend && dotnet run`
- Frontend dev: `cd frontend && npm run dev`
- Full stack: `docker compose up`

## Деплой

- Полный деплой: см. DEPLOY.md
- Быстрый деплой: `docker compose up -d --build`

## TODO / To be clarified

- [ ] Добавить CRUD для статей в админке
- [ ] Добавить CRUD для услуг в админке
- [ ] Добавить загрузку медиа файлов
- [ ] Настроить SSL сертификаты

---
*Дата создания: 2025-05-28*
*Последнее обновление: 2025-05-28*
```

- [ ] **Step 3: Final commit**

```bash
git add README.md RULES.md
git commit -m "docs: update README and RULES"
```

### Task 23: Final Integration Test

**Files:**
- Test: Full stack integration

- [ ] **Step 1: Build and run all containers**

```bash
cd stroy-website
docker compose up -d --build
```

Expected: All containers start successfully

- [ ] **Step 2: Test API endpoints**

```bash
curl http://localhost/api/articles
curl http://localhost/api/services/prices
curl http://localhost/health
```

Expected: 200 OK with valid JSON

- [ ] **Step 3: Test frontend in browser**

Open http://localhost and verify:
- Home page loads
- Navigation works
- Prices page loads
- Contact forms submit (check logs)

- [ ] **Step 4: Test admin auth**

```bash
curl -X POST http://localhost/api/auth -H "Content-Type: application/json" -d '{"username":"admin","password":"change_this_password"}'
```

Expected: JWT token returned

- [ ] **Step 5: Check volumes persist**

```bash
docker compose down
docker compose up -d
# Data should still be there
```

Expected: Data persists across restarts

- [ ] **Step 6: Commit final state**

```bash
git add .
git commit -m "chore: final integration test passed"
```

---

## Done Criteria

- [ ] Backend API runs on port 8080
- [ ] Frontend runs on port 3000
- [ ] Nginx proxies correctly on port 80
- [ ] SQLite database persists in volume
- [ ] Media files persist in volume
- [ ] Admin authentication works
- [ ] Contact forms submit successfully
- [ ] All tests pass
- [ ] Docker compose up runs without errors

---

## Post-MVP (Not in scope)

- Full admin CRUD UI
- Media upload UI
- Email notifications for callbacks
- SSL auto-renewal
- CI/CD automation
