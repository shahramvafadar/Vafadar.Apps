namespace Vafadar.Zanance.App.Features.Holdings;

public partial class HoldingsPage : ContentPage
{
    private readonly HoldingsViewModel _viewModel;

    public HoldingsPage(HoldingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    private async void OnReloadClicked(object? sender, EventArgs e) => await Presentation.Failures.GuardAsync(LoadCoveredAsync);

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Presentation.Failures.GuardAsync(LoadCoveredAsync);
    }

    /// <summary>Keeps a failed load on its translated local retry cover, without a second generic modal.</summary>
    private async Task LoadCoveredAsync()
    {
        try { await _viewModel.LoadAsync(); }
        catch (Exception ex) when (ex is not OutOfMemoryException && _viewModel.Loading.HasFailed)
        {
            System.Diagnostics.Debug.WriteLine($"Original holding overview failed: {ex}");
            Presentation.Failures.Observed?.Invoke(ex);
        }
    }

    /// <inheritdoc />
    protected override void OnDisappearing()
    {
        _viewModel.Retire();
        base.OnDisappearing();
    }
}
