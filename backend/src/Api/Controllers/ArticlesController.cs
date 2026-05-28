using Microsoft.AspNetCore.Mvc;
using Core.DTOs;
using Core.Interfaces;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ArticlesController : ControllerBase
{
    private readonly IArticleService _service;

    public ArticlesController(IArticleService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<PagedResult<ArticleListItemDto>>> Get(int page = 1, int pageSize = 12)
        => Ok(await _service.GetPublishedAsync(page, pageSize));

    [HttpGet("{slug}")]
    public async Task<ActionResult<ArticleDto>> GetBySlug(string slug)
    {
        var article = await _service.GetBySlugAsync(slug);
        return article is null ? NotFound() : Ok(article);
    }
}
