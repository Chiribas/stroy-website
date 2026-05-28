using Microsoft.AspNetCore.Mvc;
using Core.DTOs;
using Core.Interfaces;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServicesController : ControllerBase
{
    private readonly IServicePriceService _service;

    public ServicesController(IServicePriceService service) => _service = service;

    [HttpGet("prices")]
    public async Task<ActionResult<IReadOnlyList<ServicePriceDto>>> GetPrices()
        => Ok(await _service.GetAllAsync());
}
