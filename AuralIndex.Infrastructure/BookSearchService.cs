using AuralIndex.Application;
using AuralIndex.Domain;
using Microsoft.EntityFrameworkCore;

namespace AuralIndex.Infrastructure;

public class BookSearchService : IBookSearchService
{
    private readonly AuralIndexDbContext _db;
    private readonly IAlgorithmSettingsService _settingsService;

    public BookSearchService(AuralIndexDbContext db, IAlgorithmSettingsService settingsService)
    {
        _db = db;
        _settingsService = settingsService;
    }

    public async Task<IReadOnlyList<BookListItemDto>> SearchAsync(string? query, CancellationToken ct = default)
    {
        var settings = await _settingsService.GetAsync(ct);
        var books = await _db.Books.AsNoTracking().ToListAsync(ct);

        if (string.IsNullOrWhiteSpace(query))
        {
            return books.OrderBy(book => book.Title).Select(book => Map(book, 0)).ToList();
        }

        var term = query.Trim();
        var results = books
            .Select(book => new { Book = book, Score = CalculateFreeTextScore(book, term, settings) })
            .Where(result => result.Score > 0)
            .OrderByDescending(result => result.Score)
            .ThenBy(result => result.Book.Title)
            .Select(result => Map(result.Book, result.Score))
            .ToList();

        await LogEventAsync("search", $"Швидкий пошук: {term}", ct: ct);
        return results;
    }

    public async Task<IReadOnlyList<BookListItemDto>> SearchAsync(BookSearchQuery query, CancellationToken ct = default)
    {
        var settings = await _settingsService.GetAsync(ct);
        var books = await _db.Books.AsNoTracking().ToListAsync(ct);

        if (!query.HasCriteria)
        {
            return books.OrderBy(book => book.Title).Select(book => Map(book, 0)).ToList();
        }

        var results = books
            .Select(book => new { Book = book, Score = CalculateMetadataScore(book, query, settings) })
            .Where(result => result.Score > 0 && MatchesRanges(result.Book, query))
            .OrderByDescending(result => result.Score)
            .ThenBy(result => result.Book.Title)
            .Select(result => Map(result.Book, result.Score))
            .ToList();

        await LogEventAsync("advanced-search", BuildSearchSummary(query), ct: ct);
        return results;
    }

    public async Task<BookDetailsDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var favoriteIds = await _db.FavoriteBooks.AsNoTracking().Where(x => x.BookId == id).Select(x => x.BookId).ToListAsync(ct);

