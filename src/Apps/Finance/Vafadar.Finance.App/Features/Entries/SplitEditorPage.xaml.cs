namespace Vafadar.Finance.App.Features.Entries;

public partial class SplitEditorPage : ContentPage
{
    public SplitEditorPage(SplitEditorViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}