namespace Vafadar.Zanance.App.Features.DataFiles;

/// <summary>Covered loading and lifetime of the exact accountant disclosure form.</summary>
public partial class AccountantExportPage : ContentPage
{
    private readonly AccountantExportViewModel _model;
    private AppShell? _ownerShell;
    private bool _rootRetired, _publicationActive;
    /// <summary>Shares the actual original appearance/read with retained-tab publication.</summary>
    internal Task PendingPublication { get; private set; } = Task.CompletedTask;
    /// <summary>Creates the native form without opening its database during construction.</summary>
    public AccountantExportPage(AccountantExportViewModel model)
    { InitializeComponent(); BindingContext = _model = model; }
    /// <inheritdoc />
    protected override async void OnAppearing()
    {
        base.OnAppearing(); if (_rootRetired) return;
        if (Shell.Current is AppShell shell)
        {
            if (_ownerShell is not null && !ReferenceEquals(_ownerShell, shell)) { RetireShellPublication(); return; }
            _ownerShell = shell; shell.RegisterAccountantExport(this); var root = new WeakReference<AppShell>(shell);
            _model.BindRootOwner(() => root.TryGetTarget(out var original) && original.OwnsAccountantExportPublication);
        }
        await Presentation.Failures.GuardAsync(RefreshForNavigationAsync);
    }
    /// <inheritdoc />
    protected override void OnDisappearing() { RetirePublication(); base.OnDisappearing(); }
    /// <summary>Activates only actual original page return, never an old public model Load.</summary>
    internal Task RefreshForNavigationAsync()
    {
        if (_rootRetired) return Task.CompletedTask;
        if (_publicationActive) return PendingPublication;
        _publicationActive = true; _model.SetPresentationActive(true);
        return PendingPublication = PublishAfterRouteAsync();
    }
    private async Task PublishAfterRouteAsync()
    { await Task.Yield(); if (_publicationActive && !_rootRetired) await _model.LoadAsync(); }
    /// <summary>Cancels original readers and unoffered output on actual page/tab departure.</summary>
    internal void RetirePublication() { _publicationActive = false; _model.Retire(); }
    /// <summary>Permanently retires the original native form when its root is replaced.</summary>
    internal void RetireShellPublication() { _rootRetired = true; RetirePublication(); _model.RetireRoot(); }
}
