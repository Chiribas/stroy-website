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
