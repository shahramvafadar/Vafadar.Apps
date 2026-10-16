using Vafadar.Localization;

namespace Vafadar.Zanance.App.Features.Commerce;

/// <summary>A covered read-only comparison with growing choices and original access facts.</summary>
public partial class ProductPlansPage : ContentPage
{
    private readonly ProductPlansViewModel _viewModel;
    private readonly ILocalizationService _localization;
    /// <summary>Creates the original page without database work or purchase activation.</summary>
    public ProductPlansPage(ProductPlansViewModel viewModel, ILocalizationService localization)
    {
        InitializeComponent(); BindingContext = _viewModel = viewModel; _localization = localization;
    }
    /// <inheritdoc />
    protected override async void OnAppearing()
    {
        base.OnAppearing(); _localization.Changed -= OnDisplayChanged; _localization.Changed += OnDisplayChanged;
        await _viewModel.LoadAsync();
    }
    /// <inheritdoc />
    protected override void OnDisappearing()
    {
        _localization.Changed -= OnDisplayChanged; _viewModel.DeactivateBilling(); base.OnDisappearing();
    }
    private void OnDisplayChanged(object? sender, EventArgs e) => _viewModel.RefreshDisplay();
}
