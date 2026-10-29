namespace Vafadar.Zanance.App.Features.Reports;

public partial class ReportsPage : ContentPage
{
    private readonly ReportsViewModel _viewModel;
    private AppShell? _ownerShell;
    private bool _rootRetired, _publicationActive;
    /// <summary>Gets shared appearance/navigation publication for this original report presence.</summary>
    internal Task PendingPublication { get; private set; } = Task.CompletedTask;

    public ReportsPage(ReportsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing(); if (_rootRetired) return;
        if (Shell.Current is AppShell shell)
        {
            if (_ownerShell is not null && !ReferenceEquals(_ownerShell, shell)) { RetireShellPublication(); return; }
            _ownerShell = shell; shell.RegisterReport(this); var root = new WeakReference<AppShell>(shell);
            _viewModel.BindRootOwner(() => root.TryGetTarget(out var owner) && owner.OwnsReportPublication);
        }
        await Presentation.Failures.GuardAsync(RefreshForNavigationAsync);
    }

    /// <summary>Reloads the retained report from its original file after an actual tab return.</summary>
    internal Task RefreshForNavigationAsync()
    {
        if (_rootRetired) return Task.CompletedTask;
        if (_publicationActive) return PendingPublication;
        _publicationActive = true; _viewModel.SetActive(true); return PendingPublication = PublishAfterRouteAsync();
    }
    private async Task PublishAfterRouteAsync() { await Task.Yield(); if (_publicationActive && !_rootRetired) await _viewModel.LoadAsync(); }
    /// <summary>Retires the actual report parent while allowing only independently captured source-bound child handoffs.</summary>
    internal void RetirePublication() { _publicationActive = false; _viewModel.SetActive(false); }
    /// <summary>Permanently retires the old root and source ownership.</summary>
    internal void RetireShellPublication() { _rootRetired = true; RetirePublication(); _viewModel.RetireRoot(); }
    /// <inheritdoc />
    protected override void OnDisappearing() { RetirePublication(); base.OnDisappearing(); }
    /// <inheritdoc />
    protected override bool OnBackButtonPressed() => _viewModel.IsBusy || base.OnBackButtonPressed();

    // Chart axes draw their own text, so Persian digits (D-27) are applied to each label.
    private void OnAxisLabelCreated(object? sender, Syncfusion.Maui.Charts.ChartAxisLabelEventArgs e) =>
        e.Label = Vafadar.Localization.Formatting.NativeDigits.Apply(e.Label) ?? e.Label;
}
