namespace AuralIndex.Application;

public class ImportExportResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int AffectedCount { get; set; }
}
