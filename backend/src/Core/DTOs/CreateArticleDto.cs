using System.ComponentModel.DataAnnotations;

namespace Core.DTOs;

public class CreateArticleDto
{
    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^[a-z0-9-]+$", ErrorMessage = "Slug must contain only lowercase letters, numbers and hyphens")]
    public string Slug { get; set; } = string.Empty;

    public string? Summary { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;

    public string? ThumbnailPath { get; set; }
    public bool IsPublished { get; set; } = false;
}
