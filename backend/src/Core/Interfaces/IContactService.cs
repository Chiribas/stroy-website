using Core.DTOs;

namespace Core.Interfaces;

public interface IContactService
{
    Task CreateAsync(ContactRequest request);
}
