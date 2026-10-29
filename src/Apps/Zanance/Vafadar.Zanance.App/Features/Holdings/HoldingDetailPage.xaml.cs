namespace Vafadar.Zanance.App.Features.Holdings;

public partial class HoldingDetailPage : ContentPage
{
    private readonly HoldingDetailViewModel _viewModel;
    private bool _moneyPresented;
    private bool _shellRetired;
    private AppShell? _ownerShell;

    public HoldingDetailPage(HoldingDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_shellRetired) return;
        if (Shell.Current is AppShell shell)
        {
            if (_ownerShell is not null && !ReferenceEquals(_ownerShell, shell)) { RetireShellPublication(); return; }
            _ownerShell = shell; shell.RegisterHoldingDetail(this); var root = new WeakReference<AppShell>(shell);
            _viewModel.BindRootOwner(() => root.TryGetTarget(out var owner) && owner.OwnsHoldingDetailPublication);
        }
        await Presentation.Failures.GuardAsync(RefreshForNavigationAsync);
    }
    /// <summary>Resumes the original covered owner when Android retains a detail without another appearance callback.</summary>
    internal async Task RefreshForNavigationAsync()
    {
        if (_shellRetired) return;
        _moneyPresented = true;
        await LoadCoveredAsync();
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
    /// <summary>Retires the old native root, including its outstanding inline-price consent.</summary>
    internal void RetireShellPublication() { _shellRetired = true; RetirePublication(); }
    /// <summary>Retires a departed tab without discarding its original price text.</summary>
    internal void RetirePublication()
    {
        _moneyPresented = false; _viewModel.PriceEditor.SetMoneyPresentationActive(false); _viewModel.Retire();
    }
    /// <inheritdoc />
    protected override bool OnBackButtonPressed()
    {
        if (_viewModel.CanLeave)
            Dispatcher.Dispatch(async () => await Presentation.Failures.GuardAsync(() => _viewModel.BackCommand.ExecuteAsync(null)));
        return true;
    }
    protected override void OnDisappearing()
    {
        RetirePublication(); base.OnDisappearing();
    }
}
