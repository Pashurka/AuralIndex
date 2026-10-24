namespace AuralIndex.Application;

public interface IImportExportService
{
    Task<ImportExportResultDto> ExportJsonAsync(string filePath, CancellationToken ct = default);
    Task<ImportExportResultDto> ExportCsvAsync(string filePath, CancellationToken ct = default);
    Task<ImportExportResultDto> ImportJsonAsync(string filePath, bool replaceExisting, CancellationToken ct = default);
    Task<ImportExportResultDto> ImportCsvAsync(string filePath, bool replaceExisting, CancellationToken ct = default);
}
