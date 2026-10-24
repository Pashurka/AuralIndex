namespace AuralIndex.Application;

public interface IBookSearchService
{
    Task<IReadOnlyList<BookListItemDto>> SearchAsync(string? query, CancellationToken ct = default);
    Task<IReadOnlyList<BookListItemDto>> SearchAsync(BookSearchQuery query, CancellationToken ct = default);
    Task<BookDetailsDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task LogBookViewedAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<BookListItemDto>> GetRecommendationsAsync(int bookId, int limit = 5, CancellationToken ct = default);
    Task<IReadOnlyList<BookListItemDto>> GetPersonalizedRecommendationsAsync(int limit = 8, CancellationToken ct = default);
    Task<BookStatisticsDto> GetStatisticsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<HistoryEntryDto>> GetHistoryAsync(int limit = 100, CancellationToken ct = default);
    Task<IReadOnlyList<FavoriteEntryDto>> GetFavoritesAsync(CancellationToken ct = default);
    Task<bool> AddToFavoritesAsync(int bookId, CancellationToken ct = default);
    Task<bool> RemoveFromFavoritesAsync(int bookId, CancellationToken ct = default);
    Task<bool> IsFavoriteAsync(int bookId, CancellationToken ct = default);
    Task<bool> SetListenedStatusAsync(int bookId, bool isListened, CancellationToken ct = default);
    Task<int> CreateAsync(BookEditDto book, CancellationToken ct = default);
    Task<bool> UpdateAsync(BookEditDto book, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
