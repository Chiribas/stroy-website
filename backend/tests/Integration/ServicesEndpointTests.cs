using Xunit;
using System.Net;

namespace Integration;

public class ServicesEndpointTests : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client;

    public ServicesEndpointTests(ApiTestFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task GetServices_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/services");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetService_Unknown_Returns404()
    {
        var response = await _client.GetAsync("/api/services/no-such-slug");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetPrices_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/prices");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
