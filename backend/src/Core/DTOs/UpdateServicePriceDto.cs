using System.ComponentModel.DataAnnotations;

namespace Core.DTOs;

public class UpdateServicePriceDto
{
    [Required] public string Category { get; set; } = string.Empty;
    [Required] public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int PriceFrom { get; set; }
    public int? PriceTo { get; set; }
    public string? Unit { get; set; }
    public int SortOrder { get; set; }
}
