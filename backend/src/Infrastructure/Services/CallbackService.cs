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
}
