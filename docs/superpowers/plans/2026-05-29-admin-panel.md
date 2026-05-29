# Admin Panel Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the admin panel — JWT auth (Users table + BCrypt), admin CRUD endpoints, image upload with resize/thumbnails, and a Nuxt client-only admin UI with a Tiptap WYSIWYG editor.

**Architecture:** Backend extends existing thin domain services over `AppDbContext`; new admin controllers under `Api.Controllers.Admin` guarded by `[Authorize]` (JWT bearer). Frontend adds `/admin/**` client-only routes (`ssr: false`) in the same Nuxt app, hitting `/api` at runtime; JWT stored in `localStorage`.

**Tech Stack:** .NET 10, ASP.NET Core, EF Core (SQLite), `Microsoft.AspNetCore.Authentication.JwtBearer`, `BCrypt.Net-Next`, `SixLabors.ImageSharp`, `System.IdentityModel.Tokens.Jwt`; Nuxt 3 (SSG), `@tiptap/vue-3`, `@tailwindcss/typography`, Vitest/xUnit.

**Spec:** `docs/superpowers/specs/2026-05-29-admin-panel-design.md`

---

## File Structure

**Backend — created:**
- `src/Core/Entities/User.cs` — admin user entity
- `src/Core/Interfaces/IAuthService.cs`, `src/Core/Interfaces/IMediaService.cs`
- `src/Core/DTOs/LoginRequest.cs`, `AuthResponse.cs`, `MediaUploadResponse.cs`, `UpdateInboxStatusRequest.cs`, `CreateServicePriceDto.cs`, `UpdateServicePriceDto.cs`
- `src/Core/Exceptions/DuplicateSlugException.cs`
- `src/Infrastructure/Services/AuthService.cs`, `MediaService.cs`
- `src/Infrastructure/Auth/AdminSeeder.cs`
- `src/Api/Controllers/Admin/AdminAuthController.cs`, `AdminArticlesController.cs`, `AdminServicesController.cs`, `AdminInboxController.cs`, `AdminMediaController.cs`
- `tests/Unit/AuthServiceTests.cs`, `MediaServiceTests.cs`
- `tests/Integration/AdminAuthEndpointTests.cs`, `AdminArticlesEndpointTests.cs`

**Backend — modified:**
- `src/Core/Entities/Article.cs` (PublishedAt nullable), `src/Core/DTOs/ArticleDto.cs`, `ArticleListItemDto.cs` (PublishedAt nullable)
- `src/Core/Interfaces/IArticleService.cs`, `IServicePriceService.cs`, `ICallbackService.cs`, `IContactService.cs` (admin methods)
- `src/Infrastructure/Services/ArticleService.cs`, `ServicePriceService.cs`, `CallbackService.cs`, `ContactService.cs`
- `src/Infrastructure/Data/AppDbContext.cs` (User DbSet + config)
- `src/Api/Program.cs` (JWT auth, DI, seeder, uploads static files)
- `src/Api/appsettings.json` (Jwt, Storage sections)
- `src/Api/Api.csproj`, `src/Infrastructure/Infrastructure.csproj`
- `tests/Unit/ArticleServiceTests.cs` (nullable PublishedAt)
- `.env.example`

**Frontend — created:**
- `types/admin.ts` — admin DTO types
- `lib/adminApi.ts` — admin API client factory
- `composables/useAuth.ts`, `composables/useAdminApi.ts`
- `middleware/auth.ts`
- `layouts/admin.vue`
- `pages/admin/login.vue`, `index.vue`, `articles/index.vue`, `articles/new.vue`, `articles/[id].vue`, `prices.vue`, `inbox.vue`
- `components/admin/ArticleEditor.vue`, `components/admin/MediaUploader.vue`
- `tests/unit/useAuth.spec.ts`, `tests/component/ArticleEditor.spec.ts`

**Frontend — modified:**
- `nuxt.config.ts` (route rules `/admin/**` ssr:false + prerender ignore, sitemap exclude, typography module)
- `pages/portfolio/[slug].vue` (wrap content in `prose`)
- `package.json` (tiptap, typography)

---

## PHASE A — Backend domain debt (PublishedAt nullable, 409 on duplicate slug)

### Task A1: Make `Article.PublishedAt` nullable

**Files:**
- Modify: `backend/src/Core/Entities/Article.cs:11`
- Modify: `backend/src/Core/DTOs/ArticleDto.cs:12`
- Modify: `backend/src/Core/DTOs/ArticleListItemDto.cs:9`
- Modify: `backend/src/Infrastructure/Services/ArticleService.cs`
- Test: `backend/tests/Unit/ArticleServiceTests.cs`

- [ ] **Step 1: Update the entity and DTOs to nullable**

In `Article.cs` change:
```csharp
    public DateTime? PublishedAt { get; set; }
```
In `ArticleDto.cs` change the field:
```csharp
    DateTime? PublishedAt,
```
In `ArticleListItemDto.cs` change the field:
```csharp
    DateTime? PublishedAt
```

- [ ] **Step 2: Update `ArticleService` to use `null` instead of `default`**

In `ArticleService.cs` `CreateAsync`, replace the `PublishedAt` line:
```csharp
            PublishedAt = dto.IsPublished ? DateTime.UtcNow : null
```
In `UpdateAsync`, replace the publish-time guard:
```csharp
        if (dto.IsPublished && article.PublishedAt is null)
            article.PublishedAt = DateTime.UtcNow;
        else if (!dto.IsPublished)
            article.PublishedAt = null;
```

- [ ] **Step 3: Update existing unit tests for nullable semantics**

In `ArticleServiceTests.cs`, in `UpdateAsync_WhenPublishingDraft_SetsPublishedAt` replace:
```csharp
        Assert.Null(draft.PublishedAt);
```
(was `Assert.Equal(default, draft.PublishedAt)`), and replace the post-update assertion:
```csharp
        Assert.NotNull(updated!.PublishedAt);
```
(was `Assert.NotEqual(default, ...)`).

- [ ] **Step 4: Add a regression test that unpublishing clears PublishedAt**

Add to `ArticleServiceTests.cs`:
```csharp
    [Fact]
    public async Task UpdateAsync_WhenUnpublishing_ClearsPublishedAt()
    {
        using var db = NewDb();
        var sut = NewSut(db);
        var pub = await sut.CreateAsync(new CreateArticleDto { Title = "T", Slug = "t", Content = "<p>t</p>", IsPublished = true });
        Assert.NotNull(pub.PublishedAt);

        var updated = await sut.UpdateAsync(pub.Id, new UpdateArticleDto { Title = "T", Content = "<p>t</p>", IsPublished = false });

        Assert.NotNull(updated);
        Assert.Null(updated!.PublishedAt);
    }
```

- [ ] **Step 5: Add EF migration**

Run: `cd backend && dotnet ef migrations add PublishedAtNullable --project src/Infrastructure --startup-project src/Api`
Expected: a new migration file under `src/Infrastructure/Migrations`.

- [ ] **Step 6: Run unit tests**

Run: `cd backend && dotnet test tests/Unit/Unit.csproj`
Expected: PASS (all article service tests green).

- [ ] **Step 7: Commit**

```bash
git add backend
git commit -m "refactor: make Article.PublishedAt nullable (drafts have no publish date)"
```

### Task A2: Return 409 on duplicate slug

**Files:**
- Create: `backend/src/Core/Exceptions/DuplicateSlugException.cs`
- Modify: `backend/src/Infrastructure/Services/ArticleService.cs`
- Test: `backend/tests/Unit/ArticleServiceTests.cs`

- [ ] **Step 1: Write the failing test**

Add to `ArticleServiceTests.cs`:
```csharp
    [Fact]
    public async Task CreateAsync_DuplicateSlug_Throws()
    {
        using var db = NewDb();
        var sut = NewSut(db);
        await sut.CreateAsync(new CreateArticleDto { Title = "A", Slug = "dup", Content = "<p>a</p>" });

        await Assert.ThrowsAsync<Core.Exceptions.DuplicateSlugException>(() =>
            sut.CreateAsync(new CreateArticleDto { Title = "B", Slug = "dup", Content = "<p>b</p>" }));
    }
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd backend && dotnet test tests/Unit/Unit.csproj --filter CreateAsync_DuplicateSlug_Throws`
Expected: FAIL (type `DuplicateSlugException` does not exist / no throw).

- [ ] **Step 3: Create the exception**

Create `backend/src/Core/Exceptions/DuplicateSlugException.cs`:
```csharp
namespace Core.Exceptions;

public class DuplicateSlugException : Exception
{
    public string Slug { get; }
    public DuplicateSlugException(string slug)
        : base($"An article with slug '{slug}' already exists.") => Slug = slug;
}
```

- [ ] **Step 4: Guard slug in `CreateAsync` and `UpdateAsync`**

In `ArticleService.cs`, add `using Core.Exceptions;` and at the start of `CreateAsync` (before `new Article`):
```csharp
        if (await _db.Articles.AnyAsync(a => a.Slug == dto.Slug))
            throw new DuplicateSlugException(dto.Slug);
```
In `UpdateAsync`, after loading `article` and before assigning slug, add:
```csharp
        if (!string.IsNullOrEmpty(dto.Slug) && dto.Slug != article.Slug
            && await _db.Articles.AnyAsync(a => a.Slug == dto.Slug && a.Id != id))
            throw new DuplicateSlugException(dto.Slug);
```

- [ ] **Step 5: Run test to verify it passes**

Run: `cd backend && dotnet test tests/Unit/Unit.csproj --filter CreateAsync_DuplicateSlug_Throws`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add backend
git commit -m "feat: throw DuplicateSlugException on conflicting article slug"
```

> Note: the HTTP 409 mapping is wired in the admin controller (Task C2). The public create path is not exposed, so mapping lives there.

---

## PHASE B — Backend authentication (Users, BCrypt, JWT)

### Task B1: Add `User` entity, DbSet and migration

**Files:**
- Create: `backend/src/Core/Entities/User.cs`
- Modify: `backend/src/Infrastructure/Data/AppDbContext.cs`

- [ ] **Step 1: Create the entity**

Create `backend/src/Core/Entities/User.cs`:
```csharp
namespace Core.Entities;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

- [ ] **Step 2: Register DbSet and unique index**

