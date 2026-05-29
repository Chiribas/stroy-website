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
