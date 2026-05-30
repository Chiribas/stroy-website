namespace Core.DTOs;

public record ServiceListItemDto(
    int Id, string Title, string Slug, string? ShortDescription, string? IconName, int SortOrder);
