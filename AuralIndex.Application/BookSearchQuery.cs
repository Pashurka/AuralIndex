namespace AuralIndex.Application;

public class BookSearchQuery
{
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string? Genre { get; set; }
    public string? Language { get; set; }
    public string? Narrator { get; set; }
    public int? YearFrom { get; set; }
    public int? YearTo { get; set; }
    public int? MaxDurationMinutes { get; set; }

    public bool HasCriteria =>
        !string.IsNullOrWhiteSpace(Title) ||
        !string.IsNullOrWhiteSpace(Author) ||
        !string.IsNullOrWhiteSpace(Genre) ||
        !string.IsNullOrWhiteSpace(Language) ||
        !string.IsNullOrWhiteSpace(Narrator) ||
        YearFrom.HasValue ||
        YearTo.HasValue ||
        MaxDurationMinutes.HasValue;
}
