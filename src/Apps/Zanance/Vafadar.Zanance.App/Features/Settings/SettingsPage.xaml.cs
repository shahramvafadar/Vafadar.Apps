using Vafadar.Localization;

namespace Vafadar.Zanance.App.Features.Settings;

/// <summary>Settings with covered snapshot loading and live captions on the open form.</summary>
public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel _viewModel;
    private readonly ILocalizationService _localization;
    private AppShell? _ownerShell;
    private bool _rootRetired, _publicationActive;
    internal Task PendingPublication { get; private set; } = Task.CompletedTask;

    /// <summary>Creates the form and retains localization only while the page is visible.</summary>
    public SettingsPage(SettingsViewModel viewModel, ILocalizationService localization)
    {
        _localization = localization;
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
#if ANDROID || WINDOWS
        RecoverySetupGroup.IsVisible = true;
        RecoverySetupAction.Command = new CommunityToolkit.Mvvm.Input.AsyncRelayCommand(OpenRecoverySetupAsync);
#endif
    }

    /// <summary>Opens explicit device-local recovery enrollment on protected platforms.</summary>
    private Task OpenRecoverySetupAsync()
    {
#if ANDROID || WINDOWS
        return Presentation.Failures.GuardAsync(_viewModel.OpenRecoverySetupAsync);
#else
        return Task.CompletedTask;
#endif
    }

    private async void OnLockToggled(object? sender, ToggledEventArgs e) => await Presentation.Failures.GuardAsync(() => _viewModel.SetLockAsync(e.Value));

    /// <inheritdoc />
    protected override async void OnAppearing()
    {
        base.OnAppearing(); if (_rootRetired) return;
        if (Shell.Current is AppShell shell)
        {
            if (_ownerShell is not null && !ReferenceEquals(_ownerShell, shell)) { RetireShellPublication(); return; }
            _ownerShell = shell; shell.RegisterSettings(this); var root = new WeakReference<AppShell>(shell);
            _viewModel.BindRootOwner(() => root.TryGetTarget(out var owner) && owner.OwnsSettingsPublication);
        }
        _localization.Changed -= OnDisplayChanged;
        _localization.Changed += OnDisplayChanged;
        await Presentation.Failures.GuardAsync(RefreshForNavigationAsync);
    }

    /// <inheritdoc />
    protected override void OnDisappearing()
    {
        _localization.Changed -= OnDisplayChanged;
        RetirePublication(); base.OnDisappearing();
    }

    /// <summary>Shares ordinary appearance/tab-return publication and preserves the same native draft model.</summary>
    internal Task RefreshForNavigationAsync()
    {
        if (_rootRetired) return Task.CompletedTask;
        if (_publicationActive) return PendingPublication;
        _publicationActive = true; _viewModel.SetActive(true); return PendingPublication = PublishAfterRouteAsync();
    }
    private async Task PublishAfterRouteAsync() { await Task.Yield(); if (_publicationActive && !_rootRetired) await _viewModel.LoadAsync(); }
    /// <summary>Retires actual native settings commands and source reads on page/tab departure.</summary>
    internal void RetirePublication() { _publicationActive = false; _viewModel.SetActive(false); }
    /// <summary>Permanently retires a replaced root and any pending original consent.</summary>
    internal void RetireShellPublication() { _rootRetired = true; RetirePublication(); _viewModel.RetireRoot(); }
    /// <inheritdoc />
    protected override bool OnBackButtonPressed() => _viewModel.IsBusy || base.OnBackButtonPressed();

    // D-79: keep the open form and draft text; refresh only the current page's choice captions/previews.
    private void OnDisplayChanged(object? sender, EventArgs e) => _viewModel.RefreshDisplay();

    private async void OnReloadClicked(object? sender, EventArgs e) => await Presentation.Failures.GuardAsync(_viewModel.LoadAsync);
}
