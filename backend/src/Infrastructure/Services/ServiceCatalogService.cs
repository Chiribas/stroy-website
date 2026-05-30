using Microsoft.EntityFrameworkCore;
using Core.DTOs;
using Core.Entities;
using Core.Exceptions;
using Core.Interfaces;
using Infrastructure.Data;

namespace Infrastructure.Services;

public class ServiceCatalogService : IServiceCatalogService
{
    private readonly AppDbContext _db;
    private readonly IHtmlSanitizerService _sanitizer;

    public ServiceCatalogService(AppDbContext db, IHtmlSanitizerService sanitizer)
    {
        _db = db;
        _sanitizer = sanitizer;
    }

    public async Task<IReadOnlyList<ServiceListItemDto>> GetPublishedAsync() =>
        await _db.Services.Where(s => s.IsPublished)
            .OrderBy(s => s.SortOrder)
            .Select(s => new ServiceListItemDto(s.Id, s.Title, s.Slug, s.ShortDescription, s.IconName, s.SortOrder))
            .ToListAsync();

    public async Task<ServiceDto?> GetBySlugAsync(string slug)
    {
        var s = await _db.Services.FirstOrDefaultAsync(x => x.Slug == slug && x.IsPublished);
        return s is null ? null : ToDto(s);
    }

    public async Task<IReadOnlyList<ServiceDto>> GetAllForAdminAsync() =>
        await _db.Services.OrderBy(s => s.SortOrder)
            .Select(s => new ServiceDto(s.Id, s.Title, s.Slug, s.ShortDescription, s.IconName, s.Content, s.Tag, s.SortOrder, s.IsPublished))
            .ToListAsync();

    public async Task<ServiceDto?> GetByIdAsync(int id)
    {
        var s = await _db.Services.FindAsync(id);
        return s is null ? null : ToDto(s);
    }

    public async Task<ServiceDto> CreateAsync(CreateServiceDto dto)
    {
        if (await _db.Services.AnyAsync(s => s.Slug == dto.Slug))
            throw new DuplicateSlugException(dto.Slug);
        var s = new Service
        {
            Title = dto.Title, Slug = dto.Slug, ShortDescription = dto.ShortDescription,
            IconName = dto.IconName, Content = _sanitizer.Sanitize(dto.Content),
            Tag = dto.Tag, SortOrder = dto.SortOrder, IsPublished = dto.IsPublished,
        };
        _db.Services.Add(s);
        await _db.SaveChangesAsync();
        return ToDto(s);
    }

    public async Task<ServiceDto?> UpdateAsync(int id, UpdateServiceDto dto)
    {
        var s = await _db.Services.FindAsync(id);
        if (s is null) return null;
        if (!string.IsNullOrEmpty(dto.Slug) && dto.Slug != s.Slug
            && await _db.Services.AnyAsync(x => x.Slug == dto.Slug && x.Id != id))
            throw new DuplicateSlugException(dto.Slug);
        s.Title = dto.Title;
        if (!string.IsNullOrEmpty(dto.Slug)) s.Slug = dto.Slug;
        s.ShortDescription = dto.ShortDescription;
        s.IconName = dto.IconName;
        s.Content = _sanitizer.Sanitize(dto.Content);
        s.Tag = dto.Tag;
        s.SortOrder = dto.SortOrder;
        s.IsPublished = dto.IsPublished;
        await _db.SaveChangesAsync();
        return ToDto(s);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var s = await _db.Services.FindAsync(id);
        if (s is null) return false;
        _db.Services.Remove(s);
        await _db.SaveChangesAsync();
        return true;
    }

    private static ServiceDto ToDto(Service s) => new(
        s.Id, s.Title, s.Slug, s.ShortDescription, s.IconName, s.Content, s.Tag, s.SortOrder, s.IsPublished);
}
