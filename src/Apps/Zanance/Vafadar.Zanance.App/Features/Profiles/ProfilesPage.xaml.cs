using Vafadar.Localization;

namespace Vafadar.Zanance.App.Features.Profiles;

/// <summary>Original profile navigation and explicit device choice with covered publication.</summary>
public partial class ProfilesPage : ContentPage
{
    private readonly ProfilesViewModel _viewModel;
    private readonly ILocalizationService _localization;
    /// <summary>Creates the actual form without reading or writing the catalogue.</summary>
    public ProfilesPage(ProfilesViewModel viewModel, ILocalizationService localization)
    { InitializeComponent(); BindingContext = _viewModel = viewModel; _localization = localization; }
    /// <inheritdoc />
    protected override async void OnAppearing()
    {
        base.OnAppearing(); _localization.Changed -= OnDisplayChanged; _localization.Changed += OnDisplayChanged;
        await Presentation.Failures.GuardAsync(_viewModel.EnsureLoadedAsync);
    }
    /// <inheritdoc />
    protected override void OnDisappearing() { _localization.Changed -= OnDisplayChanged; base.OnDisappearing(); }
    private void OnDisplayChanged(object? sender, EventArgs e) => _viewModel.RefreshDisplay();
    /// <inheritdoc />
    protected override bool OnBackButtonPressed()
    {
        if (!_viewModel.CanLeave) return true;
        if (!_viewModel.IsDirty) return base.OnBackButtonPressed();
        Dispatcher.Dispatch(async () => await _viewModel.LeaveCommand.ExecuteAsync(null)); return true;
    }
}
