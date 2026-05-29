using Xunit;
using System.Net;
using System.Net.Http.Json;
using Core.DTOs;

namespace Integration;

public class AdminAuthEndpointTests : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client;
    public AdminAuthEndpointTests(ApiTestFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Login_ValidCreds_ReturnsToken()
    {
        var resp = await _client.PostAsJsonAsync("/api/admin/auth",
            new LoginRequest { Username = "admin", Password = "secret123" });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.Token));
    }

    [Fact]
    public async Task Login_BadCreds_Returns401()
    {
        var resp = await _client.PostAsJsonAsync("/api/admin/auth",
            new LoginRequest { Username = "admin", Password = "nope" });

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }
}
