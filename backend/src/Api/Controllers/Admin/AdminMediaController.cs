using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Core.DTOs;
using Core.Interfaces;

namespace Api.Controllers.Admin;

[ApiController]
[Authorize]
[Route("api/admin/media")]
public class AdminMediaController : ControllerBase
{
    private const long MaxBytes = 15 * 1024 * 1024;
    private readonly IMediaService _media;

    public AdminMediaController(IMediaService media) => _media = media;

    [HttpPost("upload")]
    [RequestSizeLimit(MaxBytes)]
    public async Task<ActionResult<MediaUploadResponse>> Upload([FromForm] IFormFile file, [FromForm] int? articleId)
    {
        if (file is null || file.Length == 0) return BadRequest(new { message = "No file." });
        if (file.Length > MaxBytes) return BadRequest(new { message = "File too large." });

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _media.SaveImageAsync(stream, file.FileName, file.ContentType, articleId);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
