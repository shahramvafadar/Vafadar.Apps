namespace Vafadar.Zanance.App.Features.Backup;

public partial class BackupPage : ContentPage
{
    private readonly BackupViewModel _viewModel;
    private AppShell? _ownerShell;
    private bool _rootRetired, _publicationActive;
    private WeakReference<Page>? _originalWindowRoot;
    /// <summary>Shares actual appearance work with retained-tab return rather than duplicating discovery.</summary>
    internal Task PendingPublication { get; private set; } = Task.CompletedTask;
    /// <summary>Terminal root retirement invalidates original restore consent and picker continuations.</summary>
    internal void RetireShellPublication() { _rootRetired = true; _viewModel.RetireRoot(); }
    /// <summary>Retires original native continuations on accepted retained-tab departure.</summary>
    internal void RetirePublication() { _publicationActive = false; _viewModel.SetPresentationActive(false); _viewModel.WatchAutomatic(false); }
    /// <summary>Reactivates only the actual original return; it never replays a prepared restore.</summary>
    internal Task RefreshForNavigationAsync()
    {
        if (_rootRetired) return Task.CompletedTask;
        if (_publicationActive) return PendingPublication;
        _publicationActive = true; _viewModel.SetPresentationActive(true); _viewModel.WatchAutomatic(true);
        return PendingPublication = PublishAfterRouteAsync();
    }
    private async Task PublishAfterRouteAsync()
    { await Task.Yield(); if (_publicationActive && !_rootRetired) await _viewModel.LoadAsync(); }

    public BackupPage(BackupViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        _viewModel.AttachPage(this);
    }

    /// <summary>Offers restore during first run, with a return to the unchanged onboarding draft (D-62).</summary>
    public void BeginOnboardingRestore(Func<Task> returnToOnboarding) => _viewModel.BeginOnboardingRestore(returnToOnboarding);

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_rootRetired) return;
        if (Shell.Current is AppShell shell && !_viewModel.IsRestoreOnly)
        {
            if (_ownerShell is not null && !ReferenceEquals(_ownerShell, shell)) { RetireShellPublication(); return; }
            _ownerShell = shell; shell.RegisterBackupRestore(this); var root = new WeakReference<AppShell>(shell);
            _viewModel.BindRootOwner(() => root.TryGetTarget(out var original) && original.OwnsBackupRestorePublication && (ReferenceEquals(original.CurrentPage, this) || Security.NativeSensitivePresentation.CoversOriginalPage(this)));
        }
        _originalWindowRoot ??= Application.Current?.Windows.FirstOrDefault()?.Page is { } rootPage ? new(rootPage) : null;
        if (_viewModel.IsRestoreOnly && _originalWindowRoot is { } firstRunRoot)
            _viewModel.BindRootOwner(() => firstRunRoot.TryGetTarget(out var original)
                && ReferenceEquals(Application.Current?.Windows.FirstOrDefault()?.Page, original)
                && original.Navigation.ModalStack.Contains(this));
        await Presentation.Failures.GuardAsync(RefreshForNavigationAsync);
    }

    /// <inheritdoc />
    protected override void OnDisappearing()
    {
        // An original PIN modal covers this page without departing its underlying route.
        // Root/tab retirement remains explicit, and the runtime also refuses an app lock cover.
        var sameRoute = false;
        if (_ownerShell is { } shell) sameRoute = shell.OwnsBackupRestorePublication && ReferenceEquals(shell.CurrentPage, this);
        else if (_originalWindowRoot is { } firstRunRoot && firstRunRoot.TryGetTarget(out var original))
            sameRoute = ReferenceEquals(Application.Current?.Windows.FirstOrDefault()?.Page, original)
                && original.Navigation.ModalStack.Contains(this);
        if (!_viewModel.IsConfirmingSensitiveOperation || !(sameRoute || Security.NativeSensitivePresentation.CoversOriginalPage(this))) RetirePublication();
        base.OnDisappearing();
    }

    /// <summary>Prevents a device Back action from closing first-run restore while data is being replaced.</summary>
    protected override bool OnBackButtonPressed()
    {
        if (!_viewModel.CanLeave) return true;
        if (_viewModel.IsRestoreOnly)
        {
            if (!_viewModel.IsBusy)
            {
                _viewModel.BackToOnboardingCommand.Execute(null);
            }

            return true;
        }

        return base.OnBackButtonPressed();
    }
}
