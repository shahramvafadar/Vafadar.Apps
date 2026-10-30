using Vafadar.Localization;

namespace Vafadar.Zanance.App.Features.Profiles;

/// <summary>Original profile navigation and explicit device choice with covered publication.</summary>
public partial class ProfilesPage : ContentPage
{
    private readonly ProfilesViewModel _viewModel;
    private readonly ILocalizationService _localization;
    private AppShell? _ownerShell;
    private bool _rootRetired;
    /// <summary>Retires this actual root without reviving its outstanding native prompts.</summary>
    internal void RetireShellPublication() { _rootRetired = true; _viewModel.RetireRoot(); }
    /// <summary>Retires a retained route only after its original navigation guard accepted departure.</summary>
    internal void RetirePublication() => _viewModel.SetActive(false);
    /// <summary>Reactivates the original return without replaying catalogue effects or discarding raw choices.</summary>
    internal Task RefreshForNavigationAsync()
    { if (_rootRetired) return Task.CompletedTask; _viewModel.SetActive(true); return _viewModel.EnsureLoadedAsync(); }
    /// <summary>Creates the actual form without reading or writing the catalogue.</summary>
    public ProfilesPage(ProfilesViewModel viewModel, ILocalizationService localization)
    { InitializeComponent(); BindingContext = _viewModel = viewModel; _localization = localization; }
    /// <inheritdoc />
    protected override async void OnAppearing()
    {
        base.OnAppearing(); if (_rootRetired) return;
        if (Shell.Current is AppShell shell)
        {
            if (_ownerShell is not null && !ReferenceEquals(_ownerShell, shell)) { RetireShellPublication(); return; }
            _ownerShell = shell; shell.RegisterProfiles(this); var root = new WeakReference<AppShell>(shell);
            _viewModel.BindRootOwner(() => root.TryGetTarget(out var original) && original.OwnsProfilesPublication
                && ReferenceEquals(Application.Current?.Windows.FirstOrDefault()?.Page, original)
                && (ReferenceEquals(original.CurrentPage, this) || Security.NativeSensitivePresentation.CoversOriginalPage(this)));
        }
        _localization.Changed -= OnDisplayChanged; _localization.Changed += OnDisplayChanged;
        await Presentation.Failures.GuardAsync(RefreshForNavigationAsync);
    }
    /// <inheritdoc />
    protected override void OnDisappearing()
    {
        _localization.Changed -= OnDisplayChanged;
        // Only the registered original authentication cover can temporarily hide this same route.
        if (!Security.NativeSensitivePresentation.CoversOriginalPage(this)) RetirePublication();
        base.OnDisappearing();
    }
    private void OnDisplayChanged(object? sender, EventArgs e) => _viewModel.RefreshDisplay();
    /// <inheritdoc />
    protected override bool OnBackButtonPressed()
    {
        if (!_viewModel.CanLeave) return true;
        if (!_viewModel.IsDirty) return base.OnBackButtonPressed();
        Dispatcher.Dispatch(async () => await _viewModel.LeaveCommand.ExecuteAsync(null)); return true;
    }
}
