namespace AuralIndex.Application;

public class BookStatisticsDto
{
    public int TotalBooks { get; set; }
    public int TotalGenres { get; set; }
    public int TotalLanguages { get; set; }
    public int AverageDurationMinutes { get; set; }
    public IReadOnlyList<StatisticEntryDto> TopGenres { get; set; } = [];
    public IReadOnlyList<StatisticEntryDto> TopLanguages { get; set; } = [];
    public IReadOnlyList<StatisticEntryDto> TopAuthors { get; set; } = [];
}
