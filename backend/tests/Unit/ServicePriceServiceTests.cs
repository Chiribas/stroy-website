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
    public async Task Create_Then_GetAll_MapsNewFields()
    {
        using var db = NewDb();
        var svc = new ServicePriceService(db);
        await svc.CreateAsync(new CreateServicePriceDto {
            Title="Замена фундамента 4×5", Description="сваи+швеллеры", Price=340000,
            Duration="2 дня", ArticleSlug="zamena-fundamenta-svai", Tag="foundation", SortOrder=1 });

        var all = await svc.GetAllAsync();

        Assert.Single(all);
        Assert.Equal(340000, all[0].Price);
        Assert.Equal("2 дня", all[0].Duration);
    }

    [Fact]
    public async Task Create_ThenGetAll_ReturnsItem()
    {
        using var db = NewDb();
        var sut = new ServicePriceService(db);
        var created = await sut.CreateAsync(new CreateServicePriceDto { Title = "N", Price = 100 });

        var all = await sut.GetAllAsync();

        Assert.Single(all);
        Assert.Equal("N", all[0].Title);
        Assert.True(created.Id > 0);
    }

    [Fact]
    public async Task Update_ChangesFields()
    {
        using var db = NewDb();
        var sut = new ServicePriceService(db);
        var created = await sut.CreateAsync(new CreateServicePriceDto { Title = "N", Price = 100 });

        var updated = await sut.UpdateAsync(created.Id, new UpdateServicePriceDto { Title = "N2", Price = 200 });

        Assert.NotNull(updated);
        Assert.Equal("N2", updated!.Title);
        Assert.Equal(200, updated.Price);
    }

    [Fact]
    public async Task Delete_RemovesItem()
    {
        using var db = NewDb();
        var sut = new ServicePriceService(db);
        var created = await sut.CreateAsync(new CreateServicePriceDto { Title = "N", Price = 100 });

        Assert.True(await sut.DeleteAsync(created.Id));
        Assert.Empty(await sut.GetAllAsync());
        Assert.False(await sut.DeleteAsync(9999));
    }
}
