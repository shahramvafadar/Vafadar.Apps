using Vafadar.Localization;

namespace Vafadar.Zanance.App.Features.Commerce.Billing;

/// <summary>Explicit owned account entry; opening it does not share profiles, create a financial account or launch payment.</summary>
public partial class BillingIdentityPage : ContentPage
{
    private readonly BillingIdentityViewModel model;
    private readonly ILocalizationService localization;
    /// <summary>Builds controls only; private session/credential effects wait for actual appearance and explicit actions.</summary>
    public BillingIdentityPage(BillingIdentityViewModel model, ILocalizationService localization)
    { InitializeComponent(); BindingContext = this.model = model; this.localization = localization; }
    /// <inheritdoc />
    protected override async void OnAppearing()
    {
        base.OnAppearing(); localization.Changed -= OnLanguageChanged; localization.Changed += OnLanguageChanged;
        await model.ActivateAsync();
    }
    /// <inheritdoc />
    protected override void OnDisappearing()
    { localization.Changed -= OnLanguageChanged; model.Deactivate(); base.OnDisappearing(); }
    private void OnLanguageChanged(object? sender, EventArgs args) => model.RefreshDisplay();
}
