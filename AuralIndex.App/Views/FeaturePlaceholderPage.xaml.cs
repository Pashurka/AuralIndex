using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AuralIndex_App.Views;

public sealed partial class FeaturePlaceholderPage : Page
{
    public FeaturePlaceholderPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.Parameter is FeaturePlaceholderModel model)
        {
            DescriptionText.Text = model.Description;
        }
    }
}

public sealed record FeaturePlaceholderModel(string Title, string Description);
