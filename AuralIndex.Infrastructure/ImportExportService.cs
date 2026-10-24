using System.Globalization;
using System.Text;
using System.Text.Json;
using AuralIndex.Application;
using AuralIndex.Domain;
using Microsoft.EntityFrameworkCore;

namespace AuralIndex.Infrastructure;

public class ImportExportService : IImportExportService
{
    private readonly AuralIndexDbContext _db;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public ImportExportService(AuralIndexDbContext db)
    {
        _db = db;
    }

    public async Task<ImportExportResultDto> ExportJsonAsync(string filePath, CancellationToken ct = default)
    {
        var books = await LoadBooksAsync(ct);
        EnsureDirectory(filePath);

        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, books, JsonOptions, ct);

        return new ImportExportResultDto
        {
            Success = true,
            AffectedCount = books.Count,
            Message = $"Експортовано {books.Count} аудіокниг у JSON: {filePath}"
        };
    }

    public async Task<ImportExportResultDto> ExportCsvAsync(string filePath, CancellationToken ct = default)
    {
        var books = await LoadBooksAsync(ct);
        EnsureDirectory(filePath);

        var sb = new StringBuilder();
        sb.AppendLine("Id,Title,Author,Genre,Language,Year,DurationMinutes,IsListened,Narrator,Description");

        foreach (var book in books)
        {
            sb.AppendLine(string.Join(",",
                book.Id.ToString(CultureInfo.InvariantCulture),
                EscapeCsv(book.Title),
                EscapeCsv(book.Author),
                EscapeCsv(book.Genre),
                EscapeCsv(book.Language),
                book.Year?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                book.DurationMinutes.ToString(CultureInfo.InvariantCulture),
                book.IsListened.ToString(),
                EscapeCsv(book.Narrator ?? string.Empty),
                EscapeCsv(book.Description ?? string.Empty)));
        }

        await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8, ct);

        return new ImportExportResultDto
        {
            Success = true,
            AffectedCount = books.Count,
            Message = $"Експортовано {books.Count} аудіокниг у CSV: {filePath}"
        };
    }

    public async Task<ImportExportResultDto> ImportJsonAsync(string filePath, bool replaceExisting, CancellationToken ct = default)
    {
        if (!File.Exists(filePath))
        {
            return Failure($"Файл не знайдено: {filePath}");
        }

        await using var stream = File.OpenRead(filePath);
        var books = await JsonSerializer.DeserializeAsync<List<BookEditDto>>(stream, JsonOptions, ct) ?? [];
        return await ImportBooksAsync(books, replaceExisting, "JSON", ct);
    }

    public async Task<ImportExportResultDto> ImportCsvAsync(string filePath, bool replaceExisting, CancellationToken ct = default)
    {
        if (!File.Exists(filePath))
        {
            return Failure($"Файл не знайдено: {filePath}");
        }

        var lines = await File.ReadAllLinesAsync(filePath, ct);
        var books = lines.Skip(1)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(ParseCsvLine)
            .Where(parts => parts.Count >= 10)
            .Select(parts => new BookEditDto
            {
                Title = parts[1],
                Author = parts[2],
                Genre = parts[3],
                Language = parts[4],
                Year = int.TryParse(parts[5], out var year) ? year : null,
                DurationMinutes = int.TryParse(parts[6], out var duration) ? duration : 0,
                IsListened = bool.TryParse(parts[7], out var listened) && listened,
                Narrator = string.IsNullOrWhiteSpace(parts[8]) ? null : parts[8],
                Description = string.IsNullOrWhiteSpace(parts[9]) ? null : parts[9]
            })
            .ToList();

        return await ImportBooksAsync(books, replaceExisting, "CSV", ct);
    }

    private async Task<List<BookEditDto>> LoadBooksAsync(CancellationToken ct)
    {
        return await _db.Books
            .AsNoTracking()
            .OrderBy(x => x.Title)
            .Select(x => new BookEditDto
            {
                Id = x.Id,
                Title = x.Title,
                Author = x.Author,
                Genre = x.Genre,
                Language = x.Language,
                Year = x.Year,
                DurationMinutes = x.DurationMinutes,
                Narrator = x.Narrator,
                Description = x.Description,
                IsListened = x.IsListened
            })
            .ToListAsync(ct);
    }

    private async Task<ImportExportResultDto> ImportBooksAsync(List<BookEditDto> books, bool replaceExisting, string formatName, CancellationToken ct)
    {
        var validBooks = books
            .Where(x => !string.IsNullOrWhiteSpace(x.Title)
                     && !string.IsNullOrWhiteSpace(x.Author)
                     && !string.IsNullOrWhiteSpace(x.Genre)
                     && !string.IsNullOrWhiteSpace(x.Language)
                     && x.DurationMinutes > 0)
            .ToList();

        if (validBooks.Count == 0)
        {
            return Failure($"У файлі {formatName} не знайдено коректних записів для імпорту.");
        }

        if (replaceExisting)
        {
            _db.FavoriteBooks.RemoveRange(_db.FavoriteBooks);
            _db.Books.RemoveRange(_db.Books);
            await _db.SaveChangesAsync(ct);
        }

        var entities = validBooks.Select(x => new Book
        {
            Title = x.Title.Trim(),
            Author = x.Author.Trim(),
            Genre = x.Genre.Trim(),
            Language = x.Language.Trim(),
            Year = x.Year,
            DurationMinutes = x.DurationMinutes,
            IsListened = x.IsListened,
            Narrator = string.IsNullOrWhiteSpace(x.Narrator) ? null : x.Narrator.Trim(),
            Description = string.IsNullOrWhiteSpace(x.Description) ? null : x.Description.Trim()
        });

        await _db.Books.AddRangeAsync(entities, ct);
        await _db.SaveChangesAsync(ct);

        return new ImportExportResultDto
        {
            Success = true,
            AffectedCount = validBooks.Count,
            Message = $"Імпортовано {validBooks.Count} аудіокниг з формату {formatName}."
        };
    }

    private static ImportExportResultDto Failure(string message) => new()
    {
        Success = false,
        Message = message,
        AffectedCount = 0
    };

    private static void EnsureDirectory(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static string EscapeCsv(string value)
    {
        var escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];

            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (ch == ',' && !inQuotes)
            {
                result.Add(sb.ToString());
                sb.Clear();
            }
            else
            {
                sb.Append(ch);
            }
        }

        result.Add(sb.ToString());
        return result;
    }
}
