using Core.DTOs;

namespace Core.Interfaces;

public interface IMediaService
{
    /// <summary>Saves an uploaded image (resized + thumbnail) and optionally links it to an article.</summary>
    Task<MediaUploadResponse> SaveImageAsync(
        Stream content, string originalFileName, string contentType, int? articleId);
}
