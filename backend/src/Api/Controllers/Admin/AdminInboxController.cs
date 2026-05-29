using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Core.DTOs;
using Core.Interfaces;

namespace Api.Controllers.Admin;

[ApiController]
[Authorize]
[Route("api/admin")]
public class AdminInboxController : ControllerBase
{
    private readonly ICallbackService _callbacks;
    private readonly IContactService _contacts;

    public AdminInboxController(ICallbackService callbacks, IContactService contacts)
    {
        _callbacks = callbacks;
        _contacts = contacts;
    }

    [HttpGet("callbacks")]
    public async Task<ActionResult<IReadOnlyList<CallbackDto>>> GetCallbacks()
        => Ok(await _callbacks.GetAllAsync());

    [HttpPatch("callbacks/{id:int}")]
    public async Task<IActionResult> PatchCallback(int id, UpdateInboxStatusRequest request)
        => await _callbacks.SetProcessedAsync(id, request.IsProcessed) ? NoContent() : NotFound();

    [HttpGet("contacts")]
    public async Task<ActionResult<IReadOnlyList<ContactDto>>> GetContacts()
        => Ok(await _contacts.GetAllAsync());

    [HttpPatch("contacts/{id:int}")]
    public async Task<IActionResult> PatchContact(int id, UpdateInboxStatusRequest request)
        => await _contacts.SetProcessedAsync(id, request.IsProcessed) ? NoContent() : NotFound();
}
