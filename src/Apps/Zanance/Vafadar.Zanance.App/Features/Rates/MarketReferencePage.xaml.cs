namespace Vafadar.Zanance.App.Features.Rates;

/// <summary>Original reference lookup/review lifetime; construction performs no database or network work.</summary>
public partial class MarketReferencePage : ContentPage
{
    private readonly MarketReferenceViewModel model;
    /// <summary>Constructs only native controls and their original model binding.</summary>
    public MarketReferencePage(MarketReferenceViewModel model)
    {
        InitializeComponent(); BindingContext = this.model = model;
        EcbSourceLink.Command = new Command(() => OpenEcbSource(this, EventArgs.Empty));
    }
    /// <inheritdoc />
    protected override async void OnAppearing() { base.OnAppearing(); await Presentation.Failures.GuardAsync(model.LoadAsync); }
    /// <inheritdoc />
    protected override void OnDisappearing() { model.Retire(); base.OnDisappearing(); }
    // ECB reuse terms require a full external browser, without framing or sending financial context.
    private async void OpenEcbSource(object? sender, EventArgs e) => await Presentation.Failures.GuardAsync(
        async () => { await Browser.Default.OpenAsync(new Uri(Core.Rates.EcbReference.SourceUrl), BrowserLaunchMode.External); });
}
