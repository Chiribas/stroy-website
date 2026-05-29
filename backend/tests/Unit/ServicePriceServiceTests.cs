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
