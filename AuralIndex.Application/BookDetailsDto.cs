namespace AuralIndex.Application;

public class BookDetailsDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public int? Year { get; set; }
    public int DurationMinutes { get; set; }
    public string? Narrator { get; set; }
    public string? Description { get; set; }
    public bool IsListened { get; set; }
    public bool IsFavorite { get; set; }
}
