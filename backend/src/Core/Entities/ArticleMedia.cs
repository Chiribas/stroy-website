namespace Core.Entities;

public class ArticleMedia
{
    public int Id { get; set; }
    public int ArticleId { get; set; }
    public Article Article { get; set; } = null!;
    public string Path { get; set; } = string.Empty;
    public string MediaType { get; set; } = "image";
    public string? Alt { get; set; }
    public int SortOrder { get; set; }
}
