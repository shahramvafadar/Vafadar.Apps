namespace Vafadar.Zanance.App.Features.Rates;

/// <summary>Original reference lookup/review lifetime; construction performs no database or network work.</summary>
public partial class MarketReferencePage : ContentPage
{
    private readonly MarketReferenceViewModel model;
    /// <summary>Constructs only native controls and their original model binding.</summary>
    public MarketReferencePage(MarketReferenceViewModel model) { InitializeComponent(); BindingContext = this.model = model; }
    /// <inheritdoc />
    protected override async void OnAppearing() { base.OnAppearing(); await Presentation.Failures.GuardAsync(model.LoadAsync); }
    /// <inheritdoc />
    protected override void OnDisappearing() { model.Retire(); base.OnDisappearing(); }
}
