namespace Vafadar.Zanance.App.Features.Holdings;

public partial class AssetEventEditorPage : ContentPage
{
    public AssetEventEditorPage(AssetEventEditorViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
