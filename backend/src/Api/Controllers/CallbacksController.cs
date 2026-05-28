using Microsoft.AspNetCore.Mvc;
using Core.DTOs;
using Core.Interfaces;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CallbacksController : ControllerBase
{
    private readonly ICallbackService _service;

    public CallbacksController(ICallbackService service) => _service = service;

    [HttpPost]
    public async Task<ActionResult> Create(CallbackRequest request)
    {
        await _service.CreateAsync(request);
        return Ok(new { message = "Спасибо! Мы перезвоним вам в ближайшее время." });
    }
}
