using Vafadar.Localization;

namespace Vafadar.Zanance.App.Features.Commerce;

/// <summary>A covered growing choice form with real native targets and explicitly saved in-memory drafts.</summary>
public partial class ActiveItemsPage : ContentPage
{
    private readonly ActiveItemsViewModel _viewModel;
    private readonly ILocalizationService _localization;

    /// <summary>Creates the page without starting database work in construction.</summary>
    public ActiveItemsPage(ActiveItemsViewModel viewModel, ILocalizationService localization)
    {
        InitializeComponent(); BindingContext = _viewModel = viewModel; _localization = localization;
    }

    /// <inheritdoc />
    protected override async void OnAppearing()
    {
        base.OnAppearing(); _localization.Changed -= OnDisplayChanged; _localization.Changed += OnDisplayChanged;
        await Presentation.Failures.GuardAsync(_viewModel.EnsureLoadedAsync);
    }

    /// <inheritdoc />
    protected override void OnDisappearing()
    {
        _localization.Changed -= OnDisplayChanged; base.OnDisappearing();
    }

    private void OnDisplayChanged(object? sender, EventArgs e) => _viewModel.RefreshDisplay();

    /// <inheritdoc />
    protected override bool OnBackButtonPressed()
    {
        if (!_viewModel.CanLeave) return true;
        if (!_viewModel.IsDirty) return base.OnBackButtonPressed();
        Dispatcher.Dispatch(async () => await _viewModel.LeaveCommand.ExecuteAsync(null));
        return true;
    }
}
