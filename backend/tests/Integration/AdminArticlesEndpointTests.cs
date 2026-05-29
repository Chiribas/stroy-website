using Xunit;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Core.DTOs;

namespace Integration;

public class AdminArticlesEndpointTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;
    public AdminArticlesEndpointTests(ApiTestFactory factory) => _factory = factory;

    private async Task<HttpClient> AuthedClientAsync()
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/admin/auth",
            new LoginRequest { Username = "admin", Password = "secret123" });
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        return client;
    }

    [Fact]
    public async Task GetAll_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/api/admin/articles");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Create_WithToken_Returns201()
    {
        var client = await AuthedClientAsync();
        var resp = await client.PostAsJsonAsync("/api/admin/articles",
            new CreateArticleDto { Title = "T", Slug = "create-ok", Content = "<p>x</p>" });
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
    }

    [Fact]
    public async Task Create_DuplicateSlug_Returns409()
    {
        var client = await AuthedClientAsync();
        await client.PostAsJsonAsync("/api/admin/articles",
            new CreateArticleDto { Title = "A", Slug = "dup-int", Content = "<p>a</p>" });
        var resp = await client.PostAsJsonAsync("/api/admin/articles",
            new CreateArticleDto { Title = "B", Slug = "dup-int", Content = "<p>b</p>" });
        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }
}
