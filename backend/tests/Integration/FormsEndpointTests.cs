using Xunit;
using System.Net;
using System.Net.Http.Json;
using Core.DTOs;

namespace Integration;

public class FormsEndpointTests : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client;

    public FormsEndpointTests(ApiTestFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task PostCallback_WithValidPhone_ReturnsOk()
    {
        var response = await _client.PostAsJsonAsync("/api/callbacks",
            new CallbackRequest { Phone = "+79991234567", Name = "Иван" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PostContact_WithMissingMessage_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/contacts",
            new { name = "Иван", phone = "+79991234567" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
