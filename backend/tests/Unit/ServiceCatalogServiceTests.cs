using Xunit;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;
using Infrastructure.Services;
using Core.DTOs;
using Core.Interfaces;

namespace Unit;

public class ServiceCatalogServiceTests
{
    private static AppDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static IServiceCatalogService NewSut(AppDbContext db) =>
        new ServiceCatalogService(db, new HtmlSanitizerService());

    [Fact]
    public async Task Create_Then_GetBySlug_ReturnsPublished()
    {
        using var db = NewDb();
        var sut = NewSut(db);

        await sut.CreateAsync(new CreateServiceDto
        {
            Title = "Фундамент", Slug = "fundament",
            Content = "<p>Фундаментные работы</p>",
            Tag = "foundation",
            IsPublished = true
        });

        var result = await sut.GetBySlugAsync("fundament");

        Assert.NotNull(result);
        Assert.Equal("foundation", result!.Tag);
    }

    [Fact]
    public async Task Create_DuplicateSlug_Throws()
    {
        using var db = NewDb();
        var sut = NewSut(db);

        await sut.CreateAsync(new CreateServiceDto
        {
            Title = "First", Slug = "dup", Content = "<p>first</p>", IsPublished = true
        });

        await Assert.ThrowsAsync<Core.Exceptions.DuplicateSlugException>(() =>
            sut.CreateAsync(new CreateServiceDto
            {
                Title = "Second", Slug = "dup", Content = "<p>second</p>", IsPublished = true
            }));
    }
}
