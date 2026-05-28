using Core.DTOs;
using Core.Entities;
using Core.Interfaces;
using Infrastructure.Data;

namespace Infrastructure.Services;

public class ContactService : IContactService
{
    private readonly AppDbContext _db;

    public ContactService(AppDbContext db) => _db = db;

    public async Task CreateAsync(ContactRequest request)
    {
        _db.Contacts.Add(new Contact
        {
            Name = request.Name,
            Phone = request.Phone,
            Message = request.Message
        });
        await _db.SaveChangesAsync();
    }
}
