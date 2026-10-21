namespace Vafadar.Zanance.App.Features.Holdings;

public partial class HoldingDetailPage : ContentPage
{
    private readonly HoldingDetailViewModel _viewModel;
    private bool _moneyPresented;

    public HoldingDetailPage(HoldingDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _moneyPresented = true;
        await Presentation.Failures.GuardAsync(LoadCoveredAsync);
        if (_moneyPresented) _viewModel.PriceEditor.SetMoneyPresentationActive(true);
    }
    private async void OnReloadClicked(object? sender, EventArgs e) =>
        await Presentation.Failures.GuardAsync(() => _viewModel.PriceEditor.RefreshCommand.ExecuteAsync(null));

    /// <summary>Keeps read failures on the original translated cover without a duplicate generic dialog.</summary>
    private async Task LoadCoveredAsync()
    {
        try { await _viewModel.LoadAsync(); }
        catch (Exception ex) when (ex is not OutOfMemoryException && _viewModel.Loading.HasFailed)
        {
            System.Diagnostics.Debug.WriteLine($"Original holding detail failed: {ex}");
            Presentation.Failures.Observed?.Invoke(ex);
        }
    }
    protected override void OnDisappearing()
    {
        _moneyPresented = false;
        _viewModel.PriceEditor.SetMoneyPresentationActive(false);
        _viewModel.Retire();
        base.OnDisappearing();
    }
}
