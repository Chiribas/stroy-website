using Xunit;
using System.Net;
using System.Net.Http.Json;
using Core.DTOs;

namespace Integration;

public class ArticlesEndpointTests : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client;

    public ArticlesEndpointTests(ApiTestFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task GetArticles_ReturnsOkWithPagedResult()
    {
        var response = await _client.GetAsync("/api/articles");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var paged = await response.Content.ReadFromJsonAsync<PagedResult<ArticleListItemDto>>();
        Assert.NotNull(paged);
        // ContentSeeder seeds at least 1 published article
        Assert.True(paged!.Total >= 1);
        Assert.NotEmpty(paged.Items);
    }

    [Fact]
    public async Task GetArticleBySlug_WhenMissing_Returns404()
    {
        var response = await _client.GetAsync("/api/articles/does-not-exist");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
