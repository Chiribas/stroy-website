using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;
using Infrastructure.Services;
using Core.DTOs;

namespace Unit;

public class InboxServiceTests
{
    private static AppDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Callback_Create_List_MarkProcessed()
    {
        using var db = NewDb();
        var sut = new CallbackService(db);
        await sut.CreateAsync(new CallbackRequest { Phone = "123" });

        var list = await sut.GetAllAsync();
        Assert.Single(list);
        Assert.False(list[0].IsProcessed);

        var ok = await sut.SetProcessedAsync(list[0].Id, true);
        Assert.True(ok);
        Assert.True((await sut.GetAllAsync())[0].IsProcessed);
    }

    [Fact]
    public async Task Contact_Create_List_MarkProcessed()
    {
        using var db = NewDb();
        var sut = new ContactService(db);
        await sut.CreateAsync(new ContactRequest { Name = "Name", Phone = "1", Message = "hello there" });

        var list = await sut.GetAllAsync();
        Assert.Single(list);

        var ok = await sut.SetProcessedAsync(list[0].Id, true);
        Assert.True(ok);
        Assert.False(await sut.SetProcessedAsync(9999, true));
    }
}
