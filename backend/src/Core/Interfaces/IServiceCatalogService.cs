using Core.DTOs;

namespace Core.Interfaces;

public interface IServiceCatalogService
{
    Task<IReadOnlyList<ServiceListItemDto>> GetPublishedAsync();
    Task<ServiceDto?> GetBySlugAsync(string slug);
    Task<IReadOnlyList<ServiceDto>> GetAllForAdminAsync();
    Task<ServiceDto?> GetByIdAsync(int id);
    Task<ServiceDto> CreateAsync(CreateServiceDto dto);
    Task<ServiceDto?> UpdateAsync(int id, UpdateServiceDto dto);
    Task<bool> DeleteAsync(int id);
}
