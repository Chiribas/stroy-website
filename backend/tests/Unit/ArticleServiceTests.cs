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

    [Fact]
    public async Task UpdateAsync_WhenPublishingDraft_SetsPublishedAt()
    {
        using var db = NewDb();
        var sut = NewSut(db);
        var draft = await sut.CreateAsync(new CreateArticleDto { Title = "T", Slug = "t", Content = "<p>t</p>", IsPublished = false });
        Assert.Equal(default, draft.PublishedAt);

        var updated = await sut.UpdateAsync(draft.Id, new UpdateArticleDto { Title = "T", Content = "<p>t</p>", IsPublished = true });

        Assert.NotNull(updated);
        Assert.NotEqual(default, updated!.PublishedAt);
    }

    [Fact]
    public async Task UpdateAsync_WhenAlreadyPublished_PreservesPublishedAt()
    {
        using var db = NewDb();
        var sut = NewSut(db);
        var published = await sut.CreateAsync(new CreateArticleDto { Title = "T", Slug = "t", Content = "<p>t</p>", IsPublished = true });
        var original = published.PublishedAt;

        var updated = await sut.UpdateAsync(published.Id, new UpdateArticleDto { Title = "T2", Content = "<p>t2</p>", IsPublished = true });

        Assert.NotNull(updated);
        Assert.Equal(original, updated!.PublishedAt);
    }

    [Fact]
    public async Task DeleteAsync_RemovesArticle_AndReturnsFalseWhenMissing()
    {
        using var db = NewDb();
        var sut = NewSut(db);
        var a = await sut.CreateAsync(new CreateArticleDto { Title = "T", Slug = "t", Content = "<p>t</p>", IsPublished = true });

        Assert.True(await sut.DeleteAsync(a.Id));
        Assert.Null(await sut.GetBySlugAsync("t"));
        Assert.False(await sut.DeleteAsync(9999));
    }
}
