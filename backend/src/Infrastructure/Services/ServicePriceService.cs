using Microsoft.EntityFrameworkCore;
using Core.DTOs;
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
}
