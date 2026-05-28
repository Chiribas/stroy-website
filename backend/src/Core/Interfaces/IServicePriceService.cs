using Core.DTOs;

namespace Core.Interfaces;

public interface IServicePriceService
{
    Task<IReadOnlyList<ServicePriceDto>> GetAllAsync();
}
