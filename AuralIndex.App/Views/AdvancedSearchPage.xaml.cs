using AuralIndex.Application;
using AuralIndex.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;

namespace AuralIndex_App.Views;

public sealed partial class AdvancedSearchPage : Page
{
    private readonly IBookSearchService _searchService;
    public ObservableCollection<AdvancedBookRowVm> Books { get; } = new();

    public AdvancedSearchPage()
    {
        InitializeComponent();
        _searchService = App.HostContainer.Services.GetRequiredService<IBookSearchService>();
        _ = SearchAsync();
    }

    private async Task SearchAsync()
    {
        var query = new BookSearchQuery
        {
            Title = TitleBox.Text,
            Author = AuthorBox.Text,
            Genre = GenreBox.Text,
            Language = LanguageBox.Text,
            Narrator = NarratorBox.Text,
            YearFrom = ToNullableInt(YearFromBox.Value),
            YearTo = ToNullableInt(YearToBox.Value),
            MaxDurationMinutes = ToNullableInt(MaxDurationBox.Value)
        };

        var items = await _searchService.SearchAsync(query);

        Books.Clear();
        foreach (var item in items)
        {
            Books.Add(new AdvancedBookRowVm
            {
                Id = item.Id,
                Title = item.Title,
                Author = item.Author,
                Genre = item.Genre,
                Language = item.Language,
                CoverInitials = CoverPlaceholderHelper.GetTitleInitials(item.Title),
                GenreMark = CoverPlaceholderHelper.GetGenreMark(item.Genre),
                DurationText = $"{item.DurationMinutes} хв",
                ScoreText = ToRelevanceLabel(item.Score)
            });
        }
    }

    private async void OnSearchClick(object sender, RoutedEventArgs e) =>
        await SearchAsync();

    private async void OnClearClick(object sender, RoutedEventArgs e)
    {
        TitleBox.Text = string.Empty;
        AuthorBox.Text = string.Empty;
        GenreBox.Text = string.Empty;
        LanguageBox.Text = string.Empty;
        NarratorBox.Text = string.Empty;
        YearFromBox.Value = double.NaN;
        YearToBox.Value = double.NaN;
        MaxDurationBox.Value = double.NaN;

        await SearchAsync();
    }

    private void OnBookItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is AdvancedBookRowVm book)
        {
            App.Shell?.NavigateToBookDetails(book.Id);
        }
    }

    private static int? ToNullableInt(double value)
    {
        return double.IsNaN(value) ? null : (int)value;
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

public class AdvancedBookRowVm
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string CoverInitials { get; set; } = string.Empty;
    public string GenreMark { get; set; } = string.Empty;
    public string DurationText { get; set; } = string.Empty;
    public string ScoreText { get; set; } = string.Empty;
}
