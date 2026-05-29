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
    public async Task SaveImage_CorruptBytesWithImageContentType_ThrowsArgumentException()
    {
        using var db = NewDb();
        var (sut, _) = NewSut(db);
        // Allowed content type but the bytes are not a valid image → ImageSharp fails.
        using var src = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.SaveImageAsync(src, "broken.png", "image/png", null));
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
