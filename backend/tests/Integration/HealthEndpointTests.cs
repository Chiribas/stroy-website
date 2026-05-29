using Xunit;
using System.Net;

namespace Integration;

public class HealthEndpointTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;
    public HealthEndpointTests(ApiTestFactory factory) => _factory = factory;

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task Health_IsAnonymous()
    {
        // без токена — всё равно 200 (health не за авторизацией)
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }
}
