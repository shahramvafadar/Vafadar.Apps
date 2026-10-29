namespace Vafadar.Zanance.App.Features.Reports;

public partial class KpiSheetPage : ContentPage
{
    private readonly KpiSheetViewModel _viewModel;
    private AppShell? _ownerShell;
    private bool _rootRetired;
    public KpiSheetPage(KpiSheetViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }
    /// <inheritdoc />
    protected override void OnAppearing()
    {
        base.OnAppearing(); if (_rootRetired) return;
        if (Shell.Current is AppShell shell)
        {
            if (_ownerShell is not null && !ReferenceEquals(_ownerShell, shell)) { RetireShellPublication(); return; }
            _ownerShell = shell; shell.RegisterReportSheet(this); var root = new WeakReference<AppShell>(shell);
            _viewModel.BindRootOwner(() => root.TryGetTarget(out var owner) && owner.OwnsReportPublication);
        }
        _viewModel.SetActive(true);
    }
    /// <summary>Retires an ordinary explanation departure, preserving only its own explicit close handoff.</summary>
    internal void RetirePublication() => _viewModel.SetActive(false);
    /// <summary>Permanently retires the original root and any pending close/callback.</summary>
    internal void RetireShellPublication() { _rootRetired = true; _viewModel.RetireRoot(); }
    /// <inheritdoc />
    protected override void OnDisappearing() { RetirePublication(); base.OnDisappearing(); }
    /// <inheritdoc />
    protected override bool OnBackButtonPressed() => !_viewModel.CanClose || base.OnBackButtonPressed();

}
