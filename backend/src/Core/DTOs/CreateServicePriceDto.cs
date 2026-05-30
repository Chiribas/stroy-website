using System.ComponentModel.DataAnnotations;

namespace Core.DTOs;

public class CreateServicePriceDto
{
    [Required] public string Title { get; set; } = string.Empty;
    public string? PhotoPath { get; set; }
    public string? Description { get; set; }
    public int Price { get; set; }
    public string? Duration { get; set; }
    public string? ArticleSlug { get; set; }
    public string? Tag { get; set; }
    public int SortOrder { get; set; }
}
