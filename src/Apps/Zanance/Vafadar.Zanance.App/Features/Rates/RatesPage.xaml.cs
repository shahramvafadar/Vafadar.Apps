namespace Vafadar.Zanance.App.Features.Rates;

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
        _viewModel.SetPresentationActive(true);
        await Presentation.Failures.GuardAsync(_viewModel.LoadAsync);
    }
    /// <inheritdoc />
    protected override void OnDisappearing()
    { _viewModel.SetPresentationActive(false); base.OnDisappearing(); }
}
