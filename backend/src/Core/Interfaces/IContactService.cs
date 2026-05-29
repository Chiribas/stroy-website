using Core.DTOs;

namespace Core.Interfaces;

public interface IContactService
{
    Task CreateAsync(ContactRequest request);
    Task<IReadOnlyList<ContactDto>> GetAllAsync();
    Task<bool> SetProcessedAsync(int id, bool processed);
}
