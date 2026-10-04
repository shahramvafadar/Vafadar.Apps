namespace Vafadar.Zanance.App.Features.Holdings;

public partial class HoldingsPage : ContentPage
{
    private readonly HoldingsViewModel _viewModel;

    public HoldingsPage(HoldingsViewModel viewModel)
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
