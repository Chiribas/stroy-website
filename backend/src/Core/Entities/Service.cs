namespace Core.Entities;

public class Service
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string? IconName { get; set; }          // имя lucide-иконки, напр. "home"
    public string Content { get; set; } = string.Empty; // санитайзенный HTML детальной страницы
    public string? Tag { get; set; }               // тег для подбора статей "Из практики"
    public int SortOrder { get; set; }
    public bool IsPublished { get; set; }
}
