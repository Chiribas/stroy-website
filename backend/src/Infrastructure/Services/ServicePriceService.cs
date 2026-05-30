using Microsoft.EntityFrameworkCore;
using Core.DTOs;
using Core.Entities;
using Core.Interfaces;
using Infrastructure.Data;

namespace Infrastructure.Services;

public class ServicePriceService : IServicePriceService
{
    private readonly AppDbContext _db;

    public ServicePriceService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ServicePriceDto>> GetAllAsync()
    {
        return await _db.ServicePrices
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Price)
            .Select(p => new ServicePriceDto(
                p.Id, p.Title, p.PhotoPath, p.Description, p.Price, p.Duration, p.ArticleSlug, p.Tag, p.SortOrder))
            .ToListAsync();
    }

    public async Task<ServicePriceDto> CreateAsync(CreateServicePriceDto dto)
    {
        var entity = new ServicePrice
        {
            Title = dto.Title, PhotoPath = dto.PhotoPath, Description = dto.Description,
            Price = dto.Price, Duration = dto.Duration, ArticleSlug = dto.ArticleSlug,
            Tag = dto.Tag, SortOrder = dto.SortOrder,
        };
        _db.ServicePrices.Add(entity);
        await _db.SaveChangesAsync();
        return ToDto(entity);
    }

    public async Task<ServicePriceDto?> UpdateAsync(int id, UpdateServicePriceDto dto)
    {
        var entity = await _db.ServicePrices.FindAsync(id);
        if (entity is null) return null;
        entity.Title = dto.Title; entity.PhotoPath = dto.PhotoPath; entity.Description = dto.Description;
        entity.Price = dto.Price; entity.Duration = dto.Duration; entity.ArticleSlug = dto.ArticleSlug;
        entity.Tag = dto.Tag; entity.SortOrder = dto.SortOrder;
        await _db.SaveChangesAsync();
        return ToDto(entity);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _db.ServicePrices.FindAsync(id);
        if (entity is null) return false;
        _db.ServicePrices.Remove(entity);
        await _db.SaveChangesAsync();
        return true;
    }

    private static ServicePriceDto ToDto(ServicePrice p) => new(
        p.Id, p.Title, p.PhotoPath, p.Description, p.Price, p.Duration, p.ArticleSlug, p.Tag, p.SortOrder);
}
