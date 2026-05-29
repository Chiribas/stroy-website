using Core.DTOs;

namespace Core.Interfaces;

public interface IServicePriceService
{
    Task<IReadOnlyList<ServicePriceDto>> GetAllAsync();
    Task<ServicePriceDto> CreateAsync(CreateServicePriceDto dto);
    Task<ServicePriceDto?> UpdateAsync(int id, UpdateServicePriceDto dto);
    Task<bool> DeleteAsync(int id);
}
