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
            .ThenBy(p => p.PriceFrom)
            .Select(p => new ServicePriceDto(
                p.Id, p.Category, p.Name, p.Description, p.PriceFrom, p.PriceTo, p.Unit, p.SortOrder))
            .ToListAsync();
    }

    public async Task<ServicePriceDto> CreateAsync(CreateServicePriceDto dto)
    {
        var entity = new ServicePrice
        {
            Category = dto.Category, Name = dto.Name, Description = dto.Description,
            PriceFrom = dto.PriceFrom, PriceTo = dto.PriceTo, Unit = dto.Unit, SortOrder = dto.SortOrder,
        };
        _db.ServicePrices.Add(entity);
        await _db.SaveChangesAsync();
        return ToDto(entity);
    }

    public async Task<ServicePriceDto?> UpdateAsync(int id, UpdateServicePriceDto dto)
    {
        var entity = await _db.ServicePrices.FindAsync(id);
        if (entity is null) return null;
        entity.Category = dto.Category; entity.Name = dto.Name; entity.Description = dto.Description;
        entity.PriceFrom = dto.PriceFrom; entity.PriceTo = dto.PriceTo; entity.Unit = dto.Unit; entity.SortOrder = dto.SortOrder;
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
        p.Id, p.Category, p.Name, p.Description, p.PriceFrom, p.PriceTo, p.Unit, p.SortOrder);
}
