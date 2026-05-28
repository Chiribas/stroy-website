namespace Core.DTOs;

public record ArticleMediaDto(int Id, string Path, string MediaType, string? Alt, int SortOrder);

public record ArticleDto(
    int Id,
    string Title,
    string Slug,
    string? Summary,
    string Content,
    string? ThumbnailPath,
    DateTime PublishedAt,
    IReadOnlyList<ArticleMediaDto> Media
);
