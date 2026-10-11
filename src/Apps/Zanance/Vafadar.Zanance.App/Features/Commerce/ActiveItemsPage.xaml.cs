using Vafadar.Localization;

namespace Vafadar.Zanance.App.Features.Commerce;

/// <summary>A covered growing choice form with real native targets and explicitly saved in-memory drafts.</summary>
public partial class ActiveItemsPage : ContentPage, IQueryAttributable
{
    private readonly ActiveItemsViewModel _viewModel;
    private readonly ILocalizationService _localization;

    /// <summary>Creates the page without starting database work in construction.</summary>
    public ActiveItemsPage(ActiveItemsViewModel viewModel, ILocalizationService localization)
    {
        InitializeComponent(); BindingContext = _viewModel = viewModel; _localization = localization;
    }

    /// <summary>Accepts only the existing typed initial resource destination; unknown kinds never guess another page.</summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("kind", out var kind))
        {
            if (kind is not Core.Commerce.QuotaKind target) throw new ArgumentException("Unsupported resource destination.", nameof(query));
            _viewModel.SetInitialKind(target); query.Remove("kind");
        }
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
