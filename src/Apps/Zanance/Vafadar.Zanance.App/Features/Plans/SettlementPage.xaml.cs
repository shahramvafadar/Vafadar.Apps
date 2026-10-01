namespace Vafadar.Zanance.App.Features.Plans;

public partial class SettlementPage : ContentPage
{
    public SettlementPage(SettlementViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
