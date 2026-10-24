using AuralIndex.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;

namespace AuralIndex_App.Views;

public sealed partial class StatisticsPage : Page
{
    private readonly IBookSearchService _searchService;

    public ObservableCollection<StatisticRowVm> TopGenres { get; } = new();
    public ObservableCollection<StatisticRowVm> TopLanguages { get; } = new();
    public ObservableCollection<StatisticRowVm> TopAuthors { get; } = new();

    public StatisticsPage()
    {
        InitializeComponent();
        _searchService = App.HostContainer.Services.GetRequiredService<IBookSearchService>();
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var stats = await _searchService.GetStatisticsAsync();

        TotalBooksText.Text = stats.TotalBooks.ToString();
        TotalGenresText.Text = stats.TotalGenres.ToString();
        TotalLanguagesText.Text = stats.TotalLanguages.ToString();
        AverageDurationText.Text = $"{stats.AverageDurationMinutes} хв";

        FillCollection(TopGenres, stats.TopGenres);
        FillCollection(TopLanguages, stats.TopLanguages);
        FillCollection(TopAuthors, stats.TopAuthors);
    }

    private static void FillCollection(ObservableCollection<StatisticRowVm> target, IReadOnlyList<StatisticEntryDto> source)
    {
        target.Clear();
        foreach (var item in source)
        {
            target.Add(new StatisticRowVm
            {
                Label = item.Label,
                CountText = item.Count.ToString()
            });
        }
    }
}

public class StatisticRowVm
{
    public string Label { get; set; } = string.Empty;
    public string CountText { get; set; } = string.Empty;
}
