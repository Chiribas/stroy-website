using Microsoft.AspNetCore.Mvc;
using Core.DTOs;
using Core.Interfaces;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServicesController : ControllerBase
{
    private readonly IServiceCatalogService _service;
    public ServicesController(IServiceCatalogService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ServiceListItemDto>>> Get()
        => Ok(await _service.GetPublishedAsync());

    [HttpGet("{slug}")]
    public async Task<ActionResult<ServiceDto>> GetBySlug(string slug)
    {
        var dto = await _service.GetBySlugAsync(slug);
        return dto is null ? NotFound() : Ok(dto);
    }
}
