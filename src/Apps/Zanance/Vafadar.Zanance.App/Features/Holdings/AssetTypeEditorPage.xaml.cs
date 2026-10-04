namespace Vafadar.Zanance.App.Features.Holdings;

public partial class AssetTypeEditorPage : ContentPage
{
    public AssetTypeEditorPage(AssetTypeEditorViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
