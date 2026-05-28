using Core.DTOs;

namespace Core.Interfaces;

public interface ICallbackService
{
    Task CreateAsync(CallbackRequest request);
}
