using Microsoft.AspNetCore.Mvc;
using Core.DTOs;
using Core.Interfaces;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContactsController : ControllerBase
{
    private readonly IContactService _service;

    public ContactsController(IContactService service) => _service = service;

    [HttpPost]
    public async Task<ActionResult> Create(ContactRequest request)
    {
        await _service.CreateAsync(request);
        return Ok(new { message = "Спасибо за сообщение! Мы ответим вам в ближайшее время." });
    }
}
