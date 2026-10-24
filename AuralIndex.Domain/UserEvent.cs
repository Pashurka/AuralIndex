namespace AuralIndex.Domain;

public class UserEvent
{
    public int Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? QueryText { get; set; }
    public int? BookId { get; set; }
    public string? BookTitle { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
