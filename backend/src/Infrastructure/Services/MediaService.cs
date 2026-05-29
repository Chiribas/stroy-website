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

        Image image;
        try
        {
            image = await Image.LoadAsync(content);
        }
        catch (SixLabors.ImageSharp.ImageFormatException ex)
        {
            // Unknown/corrupt image bytes — surface as a domain error (controller maps to 400).
            throw new ArgumentException("Файл не является корректным изображением.", ex);
        }
        using var _imageScope = image;
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
