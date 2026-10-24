using AuralIndex.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;

namespace AuralIndex_App.Views;

public sealed partial class CatalogPage : Page
{
    private readonly IBookSearchService _searchService;
    public ObservableCollection<BookRowVm> Books { get; } = new();

    public CatalogPage()
    {
        InitializeComponent();
        _searchService = App.HostContainer.Services.GetRequiredService<IBookSearchService>();
        _ = LoadAsync(null);
    }

    private async Task LoadAsync(string? query)
    {
        var items = await _searchService.SearchAsync(query);

        Books.Clear();
        foreach (var item in items)
        {
            Books.Add(new BookRowVm
            {
                Id = item.Id,
                Title = item.Title,
                Author = item.Author,
                Genre = item.Genre,
                Language = item.Language,
                CoverInitials = CoverPlaceholderHelper.GetTitleInitials(item.Title),
                GenreMark = CoverPlaceholderHelper.GetGenreMark(item.Genre),
                StatusText = item.IsListened ? "Слухав" : "Не слухав",
                DurationText = $"{item.DurationMinutes} хв",
                ScoreText = ToRelevanceLabel(item.Score)
            });
        }
    }

    private async void OnSearchClick(object sender, RoutedEventArgs e) =>
        await LoadAsync(SearchBox.Text);

    private async void OnClearClick(object sender, RoutedEventArgs e)
    {
        SearchBox.Text = string.Empty;
        await LoadAsync(null);
    }

    private void OnAddClick(object sender, RoutedEventArgs e)
    {
        App.Shell?.NavigateToBookEditor();
    }

    private void OnBookItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is BookRowVm book)
        {
            App.Shell?.NavigateToBookDetails(book.Id);
        }
    }

    private static string ToRelevanceLabel(double score)
    {
        return score switch
        {
            >= 45 => "Висока",
            >= 18 => "Середня",
            > 0 => "Низька",
            _ => "-"
        };
    }
}

public class BookRowVm
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string CoverInitials { get; set; } = string.Empty;
    public string GenreMark { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
    public string DurationText { get; set; } = string.Empty;
    public string ScoreText { get; set; } = string.Empty;
}
