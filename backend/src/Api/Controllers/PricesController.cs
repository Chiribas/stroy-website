using Microsoft.AspNetCore.Mvc;
using Core.DTOs;
using Core.Interfaces;

namespace Api.Controllers;

[ApiController]
[Route("api/prices")]
public class PricesController : ControllerBase
{
    private readonly IServicePriceService _service;
    public PricesController(IServicePriceService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ServicePriceDto>>> Get()
        => Ok(await _service.GetAllAsync());
}
