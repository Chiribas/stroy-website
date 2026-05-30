namespace Core.Entities;

public class ServicePrice
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;   // было Name
    public string? PhotoPath { get; set; }
    public string? Description { get; set; }
    public int Price { get; set; }                       // было PriceFrom (рубли, итог)
    public string? Duration { get; set; }                // напр. "2 дня"
    public string? ArticleSlug { get; set; }             // ссылка на статью
    public string? Tag { get; set; }
    public int SortOrder { get; set; }
}