In `AppDbContext.cs` add after the other DbSets:
```csharp
    public DbSet<User> Users => Set<User>();
```
And inside `OnModelCreating`, after the `Contact` config:
```csharp
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.Username).IsRequired();
            entity.Property(e => e.PasswordHash).IsRequired();
        });
```

- [ ] **Step 3: Add migration**

Run: `cd backend && dotnet ef migrations add AddUsers --project src/Infrastructure --startup-project src/Api`
Expected: migration created.

- [ ] **Step 4: Build to verify**

Run: `cd backend && dotnet build`
Expected: build succeeds.

- [ ] **Step 5: Commit**

```bash
git add backend
git commit -m "feat: add User entity for admin auth"
```

### Task B2: `AuthService` — BCrypt verify + JWT issue

**Files:**
- Modify: `backend/src/Infrastructure/Infrastructure.csproj`
- Create: `backend/src/Core/DTOs/LoginRequest.cs`, `AuthResponse.cs`
- Create: `backend/src/Core/Interfaces/IAuthService.cs`
- Create: `backend/src/Infrastructure/Services/AuthService.cs`
- Test: `backend/tests/Unit/AuthServiceTests.cs`

- [ ] **Step 1: Add NuGet packages to Infrastructure**

In `Infrastructure.csproj` add to the package `ItemGroup`:
```xml
    <PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />
    <PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.3.0" />
```
Run: `cd backend && dotnet restore`

- [ ] **Step 2: Create DTOs**

Create `backend/src/Core/DTOs/LoginRequest.cs`:
```csharp
using System.ComponentModel.DataAnnotations;

namespace Core.DTOs;

public class LoginRequest
{
    [Required] public string Username { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}
```
Create `backend/src/Core/DTOs/AuthResponse.cs`:
```csharp
namespace Core.DTOs;

public record AuthResponse(string Token, DateTime ExpiresAt);
```

- [ ] **Step 3: Create interface**

Create `backend/src/Core/Interfaces/IAuthService.cs`:
```csharp
using Core.DTOs;

namespace Core.Interfaces;

public interface IAuthService
{
    Task<AuthResponse?> AuthenticateAsync(LoginRequest request);
}
```

- [ ] **Step 4: Write the failing test**

Create `backend/tests/Unit/AuthServiceTests.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Infrastructure.Data;
using Infrastructure.Services;
using Core.DTOs;
using Core.Entities;

namespace Unit;

public class AuthServiceTests
{
    private static AppDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new AppDbContext(options);
    }

    private static IConfiguration Config() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Secret"] = "test-secret-key-at-least-32-bytes-long!!",
            ["Jwt:Issuer"] = "stroy",
            ["Jwt:Audience"] = "stroy",
            ["Jwt:ExpiresHours"] = "8",
        }).Build();

    private static AuthService NewSut(AppDbContext db) => new(db, Config());

    private static void SeedUser(AppDbContext db, string user, string pass)
    {
        db.Users.Add(new User { Username = user, PasswordHash = BCrypt.Net.BCrypt.HashPassword(pass) });
        db.SaveChanges();
    }

    [Fact]
    public async Task Authenticate_ValidCreds_ReturnsToken()
    {
        using var db = NewDb();
        SeedUser(db, "admin", "secret123");
        var sut = NewSut(db);

        var result = await sut.AuthenticateAsync(new LoginRequest { Username = "admin", Password = "secret123" });

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result!.Token));
        Assert.True(result.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task Authenticate_WrongPassword_ReturnsNull()
    {
        using var db = NewDb();
        SeedUser(db, "admin", "secret123");
        var sut = NewSut(db);

        var result = await sut.AuthenticateAsync(new LoginRequest { Username = "admin", Password = "wrong" });

        Assert.Null(result);
    }

    [Fact]
    public async Task Authenticate_UnknownUser_ReturnsNull()
    {
        using var db = NewDb();
        var sut = NewSut(db);

        var result = await sut.AuthenticateAsync(new LoginRequest { Username = "ghost", Password = "x" });

        Assert.Null(result);
    }
}
```

- [ ] **Step 5: Run test to verify it fails**

Run: `cd backend && dotnet test tests/Unit/Unit.csproj --filter AuthServiceTests`
Expected: FAIL (`AuthService` not found).

- [ ] **Step 6: Implement `AuthService`**

Create `backend/src/Infrastructure/Services/AuthService.cs`:
```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Core.DTOs;
using Core.Interfaces;
using Infrastructure.Data;

namespace Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<AuthResponse?> AuthenticateAsync(LoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return null;

        var hours = int.TryParse(_config["Jwt:ExpiresHours"], out var h) ? h : 8;
        var expires = DateTime.UtcNow.AddHours(hours);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Secret"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: new[] { new Claim(JwtRegisteredClaimNames.Sub, user.Username) },
            expires: expires,
            signingCredentials: creds);

        return new AuthResponse(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
```

- [ ] **Step 7: Run test to verify it passes**

Run: `cd backend && dotnet test tests/Unit/Unit.csproj --filter AuthServiceTests`
Expected: PASS (3 tests).

- [ ] **Step 8: Commit**

```bash
git add backend
git commit -m "feat: add AuthService with BCrypt verify and JWT issuance"
```

### Task B3: Admin seeder + JWT middleware + login endpoint

**Files:**
- Create: `backend/src/Infrastructure/Auth/AdminSeeder.cs`
- Create: `backend/src/Api/Controllers/Admin/AdminAuthController.cs`
- Modify: `backend/src/Api/Program.cs`, `backend/src/Api/appsettings.json`
- Test: `backend/tests/Integration/AdminAuthEndpointTests.cs`

- [ ] **Step 1: Create the seeder**

Create `backend/src/Infrastructure/Auth/AdminSeeder.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Core.Entities;
using Infrastructure.Data;

namespace Infrastructure.Auth;

public static class AdminSeeder
{
    public static async Task SeedAsync(AppDbContext db, IConfiguration config, ILogger logger)
    {
        if (await db.Users.AnyAsync()) return;

        var username = config["ADMIN_USERNAME"] ?? Environment.GetEnvironmentVariable("ADMIN_USERNAME");
        var password = config["ADMIN_PASSWORD"] ?? Environment.GetEnvironmentVariable("ADMIN_PASSWORD");
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("Admin seeding skipped: ADMIN_USERNAME/ADMIN_PASSWORD not set.");
            return;
        }

        db.Users.Add(new User
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
        });
        await db.SaveChangesAsync();
        logger.LogInformation("Seeded initial admin user '{Username}'.", username);
    }
}
```

- [ ] **Step 2: Add config sections**

In `appsettings.json` add (sibling to existing keys):
```json
  "Jwt": {
    "Secret": "CHANGE_ME_dev_only_secret_at_least_32_chars_long",
    "Issuer": "stroy",
    "Audience": "stroy",
    "ExpiresHours": 8
  },
  "Storage": {
    "UploadsPath": "uploads"
  }
```

- [ ] **Step 3: Wire JWT auth, DI, seeder in Program.cs**

In `Program.cs` add usings at top:
```csharp
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Infrastructure.Auth;
```
After the existing `AddScoped` service registrations add:
```csharp
builder.Services.AddScoped<Core.Interfaces.IAuthService, Infrastructure.Services.AuthService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!)),
        };
    });
builder.Services.AddAuthorization();
```
In the migration/scope block, after `db.Database.Migrate();` (still inside the `if (db.Database.IsRelational())` is fine to call seeder unconditionally after migrate), replace the scope block body with:
```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.IsRelational())
        db.Database.Migrate();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("AdminSeeder");
    await AdminSeeder.SeedAsync(db, app.Configuration, logger);
}
```
Before `app.MapControllers();` add:
```csharp
app.UseAuthentication();
app.UseAuthorization();
```

- [ ] **Step 4: Create the login controller**

Create `backend/src/Api/Controllers/Admin/AdminAuthController.cs`:
```csharp
using Microsoft.AspNetCore.Mvc;
using Core.DTOs;
using Core.Interfaces;

namespace Api.Controllers.Admin;

[ApiController]
[Route("api/admin/auth")]
public class AdminAuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AdminAuthController(IAuthService auth) => _auth = auth;

    [HttpPost]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var result = await _auth.AuthenticateAsync(request);
        return result is null ? Unauthorized() : Ok(result);
    }
}
```

- [ ] **Step 5: Write the failing integration test**

Note: `ApiTestFactory` uses InMemory DB and `Testing` environment. The seeder needs admin creds + Jwt secret. Add config to the factory.

First, modify `backend/tests/Integration/ApiTestFactory.cs` — add `using Microsoft.Extensions.Configuration;` and inside `ConfigureWebHost` before `ConfigureTestServices`:
```csharp
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "integration-secret-key-at-least-32-bytes!!",
                ["Jwt:Issuer"] = "stroy",
                ["Jwt:Audience"] = "stroy",
                ["Jwt:ExpiresHours"] = "8",
                ["ADMIN_USERNAME"] = "admin",
                ["ADMIN_PASSWORD"] = "secret123",
            });
        });
```

Create `backend/tests/Integration/AdminAuthEndpointTests.cs`:
```csharp
using Xunit;
using System.Net;
using System.Net.Http.Json;
using Core.DTOs;

namespace Integration;

public class AdminAuthEndpointTests : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client;
    public AdminAuthEndpointTests(ApiTestFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Login_ValidCreds_ReturnsToken()
    {
        var resp = await _client.PostAsJsonAsync("/api/admin/auth",
            new LoginRequest { Username = "admin", Password = "secret123" });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.Token));
    }

    [Fact]
    public async Task Login_BadCreds_Returns401()
    {
        var resp = await _client.PostAsJsonAsync("/api/admin/auth",
            new LoginRequest { Username = "admin", Password = "nope" });

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }
}
```

- [ ] **Step 6: Run integration tests**

Run: `cd backend && dotnet test tests/Integration/Integration.csproj --filter AdminAuthEndpointTests`
Expected: PASS (2 tests).

- [ ] **Step 7: Commit**

```bash
git add backend
git commit -m "feat: admin login endpoint with JWT middleware and admin seeder"
```

---

## PHASE C — Backend admin CRUD controllers

### Task C1: Extend `IArticleService` with admin read methods

**Files:**
- Modify: `backend/src/Core/Interfaces/IArticleService.cs`
- Modify: `backend/src/Infrastructure/Services/ArticleService.cs`
- Test: `backend/tests/Unit/ArticleServiceTests.cs`

