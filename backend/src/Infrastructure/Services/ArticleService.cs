using Microsoft.EntityFrameworkCore;
using Core.DTOs;
using Core.Entities;
using Core.Exceptions;
using Core.Interfaces;
using Infrastructure.Data;

namespace Infrastructure.Services;

public class ArticleService : IArticleService
{
    private readonly AppDbContext _db;
    private readonly IHtmlSanitizerService _sanitizer;

    public ArticleService(AppDbContext db, IHtmlSanitizerService sanitizer)
    {
        _db = db;
        _sanitizer = sanitizer;
    }

    public async Task<PagedResult<ArticleListItemDto>> GetPublishedAsync(int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 12;

        var query = _db.Articles.Where(a => a.IsPublished);
        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(a => a.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new ArticleListItemDto(
                a.Id, a.Title, a.Slug, a.Summary, a.ThumbnailPath, a.PublishedAt))
            .ToListAsync();

        return new PagedResult<ArticleListItemDto>(items, total, page, pageSize);
    }

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

    public async Task<ArticleDto?> GetBySlugAsync(string slug)
    {
        var article = await _db.Articles
            .Include(a => a.Media)
            .FirstOrDefaultAsync(a => a.Slug == slug && a.IsPublished);

        return article is null ? null : ToDto(article);
    }

    public async Task<ArticleDto?> GetByIdAsync(int id)
    {
        var article = await _db.Articles.Include(a => a.Media).FirstOrDefaultAsync(a => a.Id == id);
        return article is null ? null : ToDto(article);
    }

    public async Task<ArticleDto> CreateAsync(CreateArticleDto dto)
    {
        if (await _db.Articles.AnyAsync(a => a.Slug == dto.Slug))
            throw new DuplicateSlugException(dto.Slug);

        var article = new Article
        {
            Title = dto.Title,
            Slug = dto.Slug,
            Summary = dto.Summary,
            Content = _sanitizer.Sanitize(dto.Content),
            ThumbnailPath = dto.ThumbnailPath,
            IsPublished = dto.IsPublished,
            PublishedAt = dto.IsPublished ? DateTime.UtcNow : null
        };

        _db.Articles.Add(article);
        await _db.SaveChangesAsync();
        return ToDto(article);
    }

    public async Task<ArticleDto?> UpdateAsync(int id, UpdateArticleDto dto)
    {
        // Include media because ToDto (the return value) maps it.
        var article = await _db.Articles.Include(a => a.Media).FirstOrDefaultAsync(a => a.Id == id);
        if (article is null) return null;

        if (!string.IsNullOrEmpty(dto.Slug) && dto.Slug != article.Slug
            && await _db.Articles.AnyAsync(a => a.Slug == dto.Slug && a.Id != id))
            throw new DuplicateSlugException(dto.Slug);

        article.Title = dto.Title;
        if (!string.IsNullOrEmpty(dto.Slug)) article.Slug = dto.Slug;
        article.Summary = dto.Summary;
        article.Content = _sanitizer.Sanitize(dto.Content);
        article.ThumbnailPath = dto.ThumbnailPath;
        article.IsPublished = dto.IsPublished;
        if (dto.IsPublished && article.PublishedAt is null)
            article.PublishedAt = DateTime.UtcNow;
        else if (!dto.IsPublished)
            article.PublishedAt = null;

        await _db.SaveChangesAsync();
        return ToDto(article);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var article = await _db.Articles.FindAsync(id);
        if (article is null) return false;
        _db.Articles.Remove(article);
        await _db.SaveChangesAsync();
        return true;
    }

    private static ArticleDto ToDto(Article a) => new(
        a.Id, a.Title, a.Slug, a.Summary, a.Content, a.ThumbnailPath, a.PublishedAt,
        a.Media.OrderBy(m => m.SortOrder)
            .Select(m => new ArticleMediaDto(m.Id, m.Path, m.MediaType, m.Alt, m.SortOrder))
            .ToList());
}
