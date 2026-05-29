using Microsoft.AspNetCore.Mvc;
using Core.DTOs;
using Core.Interfaces;

namespace Api.Controllers.Admin;

[ApiController]
[Route("api/admin/auth")]
public class AdminAuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AdminAuthController(IAuthService auth) => _auth = auth;

    [HttpPost]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var result = await _auth.AuthenticateAsync(request);
        return result is null ? Unauthorized() : Ok(result);
    }
}
