namespace Vafadar.Zanance.App.Features.Forecast;

public partial class SnapshotPage : ContentPage
{
    private readonly SnapshotViewModel _viewModel;
    private AppShell? _ownerShell;
    private bool _shellRetired, _publicationActive;

    /// <summary>Gets the original appearance/navigation read without starting a substitute comparison.</summary>
    internal Task PendingPublication { get; private set; } = Task.CompletedTask;

    public SnapshotPage(SnapshotViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing(); if (_shellRetired) return;
        if (Shell.Current is AppShell shell)
        {
            if (_ownerShell is not null && !ReferenceEquals(_ownerShell, shell)) { RetireShellPublication(); return; }
            _ownerShell = shell; shell.RegisterSavedComparison(this); var root = new WeakReference<AppShell>(shell);
            _viewModel.BindRootOwner(() => root.TryGetTarget(out var owner) && owner.OwnsSavedComparisonPublication);
        }
        await Presentation.Failures.GuardAsync(RefreshForNavigationAsync);
    }

    /// <summary>Shares the original covered comparison read after appearance or retained Shell-tab activation.</summary>
    internal Task RefreshForNavigationAsync()
    {
        if (_shellRetired) return Task.CompletedTask;
        if (_publicationActive) return PendingPublication;
        _publicationActive = true; _viewModel.SetActive(true);
        return PendingPublication = PublishAfterRouteAsync();
    }
    private async Task PublishAfterRouteAsync()
    { await Task.Yield(); if (_publicationActive && !_shellRetired) await _viewModel.LoadAsync(); }
    /// <summary>Retires the actual retained tab and pending saved-row consent together.</summary>
    internal void RetirePublication() { _publicationActive = false; _viewModel.SetActive(false); }
    /// <summary>Permanently retires the old native root and its localization subscription.</summary>
    internal void RetireShellPublication() { _shellRetired = true; RetirePublication(); }
    /// <inheritdoc />
    protected override void OnDisappearing() { RetirePublication(); base.OnDisappearing(); }
    /// <inheritdoc />
    protected override bool OnBackButtonPressed() => !_viewModel.CanLeave || base.OnBackButtonPressed();
}
