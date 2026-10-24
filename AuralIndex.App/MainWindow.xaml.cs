using AuralIndex_App.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AuralIndex_App
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Title = "AuralIndex";
            AppWindow.SetIcon("Assets/AppIcon.ico");
            RootFrame.Navigate(typeof(CatalogPage));
            UpdateBackButtonState();
        }

        public void NavigateToBookDetails(int bookId)
        {
            SetHeader("Картка аудіокниги", "audiobook-details.svg");
            RootFrame.Navigate(typeof(BookDetailsPage), bookId);
            UpdateBackButtonState();
        }

        public void NavigateToBookEditor(int? bookId = null)
        {
            SetHeader(bookId.HasValue ? "Редагування аудіокниги" : "Додавання аудіокниги", "edit-audiobook.svg");
            RootFrame.Navigate(typeof(BookEditPage), bookId);
            UpdateBackButtonState();
        }

        public void NavigateToCatalog()
        {
            SetHeader("Каталог аудіокниг", "catalog.svg");
            RootFrame.Navigate(typeof(CatalogPage));
            UpdateBackButtonState();
        }

        private void OnNavigationSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is not NavigationViewItem item || item.Tag is not string tag)
            {
                return;
            }

            switch (tag)
            {
                case "catalog":
                    NavigateToCatalog();
                    break;
                case "edit":
                    NavigateToBookEditor();
                    break;
                case "advanced-search":
                    SetHeader("Розширений пошук", "advanced-search.svg");
                    RootFrame.Navigate(typeof(AdvancedSearchPage));
                    UpdateBackButtonState();
                    break;
                case "recommendations":
                    SetHeader("Рекомендації", "recommendations.svg");
                    RootFrame.Navigate(typeof(RecommendationsPage));
                    UpdateBackButtonState();
                    break;
                case "favorites":
                    SetHeader("Обране", "favorites.svg");
                    RootFrame.Navigate(typeof(FavoritesPage));
                    UpdateBackButtonState();
                    break;
                case "history":
                    SetHeader("Історія", "history.svg");
                    RootFrame.Navigate(typeof(HistoryPage));
                    UpdateBackButtonState();
                    break;
                case "statistics":
                    SetHeader("Статистика", "statistics.svg");
                    RootFrame.Navigate(typeof(StatisticsPage));
                    UpdateBackButtonState();
                    break;
                case "import-export":
                    SetHeader("Імпорт / експорт", "import-export.svg");
                    RootFrame.Navigate(typeof(ImportExportPage));
                    UpdateBackButtonState();
                    break;
                case "settings":
                    SetHeader("Налаштування", "settings.svg");
                    RootFrame.Navigate(typeof(SettingsPage));
                    UpdateBackButtonState();
                    break;
            }
        }

        private void SetHeader(string title, string iconFileName)
        {
            HeaderTitle.Text = title;
            HeaderIcon.UriSource = new System.Uri($"ms-appx:///Assets/Icons/{iconFileName}");
        }

        private void OnNavigationBackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            if (RootFrame.CanGoBack)
            {
                RootFrame.GoBack();
                UpdateHeaderFromCurrentPage();
            }

            UpdateBackButtonState();
        }

        private void UpdateBackButtonState()
        {
            Navigation.IsBackEnabled = RootFrame.CanGoBack;
        }

        private void UpdateHeaderFromCurrentPage()
        {
            if (RootFrame.CurrentSourcePageType == typeof(CatalogPage))
            {
                SetHeader("Каталог аудіокниг", "catalog.svg");
            }
            else if (RootFrame.CurrentSourcePageType == typeof(AdvancedSearchPage))
            {
                SetHeader("Розширений пошук", "advanced-search.svg");
            }
            else if (RootFrame.CurrentSourcePageType == typeof(BookDetailsPage))
            {
                SetHeader("Картка аудіокниги", "audiobook-details.svg");
            }
            else if (RootFrame.CurrentSourcePageType == typeof(BookEditPage))
            {
                SetHeader("Додавання / редагування", "edit-audiobook.svg");
            }
            else if (RootFrame.CurrentSourcePageType == typeof(RecommendationsPage))
            {
                SetHeader("Рекомендації", "recommendations.svg");
            }
            else if (RootFrame.CurrentSourcePageType == typeof(FavoritesPage))
            {
                SetHeader("Обране", "favorites.svg");
            }
            else if (RootFrame.CurrentSourcePageType == typeof(HistoryPage))
            {
                SetHeader("Історія", "history.svg");
            }
            else if (RootFrame.CurrentSourcePageType == typeof(StatisticsPage))
            {
                SetHeader("Статистика", "statistics.svg");
            }
            else if (RootFrame.CurrentSourcePageType == typeof(ImportExportPage))
            {
                SetHeader("Імпорт / експорт", "import-export.svg");
            }
            else if (RootFrame.CurrentSourcePageType == typeof(SettingsPage))
            {
                SetHeader("Налаштування", "settings.svg");
            }
        }
    }
}
