using AuralIndex.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AuralIndex_App.Views;

public sealed partial class SettingsPage : Page
{
    private readonly IAlgorithmSettingsService _settingsService;

    public SettingsPage()
    {
        InitializeComponent();
        _settingsService = App.HostContainer.Services.GetRequiredService<IAlgorithmSettingsService>();
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var settings = await _settingsService.GetAsync();
        Apply(settings);
        StatusBar.IsOpen = false;
    }

    private void Apply(AlgorithmWeightsDto settings)
    {
        SearchTitleExactBox.Value = settings.SearchTitleExact;
        SearchTitleStartsWithBox.Value = settings.SearchTitleStartsWith;
        SearchTitleContainsBox.Value = settings.SearchTitleContains;
        SearchAuthorExactBox.Value = settings.SearchAuthorExact;
        SearchGenreExactBox.Value = settings.SearchGenreExact;
        RecommendationGenreBox.Value = settings.RecommendationGenre;
        RecommendationLanguageBox.Value = settings.RecommendationLanguage;
        RecommendationAuthorBox.Value = settings.RecommendationAuthor;
        PersonalizedFavoriteBoostBox.Value = settings.PersonalizedFavoriteBoost;
        PersonalizedViewStartBoostBox.Value = settings.PersonalizedViewStartBoost;
        PersonalizedGenrePreferenceBox.Value = settings.PersonalizedGenrePreference;
    }

    private AlgorithmWeightsDto Read()
    {
        return new AlgorithmWeightsDto
        {
            SearchTitleExact = SearchTitleExactBox.Value,
            SearchTitleStartsWith = SearchTitleStartsWithBox.Value,
            SearchTitleContains = SearchTitleContainsBox.Value,
            SearchAuthorExact = SearchAuthorExactBox.Value,
            SearchGenreExact = SearchGenreExactBox.Value,
            RecommendationGenre = RecommendationGenreBox.Value,
            RecommendationLanguage = RecommendationLanguageBox.Value,
            RecommendationAuthor = RecommendationAuthorBox.Value,
            PersonalizedFavoriteBoost = PersonalizedFavoriteBoostBox.Value,
            PersonalizedViewStartBoost = PersonalizedViewStartBoostBox.Value,
            PersonalizedGenrePreference = PersonalizedGenrePreferenceBox.Value
        };
    }

    private async void OnSaveClick(object sender, RoutedEventArgs e)
    {
        await _settingsService.SaveAsync(Read());
        StatusBar.Severity = InfoBarSeverity.Success;
        StatusBar.Title = "Налаштування оновлено";
        StatusBar.Message = "Нові ваги збережені. Пошук і рекомендації використовуватимуть їх у наступних запитах.";
        StatusBar.IsOpen = true;
    }

    private async void OnResetClick(object sender, RoutedEventArgs e)
    {
        await _settingsService.ResetAsync();
        await LoadAsync();
        StatusBar.Severity = InfoBarSeverity.Informational;
        StatusBar.Title = "Стандартні значення відновлено";
        StatusBar.Message = "Використовуються базові ваги алгоритму.";
        StatusBar.IsOpen = true;
    }

    private async void OnReloadClick(object sender, RoutedEventArgs e)
    {
        await LoadAsync();
        StatusBar.Severity = InfoBarSeverity.Informational;
        StatusBar.Title = "Значення оновлено";
        StatusBar.Message = "На сторінку завантажено останні збережені ваги.";
        StatusBar.IsOpen = true;
    }
}
