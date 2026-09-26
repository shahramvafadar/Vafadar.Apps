namespace Vafadar.Finance.App.Features.Forecast;

public partial class ForecastPage : ContentPage
{
    private readonly ForecastViewModel _viewModel;

    public ForecastPage(ForecastViewModel viewModel)
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
