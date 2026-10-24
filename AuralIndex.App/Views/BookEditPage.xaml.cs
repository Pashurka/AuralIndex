using AuralIndex.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AuralIndex_App.Views;

public sealed partial class BookEditPage : Page
{
    private readonly IBookSearchService _bookService;
    private int? _bookId;

    public BookEditPage()
    {
        InitializeComponent();
        _bookService = App.HostContainer.Services.GetRequiredService<IBookSearchService>();
        SetComboBoxValue(LanguageBox, "Українська");
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.Parameter is int bookId)
        {
            _bookId = bookId;
            await LoadAsync(bookId);
        }
    }

    private async Task LoadAsync(int bookId)
    {
        var book = await _bookService.GetByIdAsync(bookId);
        if (book is null)
        {
            ShowValidation("Аудіокнигу не знайдено.");
            return;
        }

        TitleBox.Text = book.Title;
        AuthorBox.Text = book.Author;
        SetComboBoxValue(GenreBox, book.Genre);
        SetComboBoxValue(LanguageBox, book.Language);
        YearBox.Value = book.Year ?? double.NaN;
        DurationBox.Value = book.DurationMinutes;
        NarratorBox.Text = book.Narrator ?? string.Empty;
        DescriptionBox.Text = book.Description ?? string.Empty;
        IsListenedCheckBox.IsChecked = book.IsListened;
    }

    private async void OnSaveClick(object sender, RoutedEventArgs e)
    {
        if (!Validate())
        {
            return;
        }

        var dto = new BookEditDto
        {
            Id = _bookId ?? 0,
            Title = TitleBox.Text,
            Author = AuthorBox.Text,
            Genre = GetComboBoxValue(GenreBox),
            Language = GetComboBoxValue(LanguageBox),
            Year = ToNullableInt(YearBox.Value),
            DurationMinutes = ToRequiredInt(DurationBox.Value),
            Narrator = NarratorBox.Text,
            Description = DescriptionBox.Text,
            IsListened = IsListenedCheckBox.IsChecked == true
        };

        if (_bookId.HasValue)
        {
            await _bookService.UpdateAsync(dto);
            App.Shell?.NavigateToBookDetails(_bookId.Value);
        }
        else
        {
            var newId = await _bookService.CreateAsync(dto);
            App.Shell?.NavigateToBookDetails(newId);
        }
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        if (_bookId.HasValue)
        {
            App.Shell?.NavigateToBookDetails(_bookId.Value);
        }
        else
        {
            App.Shell?.NavigateToCatalog();
        }
    }

    private bool Validate()
    {
        if (string.IsNullOrWhiteSpace(TitleBox.Text) ||
            string.IsNullOrWhiteSpace(AuthorBox.Text) ||
            string.IsNullOrWhiteSpace(GetComboBoxValue(GenreBox)) ||
            string.IsNullOrWhiteSpace(GetComboBoxValue(LanguageBox)))
        {
            ShowValidation("Назва, автор, жанр і мова є обов'язковими.");
            return false;
        }

        if (ToRequiredInt(DurationBox.Value) <= 0)
        {
            ShowValidation("Тривалість має бути більшою за 0 хв.");
            return false;
        }

        ValidationBar.IsOpen = false;
        return true;
    }

    private void ShowValidation(string message)
    {
        ValidationBar.Message = message;
        ValidationBar.IsOpen = true;
    }

    private static int? ToNullableInt(double value) => double.IsNaN(value) || value <= 0 ? null : (int)value;
    private static int ToRequiredInt(double value) => double.IsNaN(value) ? 0 : (int)value;
    private static string GetComboBoxValue(ComboBox comboBox) => (comboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty;

    private static void SetComboBoxValue(ComboBox comboBox, string? value)
    {
        value = NormalizeLanguageName(value);

        foreach (var item in comboBox.Items.OfType<ComboBoxItem>())
        {
            if (string.Equals(item.Content?.ToString(), value, StringComparison.CurrentCultureIgnoreCase))
            {
                comboBox.SelectedItem = item;
                return;
            }
        }

        comboBox.SelectedIndex = -1;
    }

    private static string? NormalizeLanguageName(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "uk" => "Українська",
            "en" => "Англійська",
            "de" => "Німецька",
            "fr" => "Французька",
            "pl" => "Польська",
            _ => value
        };
    }
}