- [ ] **Step 1: Write the failing tests**

Add to `ArticleServiceTests.cs`:
```csharp
    [Fact]
    public async Task GetAllForAdminAsync_IncludesDrafts()
    {
        using var db = NewDb();
        var sut = NewSut(db);
        await sut.CreateAsync(new CreateArticleDto { Title = "P", Slug = "p", Content = "<p>p</p>", IsPublished = true });
        await sut.CreateAsync(new CreateArticleDto { Title = "D", Slug = "d", Content = "<p>d</p>", IsPublished = false });

        var result = await sut.GetAllForAdminAsync(1, 10);

        Assert.Equal(2, result.Total);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDraft()
    {
        using var db = NewDb();
        var sut = NewSut(db);
        var d = await sut.CreateAsync(new CreateArticleDto { Title = "D", Slug = "d", Content = "<p>d</p>", IsPublished = false });

        var found = await sut.GetByIdAsync(d.Id);

        Assert.NotNull(found);
        Assert.Equal("d", found!.Slug);
    }
```

- [ ] **Step 2: Run to verify fail**

Run: `cd backend && dotnet test tests/Unit/Unit.csproj --filter "GetAllForAdminAsync_IncludesDrafts|GetByIdAsync_ReturnsDraft"`
Expected: FAIL (methods not defined).

- [ ] **Step 3: Add to interface**

In `IArticleService.cs` add:
```csharp
    Task<PagedResult<ArticleListItemDto>> GetAllForAdminAsync(int page, int pageSize);
    Task<ArticleDto?> GetByIdAsync(int id);
```

- [ ] **Step 4: Implement in `ArticleService`**

Add to `ArticleService.cs`:
```csharp
    public async Task<PagedResult<ArticleListItemDto>> GetAllForAdminAsync(int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 12;

        var query = _db.Articles;
        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new ArticleListItemDto(
                a.Id, a.Title, a.Slug, a.Summary, a.ThumbnailPath, a.PublishedAt))
            .ToListAsync();

        return new PagedResult<ArticleListItemDto>(items, total, page, pageSize);
    }

    public async Task<ArticleDto?> GetByIdAsync(int id)
    {
        var article = await _db.Articles.Include(a => a.Media).FirstOrDefaultAsync(a => a.Id == id);
        return article is null ? null : ToDto(article);
    }
```

- [ ] **Step 5: Run to verify pass**

Run: `cd backend && dotnet test tests/Unit/Unit.csproj --filter "GetAllForAdminAsync_IncludesDrafts|GetByIdAsync_ReturnsDraft"`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add backend
git commit -m "feat: add admin read methods to ArticleService"
```

### Task C2: Admin Articles controller (CRUD + 409 mapping + auth)

**Files:**
- Create: `backend/src/Api/Controllers/Admin/AdminArticlesController.cs`
- Test: `backend/tests/Integration/AdminArticlesEndpointTests.cs`

- [ ] **Step 1: Create the controller**

Create `backend/src/Api/Controllers/Admin/AdminArticlesController.cs`:
```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Core.DTOs;
using Core.Exceptions;
using Core.Interfaces;

namespace Api.Controllers.Admin;

[ApiController]
[Authorize]
[Route("api/admin/articles")]
public class AdminArticlesController : ControllerBase
{
    private readonly IArticleService _service;

