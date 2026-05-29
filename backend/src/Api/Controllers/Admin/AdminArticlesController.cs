using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Core.DTOs;
using Core.Exceptions;
using Core.Interfaces;

namespace Api.Controllers.Admin;

[ApiController]
[Authorize]
[Route("api/admin/articles")]
public class AdminArticlesController : ControllerBase
{
    private readonly IArticleService _service;

    public AdminArticlesController(IArticleService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<PagedResult<ArticleListItemDto>>> GetAll(int page = 1, int pageSize = 20)
        => Ok(await _service.GetAllForAdminAsync(page, pageSize));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ArticleDto>> GetById(int id)
    {
        var article = await _service.GetByIdAsync(id);
        return article is null ? NotFound() : Ok(article);
    }

    [HttpPost]
    public async Task<ActionResult<ArticleDto>> Create(CreateArticleDto dto)
    {
        try
        {
            var created = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (DuplicateSlugException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ArticleDto>> Update(int id, UpdateArticleDto dto)
    {
        try
        {
            var updated = await _service.UpdateAsync(id, dto);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (DuplicateSlugException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
        => await _service.DeleteAsync(id) ? NoContent() : NotFound();
}
