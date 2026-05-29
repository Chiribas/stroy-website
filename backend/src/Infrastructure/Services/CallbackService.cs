using Microsoft.EntityFrameworkCore;
using Core.DTOs;
using Core.Entities;
using Core.Interfaces;
using Infrastructure.Data;

namespace Infrastructure.Services;

public class CallbackService : ICallbackService
{
    private readonly AppDbContext _db;

    public CallbackService(AppDbContext db) => _db = db;

    public async Task CreateAsync(CallbackRequest request)
    {
        _db.Callbacks.Add(new Callback { Phone = request.Phone, Name = request.Name });
        await _db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<CallbackDto>> GetAllAsync()
        => await _db.Callbacks
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CallbackDto(c.Id, c.Phone, c.Name, c.CreatedAt, c.IsProcessed))
            .ToListAsync();

    public async Task<bool> SetProcessedAsync(int id, bool processed)
    {
        var c = await _db.Callbacks.FindAsync(id);
        if (c is null) return false;
        c.IsProcessed = processed;
        await _db.SaveChangesAsync();
        return true;
    }
}
