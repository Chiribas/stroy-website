using Core.DTOs;

namespace Core.Interfaces;

public interface ICallbackService
{
    Task CreateAsync(CallbackRequest request);
    Task<IReadOnlyList<CallbackDto>> GetAllAsync();
    Task<bool> SetProcessedAsync(int id, bool processed);
}
