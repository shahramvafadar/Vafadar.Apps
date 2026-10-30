using Vafadar.Localization;

namespace Vafadar.Zanance.App.Features.Commerce.Billing;

/// <summary>Explicit owned account entry; opening it does not share profiles, create a financial account or launch payment.</summary>
public partial class BillingIdentityPage : ContentPage
{
    private readonly BillingIdentityViewModel model;
    private readonly ILocalizationService localization;
    private AppShell? ownerShell;
    private bool rootRetired;
    /// <summary>Retires only this original page and clears its request-only secrets on actual departure.</summary>
    internal void RetirePublication() => model.Deactivate();
    /// <summary>A replaced root cannot reactivate old native identity consent.</summary>
    internal void RetireShellPublication() { rootRetired = true; model.RetireRoot(); }
    /// <summary>Shares actual appearance/navigation under its original page and owned native sensitive cover.</summary>
    internal Task RefreshForNavigationAsync()
    {
        if (rootRetired) return Task.CompletedTask;
        if (Shell.Current is AppShell shell)
        {
            if (ownerShell is not null && !ReferenceEquals(ownerShell, shell)) { RetireShellPublication(); return Task.CompletedTask; }
            ownerShell = shell; shell.RegisterBillingIdentity(this); var root = new WeakReference<AppShell>(shell);
            model.BindRootOwner(() => root.TryGetTarget(out var original) && original.OwnsBillingIdentityPublication
                && ReferenceEquals(Application.Current?.Windows.FirstOrDefault()?.Page, original)
                && (ReferenceEquals(original.CurrentPage, this) || Security.NativeSensitivePresentation.CoversOriginalPage(this)));
        }
        return model.ActivateAsync();
    }
    /// <summary>Builds controls only; private session/credential effects wait for actual appearance and explicit actions.</summary>
    public BillingIdentityPage(BillingIdentityViewModel model, ILocalizationService localization)
    { InitializeComponent(); BindingContext = this.model = model; this.localization = localization; }
    /// <inheritdoc />
    protected override async void OnAppearing()
    {
        base.OnAppearing(); localization.Changed -= OnLanguageChanged; localization.Changed += OnLanguageChanged;
        await Presentation.Failures.GuardAsync(RefreshForNavigationAsync);
    }
    /// <inheritdoc />
    protected override void OnDisappearing()
    { localization.Changed -= OnLanguageChanged; if (!Security.NativeSensitivePresentation.CoversOriginalPage(this)) RetirePublication(); base.OnDisappearing(); }
    /// <inheritdoc />
    protected override bool OnBackButtonPressed() => !model.CanLeave || base.OnBackButtonPressed();
    private void OnLanguageChanged(object? sender, EventArgs args) => model.RefreshDisplay();
}
