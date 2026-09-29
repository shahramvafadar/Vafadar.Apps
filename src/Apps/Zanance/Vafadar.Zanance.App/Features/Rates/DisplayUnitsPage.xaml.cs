namespace Vafadar.Zanance.App.Features.Rates;

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
        await Presentation.Failures.GuardAsync(_viewModel.LoadAsync);
    }
}