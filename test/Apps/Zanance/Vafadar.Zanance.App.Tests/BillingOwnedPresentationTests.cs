using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Commerce;
using Vafadar.Zanance.App.Features.Commerce.Billing;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Data.Billing;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-310: original verified history stays separate from current access, guessed charges and financial writes.</summary>
[Trait("AT", "AT-310")]
public sealed class BillingOwnedPresentationTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 20, 0, 0, 0, TimeSpan.Zero);

    /// <summary>Every lifecycle and device/identity horizon uses real localized dates and retains original data without buying.</summary>
    [Theory]
    [InlineData("en")][InlineData("fa")][InlineData("de")][InlineData("es")][InlineData("fr")][InlineData("it")]
    public async Task Verified_history_preserves_lifecycle_dates_scopes_and_finance_without_granting_or_charging(string language)
    {
        using var f = new FlowFixture(); f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(x => x.CultureName == language));
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var dates = new DateFormatter(f.Localization);
        var grants = Enum.GetValues<BillingPurchaseState>().Select(state => new BillingCachedGrant(BillingIssuer.GooglePlay, BillingEnvironment.Sandbox,
            new(BillingProduct.PlusMonthly, state, Now.AddMonths(-1), Now.AddDays(7), state == BillingPurchaseState.Grace ? Now.AddDays(9) : null,
                false, false, true, "fictional-terms", ScheduledChange: state == BillingPurchaseState.Active ? new(BillingProduct.ProYearly, Now.AddDays(7), "fictional-next") : null),
            false, Now.AddDays(-1), "private-purchase-reference")).ToArray();
        var lifetime = new BillingCachedGrant(BillingIssuer.AppStore, BillingEnvironment.Sandbox,
            new(BillingProduct.PlusLifetime, BillingPurchaseState.Active, Now, null, null, false, false, true, "fictional-terms"), true, Now);
        grants = [.. grants, grants.Single(g => g.Facts.State == BillingPurchaseState.Active) with
        { Facts = grants.Single(g => g.Facts.State == BillingPurchaseState.Active).Facts with { AutoRenews = true, ScheduledChange = null } }];
        foreach (var grant in grants) grant.Facts.Validate(); lifetime.Facts.Validate();
        var local = View(grants, new(ProductPlan.Free, true, true));
        var identity = View([lifetime], new(ProductPlan.Plus, false, false));
        var store = new Store(new(ProductBillingStatus.VerificationUnavailable, local, identity, [], true, true));
        var vm = new ProductPlansViewModel(f.Services.GetRequiredService<CommercialAccessStore>(), f.Translator, f.Platform, store, dates);
        await vm.LoadAsync(); var rows = vm.BillingOwnedPurchases;
        Assert.Equal(9, rows.Count); Assert.False(vm.CanBuyBilling);
        for (var i = 0; i < grants.Length; i++)
        {
            var text = rows[i].Details; var fact = grants[i].Facts;
            Assert.Contains(f.Translator["Billing_Owned_Device"], text); Assert.Contains("Google Play", rows[i].Title);
            Assert.Contains(f.Translator["Billing_Owned_State" + fact.State], text);
            Assert.Contains(f.Translator.Format("Billing_Owned_PaidThrough", dates.Format(fact.PaidUntil!.Value)), text);
            Assert.Contains(f.Translator.Format("Billing_Owned_Verified", dates.Format(grants[i].VerifiedAt)), text);
            Assert.Contains(f.Translator["Billing_Owned_Refresh"], text);
            Assert.Equal(fact.State == BillingPurchaseState.Grace, text.Contains(f.Translator.Format("Billing_Owned_GraceThrough", dates.Format(Now.AddDays(9))), StringComparison.Ordinal));
            Assert.Contains(f.Translator[fact.AutoRenews ? "Billing_Owned_RenewOn" : "Billing_Owned_RenewOff"], text);
            Assert.DoesNotContain("private-purchase-reference", text); Assert.DoesNotContain("Billing_Owned_", text);
        }
        var permanent = rows[^1]; Assert.Contains(f.Translator["Billing_Owned_Identity"], permanent.Details); Assert.Contains("App Store", permanent.Title);
        Assert.Contains(f.Translator["Billing_Owned_Lifetime"], permanent.Details); Assert.DoesNotContain(f.Translator["Billing_Owned_Refresh"], permanent.Details);
        Assert.DoesNotContain(f.Translator["Billing_Owned_RenewOff"], permanent.Details);
        Assert.Contains(dates.Format(Now.AddDays(7)), rows.Single(r => r.Details.Contains(f.Translator["Billing_Owned_RenewOn"], StringComparison.Ordinal)).Details);
        var scheduled = rows[Array.FindIndex(grants, grant => grant.Facts.ScheduledChange is not null)];
        Assert.Contains(f.Translator.Format("Billing_Owned_Change", "Pro · " + f.Translator["Billing_PeriodYear"], dates.Format(Now.AddDays(7))), scheduled.Details);
        var reads = store.Reads;
        f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(x => x.CultureName == (language == "de" ? "fa" : "de")));
        vm.RefreshDisplay(); Assert.Equal(reads, store.Reads);
        Assert.Contains(f.Translator["Billing_Owned_Device"], vm.BillingOwnedPurchases[0].Details);
        Assert.Contains(dates.Format(Now.AddDays(7)), vm.BillingOwnedPurchases[0].Details);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        vm.DeactivateBilling(); Assert.Empty(vm.BillingOwnedPurchases); Assert.False(vm.CanBuyBilling);
    }

    /// <summary>Absent signed facts never invent a lifecycle/date; live translation rebuilds text without another store read.</summary>
    [Fact]
    public async Task No_snapshot_has_no_purchase_history_and_live_translation_does_not_query_or_change_access()
    {
        using var f = new FlowFixture(); var store = new Store(new(ProductBillingStatus.NotConfigured, null, null, [], false, false));
        var vm = new ProductPlansViewModel(f.Services.GetRequiredService<CommercialAccessStore>(), f.Translator, f.Platform, store, new DateFormatter(f.Localization));
        await vm.LoadAsync(); Assert.Empty(vm.BillingOwnedPurchases); Assert.Equal(1, store.Reads);
        f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(x => x.CultureName == "de")); vm.RefreshDisplay();
        Assert.Empty(vm.BillingOwnedPurchases); Assert.Equal(1, store.Reads); Assert.False(vm.CanBuyBilling); vm.DeactivateBilling();
    }

    private static BillingClientView View(IReadOnlyList<BillingCachedGrant> grants, BillingCacheDecision decision) => new(
        new(1, BillingSnapshotKind.Current, Guid.NewGuid(), Guid.NewGuid(), new string('a', 64), BillingIssuer.GooglePlay,
            BillingEnvironment.Sandbox, null, Guid.NewGuid(), 0, Now, grants, false), decision, false, null, false);

    private sealed class Store(ProductBillingPresentation state) : IProductBillingEnvironment
    {
        public bool Available => true;
        internal int Reads;
        public Task<ProductBillingPresentation> LoadAsync(CancellationToken ct) { Reads++; return Task.FromResult(state); }
        public Task<ProductBillingPresentation> PurchaseAsync(NativeBillingOffer offer, CancellationToken ct) => throw new InvalidOperationException("No payment belongs to history review.");
        public Task<ProductBillingPresentation> RestoreAsync(CancellationToken ct) => throw new InvalidOperationException("No restore belongs to history review.");
        public Task<ProductBillingPresentation> ContinueAsync(CancellationToken ct) => throw new InvalidOperationException("No continuation belongs to history review.");
        public Task ManageAsync(CancellationToken ct) => throw new InvalidOperationException("No store navigation belongs to history review.");
    }
}
