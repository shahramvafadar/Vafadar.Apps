using Vafadar.Localization;

namespace Vafadar.Zanance.App.Features.Settings;

/// <summary>Settings with covered snapshot loading and live captions on the open form.</summary>
public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel _viewModel;
    private readonly ILocalizationService _localization;

    /// <summary>Creates the form and retains localization only while the page is visible.</summary>
    public SettingsPage(SettingsViewModel viewModel, ILocalizationService localization)
    {
        _localization = localization;
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    private async void OnLockToggled(object? sender, ToggledEventArgs e) => await Presentation.Failures.GuardAsync(() => _viewModel.SetLockAsync(e.Value));

    /// <inheritdoc />
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _localization.Changed -= OnDisplayChanged;
        _localization.Changed += OnDisplayChanged;
        await Presentation.Failures.GuardAsync(_viewModel.LoadAsync);
    }

    /// <inheritdoc />
    protected override void OnDisappearing()
    {
        _localization.Changed -= OnDisplayChanged;
        base.OnDisappearing();
    }

    // D-79: keep the open form and draft text; refresh only the current page's choice captions/previews.
    private void OnDisplayChanged(object? sender, EventArgs e) => _viewModel.RefreshDisplay();

    private async void OnReloadClicked(object? sender, EventArgs e) => await Presentation.Failures.GuardAsync(_viewModel.LoadAsync);
}
