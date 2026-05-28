namespace Core.Entities;

public class ServicePrice
{
    public int Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int PriceFrom { get; set; }
    public int? PriceTo { get; set; }
    public string? Unit { get; set; }
    public int SortOrder { get; set; }
}
