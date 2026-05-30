namespace Core.DTOs;

public record ServicePriceDto(
    int Id, string Title, string? PhotoPath, string? Description,
    int Price, string? Duration, string? ArticleSlug, string? Tag, int SortOrder);
