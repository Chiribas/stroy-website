namespace Core.Entities;

public class Article
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string Content { get; set; } = string.Empty;
    /// <summary>Теги через запятую (напр. "foundation,piles"). Связывают статью с услугами/ценами.</summary>
    public string? Tags { get; set; }
    public string? ThumbnailPath { get; set; }
    public DateTime? PublishedAt { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<ArticleMedia> Media { get; set; } = new();
}
