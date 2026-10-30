namespace Vafadar.Zanance.App.Features.Rates;

/// <summary>Original reference lookup/review lifetime; construction performs no database or network work.</summary>
public partial class MarketReferencePage : ContentPage
{
    private readonly MarketReferenceViewModel model;
    private AppShell? ownerShell;
    private bool rootRetired, publicationActive;
    /// <summary>Shares only this actual page's publication between native appearance and retained-tab return.</summary>
    internal Task PendingPublication { get; private set; } = Task.CompletedTask;
    /// <summary>Constructs only native controls and their original model binding.</summary>
    public MarketReferencePage(MarketReferenceViewModel model)
    {
        InitializeComponent(); BindingContext = this.model = model;
        EcbSourceLink.Command = new Command(() => OpenEcbSource(this, EventArgs.Empty));
    }
    /// <inheritdoc />
    protected override async void OnAppearing()
    {
        base.OnAppearing(); if (rootRetired) return;
        if (Shell.Current is AppShell shell)
        {
            if (ownerShell is not null && !ReferenceEquals(ownerShell, shell)) { RetireShellPublication(); return; }
            ownerShell = shell; shell.RegisterMarketReference(this); var root = new WeakReference<AppShell>(shell);
            model.BindRootOwner(() => root.TryGetTarget(out var original) && original.OwnsMarketReferencePublication);
        }
        await Presentation.Failures.GuardAsync(RefreshForNavigationAsync);
    }
    /// <inheritdoc />
    protected override void OnDisappearing() { RetirePublication(); base.OnDisappearing(); }
    /// <summary>Only actual native return can activate this original reference workspace.</summary>
    internal Task RefreshForNavigationAsync()
    {
        if (rootRetired) return Task.CompletedTask;
        if (publicationActive) return PendingPublication;
        publicationActive = true; model.SetPresentationActive(true);
        return PendingPublication = PublishAfterRouteAsync();
    }
    private async Task PublishAfterRouteAsync()
    { await Task.Yield(); if (publicationActive && !rootRetired) await model.LoadAsync(); }
    /// <summary>Retires current cache/network/source reads and native consent when the actual page leaves.</summary>
    internal void RetirePublication() { publicationActive = false; model.Retire(); }
    /// <summary>Never transfers this page's authority to a replacement application root.</summary>
    internal void RetireShellPublication() { rootRetired = true; RetirePublication(); model.RetireRoot(); }
    // ECB reuse terms require a full external browser, without framing or sending financial context.
    private async void OpenEcbSource(object? sender, EventArgs e)
    {
        if (!publicationActive || rootRetired || ownerShell?.OwnsMarketReferencePublication == false) return;
        await Presentation.Failures.GuardAsync(async () =>
        { await Browser.Default.OpenAsync(new Uri(Core.Rates.EcbReference.SourceUrl), BrowserLaunchMode.External); });
    }
}
