using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;
using Core.Entities;

namespace Unit;

public class ContentSeederTests
{
    private static AppDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Seed_IsIdempotent_NoDuplicateServices()
    {
        using var db = NewDb();

        await ContentSeeder.SeedAsync(db);
        await ContentSeeder.SeedAsync(db);

        Assert.Equal(5, await db.Services.CountAsync());
    }

    [Fact]
    public async Task Seed_SkipsPricesWhenNotEmpty()
    {
        using var db = NewDb();
        db.ServicePrices.Add(new ServicePrice { Title = "боевая", Price = 1 });
        await db.SaveChangesAsync();

        await ContentSeeder.SeedAsync(db);

        Assert.Equal(1, await db.ServicePrices.CountAsync());
    }
}
