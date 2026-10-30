namespace Vafadar.Zanance.App.Features.DataFiles;

/// <summary>Owns original CSV publication, raw drafts and native continuations for one actual root.</summary>
public partial class ImportExportPage : ContentPage
{
    private readonly ImportExportViewModel _viewModel;
    private AppShell? _ownerShell;
    private bool _appearing, _rootRetired;
    /// <summary>Shares actual appearance loading with a retained-tab return.</summary>
    internal Task PendingPublication { get; private set; } = Task.CompletedTask;
    /// <summary>Creates the form without opening its original database.</summary>
    public ImportExportPage(ImportExportViewModel viewModel)
    { InitializeComponent(); BindingContext = _viewModel = viewModel; }
    /// <inheritdoc />
    protected override async void OnAppearing()
    {
        base.OnAppearing(); if (_rootRetired) return;
        if (Shell.Current is AppShell shell)
        {
            if (_ownerShell is not null && !ReferenceEquals(_ownerShell, shell)) { RetireShellPublication(); return; }
            _ownerShell = shell; shell.RegisterDataFiles(this); var root = new WeakReference<AppShell>(shell);
            _viewModel.BindRootOwner(() => root.TryGetTarget(out var original) && original.OwnsDataFilePublication);
        }
        await Presentation.Failures.GuardAsync(RefreshForNavigationAsync);
    }
    /// <summary>Refreshes metadata only on actual page return, retaining raw mapping and known receipts.</summary>
    internal Task RefreshForNavigationAsync()
    {
        if (_rootRetired) return Task.CompletedTask;
        if (_appearing) return PendingPublication;
        _appearing = true; _viewModel.SetPresentationActive(true);
        _viewModel.LedgerPreviewPageChanged -= OnLedgerPreviewPageChanged;
        _viewModel.LedgerPreviewPageChanged += OnLedgerPreviewPageChanged;
        return PendingPublication = PublishAfterRouteAsync();
    }
    private async Task PublishAfterRouteAsync()
    { await Task.Yield(); if (_appearing && !_rootRetired) await _viewModel.LoadAsync(); }
    /// <inheritdoc />
    protected override void OnDisappearing() { RetirePublication(); base.OnDisappearing(); }
    /// <summary>Cancels pending original readers and native consent on actual accepted departure.</summary>
    internal void RetirePublication()
    {
        _appearing = false; _viewModel.LedgerPreviewPageChanged -= OnLedgerPreviewPageChanged;
        _viewModel.Deactivate();
    }
    /// <summary>Terminally retires original consent when the actual application root changes.</summary>
    internal void RetireShellPublication() { _rootRetired = true; RetirePublication(); _viewModel.RetireRoot(); }
    // D-332: scroll only the still-arranged current source after replacing interpreted cards.
    private void OnLedgerPreviewPageChanged(object? sender, EventArgs e) => Dispatcher.Dispatch(async () =>
    {
        if (!_appearing || _rootRetired || !_viewModel.Loading.IsReady || !_viewModel.HasLedgerPreview) return;
        await Presentation.Failures.GuardAsync(() => ImportViewport.ScrollToAsync(LedgerPreviewSection, ScrollToPosition.Start, animated: false));
    });
}
