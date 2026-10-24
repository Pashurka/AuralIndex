namespace AuralIndex.Application;

public class BookListItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public bool IsListened { get; set; }
    public double Score { get; set; }
    public string? RecommendationReason { get; set; }
}