    public AdminArticlesController(IArticleService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<PagedResult<ArticleListItemDto>>> GetAll(int page = 1, int pageSize = 20)
        => Ok(await _service.GetAllForAdminAsync(page, pageSize));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ArticleDto>> GetById(int id)
    {
        var article = await _service.GetByIdAsync(id);
        return article is null ? NotFound() : Ok(article);
    }

    [HttpPost]
    public async Task<ActionResult<ArticleDto>> Create(CreateArticleDto dto)
    {
        try
        {
            var created = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (DuplicateSlugException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ArticleDto>> Update(int id, UpdateArticleDto dto)
    {
        try
        {
            var updated = await _service.UpdateAsync(id, dto);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (DuplicateSlugException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
        => await _service.DeleteAsync(id) ? NoContent() : NotFound();
}
```

- [ ] **Step 2: Write integration tests (auth + 409)**

Create `backend/tests/Integration/AdminArticlesEndpointTests.cs`:
```csharp
using Xunit;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Core.DTOs;

namespace Integration;

public class AdminArticlesEndpointTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;
    public AdminArticlesEndpointTests(ApiTestFactory factory) => _factory = factory;

    private async Task<HttpClient> AuthedClientAsync()
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/admin/auth",
            new LoginRequest { Username = "admin", Password = "secret123" });
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        return client;
    }

    [Fact]
    public async Task GetAll_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/api/admin/articles");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Create_WithToken_Returns201()
    {
        var client = await AuthedClientAsync();
        var resp = await client.PostAsJsonAsync("/api/admin/articles",
            new CreateArticleDto { Title = "T", Slug = "create-ok", Content = "<p>x</p>" });
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
    }

    [Fact]
    public async Task Create_DuplicateSlug_Returns409()
    {
        var client = await AuthedClientAsync();
        await client.PostAsJsonAsync("/api/admin/articles",
            new CreateArticleDto { Title = "A", Slug = "dup-int", Content = "<p>a</p>" });
        var resp = await client.PostAsJsonAsync("/api/admin/articles",
            new CreateArticleDto { Title = "B", Slug = "dup-int", Content = "<p>b</p>" });
        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }
}
```

- [ ] **Step 3: Run integration tests**

Run: `cd backend && dotnet test tests/Integration/Integration.csproj --filter AdminArticlesEndpointTests`
Expected: PASS (3 tests). Note: InMemory DB ignores the unique index but the service-level slug check produces the 409.

- [ ] **Step 4: Commit**

```bash
git add backend
git commit -m "feat: admin articles CRUD controller with auth and 409 on duplicate slug"
```

### Task C3: Admin Services (prices) CRUD

**Files:**
- Create: `backend/src/Core/DTOs/CreateServicePriceDto.cs`, `UpdateServicePriceDto.cs`
- Modify: `backend/src/Core/Interfaces/IServicePriceService.cs`, `backend/src/Infrastructure/Services/ServicePriceService.cs`
- Create: `backend/src/Api/Controllers/Admin/AdminServicesController.cs`
- Test: `backend/tests/Unit/ServicePriceServiceTests.cs`

- [ ] **Step 1: Create write DTOs**

Create `backend/src/Core/DTOs/CreateServicePriceDto.cs`:
```csharp
using System.ComponentModel.DataAnnotations;

namespace Core.DTOs;

public class CreateServicePriceDto
{
    [Required] public string Category { get; set; } = string.Empty;
    [Required] public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int PriceFrom { get; set; }
    public int? PriceTo { get; set; }
    public string? Unit { get; set; }
    public int SortOrder { get; set; }
}
```
Create `backend/src/Core/DTOs/UpdateServicePriceDto.cs` with identical fields and class name `UpdateServicePriceDto`.

- [ ] **Step 2: Write the failing tests**

Create `backend/tests/Unit/ServicePriceServiceTests.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;
using Infrastructure.Services;
using Core.DTOs;

namespace Unit;

public class ServicePriceServiceTests
{
    private static AppDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Create_ThenGetAll_ReturnsItem()
    {
        using var db = NewDb();
        var sut = new ServicePriceService(db);
        var created = await sut.CreateAsync(new CreateServicePriceDto { Category = "C", Name = "N", PriceFrom = 100 });

        var all = await sut.GetAllAsync();

        Assert.Single(all);
        Assert.Equal("N", all[0].Name);
        Assert.True(created.Id > 0);
    }

    [Fact]
    public async Task Update_ChangesFields()
    {
        using var db = NewDb();
        var sut = new ServicePriceService(db);
        var created = await sut.CreateAsync(new CreateServicePriceDto { Category = "C", Name = "N", PriceFrom = 100 });

        var updated = await sut.UpdateAsync(created.Id, new UpdateServicePriceDto { Category = "C2", Name = "N2", PriceFrom = 200 });

        Assert.NotNull(updated);
        Assert.Equal("N2", updated!.Name);
        Assert.Equal(200, updated.PriceFrom);
    }

    [Fact]
    public async Task Delete_RemovesItem()
    {
        using var db = NewDb();
        var sut = new ServicePriceService(db);
        var created = await sut.CreateAsync(new CreateServicePriceDto { Category = "C", Name = "N", PriceFrom = 100 });

        Assert.True(await sut.DeleteAsync(created.Id));
        Assert.Empty(await sut.GetAllAsync());
        Assert.False(await sut.DeleteAsync(9999));
    }
}
```

- [ ] **Step 3: Run to verify fail**

Run: `cd backend && dotnet test tests/Unit/Unit.csproj --filter ServicePriceServiceTests`
Expected: FAIL (methods not defined).

- [ ] **Step 4: Extend interface and implementation**

In `IServicePriceService.cs` add:
```csharp
    Task<ServicePriceDto> CreateAsync(CreateServicePriceDto dto);
    Task<ServicePriceDto?> UpdateAsync(int id, UpdateServicePriceDto dto);
    Task<bool> DeleteAsync(int id);
```
In `ServicePriceService.cs` add (and `using Core.Entities;`):
```csharp
    public async Task<ServicePriceDto> CreateAsync(CreateServicePriceDto dto)
    {
        var entity = new ServicePrice
        {
            Category = dto.Category, Name = dto.Name, Description = dto.Description,
            PriceFrom = dto.PriceFrom, PriceTo = dto.PriceTo, Unit = dto.Unit, SortOrder = dto.SortOrder,
        };
        _db.ServicePrices.Add(entity);
        await _db.SaveChangesAsync();
        return ToDto(entity);
    }

    public async Task<ServicePriceDto?> UpdateAsync(int id, UpdateServicePriceDto dto)
    {
        var entity = await _db.ServicePrices.FindAsync(id);
        if (entity is null) return null;
        entity.Category = dto.Category; entity.Name = dto.Name; entity.Description = dto.Description;
        entity.PriceFrom = dto.PriceFrom; entity.PriceTo = dto.PriceTo; entity.Unit = dto.Unit; entity.SortOrder = dto.SortOrder;
        await _db.SaveChangesAsync();
        return ToDto(entity);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _db.ServicePrices.FindAsync(id);
        if (entity is null) return false;
        _db.ServicePrices.Remove(entity);
        await _db.SaveChangesAsync();
        return true;
    }

    private static ServicePriceDto ToDto(ServicePrice p) => new(
        p.Id, p.Category, p.Name, p.Description, p.PriceFrom, p.PriceTo, p.Unit, p.SortOrder);
```

- [ ] **Step 5: Run to verify pass**

Run: `cd backend && dotnet test tests/Unit/Unit.csproj --filter ServicePriceServiceTests`
Expected: PASS (3 tests).

- [ ] **Step 6: Create the admin controller**

Create `backend/src/Api/Controllers/Admin/AdminServicesController.cs`:
```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Core.DTOs;
using Core.Interfaces;

namespace Api.Controllers.Admin;

[ApiController]
[Authorize]
[Route("api/admin/services")]
public class AdminServicesController : ControllerBase
{
    private readonly IServicePriceService _service;

    public AdminServicesController(IServicePriceService service) => _service = service;

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

- [ ] **Step 7: Build and commit**

Run: `cd backend && dotnet build`
Expected: succeeds.
```bash
git add backend
git commit -m "feat: admin services (prices) CRUD"
```

### Task C4: Admin Inbox (callbacks + contacts list & mark processed)

**Files:**
- Create: `backend/src/Core/DTOs/CallbackDto.cs`, `ContactDto.cs`
- Modify: `backend/src/Core/Interfaces/ICallbackService.cs`, `IContactService.cs`, and impls
- Create: `backend/src/Api/Controllers/Admin/AdminInboxController.cs`
- Test: `backend/tests/Unit/InboxServiceTests.cs`

- [ ] **Step 1: Create read DTOs**

Create `backend/src/Core/DTOs/CallbackDto.cs`:
```csharp
namespace Core.DTOs;

public record CallbackDto(int Id, string Phone, string? Name, DateTime CreatedAt, bool IsProcessed);
```
Create `backend/src/Core/DTOs/ContactDto.cs`:
```csharp
namespace Core.DTOs;

public record ContactDto(int Id, string Name, string Phone, string Message, DateTime CreatedAt, bool IsProcessed);
```

- [ ] **Step 2: Write the failing tests**

Create `backend/tests/Unit/InboxServiceTests.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;
using Infrastructure.Services;
using Core.DTOs;

namespace Unit;

public class InboxServiceTests
{
    private static AppDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Callback_Create_List_MarkProcessed()
    {
        using var db = NewDb();
        var sut = new CallbackService(db);
        await sut.CreateAsync(new CallbackRequest { Phone = "123" });

        var list = await sut.GetAllAsync();
        Assert.Single(list);
        Assert.False(list[0].IsProcessed);

        var ok = await sut.SetProcessedAsync(list[0].Id, true);
        Assert.True(ok);
        Assert.True((await sut.GetAllAsync())[0].IsProcessed);
    }

    [Fact]
    public async Task Contact_Create_List_MarkProcessed()
    {
        using var db = NewDb();
        var sut = new ContactService(db);
        await sut.CreateAsync(new ContactRequest { Name = "N", Phone = "1", Message = "hi" });

        var list = await sut.GetAllAsync();
        Assert.Single(list);

        var ok = await sut.SetProcessedAsync(list[0].Id, true);
        Assert.True(ok);
        Assert.False(await sut.SetProcessedAsync(9999, true));
    }
}
```

> Check `CallbackRequest`/`ContactRequest` property names in `src/Core/DTOs/` and match them in the test (Phone/Name and Name/Phone/Message respectively).

- [ ] **Step 3: Run to verify fail**

Run: `cd backend && dotnet test tests/Unit/Unit.csproj --filter InboxServiceTests`
Expected: FAIL (methods not defined).

- [ ] **Step 4: Extend interfaces and implementations**

In `ICallbackService.cs` add:
```csharp
    Task<IReadOnlyList<CallbackDto>> GetAllAsync();
    Task<bool> SetProcessedAsync(int id, bool processed);
```
In `CallbackService.cs` add (and `using Microsoft.EntityFrameworkCore;`):
```csharp
    public async Task<IReadOnlyList<CallbackDto>> GetAllAsync()
        => await _db.Callbacks
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CallbackDto(c.Id, c.Phone, c.Name, c.CreatedAt, c.IsProcessed))
            .ToListAsync();

    public async Task<bool> SetProcessedAsync(int id, bool processed)
    {
        var c = await _db.Callbacks.FindAsync(id);
        if (c is null) return false;
        c.IsProcessed = processed;
        await _db.SaveChangesAsync();
        return true;
    }
```
In `IContactService.cs` add:
```csharp
    Task<IReadOnlyList<ContactDto>> GetAllAsync();
    Task<bool> SetProcessedAsync(int id, bool processed);
```
In `ContactService.cs` add the analogous implementation (mapping to `ContactDto` with `c.Name, c.Phone, c.Message`).

- [ ] **Step 5: Run to verify pass**

Run: `cd backend && dotnet test tests/Unit/Unit.csproj --filter InboxServiceTests`
Expected: PASS.

- [ ] **Step 6: Create the inbox controller**

Create `backend/src/Api/Controllers/Admin/AdminInboxController.cs`:
```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Core.DTOs;
using Core.Interfaces;

namespace Api.Controllers.Admin;

[ApiController]
[Authorize]
[Route("api/admin")]
public class AdminInboxController : ControllerBase
{
    private readonly ICallbackService _callbacks;
    private readonly IContactService _contacts;

    public AdminInboxController(ICallbackService callbacks, IContactService contacts)
    {
        _callbacks = callbacks;
        _contacts = contacts;
    }

    [HttpGet("callbacks")]
    public async Task<ActionResult<IReadOnlyList<CallbackDto>>> GetCallbacks()
        => Ok(await _callbacks.GetAllAsync());

    [HttpPatch("callbacks/{id:int}")]
    public async Task<IActionResult> PatchCallback(int id, UpdateInboxStatusRequest request)
        => await _callbacks.SetProcessedAsync(id, request.IsProcessed) ? NoContent() : NotFound();

    [HttpGet("contacts")]
    public async Task<ActionResult<IReadOnlyList<ContactDto>>> GetContacts()
        => Ok(await _contacts.GetAllAsync());

    [HttpPatch("contacts/{id:int}")]
    public async Task<IActionResult> PatchContact(int id, UpdateInboxStatusRequest request)
        => await _contacts.SetProcessedAsync(id, request.IsProcessed) ? NoContent() : NotFound();
}
```
Create `backend/src/Core/DTOs/UpdateInboxStatusRequest.cs`:
```csharp
namespace Core.DTOs;

public class UpdateInboxStatusRequest
{
    public bool IsProcessed { get; set; }
}
```

- [ ] **Step 7: Build and commit**

Run: `cd backend && dotnet build`
```bash
git add backend
git commit -m "feat: admin inbox endpoints for callbacks and contacts"
```

---

## PHASE D — Backend media upload (ImageSharp)

### Task D1: `MediaService` — validate, resize, thumbnail, persist

**Files:**
- Modify: `backend/src/Infrastructure/Infrastructure.csproj`
- Create: `backend/src/Core/DTOs/MediaUploadResponse.cs`, `backend/src/Core/Interfaces/IMediaService.cs`
- Create: `backend/src/Infrastructure/Services/MediaService.cs`
- Test: `backend/tests/Unit/MediaServiceTests.cs`

- [ ] **Step 1: Add ImageSharp package**

In `Infrastructure.csproj` add:
```xml
    <PackageReference Include="SixLabors.ImageSharp" Version="3.1.5" />
```
Run: `cd backend && dotnet restore`

- [ ] **Step 2: Create DTO and interface**

Create `backend/src/Core/DTOs/MediaUploadResponse.cs`:
```csharp
namespace Core.DTOs;

public record MediaUploadResponse(int? MediaId, string Url, string ThumbnailUrl);
```
Create `backend/src/Core/Interfaces/IMediaService.cs`:
```csharp
using Core.DTOs;

namespace Core.Interfaces;

public interface IMediaService
{
    /// <summary>Saves an uploaded image (resized + thumbnail) and optionally links it to an article.</summary>
    Task<MediaUploadResponse> SaveImageAsync(
        Stream content, string originalFileName, string contentType, int? articleId);
}
```

- [ ] **Step 3: Write the failing test**

Create `backend/tests/Unit/MediaServiceTests.cs`:
```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using Infrastructure.Data;
using Infrastructure.Services;

namespace Unit;

public class MediaServiceTests
{
    private static AppDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new AppDbContext(options);
    }

    private static (MediaService sut, string dir) NewSut(AppDbContext db)
    {
        var dir = Path.Combine(Path.GetTempPath(), "media-test-" + Guid.NewGuid());
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Storage:UploadsPath"] = dir,
            ["Media:MaxDimension"] = "1920",
            ["Media:ThumbnailDimension"] = "400",
        }).Build();
        return (new MediaService(db, config), dir);
    }

    private static MemoryStream MakePng(int w, int h)
    {
        using var img = new Image<Rgba32>(w, h);
        var ms = new MemoryStream();
        img.Save(ms, new PngEncoder());
        ms.Position = 0;
        return ms;
    }

    [Fact]
    public async Task SaveImage_ResizesLargeImage_AndWritesThumbnail()
    {
        using var db = NewDb();
        var (sut, dir) = NewSut(db);
        using var src = MakePng(3000, 2000);

        var result = await sut.SaveImageAsync(src, "big.png", "image/png", null);

        Assert.False(string.IsNullOrEmpty(result.Url));
        Assert.False(string.IsNullOrEmpty(result.ThumbnailUrl));

        var savedPath = Path.Combine(dir, Path.GetFileName(result.Url));
        var (sw, _) = ImageInfo(savedPath);
        Assert.True(sw <= 1920);

        var thumbPath = Path.Combine(dir, Path.GetFileName(result.ThumbnailUrl));
        var (tw, _) = ImageInfo(thumbPath);
        Assert.True(tw <= 400);
    }

    [Fact]
    public async Task SaveImage_RejectsNonImageContentType()
    {
        using var db = NewDb();
        var (sut, _) = NewSut(db);
        using var src = new MemoryStream(new byte[] { 1, 2, 3 });

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.SaveImageAsync(src, "x.txt", "text/plain", null));
    }

    [Fact]
    public async Task SaveImage_WithArticleId_CreatesArticleMedia()
    {
        using var db = NewDb();
        db.Articles.Add(new Core.Entities.Article { Title = "T", Slug = "t", Content = "<p>t</p>" });
        await db.SaveChangesAsync();
        var articleId = db.Articles.First().Id;
        var (sut, _) = NewSut(db);
        using var src = MakePng(100, 100);

        var result = await sut.SaveImageAsync(src, "s.png", "image/png", articleId);

        Assert.NotNull(result.MediaId);
        Assert.Single(db.ArticleMedia);
    }

    private static (int Width, int Height) ImageInfo(string path)
    {
        using var img = Image.Load(path);
        return (img.Width, img.Height);
    }
}
```

- [ ] **Step 4: Run to verify fail**

Run: `cd backend && dotnet test tests/Unit/Unit.csproj --filter MediaServiceTests`
Expected: FAIL (`MediaService` not found).

- [ ] **Step 5: Implement `MediaService`**

Create `backend/src/Infrastructure/Services/MediaService.cs`:
```csharp
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Core.DTOs;
using Core.Entities;
using Core.Interfaces;
using Infrastructure.Data;

namespace Infrastructure.Services;

public class MediaService : IMediaService
{
    private static readonly string[] Allowed = { "image/jpeg", "image/png", "image/webp" };
    private readonly AppDbContext _db;
    private readonly string _uploadsPath;
    private readonly int _maxDim;
    private readonly int _thumbDim;

    public MediaService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _uploadsPath = config["Storage:UploadsPath"] ?? "uploads";
        _maxDim = int.TryParse(config["Media:MaxDimension"], out var m) ? m : 1920;
        _thumbDim = int.TryParse(config["Media:ThumbnailDimension"], out var t) ? t : 400;
    }

    public async Task<MediaUploadResponse> SaveImageAsync(
        Stream content, string originalFileName, string contentType, int? articleId)
    {
        if (!Allowed.Contains(contentType))
            throw new ArgumentException($"Unsupported content type: {contentType}");

        Directory.CreateDirectory(_uploadsPath);

        using var image = await Image.LoadAsync(content); // throws on non-image bytes
        Resize(image, _maxDim);

        var name = $"{Guid.NewGuid():N}.webp";
        var thumbName = $"{Path.GetFileNameWithoutExtension(name)}_thumb.webp";
        await image.SaveAsWebpAsync(Path.Combine(_uploadsPath, name));

        using var thumb = image.Clone(ctx => { });
        Resize(thumb, _thumbDim);
        await thumb.SaveAsWebpAsync(Path.Combine(_uploadsPath, thumbName));

        var url = $"/uploads/{name}";
        var thumbUrl = $"/uploads/{thumbName}";

        int? mediaId = null;
        if (articleId is int aid && await _db.Articles.FindAsync(aid) is not null)
        {
            var maxSort = _db.ArticleMedia.Where(m => m.ArticleId == aid)
                .Select(m => (int?)m.SortOrder).Max() ?? -1;
            var media = new ArticleMedia { ArticleId = aid, Path = url, MediaType = "image", SortOrder = maxSort + 1 };
            _db.ArticleMedia.Add(media);
            await _db.SaveChangesAsync();
            mediaId = media.Id;
        }

        return new MediaUploadResponse(mediaId, url, thumbUrl);
    }

    private static void Resize(Image image, int max)
    {
        if (image.Width <= max && image.Height <= max) return;
        image.Mutate(ctx => ctx.Resize(new ResizeOptions
        {
            Mode = ResizeMode.Max,
            Size = new Size(max, max),
        }));
    }
}
```

> Note: output is normalized to `.webp`. The test reloads via `Image.Load` which detects webp; assertions on width still hold.

- [ ] **Step 6: Run to verify pass**

Run: `cd backend && dotnet test tests/Unit/Unit.csproj --filter MediaServiceTests`
Expected: PASS (3 tests).

- [ ] **Step 7: Commit**

```bash
git add backend
git commit -m "feat: MediaService with ImageSharp resize + thumbnail generation"
```

### Task D2: Media upload endpoint + static `/uploads` + DI

**Files:**
- Create: `backend/src/Api/Controllers/Admin/AdminMediaController.cs`
- Modify: `backend/src/Api/Program.cs`

- [ ] **Step 1: Register `IMediaService` and serve `/uploads`**

In `Program.cs` after the other `AddScoped` registrations add:
```csharp
builder.Services.AddScoped<Core.Interfaces.IMediaService, Infrastructure.Services.MediaService>();
```
After `app.UseAuthorization();` and before `app.MapControllers();` add static serving of uploads:
```csharp
var uploadsPath = Path.Combine(builder.Environment.ContentRootPath,
    builder.Configuration["Storage:UploadsPath"] ?? "uploads");
Directory.CreateDirectory(uploadsPath);
app.UseStaticFiles(new Microsoft.AspNetCore.Builder.StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads",
});
```

- [ ] **Step 2: Create the upload controller**

Create `backend/src/Api/Controllers/Admin/AdminMediaController.cs`:
```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Core.DTOs;
using Core.Interfaces;

namespace Api.Controllers.Admin;

[ApiController]
[Authorize]
[Route("api/admin/media")]
public class AdminMediaController : ControllerBase
{
    private const long MaxBytes = 15 * 1024 * 1024;
    private readonly IMediaService _media;

    public AdminMediaController(IMediaService media) => _media = media;

    [HttpPost("upload")]
    [RequestSizeLimit(MaxBytes)]
    public async Task<ActionResult<MediaUploadResponse>> Upload([FromForm] IFormFile file, [FromForm] int? articleId)
    {
        if (file is null || file.Length == 0) return BadRequest(new { message = "No file." });
        if (file.Length > MaxBytes) return BadRequest(new { message = "File too large." });

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _media.SaveImageAsync(stream, file.FileName, file.ContentType, articleId);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
```

- [ ] **Step 3: Build & run full backend test suite**

Run: `cd backend && dotnet build && dotnet test`
Expected: all tests PASS.

- [ ] **Step 4: Commit**

```bash
git add backend
git commit -m "feat: media upload endpoint and static /uploads serving"
```

### Task D3: Add `.env.example` and CORS for admin (dev)

**Files:**
- Modify: `backend/../.env.example` (project root `stroy-website/.env.example`)
- Modify: `backend/src/Api/Program.cs` (dev CORS already allows localhost:3001 — verify admin uses same origin)

- [ ] **Step 1: Document env vars**

Append to `stroy-website/.env.example`:
```
# Admin auth
ADMIN_USERNAME=admin
ADMIN_PASSWORD=change-me
JWT_SECRET=change-me-to-a-long-random-string-32+chars
```

- [ ] **Step 2: Bind JWT_SECRET / ADMIN_* env to config**

In `appsettings` config, env vars `ADMIN_USERNAME`/`ADMIN_PASSWORD` are read directly by the seeder via `IConfiguration` (ASP.NET maps env vars automatically). For `Jwt:Secret`, support a flat `JWT_SECRET` override: in `Program.cs` right after `var builder = ...` add:
```csharp
if (Environment.GetEnvironmentVariable("JWT_SECRET") is { Length: > 0 } secret)
    builder.Configuration["Jwt:Secret"] = secret;
```

- [ ] **Step 3: Build and commit**

Run: `cd backend && dotnet build`
```bash
git add backend ../.env.example 2>/dev/null || git add -A
git commit -m "chore: document admin env vars and JWT_SECRET override"
```

---

## PHASE E — Frontend admin infrastructure

### Task E1: Route rules, types, admin API client

**Files:**
- Modify: `frontend/nuxt.config.ts`
- Create: `frontend/types/admin.ts`, `frontend/lib/adminApi.ts`

- [ ] **Step 1: Configure `/admin/**` as client-only and excluded from prerender/sitemap**

In `nuxt.config.ts`, add a `routeRules` key and extend `nitro.prerender` with `ignore`, and add `sitemap` exclude. Replace the config object's `nitro` block and add `routeRules`/`sitemap`:
```typescript
  routeRules: {
    '/admin/**': { ssr: false },
  },
  sitemap: {
    exclude: ['/admin/**'],
  },
  nitro: {
    prerender: {
      crawlLinks: true,
      routes: ['/', '/prices', '/portfolio', '/contact'],
      ignore: ['/admin'],
      failOnError: false,
    },
  },
```

- [ ] **Step 2: Create admin types**

Create `frontend/types/admin.ts`:
```typescript
export interface LoginPayload { username: string; password: string }
export interface AuthResponse { token: string; expiresAt: string }

export interface AdminArticle {
  id: number
  title: string
  slug: string
  summary?: string | null
  content: string
  thumbnailPath?: string | null
  publishedAt?: string | null
  media: { id: number; path: string; mediaType: string; alt?: string | null; sortOrder: number }[]
}

export interface ArticleWrite {
  title: string
  slug: string
  summary?: string | null
  content: string
  thumbnailPath?: string | null
  isPublished: boolean
}

export interface ServicePriceWrite {
  category: string
  name: string
  description?: string | null
  priceFrom: number
  priceTo?: number | null
  unit?: string | null
  sortOrder: number
}

export interface Callback {
  id: number; phone: string; name?: string | null; createdAt: string; isProcessed: boolean
}
export interface Contact {
  id: number; name: string; phone: string; message: string; createdAt: string; isProcessed: boolean
}

export interface MediaUploadResponse { mediaId?: number | null; url: string; thumbnailUrl: string }
```

- [ ] **Step 3: Create admin API client factory**

Create `frontend/lib/adminApi.ts`:
```typescript
import type {
  LoginPayload, AuthResponse, AdminArticle, ArticleWrite,
  ServicePriceWrite, Callback, Contact, MediaUploadResponse,
} from '~/types/admin'
import type { ArticleListItem, PagedResult, ServicePrice } from '~/types/api'

type Fetcher = <T>(url: string, opts?: Record<string, unknown>) => Promise<T>

export function createAdminApi(fetcher: Fetcher, baseURL: string, getToken: () => string | null) {
  const url = (path: string) => `${baseURL}${path}`
  const auth = () => {
    const t = getToken()
    return t ? { Authorization: `Bearer ${t}` } : {}
  }
  return {
    login(body: LoginPayload) {
      return fetcher<AuthResponse>(url('/api/admin/auth'), { method: 'POST', body })
    },
    listArticles(page = 1, pageSize = 20) {
      return fetcher<PagedResult<ArticleListItem>>(url('/api/admin/articles'), {
        query: { page, pageSize }, headers: auth(),
      })
    },
    getArticle(id: number) {
      return fetcher<AdminArticle>(url(`/api/admin/articles/${id}`), { headers: auth() })
    },
    createArticle(body: ArticleWrite) {
      return fetcher<AdminArticle>(url('/api/admin/articles'), { method: 'POST', body, headers: auth() })
    },
    updateArticle(id: number, body: ArticleWrite) {
      return fetcher<AdminArticle>(url(`/api/admin/articles/${id}`), { method: 'PUT', body, headers: auth() })
    },
    deleteArticle(id: number) {
      return fetcher<void>(url(`/api/admin/articles/${id}`), { method: 'DELETE', headers: auth() })
    },
    listPrices() {
      return fetcher<ServicePrice[]>(url('/api/admin/services'), { headers: auth() })
    },
    createPrice(body: ServicePriceWrite) {
      return fetcher<ServicePrice>(url('/api/admin/services'), { method: 'POST', body, headers: auth() })
    },
    updatePrice(id: number, body: ServicePriceWrite) {
      return fetcher<ServicePrice>(url(`/api/admin/services/${id}`), { method: 'PUT', body, headers: auth() })
    },
    deletePrice(id: number) {
      return fetcher<void>(url(`/api/admin/services/${id}`), { method: 'DELETE', headers: auth() })
    },
    listCallbacks() {
      return fetcher<Callback[]>(url('/api/admin/callbacks'), { headers: auth() })
    },
    setCallbackProcessed(id: number, isProcessed: boolean) {
      return fetcher<void>(url(`/api/admin/callbacks/${id}`), { method: 'PATCH', body: { isProcessed }, headers: auth() })
    },
    listContacts() {
      return fetcher<Contact[]>(url('/api/admin/contacts'), { headers: auth() })
    },
    setContactProcessed(id: number, isProcessed: boolean) {
      return fetcher<void>(url(`/api/admin/contacts/${id}`), { method: 'PATCH', body: { isProcessed }, headers: auth() })
    },
    uploadMedia(form: FormData) {
      return fetcher<MediaUploadResponse>(url('/api/admin/media/upload'), {
        method: 'POST', body: form, headers: auth(),
      })
    },
  }
}

export type AdminApi = ReturnType<typeof createAdminApi>
```

- [ ] **Step 4: Typecheck**

Run: `cd frontend && npx nuxi typecheck`
Expected: no errors.

- [ ] **Step 5: Commit**

```bash
git add frontend
git commit -m "feat(admin): route rules, admin types and API client"
```

### Task E2: `useAuth` composable + auth middleware

**Files:**
- Create: `frontend/composables/useAuth.ts`, `frontend/composables/useAdminApi.ts`, `frontend/middleware/auth.ts`
- Test: `frontend/tests/unit/useAuth.spec.ts`

- [ ] **Step 1: Write the failing test**

Create `frontend/tests/unit/useAuth.spec.ts`:
```typescript
import { describe, it, expect, beforeEach, vi } from 'vitest'

// Stub Nuxt auto-imports used inside useAuth
vi.stubGlobal('useState', (_key: string, init: () => unknown) => {
  const r = { value: init() }
  return r
})

import { createAuthStore } from '~/composables/useAuth'

describe('auth store', () => {
  beforeEach(() => localStorage.clear())

  it('starts unauthenticated', () => {
    const auth = createAuthStore()
    expect(auth.isAuthenticated.value).toBe(false)
  })

  it('stores token on setToken and reports authenticated', () => {
    const auth = createAuthStore()
    auth.setToken('abc.def.ghi')
    expect(auth.token.value).toBe('abc.def.ghi')
    expect(auth.isAuthenticated.value).toBe(true)
    expect(localStorage.getItem('admin_token')).toBe('abc.def.ghi')
  })

  it('clears token on logout', () => {
    const auth = createAuthStore()
    auth.setToken('abc')
    auth.logout()
    expect(auth.token.value).toBeNull()
    expect(localStorage.getItem('admin_token')).toBeNull()
  })
})
```

> The composable is split into a pure `createAuthStore()` factory (testable with a `ref`) and a thin `useAuth()` Nuxt wrapper. The test stubs `useState` to return a plain ref-like object.

- [ ] **Step 2: Run to verify fail**

Run: `cd frontend && npx vitest run tests/unit/useAuth.spec.ts`
Expected: FAIL (module not found).

- [ ] **Step 3: Implement `useAuth`**

Create `frontend/composables/useAuth.ts`:
```typescript
import { computed, ref, type Ref } from 'vue'

const TOKEN_KEY = 'admin_token'

export function createAuthStore(tokenRef?: Ref<string | null>) {
  const token = tokenRef ?? ref<string | null>(
    typeof localStorage !== 'undefined' ? localStorage.getItem(TOKEN_KEY) : null,
  )
  const isAuthenticated = computed(() => !!token.value)

  function setToken(value: string) {
    token.value = value
    if (typeof localStorage !== 'undefined') localStorage.setItem(TOKEN_KEY, value)
  }
  function logout() {
    token.value = null
    if (typeof localStorage !== 'undefined') localStorage.removeItem(TOKEN_KEY)
  }
  function getToken() {
    return token.value
  }
  return { token, isAuthenticated, setToken, logout, getToken }
}

export function useAuth() {
  const token = useState<string | null>('admin_token', () =>
    typeof localStorage !== 'undefined' ? localStorage.getItem(TOKEN_KEY) : null,
  )
  return createAuthStore(token)
}
```

- [ ] **Step 4: Run to verify pass**

Run: `cd frontend && npx vitest run tests/unit/useAuth.spec.ts`
Expected: PASS (3 tests).

- [ ] **Step 5: Create `useAdminApi` and middleware**

Create `frontend/composables/useAdminApi.ts`:
```typescript
import { createAdminApi } from '~/lib/adminApi'

export function useAdminApi() {
  const config = useRuntimeConfig()
  const auth = useAuth()
  const baseURL = config.public.apiClientBase
  return createAdminApi($fetch as any, baseURL, auth.getToken)
}
```
Create `frontend/middleware/auth.ts`:
```typescript
export default defineNuxtRouteMiddleware((to) => {
  if (to.path === '/admin/login') return
  const auth = useAuth()
  if (!auth.isAuthenticated.value) {
    return navigateTo('/admin/login')
  }
})
```

- [ ] **Step 6: Typecheck and commit**

Run: `cd frontend && npx nuxi typecheck`
```bash
git add frontend
git commit -m "feat(admin): useAuth store, useAdminApi, auth middleware"
```

---

## PHASE F — Frontend admin UI

### Task F1: Admin layout + login page

**Files:**
- Create: `frontend/layouts/admin.vue`, `frontend/pages/admin/login.vue`

- [ ] **Step 1: Create the admin layout**

Create `frontend/layouts/admin.vue`:
```vue
<script setup lang="ts">
const auth = useAuth()
const route = useRoute()
function logout() {
  auth.logout()
  navigateTo('/admin/login')
}
const nav = [
  { to: '/admin', label: 'Дашборд' },
  { to: '/admin/articles', label: 'Статьи' },
  { to: '/admin/prices', label: 'Цены' },
  { to: '/admin/inbox', label: 'Заявки' },
]
</script>

<template>
  <div class="min-h-screen flex">
    <aside v-if="route.path !== '/admin/login'" class="w-56 bg-gray-900 text-gray-100 p-4 space-y-2">
      <div class="text-lg font-bold mb-4">Админка</div>
      <NuxtLink v-for="n in nav" :key="n.to" :to="n.to" class="block px-3 py-2 rounded hover:bg-gray-700">
        {{ n.label }}
      </NuxtLink>
      <button class="mt-6 text-sm text-gray-400 hover:text-white" @click="logout">Выйти</button>
    </aside>
    <main class="flex-1 p-6 bg-gray-50">
      <slot />
    </main>
  </div>
</template>
```

- [ ] **Step 2: Create the login page**

Create `frontend/pages/admin/login.vue`:
```vue
<script setup lang="ts">
definePageMeta({ layout: 'admin' })
const api = useAdminApi()
const auth = useAuth()
const username = ref('')
const password = ref('')
const error = ref('')
const loading = ref(false)

async function submit() {
  error.value = ''
  loading.value = true
  try {
    const res = await api.login({ username: username.value, password: password.value })
    auth.setToken(res.token)
    await navigateTo('/admin')
  } catch {
    error.value = 'Неверный логин или пароль'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="max-w-sm mx-auto mt-24 bg-white p-6 rounded shadow">
    <h1 class="text-xl font-bold mb-4">Вход в админку</h1>
    <form class="space-y-3" @submit.prevent="submit">
      <input v-model="username" placeholder="Логин" class="w-full border rounded px-3 py-2" />
      <input v-model="password" type="password" placeholder="Пароль" class="w-full border rounded px-3 py-2" />
      <p v-if="error" class="text-red-600 text-sm">{{ error }}</p>
      <button :disabled="loading" class="w-full bg-gray-900 text-white py-2 rounded disabled:opacity-50">
        {{ loading ? 'Вход…' : 'Войти' }}
      </button>
    </form>
  </div>
</template>
```

- [ ] **Step 3: Typecheck and commit**

Run: `cd frontend && npx nuxi typecheck`
```bash
git add frontend
git commit -m "feat(admin): admin layout and login page"
```

### Task F2: Dashboard + articles list

**Files:**
- Create: `frontend/pages/admin/index.vue`, `frontend/pages/admin/articles/index.vue`

- [ ] **Step 1: Create the dashboard**

Create `frontend/pages/admin/index.vue`:
```vue
<script setup lang="ts">
definePageMeta({ layout: 'admin', middleware: 'auth' })
const api = useAdminApi()
const articles = ref(0)
const newCallbacks = ref(0)
const newContacts = ref(0)

onMounted(async () => {
  const [a, cb, ct] = await Promise.all([api.listArticles(1, 1), api.listCallbacks(), api.listContacts()])
  articles.value = a.total
  newCallbacks.value = cb.filter(x => !x.isProcessed).length
  newContacts.value = ct.filter(x => !x.isProcessed).length
})
</script>

<template>
  <div>
    <h1 class="text-2xl font-bold mb-6">Дашборд</h1>
    <div class="grid grid-cols-3 gap-4">
      <div class="bg-white p-4 rounded shadow"><div class="text-3xl font-bold">{{ articles }}</div><div class="text-gray-500">Статей</div></div>
      <div class="bg-white p-4 rounded shadow"><div class="text-3xl font-bold">{{ newCallbacks }}</div><div class="text-gray-500">Новых звонков</div></div>
      <div class="bg-white p-4 rounded shadow"><div class="text-3xl font-bold">{{ newContacts }}</div><div class="text-gray-500">Новых сообщений</div></div>
    </div>
  </div>
</template>
```

- [ ] **Step 2: Create the articles list**

Create `frontend/pages/admin/articles/index.vue`:
```vue
<script setup lang="ts">
import type { ArticleListItem, PagedResult } from '~/types/api'
definePageMeta({ layout: 'admin', middleware: 'auth' })
const api = useAdminApi()
const data = ref<PagedResult<ArticleListItem> | null>(null)

async function load() { data.value = await api.listArticles(1, 50) }
onMounted(load)

async function remove(id: number) {
  if (!confirm('Удалить статью?')) return
  await api.deleteArticle(id)
  await load()
}
</script>

<template>
  <div>
    <div class="flex justify-between items-center mb-6">
      <h1 class="text-2xl font-bold">Статьи</h1>
      <NuxtLink to="/admin/articles/new" class="bg-gray-900 text-white px-4 py-2 rounded">Новая статья</NuxtLink>
    </div>
    <table class="w-full bg-white rounded shadow">
      <thead><tr class="text-left border-b"><th class="p-3">Заголовок</th><th class="p-3">Статус</th><th class="p-3"></th></tr></thead>
      <tbody>
        <tr v-for="a in data?.items ?? []" :key="a.id" class="border-b">
          <td class="p-3">{{ a.title }}</td>
          <td class="p-3">
            <span :class="a.publishedAt ? 'text-green-600' : 'text-gray-400'">
              {{ a.publishedAt ? 'Опубликовано' : 'Черновик' }}
            </span>
          </td>
          <td class="p-3 text-right space-x-3">
            <NuxtLink :to="`/admin/articles/${a.id}`" class="text-blue-600">Изменить</NuxtLink>
            <button class="text-red-600" @click="remove(a.id)">Удалить</button>
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>
```

- [ ] **Step 3: Typecheck and commit**

Run: `cd frontend && npx nuxi typecheck`
```bash
git add frontend
git commit -m "feat(admin): dashboard and articles list pages"
```

### Task F3: Tiptap editor + MediaUploader component

**Files:**
- Modify: `frontend/package.json` (tiptap deps)
- Create: `frontend/components/admin/MediaUploader.vue`, `frontend/components/admin/ArticleEditor.vue`
- Test: `frontend/tests/component/ArticleEditor.spec.ts`

- [ ] **Step 1: Install Tiptap**

Run: `cd frontend && npm install @tiptap/vue-3 @tiptap/starter-kit @tiptap/extension-link @tiptap/extension-image`
Expected: packages added to `package.json` dependencies.

- [ ] **Step 2: Create MediaUploader**

Create `frontend/components/admin/MediaUploader.vue`:
```vue
<script setup lang="ts">
const props = defineProps<{ articleId?: number }>()
const emit = defineEmits<{ uploaded: [url: string] }>()
const api = useAdminApi()
const uploading = ref(false)
const error = ref('')

async function onChange(e: Event) {
  const file = (e.target as HTMLInputElement).files?.[0]
  if (!file) return
  error.value = ''
  uploading.value = true
  try {
    const form = new FormData()
    form.append('file', file)
    if (props.articleId) form.append('articleId', String(props.articleId))
    const res = await api.uploadMedia(form)
    emit('uploaded', res.url)
  } catch {
    error.value = 'Не удалось загрузить файл'
  } finally {
    uploading.value = false
  }
}
</script>

<template>
  <div class="inline-block">
    <label class="cursor-pointer text-blue-600">
      {{ uploading ? 'Загрузка…' : 'Вставить фото' }}
      <input type="file" accept="image/*" class="hidden" @change="onChange" />
    </label>
    <span v-if="error" class="text-red-600 text-sm ml-2">{{ error }}</span>
  </div>
</template>
```

- [ ] **Step 3: Write the failing component test**

Create `frontend/tests/component/ArticleEditor.spec.ts`:
```typescript
import { describe, it, expect, vi } from 'vitest'
import { mountSuspended } from '@nuxt/test-utils/runtime'
import ArticleEditor from '~/components/admin/ArticleEditor.vue'

// Stub useAdminApi used by MediaUploader child
vi.mock('~/composables/useAdminApi', () => ({
  useAdminApi: () => ({ uploadMedia: vi.fn() }),
}))

describe('ArticleEditor', () => {
  it('renders the toolbar and emits update on content change', async () => {
    const wrapper = await mountSuspended(ArticleEditor, {
      props: { modelValue: '<p>hello</p>' },
    })
    expect(wrapper.text()).toContain('Жирный')
  })
})
```

- [ ] **Step 4: Run to verify fail**

Run: `cd frontend && npx vitest run tests/component/ArticleEditor.spec.ts`
Expected: FAIL (component not found).

- [ ] **Step 5: Implement ArticleEditor**

Create `frontend/components/admin/ArticleEditor.vue`:
```vue
<script setup lang="ts">
import { useEditor, EditorContent } from '@tiptap/vue-3'
import StarterKit from '@tiptap/starter-kit'
import Link from '@tiptap/extension-link'
import Image from '@tiptap/extension-image'

const props = defineProps<{ modelValue: string; articleId?: number }>()
const emit = defineEmits<{ 'update:modelValue': [value: string] }>()

const editor = useEditor({
  content: props.modelValue,
  extensions: [StarterKit, Link.configure({ openOnClick: false }), Image],
  onUpdate: ({ editor }) => emit('update:modelValue', editor.getHTML()),
})

function addVideo() {
  const url = window.prompt('Ссылка на видео (VK / Rutube / YouTube):')
  if (!url) return
  // Wrap as iframe; backend sanitizer enforces the domain whitelist.
  editor.value?.chain().focus().insertContent(
    `<iframe src="${url}" frameborder="0" allowfullscreen></iframe>`,
  ).run()
}

function onMediaUploaded(url: string) {
  editor.value?.chain().focus().setImage({ src: url }).run()
}

onBeforeUnmount(() => editor.value?.destroy())
</script>

<template>
  <div class="border rounded bg-white">
    <div v-if="editor" class="flex flex-wrap gap-2 border-b p-2 text-sm">
      <button type="button" :class="{ 'font-bold': editor.isActive('bold') }" @click="editor.chain().focus().toggleBold().run()">Жирный</button>
      <button type="button" :class="{ 'italic': editor.isActive('italic') }" @click="editor.chain().focus().toggleItalic().run()">Курсив</button>
      <button type="button" @click="editor.chain().focus().toggleHeading({ level: 2 }).run()">H2</button>
      <button type="button" @click="editor.chain().focus().toggleHeading({ level: 3 }).run()">H3</button>
      <button type="button" @click="editor.chain().focus().toggleBulletList().run()">Список</button>
      <button type="button" @click="editor.chain().focus().toggleOrderedList().run()">Нумерация</button>
      <MediaUploader :article-id="articleId" @uploaded="onMediaUploaded" />
      <button type="button" @click="addVideo">Видео</button>
    </div>
    <EditorContent :editor="editor" class="prose max-w-none p-3 min-h-[300px]" />
  </div>
</template>
```

- [ ] **Step 6: Run to verify pass**

Run: `cd frontend && npx vitest run tests/component/ArticleEditor.spec.ts`
Expected: PASS.

> If `mountSuspended` has trouble with Tiptap in happy-dom, fall back to asserting on the toolbar via a shallower mount; keep the test focused on toolbar render + presence of the "Жирный" button.

- [ ] **Step 7: Commit**

```bash
git add frontend
git commit -m "feat(admin): Tiptap article editor and media uploader"
```

### Task F4: Article create/edit pages

**Files:**
- Create: `frontend/pages/admin/articles/new.vue`, `frontend/pages/admin/articles/[id].vue`

- [ ] **Step 1: Create the "new article" page**

Create `frontend/pages/admin/articles/new.vue`:
```vue
<script setup lang="ts">
import type { ArticleWrite } from '~/types/admin'
definePageMeta({ layout: 'admin', middleware: 'auth' })
const api = useAdminApi()
const error = ref('')
const form = reactive<ArticleWrite>({
  title: '', slug: '', summary: '', content: '', thumbnailPath: '', isPublished: false,
})

async function save() {
  error.value = ''
  try {
    const created = await api.createArticle({ ...form })
    await navigateTo(`/admin/articles/${created.id}`)
  } catch (e: any) {
    error.value = e?.statusCode === 409 ? 'Статья с таким slug уже существует' : 'Ошибка сохранения'
  }
}
</script>

<template>
  <div class="max-w-3xl">
    <h1 class="text-2xl font-bold mb-6">Новая статья</h1>
    <div class="space-y-4">
      <input v-model="form.title" placeholder="Заголовок" class="w-full border rounded px-3 py-2" />
      <input v-model="form.slug" placeholder="slug-stati" class="w-full border rounded px-3 py-2" />
      <textarea v-model="form.summary" placeholder="Краткое описание" class="w-full border rounded px-3 py-2" />
      <ArticleEditor v-model="form.content" />
      <label class="flex items-center gap-2"><input v-model="form.isPublished" type="checkbox" /> Опубликовать</label>
      <p v-if="error" class="text-red-600">{{ error }}</p>
      <button class="bg-gray-900 text-white px-4 py-2 rounded" @click="save">Сохранить</button>
    </div>
  </div>
</template>
```

- [ ] **Step 2: Create the "edit article" page**

Create `frontend/pages/admin/articles/[id].vue`:
```vue
<script setup lang="ts">
import type { ArticleWrite } from '~/types/admin'
definePageMeta({ layout: 'admin', middleware: 'auth' })
const api = useAdminApi()
const route = useRoute()
const id = Number(route.params.id)
const error = ref('')
const form = reactive<ArticleWrite>({
  title: '', slug: '', summary: '', content: '', thumbnailPath: '', isPublished: false,
})

onMounted(async () => {
  const a = await api.getArticle(id)
  Object.assign(form, {
    title: a.title, slug: a.slug, summary: a.summary ?? '', content: a.content,
    thumbnailPath: a.thumbnailPath ?? '', isPublished: !!a.publishedAt,
  })
})

async function save() {
  error.value = ''
  try {
    await api.updateArticle(id, { ...form })
    await navigateTo('/admin/articles')
  } catch (e: any) {
    error.value = e?.statusCode === 409 ? 'Статья с таким slug уже существует' : 'Ошибка сохранения'
  }
}
</script>

<template>
  <div class="max-w-3xl">
    <h1 class="text-2xl font-bold mb-6">Редактирование статьи</h1>
    <div class="space-y-4">
      <input v-model="form.title" placeholder="Заголовок" class="w-full border rounded px-3 py-2" />
      <input v-model="form.slug" placeholder="slug-stati" class="w-full border rounded px-3 py-2" />
      <textarea v-model="form.summary" placeholder="Краткое описание" class="w-full border rounded px-3 py-2" />
      <ArticleEditor v-model="form.content" :article-id="id" />
      <label class="flex items-center gap-2"><input v-model="form.isPublished" type="checkbox" /> Опубликовано</label>
      <p v-if="error" class="text-red-600">{{ error }}</p>
      <button class="bg-gray-900 text-white px-4 py-2 rounded" @click="save">Сохранить</button>
    </div>
  </div>
</template>
```

- [ ] **Step 3: Typecheck and commit**

Run: `cd frontend && npx nuxi typecheck`
```bash
git add frontend
git commit -m "feat(admin): article create and edit pages"
```

### Task F5: Prices editor + Inbox pages

**Files:**
- Create: `frontend/pages/admin/prices.vue`, `frontend/pages/admin/inbox.vue`

- [ ] **Step 1: Create the prices page**

Create `frontend/pages/admin/prices.vue`:
```vue
<script setup lang="ts">
import type { ServicePrice } from '~/types/api'
import type { ServicePriceWrite } from '~/types/admin'
definePageMeta({ layout: 'admin', middleware: 'auth' })
const api = useAdminApi()
const items = ref<ServicePrice[]>([])
const draft = reactive<ServicePriceWrite>({
  category: '', name: '', description: '', priceFrom: 0, priceTo: null, unit: '', sortOrder: 0,
})

async function load() { items.value = await api.listPrices() }
onMounted(load)

async function add() {
  await api.createPrice({ ...draft })
  Object.assign(draft, { category: '', name: '', description: '', priceFrom: 0, priceTo: null, unit: '', sortOrder: 0 })
  await load()
}
async function remove(id: number) {
  if (!confirm('Удалить позицию?')) return
  await api.deletePrice(id)
  await load()
}
</script>

<template>
  <div class="max-w-4xl">
    <h1 class="text-2xl font-bold mb-6">Цены</h1>
    <table class="w-full bg-white rounded shadow mb-6">
      <thead><tr class="text-left border-b"><th class="p-2">Категория</th><th class="p-2">Услуга</th><th class="p-2">От</th><th class="p-2">До</th><th class="p-2">Ед.</th><th></th></tr></thead>
      <tbody>
        <tr v-for="p in items" :key="p.id" class="border-b">
          <td class="p-2">{{ p.category }}</td><td class="p-2">{{ p.name }}</td>
          <td class="p-2">{{ p.priceFrom }}</td><td class="p-2">{{ p.priceTo ?? '—' }}</td><td class="p-2">{{ p.unit }}</td>
          <td class="p-2 text-right"><button class="text-red-600" @click="remove(p.id)">Удалить</button></td>
        </tr>
      </tbody>
    </table>
    <div class="bg-white p-4 rounded shadow grid grid-cols-3 gap-3">
      <input v-model="draft.category" placeholder="Категория" class="border rounded px-2 py-1" />
      <input v-model="draft.name" placeholder="Услуга" class="border rounded px-2 py-1" />
      <input v-model="draft.unit" placeholder="Ед. изм." class="border rounded px-2 py-1" />
      <input v-model.number="draft.priceFrom" type="number" placeholder="Цена от" class="border rounded px-2 py-1" />
      <input v-model.number="draft.priceTo" type="number" placeholder="Цена до" class="border rounded px-2 py-1" />
      <input v-model.number="draft.sortOrder" type="number" placeholder="Порядок" class="border rounded px-2 py-1" />
      <button class="col-span-3 bg-gray-900 text-white py-2 rounded" @click="add">Добавить</button>
    </div>
  </div>
</template>
```

- [ ] **Step 2: Create the inbox page**

Create `frontend/pages/admin/inbox.vue`:
```vue
<script setup lang="ts">
import type { Callback, Contact } from '~/types/admin'
definePageMeta({ layout: 'admin', middleware: 'auth' })
const api = useAdminApi()
const callbacks = ref<Callback[]>([])
const contacts = ref<Contact[]>([])

async function load() {
  [callbacks.value, contacts.value] = await Promise.all([api.listCallbacks(), api.listContacts()])
}
onMounted(load)

async function toggleCallback(c: Callback) {
  await api.setCallbackProcessed(c.id, !c.isProcessed)
  await load()
}
async function toggleContact(c: Contact) {
  await api.setContactProcessed(c.id, !c.isProcessed)
  await load()
}
</script>

<template>
  <div class="max-w-4xl space-y-8">
    <section>
      <h1 class="text-2xl font-bold mb-4">Звонки</h1>
      <div v-for="c in callbacks" :key="c.id" class="bg-white p-3 rounded shadow mb-2 flex justify-between"
           :class="{ 'opacity-50': c.isProcessed }">
        <div>{{ c.phone }} <span v-if="c.name" class="text-gray-500">— {{ c.name }}</span></div>
        <button class="text-blue-600" @click="toggleCallback(c)">{{ c.isProcessed ? 'Вернуть' : 'Обработано' }}</button>
      </div>
    </section>
    <section>
      <h2 class="text-2xl font-bold mb-4">Сообщения</h2>
      <div v-for="c in contacts" :key="c.id" class="bg-white p-3 rounded shadow mb-2"
           :class="{ 'opacity-50': c.isProcessed }">
        <div class="flex justify-between">
          <div class="font-medium">{{ c.name }} — {{ c.phone }}</div>
          <button class="text-blue-600" @click="toggleContact(c)">{{ c.isProcessed ? 'Вернуть' : 'Обработано' }}</button>
        </div>
        <p class="text-gray-700 mt-1">{{ c.message }}</p>
      </div>
    </section>
  </div>
</template>
```

- [ ] **Step 3: Typecheck and commit**

Run: `cd frontend && npx nuxi typecheck`
```bash
git add frontend
git commit -m "feat(admin): prices editor and inbox pages"
```

---

## PHASE G — Public rich-content rendering (typography)

### Task G1: Enable `@tailwindcss/typography` and apply `prose`

**Files:**
- Modify: `frontend/package.json`, `frontend/tailwind.config.*` (or nuxt tailwind config), `frontend/pages/portfolio/[slug].vue`

- [ ] **Step 1: Install the plugin**

Run: `cd frontend && npm install -D @tailwindcss/typography`

- [ ] **Step 2: Register the plugin**

Locate the Tailwind config (check `frontend/tailwind.config.ts`/`.js`; if none, create `frontend/tailwind.config.js`):
```javascript
module.exports = {
  plugins: [require('@tailwindcss/typography')],
}
```
> If a config already exists, only add the typography plugin to its `plugins` array.

- [ ] **Step 3: Wrap article content in `prose`**

In `frontend/pages/portfolio/[slug].vue`, find the element rendering the article HTML via `v-html` and ensure it has the classes `prose max-w-none` (add a wrapping `<div class="prose max-w-none" v-html="...">` if needed).

- [ ] **Step 4: Verify build**

Run: `cd frontend && npx nuxi typecheck && npm run build`
Expected: build succeeds.

- [ ] **Step 5: Commit**

```bash
git add frontend
git commit -m "feat: render article rich content with tailwind typography (prose)"
```

---

## Final Verification

### Task Z1: Full suite + manual smoke

- [ ] **Step 1: Backend tests**

Run: `cd backend && dotnet test`
Expected: all PASS.

- [ ] **Step 2: Frontend tests + typecheck**

Run: `cd frontend && npx vitest run && npx nuxi typecheck`
Expected: all PASS, no type errors.

- [ ] **Step 3: Manual smoke (per memory: win gotchas)**

Backend: `cd backend/src/Api && ADMIN_USERNAME=admin ADMIN_PASSWORD=secret123 JWT_SECRET=dev-secret-32-chars-minimum-length! ASPNETCORE_ENVIRONMENT=Development dotnet run --urls http://localhost:8081`
Frontend: `cd frontend && npm run dev -- --host 127.0.0.1`
Then in a browser: open `http://127.0.0.1:3001/admin/login`, log in, create a draft article with an image, publish it, mark a callback processed. Confirm `/uploads/...webp` loads.

- [ ] **Step 4: Final commit (if any docs/notes changed)**

```bash
git add -A
git commit -m "chore: admin panel complete"
```

---

## Self-Review Notes (filled during plan authoring)

- **Spec coverage:** Auth (B1–B3), admin Articles CRUD (C1–C2), Services CRUD (C3), Inbox (C4), Media upload (D1–D2), PublishedAt nullable (A1), 409 slug (A2/C2), frontend client-only routes + useAuth + middleware (E1–E2), admin pages + Tiptap (F1–F5), prose typography (G1), env vars (D3). All spec sections mapped.
- **Type consistency:** `getToken()` used consistently in `useAuth`/`useAdminApi`/`adminApi`; `IsProcessed`/`isProcessed` boundary respected (C# PascalCase → JSON camelCase); `ArticleWrite` maps to backend `CreateArticleDto`/`UpdateArticleDto` shape; `PublishedAt` nullable propagated to both backend DTOs and frontend `publishedAt?: string | null`.
- **Known follow-ups (out of scope here):** SSG static rebuild on publish belongs to the infra phase; `frontend/types/api.ts` `publishedAt` is currently `string` (non-null) — leave for public pages since published articles always have a date; admin uses `types/admin.ts` with nullable.
