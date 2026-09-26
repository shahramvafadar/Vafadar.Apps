namespace Vafadar.Finance.App.Features.Rates;

public partial class RatesPage : ContentPage
{
    private readonly RatesViewModel _viewModel;

    public RatesPage(RatesViewModel viewModel)
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
