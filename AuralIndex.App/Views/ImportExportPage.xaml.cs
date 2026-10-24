using AuralIndex.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AuralIndex_App.Views;

public sealed partial class ImportExportPage : Page
{
    private readonly IImportExportService _importExportService;

    public ImportExportPage()
    {
        InitializeComponent();
        _importExportService = App.HostContainer.Services.GetRequiredService<IImportExportService>();
        InitializeDefaultPaths();
    }

    private void InitializeDefaultPaths()
    {
        var transfers = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AuralIndex", "Transfers");
        ExportJsonPathBox.Text = Path.Combine(transfers, "catalog-export.json");
        ExportCsvPathBox.Text = Path.Combine(transfers, "catalog-export.csv");
        ImportJsonPathBox.Text = Path.Combine(transfers, "catalog-export.json");
        ImportCsvPathBox.Text = Path.Combine(transfers, "catalog-export.csv");
    }

    private async void OnExportJsonClick(object sender, RoutedEventArgs e)
    {
        await ExecuteAsync(() => _importExportService.ExportJsonAsync(ExportJsonPathBox.Text));
    }

    private async void OnExportCsvClick(object sender, RoutedEventArgs e)
    {
        await ExecuteAsync(() => _importExportService.ExportCsvAsync(ExportCsvPathBox.Text));
    }

    private async void OnImportJsonClick(object sender, RoutedEventArgs e)
    {
        await ExecuteAsync(() => _importExportService.ImportJsonAsync(ImportJsonPathBox.Text, ReplaceExistingCheckBox.IsChecked == true));
    }

    private async void OnImportCsvClick(object sender, RoutedEventArgs e)
    {
        await ExecuteAsync(() => _importExportService.ImportCsvAsync(ImportCsvPathBox.Text, ReplaceExistingCheckBox.IsChecked == true));
    }

    private async Task ExecuteAsync(Func<Task<ImportExportResultDto>> action)
    {
        var result = await action();
        StatusBar.Severity = result.Success ? InfoBarSeverity.Success : InfoBarSeverity.Error;
        StatusBar.Title = result.Success ? "Операцію виконано" : "Операцію не виконано";
        StatusBar.Message = result.Message;
        StatusBar.IsOpen = true;
    }
}
