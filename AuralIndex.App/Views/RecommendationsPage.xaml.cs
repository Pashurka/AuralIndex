using AuralIndex.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;

namespace AuralIndex_App.Views;

public sealed partial class RecommendationsPage : Page
{
    private readonly IBookSearchService _searchService;

    public ObservableCollection<RecommendationItemVm> PersonalizedRecommendations { get; } = new();

    public RecommendationsPage()
    {
        InitializeComponent();
        _searchService = App.HostContainer.Services.GetRequiredService<IBookSearchService>();
        _ = LoadPersonalizedAsync();
    }

    private async Task LoadPersonalizedAsync()
    {
        var items = await _searchService.GetPersonalizedRecommendationsAsync();

        PersonalizedRecommendations.Clear();
        foreach (var item in items)
        {
            PersonalizedRecommendations.Add(new RecommendationItemVm
            {
                Id = item.Id,
                Title = item.Title,
                Author = item.Author,
                Genre = item.Genre,
                Language = item.Language,
                CoverInitials = CoverPlaceholderHelper.GetTitleInitials(item.Title),
                GenreMark = CoverPlaceholderHelper.GetGenreMark(item.Genre),
                MatchLabel = ToPersonalizedLabel(item.Score),
                ExplanationText = item.RecommendationReason ?? string.Empty
            });
        }

        NoPersonalizedText.Visibility = PersonalizedRecommendations.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;

        PersonalHintText.Text = PersonalizedRecommendations.Count == 0
            ? "Поки що недостатньо поведінкових даних. Додай книги в обране або відкрий кілька карток, щоб система сформувала смак-профіль."
            : "Добірка враховує обране, перегляди карток та повторювані метадані: жанр, мову, автора і схожість з уже цікавими книгами.";
    }

    private void OnRecommendationClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is RecommendationItemVm recommendation)
        {
            App.Shell?.NavigateToBookDetails(recommendation.Id);
        }
    }

    private static string ToPersonalizedLabel(double score)
    {
        return score switch
        {
            >= 120 => "Дуже рекомендовано",
            >= 70 => "Рекомендовано",
            _ => "Може сподобатись"
        };
    }
}

public class RecommendationItemVm
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string CoverInitials { get; set; } = string.Empty;
    public string GenreMark { get; set; } = string.Empty;
    public string MatchLabel { get; set; } = string.Empty;
    public string ExplanationText { get; set; } = string.Empty;
}
