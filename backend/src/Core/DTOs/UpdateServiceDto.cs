using System.ComponentModel.DataAnnotations;

namespace Core.DTOs;

public class UpdateServiceDto
{
    [Required] public string Title { get; set; } = string.Empty;
    [RegularExpression(@"^[a-z0-9-]+$", ErrorMessage = "Slug must contain only lowercase letters, numbers and hyphens")]
    public string? Slug { get; set; }
    public string? ShortDescription { get; set; }
    public string? IconName { get; set; }
    [Required] public string Content { get; set; } = string.Empty;
    public string? Tag { get; set; }
    public int SortOrder { get; set; }
    public bool IsPublished { get; set; }
}
