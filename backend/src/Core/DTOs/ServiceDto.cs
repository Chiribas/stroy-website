namespace Core.DTOs;

public record ServiceDto(
    int Id, string Title, string Slug, string? ShortDescription, string? IconName,
    string Content, string? Tag, int SortOrder, bool IsPublished);
