namespace Vafadar.Zanance.App.Features.Rates;

/// <summary>Actual original unit page with covered reads, retained raw input and page/tab/root authority.</summary>
public partial class DisplayUnitsPage : ContentPage
{
    private readonly DisplayUnitsViewModel _viewModel;
    private AppShell? _ownerShell;
    private bool _rootRetired, _publicationActive;
    /// <summary>Shares only this actual native page's ordinary publication.</summary>
    internal Task PendingPublication { get; private set; } = Task.CompletedTask;
    /// <summary>Retains the original explicit unit controls and model.</summary>
    public DisplayUnitsPage(DisplayUnitsViewModel viewModel)
    { InitializeComponent(); BindingContext = _viewModel = viewModel; }
    /// <inheritdoc />
    protected override async void OnAppearing()
    {
        base.OnAppearing(); if (_rootRetired) return;
        if (Shell.Current is AppShell shell)
        {
            if (_ownerShell is not null && !ReferenceEquals(_ownerShell, shell)) { RetireShellPublication(); return; }
            _ownerShell = shell; shell.RegisterDisplayUnits(this); var root = new WeakReference<AppShell>(shell);
            _viewModel.BindRootOwner(() => root.TryGetTarget(out var owner) && owner.OwnsDisplayUnitsPublication);
        }
        await Presentation.Failures.GuardAsync(RefreshForNavigationAsync);
    }
    /// <inheritdoc />
    protected override void OnDisappearing() { RetirePublication(); base.OnDisappearing(); }
    /// <summary>Activates only actual native appearance/return, sharing the original read across Shell callbacks.</summary>
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
        catch (Exception exception) when (exception is not OutOfMemoryException && (_rootRetired || !_publicationActive || _viewModel.Loading.HasFailed))
        { /* The original inline source cover owns Refresh; no generic modal may hide it. */ }
    }
    /// <summary>Cancels original source acquisition and native consent on actual page/tab departure.</summary>
    internal void RetirePublication() { _publicationActive = false; _viewModel.Deactivate(); }
    /// <summary>Never grants a replacement application root this original unit form's commands.</summary>
    internal void RetireShellPublication() { _rootRetired = true; RetirePublication(); _viewModel.RetireRoot(); }
}
