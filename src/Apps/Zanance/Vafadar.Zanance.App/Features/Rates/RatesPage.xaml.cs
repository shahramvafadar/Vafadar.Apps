namespace Vafadar.Zanance.App.Features.Rates;

/// <summary>Original manual-rate workspace with covered reads and actual native page/tab/root ownership.</summary>
public partial class RatesPage : ContentPage
{
    private readonly RatesViewModel _viewModel;
    private AppShell? _ownerShell;
    private bool _rootRetired, _publicationActive;
    /// <summary>Shares only this actual native page’s ordinary publication.</summary>
    internal Task PendingPublication { get; private set; } = Task.CompletedTask;
    /// <summary>Retains the actual original manual-rate model and controls.</summary>
    public RatesPage(RatesViewModel viewModel)
    { InitializeComponent(); BindingContext = _viewModel = viewModel; }
    /// <inheritdoc />
    protected override async void OnAppearing()
    {
        base.OnAppearing(); if (_rootRetired) return;
        if (Shell.Current is AppShell shell)
        {
            if (_ownerShell is not null && !ReferenceEquals(_ownerShell, shell)) { RetireShellPublication(); return; }
            _ownerShell = shell; shell.RegisterRates(this); var root = new WeakReference<AppShell>(shell);
            _viewModel.BindRootOwner(() => root.TryGetTarget(out var owner) && owner.OwnsRatesPublication);
        }
        await Presentation.Failures.GuardAsync(RefreshForNavigationAsync);
    }
    /// <inheritdoc />
    protected override void OnDisappearing() { RetirePublication(); base.OnDisappearing(); }
    /// <summary>Shares ordinary appearance and retained-tab return without a duplicate original read.</summary>
    internal Task RefreshForNavigationAsync()
    {
        if (_rootRetired) return Task.CompletedTask;
        if (_publicationActive) return PendingPublication;
        _publicationActive = true; _viewModel.SetPresentationActive(true); return PendingPublication = PublishAfterRouteAsync();
    }
    private async Task PublishAfterRouteAsync()
    {
        await Task.Yield(); if (!_publicationActive || _rootRetired) return;
        try { await _viewModel.LoadAsync(); }
        catch (Exception exception) when (exception is not OutOfMemoryException
            && (_rootRetired || !_publicationActive || _viewModel.Loading.HasFailed))
        {
            // D-649: the original current failed cover already owns Refresh; no second generic modal may hide it.
        }
    }
    /// <summary>Cancels actual source acquisition and native effects at page/tab departure.</summary>
    internal void RetirePublication() { _publicationActive = false; _viewModel.SetPresentationActive(false); }
    /// <summary>Never grants another root the original manual-rate command authority.</summary>
    internal void RetireShellPublication() { _rootRetired = true; RetirePublication(); _viewModel.RetireRoot(); }
}
