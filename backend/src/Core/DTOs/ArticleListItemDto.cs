namespace Core.DTOs;

public record ArticleListItemDto(
    int Id,
    string Title,
    string Slug,
    string? Summary,
    string? ThumbnailPath,
    DateTime? PublishedAt
);
