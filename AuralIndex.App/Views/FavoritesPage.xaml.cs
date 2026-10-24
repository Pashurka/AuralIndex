using AuralIndex.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;

namespace AuralIndex_App.Views;

public sealed partial class FavoritesPage : Page
{
    private readonly IBookSearchService _searchService;

    public ObservableCollection<FavoriteRowVm> Entries { get; } = new();

    public FavoritesPage()
    {
        InitializeComponent();
        _searchService = App.HostContainer.Services.GetRequiredService<IBookSearchService>();
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var items = await _searchService.GetFavoritesAsync();

        Entries.Clear();
        foreach (var item in items)
        {
            Entries.Add(new FavoriteRowVm
            {
                BookId = item.BookId,
                Title = item.Title,
                Author = item.Author,
                Genre = item.Genre,
                Language = item.Language,
                CoverInitials = CoverPlaceholderHelper.GetTitleInitials(item.Title),
                GenreMark = CoverPlaceholderHelper.GetGenreMark(item.Genre),
                AddedAtText = item.CreatedAtUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm")
            });
        }

        SummaryText.Text = Entries.Count == 0
            ? "Обране порожнє"
            : $"Збережені аудіокниги: {Entries.Count}";
    }

    private void OnFavoriteClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is FavoriteRowVm row)
        {
            App.Shell?.NavigateToBookDetails(row.BookId);
        }
    }
}

public class FavoriteRowVm
{
    public int BookId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string CoverInitials { get; set; } = string.Empty;
    public string GenreMark { get; set; } = string.Empty;
    public string AddedAtText { get; set; } = string.Empty;
}
