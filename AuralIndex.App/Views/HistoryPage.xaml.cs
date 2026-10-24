using AuralIndex.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;

namespace AuralIndex_App.Views;

public sealed partial class HistoryPage : Page
{
    private readonly IBookSearchService _searchService;

    public ObservableCollection<HistoryRowVm> Entries { get; } = new();

    public HistoryPage()
    {
        InitializeComponent();
        _searchService = App.HostContainer.Services.GetRequiredService<IBookSearchService>();
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var items = await _searchService.GetHistoryAsync();

        Entries.Clear();
        foreach (var item in items)
        {
            Entries.Add(new HistoryRowVm
            {
                BookId = item.BookId,
                EventTypeLabel = ToEventTypeLabel(item.EventType),
                Title = item.Title,
                Description = item.Description,
                CreatedAtText = item.CreatedAtUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm")
            });
        }

        SummaryText.Text = Entries.Count == 0
            ? "Історія порожня"
            : $"Останні події: {Entries.Count}";
    }

    private void OnEntryClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is HistoryRowVm row && row.BookId.HasValue)
        {
            App.Shell?.NavigateToBookDetails(row.BookId.Value);
        }
    }

    private static string ToEventTypeLabel(string eventType)
    {
        return eventType switch
        {
            "view" => "Перегляд",
            "advanced-search" => "Розширений пошук",
            "catalog-change" => "Зміна каталогу",
            "favorite" => "Обране",
            _ => "Пошук"
        };
    }
}

public class HistoryRowVm
{
    public int? BookId { get; set; }
    public string EventTypeLabel { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CreatedAtText { get; set; } = string.Empty;
}
