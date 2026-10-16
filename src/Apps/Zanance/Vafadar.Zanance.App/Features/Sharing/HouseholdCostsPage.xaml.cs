namespace Vafadar.Zanance.App.Features.Sharing;

/// <summary>Original native actual-cost page; the persistent actions stay outside the scrolling form.</summary>
public partial class HouseholdCostsPage : ContentPage
{
    private readonly HouseholdCostsViewModel viewModel;
    /// <summary>Constructs controls only; protected storage reads wait for appearance.</summary>
    public HouseholdCostsPage(HouseholdCostsViewModel viewModel) { InitializeComponent(); BindingContext = this.viewModel = viewModel; }
    /// <inheritdoc />
    protected override async void OnAppearing() { base.OnAppearing(); await viewModel.ActivateAsync(); }
    /// <inheritdoc />
    protected override void OnDisappearing() { viewModel.Deactivate(); base.OnDisappearing(); }
    /// <inheritdoc />
    protected override bool OnBackButtonPressed() => !viewModel.CanLeave || base.OnBackButtonPressed();
}
