namespace Vafadar.Finance.App.Features.Rates;

public partial class DisplayUnitsPage : ContentPage
{
    private readonly DisplayUnitsViewModel _viewModel;

    public DisplayUnitsPage(DisplayUnitsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
}