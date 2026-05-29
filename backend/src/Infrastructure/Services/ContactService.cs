using Microsoft.EntityFrameworkCore;
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

    public async Task<IReadOnlyList<ContactDto>> GetAllAsync()
        => await _db.Contacts
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new ContactDto(c.Id, c.Name, c.Phone, c.Message, c.CreatedAt, c.IsProcessed))
            .ToListAsync();

    public async Task<bool> SetProcessedAsync(int id, bool processed)
    {
        var c = await _db.Contacts.FindAsync(id);
        if (c is null) return false;
        c.IsProcessed = processed;
        await _db.SaveChangesAsync();
        return true;
    }
}
