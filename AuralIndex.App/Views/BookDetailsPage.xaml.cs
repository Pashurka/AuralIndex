using AuralIndex.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System.Collections.ObjectModel;

namespace AuralIndex_App.Views;

public sealed partial class BookDetailsPage : Page
{
    private readonly IBookSearchService _searchService;
    private int _bookId;
    private bool _isFavorite;
    private bool _isListened;

    public ObservableCollection<RecommendationRowVm> Recommendations { get; } = new();

    public BookDetailsPage()
    {
        InitializeComponent();
        _searchService = App.HostContainer.Services.GetRequiredService<IBookSearchService>();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.Parameter is int bookId)
        {
            _bookId = bookId;
            await LoadAsync(bookId);
            await _searchService.LogBookViewedAsync(bookId);
            await LoadRecommendationsAsync(bookId);
        }
    }

    private async Task LoadAsync(int bookId)
    {
        var book = await _searchService.GetByIdAsync(bookId);
        if (book is null)
        {
            NotFoundBar.IsOpen = true;
            return;
        }

        _isFavorite = book.IsFavorite;
        _isListened = book.IsListened;
        FavoriteButton.Content = _isFavorite ? "Прибрати з обраного" : "Додати в обране";
        ListenedStatusButton.Content = _isListened ? "Позначити як не прослухану" : "Позначити як прослухану";
        CoverInitialsText.Text = CoverPlaceholderHelper.GetTitleInitials(book.Title);
        CoverGenreText.Text = CoverPlaceholderHelper.GetGenreMark(book.Genre);
        TitleText.Text = book.Title;
        AuthorText.Text = book.Author;
        GenreText.Text = book.Genre;
        LanguageText.Text = book.Language;
        YearText.Text = book.Year?.ToString() ?? "Не вказано";
        NarratorText.Text = string.IsNullOrWhiteSpace(book.Narrator) ? "Не вказано" : book.Narrator;
        DurationText.Text = $"{book.DurationMinutes} хв";
        ListenedText.Text = book.IsListened ? "Слухав" : "Не слухав";
        DescriptionText.Text = string.IsNullOrWhiteSpace(book.Description)
            ? "Опис аудіокниги поки не заповнено."
            : book.Description;
    }

    private async Task LoadRecommendationsAsync(int bookId)
    {
        var items = await _searchService.GetRecommendationsAsync(bookId);

        Recommendations.Clear();
        foreach (var item in items)
        {
            Recommendations.Add(new RecommendationRowVm
            {
                Id = item.Id,
                Title = item.Title,
                Author = item.Author,
                Genre = item.Genre,
                Language = item.Language,
                CoverInitials = CoverPlaceholderHelper.GetTitleInitials(item.Title),
                GenreMark = CoverPlaceholderHelper.GetGenreMark(item.Genre),
                MatchText = ToRecommendationLabel(item.Score)
            });
        }

        NoRecommendationsText.Visibility = Recommendations.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void OnFavoriteClick(object sender, RoutedEventArgs e)
    {
        if (_isFavorite)
        {
            await _searchService.RemoveFromFavoritesAsync(_bookId);
            _isFavorite = false;
        }
        else
        {
            await _searchService.AddToFavoritesAsync(_bookId);
            _isFavorite = true;
        }

        FavoriteButton.Content = _isFavorite ? "Прибрати з обраного" : "Додати в обране";
    }

    private async void OnListenedStatusClick(object sender, RoutedEventArgs e)
    {
        var newStatus = !_isListened;
        var changed = await _searchService.SetListenedStatusAsync(_bookId, newStatus);
        if (!changed)
        {
            return;
        }

        _isListened = newStatus;
        ListenedText.Text = _isListened ? "Слухав" : "Не слухав";
        ListenedStatusButton.Content = _isListened ? "Позначити як не прослухану" : "Позначити як прослухану";
    }

    private void OnRecommendationClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is RecommendationRowVm recommendation)
        {
            App.Shell?.NavigateToBookDetails(recommendation.Id);
        }
    }

    private void OnEditClick(object sender, RoutedEventArgs e)
    {
        App.Shell?.NavigateToBookEditor(_bookId);
    }

    private async void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Видалити аудіокнигу?",
            Content = "Запис буде вилучено з локального каталогу.",
            PrimaryButtonText = "Видалити",
            CloseButtonText = "Скасувати",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await _searchService.DeleteAsync(_bookId);
            App.Shell?.NavigateToCatalog();
        }
    }

    private static string ToRecommendationLabel(double score)
    {
        return score switch
        {
            >= 60 => "Дуже схожа",
            >= 35 => "Схожа",
            _ => "Частково схожа"
        };
    }
}

public class RecommendationRowVm
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string CoverInitials { get; set; } = string.Empty;
    public string GenreMark { get; set; } = string.Empty;
    public string MatchText { get; set; } = string.Empty;
}
