namespace Core.DTOs;

public record ServicePriceDto(
    int Id,
    string Category,
    string Name,
    string? Description,
    int PriceFrom,
    int? PriceTo,
    string? Unit,
    int SortOrder
);