        return await _db.Books
            .AsNoTracking()
            .Where(book => book.Id == id)
            .Select(book => new BookDetailsDto
            {
                Id = book.Id,
                Title = book.Title,
                Author = book.Author,
                Genre = book.Genre,
                Language = book.Language,
                Year = book.Year,
                DurationMinutes = book.DurationMinutes,
                Narrator = book.Narrator,
                Description = book.Description,
                IsListened = book.IsListened,
                IsFavorite = favoriteIds.Contains(book.Id)
            })
            .SingleOrDefaultAsync(ct);
    }

    public async Task LogBookViewedAsync(int id, CancellationToken ct = default)
    {
        var book = await _db.Books.AsNoTracking().Where(x => x.Id == id).Select(x => new { x.Id, x.Title, x.Author }).SingleOrDefaultAsync(ct);
        if (book is not null)
        {
            await LogEventAsync("view", $"Перегляд: {book.Title} — {book.Author}", book.Id, book.Title, ct);
        }
    }

    public async Task<IReadOnlyList<BookListItemDto>> GetRecommendationsAsync(int bookId, int limit = 5, CancellationToken ct = default)
    {
        var settings = await _settingsService.GetAsync(ct);
        var books = await _db.Books.AsNoTracking().ToListAsync(ct);
        var source = books.SingleOrDefault(book => book.Id == bookId);
        if (source is null)
        {
            return [];
        }

        return books
            .Where(book => book.Id != bookId)
            .Select(book => new
            {
                Book = book,
                Score = CalculateRecommendationScore(source, book, settings),
                Reason = BuildSimilarityReason(source, book)
            })
            .Where(result => result.Score > 0)
            .OrderByDescending(result => result.Score)
            .ThenBy(result => result.Book.Title)
            .Take(limit)
            .Select(result => Map(result.Book, result.Score, result.Reason))
            .ToList();
    }

    public async Task<IReadOnlyList<BookListItemDto>> GetPersonalizedRecommendationsAsync(int limit = 8, CancellationToken ct = default)
    {
        var settings = await _settingsService.GetAsync(ct);
        var books = await _db.Books.AsNoTracking().ToListAsync(ct);
        var favorites = await _db.FavoriteBooks.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).ToListAsync(ct);
        var viewedBookIds = await _db.UserEvents.AsNoTracking()
            .Where(x => x.EventType == "view" && x.BookId.HasValue)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => x.BookId!.Value)
            .ToListAsync(ct);

        var sourceWeights = new Dictionary<int, double>();
        foreach (var favorite in favorites.Take(10))
        {
            sourceWeights[favorite.BookId] = sourceWeights.GetValueOrDefault(favorite.BookId) + settings.PersonalizedFavoriteBoost;
        }

        var recencyWeight = settings.PersonalizedViewStartBoost;
        foreach (var viewedBookId in viewedBookIds.Take(12))
        {
            sourceWeights[viewedBookId] = sourceWeights.GetValueOrDefault(viewedBookId) + recencyWeight;
            recencyWeight = Math.Max(0.5, recencyWeight - 0.2);
        }

        if (sourceWeights.Count == 0)
        {
            return [];
        }

        var sourceBooks = books.Where(book => sourceWeights.ContainsKey(book.Id)).ToList();
        var favoriteIds = favorites.Select(x => x.BookId).ToHashSet();
        var genrePreference = sourceBooks.GroupBy(x => x.Genre, StringComparer.CurrentCultureIgnoreCase).ToDictionary(g => g.Key, g => g.Count() * settings.PersonalizedGenrePreference, StringComparer.CurrentCultureIgnoreCase);
        var languagePreference = sourceBooks.GroupBy(x => x.Language, StringComparer.CurrentCultureIgnoreCase).ToDictionary(g => g.Key, g => g.Count() * settings.PersonalizedLanguagePreference, StringComparer.CurrentCultureIgnoreCase);
        var authorPreference = sourceBooks.GroupBy(x => x.Author, StringComparer.CurrentCultureIgnoreCase).ToDictionary(g => g.Key, g => g.Count() * settings.PersonalizedAuthorPreference, StringComparer.CurrentCultureIgnoreCase);

        return books
            .Where(book => !sourceWeights.ContainsKey(book.Id) && !favoriteIds.Contains(book.Id))
            .Select(book => new
            {
                Book = book,
                Score = CalculatePersonalizedScore(book, sourceBooks, sourceWeights, genrePreference, languagePreference, authorPreference, settings),
                Reason = BuildPersonalizedReason(book, sourceBooks, genrePreference, languagePreference, authorPreference, settings)
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Book.Title)
            .Take(limit)
            .Select(x => Map(x.Book, x.Score, x.Reason))
            .ToList();
    }

    public async Task<BookStatisticsDto> GetStatisticsAsync(CancellationToken ct = default)
    {
        var books = await _db.Books.AsNoTracking().ToListAsync(ct);
        var totalBooks = books.Count;

        return new BookStatisticsDto
        {
            TotalBooks = totalBooks,
            TotalGenres = books.Select(book => book.Genre).Distinct(StringComparer.CurrentCultureIgnoreCase).Count(),
            TotalLanguages = books.Select(book => book.Language).Distinct(StringComparer.CurrentCultureIgnoreCase).Count(),
            AverageDurationMinutes = totalBooks == 0 ? 0 : (int)Math.Round(books.Average(book => book.DurationMinutes)),
            TopGenres = books.GroupBy(book => book.Genre).OrderByDescending(group => group.Count()).ThenBy(group => group.Key).Take(5).Select(group => new StatisticEntryDto { Label = group.Key, Count = group.Count() }).ToList(),
            TopLanguages = books.GroupBy(book => book.Language).OrderByDescending(group => group.Count()).ThenBy(group => group.Key).Take(5).Select(group => new StatisticEntryDto { Label = group.Key, Count = group.Count() }).ToList(),
            TopAuthors = books.GroupBy(book => book.Author).OrderByDescending(group => group.Count()).ThenBy(group => group.Key).Take(5).Select(group => new StatisticEntryDto { Label = group.Key, Count = group.Count() }).ToList()
        };
    }

    public async Task<IReadOnlyList<HistoryEntryDto>> GetHistoryAsync(int limit = 100, CancellationToken ct = default)
    {
        return await _db.UserEvents.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).Take(limit).Select(x => new HistoryEntryDto
        {
            BookId = x.BookId,
            EventType = x.EventType,
            Title = x.EventType == "view" ? "Перегляд аудіокниги" : x.EventType == "advanced-search" ? "Розширений пошук" : x.EventType == "catalog-change" ? "Зміна каталогу" : x.EventType == "favorite" ? "Обране" : "Пошук",
            Description = x.QueryText ?? x.BookTitle ?? "Подія без опису",
            CreatedAtUtc = x.CreatedAtUtc
        }).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<FavoriteEntryDto>> GetFavoritesAsync(CancellationToken ct = default)
    {
        return await _db.FavoriteBooks.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Title).Select(x => new FavoriteEntryDto
        {
            BookId = x.BookId,
            Title = x.Title,
            Author = x.Author,
            Genre = x.Genre,
            Language = x.Language,
            CreatedAtUtc = x.CreatedAtUtc
        }).ToListAsync(ct);
    }

    public async Task<bool> AddToFavoritesAsync(int bookId, CancellationToken ct = default)
    {
        var exists = await _db.FavoriteBooks.AnyAsync(x => x.BookId == bookId, ct);
        if (exists) return true;

        var book = await _db.Books.AsNoTracking().Where(x => x.Id == bookId).Select(x => new { x.Id, x.Title, x.Author, x.Genre, x.Language }).SingleOrDefaultAsync(ct);
        if (book is null) return false;

        _db.FavoriteBooks.Add(new FavoriteBook { BookId = book.Id, Title = book.Title, Author = book.Author, Genre = book.Genre, Language = book.Language, CreatedAtUtc = DateTime.UtcNow });
        await _db.SaveChangesAsync(ct);
        await LogEventAsync("favorite", $"Додано в обране: {book.Title}", book.Id, book.Title, ct);
        return true;
    }

    public async Task<bool> RemoveFromFavoritesAsync(int bookId, CancellationToken ct = default)
    {
        var entity = await _db.FavoriteBooks.SingleOrDefaultAsync(x => x.BookId == bookId, ct);
        if (entity is null) return false;

        var title = entity.Title;
        _db.FavoriteBooks.Remove(entity);
        await _db.SaveChangesAsync(ct);
        await LogEventAsync("favorite", $"Видалено з обраного: {title}", bookId, title, ct);
        return true;
    }

    public Task<bool> IsFavoriteAsync(int bookId, CancellationToken ct = default) => _db.FavoriteBooks.AnyAsync(x => x.BookId == bookId, ct);

    public async Task<bool> SetListenedStatusAsync(int bookId, bool isListened, CancellationToken ct = default)
    {
        var entity = await _db.Books.FindAsync([bookId], ct);
        if (entity is null) return false;

        entity.IsListened = isListened;
        await _db.SaveChangesAsync(ct);
        await LogEventAsync("catalog-change", $"Статус прослуховування змінено: {entity.Title} -> {(isListened ? "Слухав" : "Не слухав")}", entity.Id, entity.Title, ct);
        return true;
    }

    public async Task<int> CreateAsync(BookEditDto book, CancellationToken ct = default)
    {
        var entity = new Book
        {
            Title = book.Title.Trim(),
            Author = book.Author.Trim(),
            Genre = book.Genre.Trim(),
            Language = NormalizeOptional(book.Language) ?? "Українська",
            Year = book.Year,
            DurationMinutes = book.DurationMinutes,
            Narrator = NormalizeOptional(book.Narrator),
            Description = NormalizeOptional(book.Description),
            IsListened = book.IsListened
        };

        _db.Books.Add(entity);
        await _db.SaveChangesAsync(ct);
        await LogEventAsync("catalog-change", $"Додано: {entity.Title}", entity.Id, entity.Title, ct);
        return entity.Id;
    }

    public async Task<bool> UpdateAsync(BookEditDto book, CancellationToken ct = default)
    {
        var entity = await _db.Books.FindAsync([book.Id], ct);
        if (entity is null) return false;

        entity.Title = book.Title.Trim();
        entity.Author = book.Author.Trim();
        entity.Genre = book.Genre.Trim();
        entity.Language = NormalizeOptional(book.Language) ?? "Українська";
        entity.Year = book.Year;
        entity.DurationMinutes = book.DurationMinutes;
        entity.Narrator = NormalizeOptional(book.Narrator);
        entity.Description = NormalizeOptional(book.Description);
        entity.IsListened = book.IsListened;
        await _db.SaveChangesAsync(ct);

        var favorite = await _db.FavoriteBooks.SingleOrDefaultAsync(x => x.BookId == entity.Id, ct);
        if (favorite is not null)
        {
            favorite.Title = entity.Title;
            favorite.Author = entity.Author;
            favorite.Genre = entity.Genre;
            favorite.Language = entity.Language;
            await _db.SaveChangesAsync(ct);
        }

        await LogEventAsync("catalog-change", $"Оновлено: {entity.Title}", entity.Id, entity.Title, ct);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.Books.FindAsync([id], ct);
        if (entity is null) return false;

        var favorite = await _db.FavoriteBooks.SingleOrDefaultAsync(x => x.BookId == id, ct);
        if (favorite is not null) _db.FavoriteBooks.Remove(favorite);

        await LogEventAsync("catalog-change", $"Видалено: {entity.Title}", entity.Id, entity.Title, ct);
        _db.Books.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private async Task LogEventAsync(string eventType, string? description, int? bookId = null, string? bookTitle = null, CancellationToken ct = default)
    {
        _db.UserEvents.Add(new UserEvent { EventType = eventType, QueryText = NormalizeOptional(description), BookId = bookId, BookTitle = NormalizeOptional(bookTitle), CreatedAtUtc = DateTime.UtcNow });
        await _db.SaveChangesAsync(ct);
    }

    private static string BuildSearchSummary(BookSearchQuery query)
    {
        var parts = new List<string>();
        Append(parts, "Назва", query.Title);
        Append(parts, "Автор", query.Author);
        Append(parts, "Жанр", query.Genre);
        Append(parts, "Мова", query.Language);
        Append(parts, "Диктор", query.Narrator);
        if (query.YearFrom.HasValue || query.YearTo.HasValue) parts.Add($"Роки: {query.YearFrom?.ToString() ?? "-"} - {query.YearTo?.ToString() ?? "-"}");
        if (query.MaxDurationMinutes.HasValue) parts.Add($"До {query.MaxDurationMinutes.Value} хв");
        return string.Join("; ", parts);
    }

    private static void Append(List<string> parts, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)) parts.Add($"{label}: {value.Trim()}");
    }

    private static BookListItemDto Map(Book book, double score, string? recommendationReason = null) => new()
    {
        Id = book.Id,
        Title = book.Title,
        Author = book.Author,
        Genre = book.Genre,
        Language = book.Language,
        DurationMinutes = book.DurationMinutes,
        IsListened = book.IsListened,
        Score = score,
        RecommendationReason = recommendationReason
    };

    private static double CalculateFreeTextScore(Book book, string term, AlgorithmWeightsDto settings)
    {
        var score = 0d;
        score += ScoreField(book.Title, term, settings.SearchTitleExact, settings.SearchTitleStartsWith, settings.SearchTitleContains);
        score += ScoreField(book.Author, term, settings.SearchAuthorExact, settings.SearchAuthorExact * 0.72, settings.SearchAuthorExact * 0.51);
        score += ScoreField(book.Genre, term, settings.SearchGenreExact, settings.SearchGenreExact * 0.72, settings.SearchGenreExact * 0.48);
        score += ScoreField(book.Language, term, settings.SearchLanguageExact, settings.SearchLanguageExact * 0.7, settings.SearchLanguageExact * 0.4);
        score += ScoreField(book.Narrator, term, 18, 12, 8);
        score += ScoreField(book.Description, term, 10, 7, 4);
        return Math.Round(score, 2);
    }

    private static double CalculateMetadataScore(Book book, BookSearchQuery query, AlgorithmWeightsDto settings)
    {
        var score = 0d;
        score += ScoreField(book.Title, query.Title, settings.SearchTitleExact, settings.SearchTitleStartsWith, settings.SearchTitleContains);
        score += ScoreField(book.Author, query.Author, settings.SearchAuthorExact, settings.SearchAuthorExact * 0.72, settings.SearchAuthorExact * 0.51);
        score += ScoreField(book.Genre, query.Genre, settings.SearchGenreExact, settings.SearchGenreExact * 0.74, settings.SearchGenreExact * 0.5);
        score += ScoreField(book.Language, query.Language, settings.SearchLanguageExact, settings.SearchLanguageExact * 0.64, settings.SearchLanguageExact * 0.4);
        score += ScoreField(book.Narrator, query.Narrator, 20, 14, 9);
        if (MatchesRanges(book, query))
        {
            if (query.YearFrom.HasValue || query.YearTo.HasValue) score += 10;
            if (query.MaxDurationMinutes.HasValue) score += 8;
        }
        return Math.Round(score, 2);
    }

    private static double CalculateRecommendationScore(Book source, Book candidate, AlgorithmWeightsDto settings)
    {
        var score = 0d;
        if (Same(source.Genre, candidate.Genre)) score += settings.RecommendationGenre;
        if (Same(source.Language, candidate.Language)) score += settings.RecommendationLanguage;
        if (Same(source.Author, candidate.Author)) score += settings.RecommendationAuthor;
        if (source.Year.HasValue && candidate.Year.HasValue)
        {
            var yearDistance = Math.Abs(source.Year.Value - candidate.Year.Value);
            if (yearDistance <= 3) score += settings.RecommendationYearNear;
            else if (yearDistance <= 10) score += settings.RecommendationYearMedium;
        }
        var durationDistance = Math.Abs(source.DurationMinutes - candidate.DurationMinutes);
        if (durationDistance <= 60) score += settings.RecommendationDurationNear;
        else if (durationDistance <= 180) score += settings.RecommendationDurationMedium;
        return Math.Round(score, 2);
    }

    private static double CalculatePersonalizedScore(Book candidate, IReadOnlyList<Book> sourceBooks, IReadOnlyDictionary<int, double> sourceWeights, IReadOnlyDictionary<string, double> genrePreference, IReadOnlyDictionary<string, double> languagePreference, IReadOnlyDictionary<string, double> authorPreference, AlgorithmWeightsDto settings)
    {
        var similarityScore = sourceBooks.Sum(source => CalculateRecommendationScore(source, candidate, settings) * sourceWeights.GetValueOrDefault(source.Id));
        var preferenceScore = 0d;
        if (genrePreference.TryGetValue(candidate.Genre, out var genreWeight)) preferenceScore += genreWeight;
        if (languagePreference.TryGetValue(candidate.Language, out var languageWeight)) preferenceScore += languageWeight;
        if (authorPreference.TryGetValue(candidate.Author, out var authorWeight)) preferenceScore += authorWeight;
        return Math.Round((similarityScore / 10d) + preferenceScore, 2);
    }

    private static string BuildSimilarityReason(Book source, Book candidate)
    {
        var reasons = new List<string>();
        if (Same(source.Genre, candidate.Genre)) reasons.Add($"збігається жанр «{candidate.Genre}»");
        if (Same(source.Author, candidate.Author)) reasons.Add("той самий автор");
        if (Same(source.Language, candidate.Language)) reasons.Add($"та сама мова: {candidate.Language}");
        if (reasons.Count == 0) reasons.Add($"схожа на «{source.Title}» за сукупністю метаданих");
        return $"Причина: {string.Join(", ", reasons.Take(2))}.";
    }

    private static string BuildPersonalizedReason(Book candidate, IReadOnlyList<Book> sourceBooks, IReadOnlyDictionary<string, double> genrePreference, IReadOnlyDictionary<string, double> languagePreference, IReadOnlyDictionary<string, double> authorPreference, AlgorithmWeightsDto settings)
    {
        var reasons = new List<string>();
        var bestSource = sourceBooks.Select(source => new { source.Title, Score = CalculateRecommendationScore(source, candidate, settings) }).OrderByDescending(x => x.Score).FirstOrDefault();
        if (bestSource is not null && bestSource.Score > 0) reasons.Add($"схожа на «{bestSource.Title}»");
        if (authorPreference.ContainsKey(candidate.Author)) reasons.Add($"є інтерес до автора {candidate.Author}");
        if (genrePreference.ContainsKey(candidate.Genre)) reasons.Add($"ти часто обираєш жанр «{candidate.Genre}»");
        if (languagePreference.ContainsKey(candidate.Language)) reasons.Add($"є інтерес до книг мовою {candidate.Language}");
        if (reasons.Count == 0) reasons.Add("рекомендація побудована на основі обраного та попередніх переглядів");
        return $"Чому рекомендовано: {string.Join(", ", reasons.Distinct().Take(2))}.";
    }

    private static bool MatchesRanges(Book book, BookSearchQuery query)
    {
        if (query.YearFrom.HasValue && (!book.Year.HasValue || book.Year.Value < query.YearFrom.Value)) return false;
        if (query.YearTo.HasValue && (!book.Year.HasValue || book.Year.Value > query.YearTo.Value)) return false;
        if (query.MaxDurationMinutes.HasValue && book.DurationMinutes > query.MaxDurationMinutes.Value) return false;
        return true;
    }

    private static double ScoreField(string? value, string? term, double exactWeight, double startsWithWeight, double containsWeight)
    {
        if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(term)) return 0;
        var trimmedTerm = term.Trim();
        if (value.Equals(trimmedTerm, StringComparison.CurrentCultureIgnoreCase)) return exactWeight;
        if (value.StartsWith(trimmedTerm, StringComparison.CurrentCultureIgnoreCase)) return startsWithWeight;
        return value.Contains(trimmedTerm, StringComparison.CurrentCultureIgnoreCase) ? containsWeight : 0;
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static bool Same(string? left, string? right) => string.Equals(left?.Trim(), right?.Trim(), StringComparison.CurrentCultureIgnoreCase);
}


