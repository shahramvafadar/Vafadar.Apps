using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Commerce;
using Vafadar.Zanance.App.Features.Commerce.Billing;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual comparison/store view model with explicit native effects, real translations and unchanged isolated financial data.</summary>
[Trait("AT", "AT-295")]
public sealed class BillingScreenFlowTests
{
    /// <summary>Every language shows all original phases before a real confirmation; Cancel never reaches the store.</summary>
    [Theory]
    [InlineData("en")][InlineData("fa")][InlineData("de")][InlineData("es")][InlineData("fr")][InlineData("it")]
    public async Task Complete_price_review_cancel_never_launches_payment_or_changes_money(string language)
    {
        using var f = new FlowFixture(); f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(l => l.CultureName == language));
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var environment = new Store();
        var vm = new ProductPlansViewModel(f.Services.GetRequiredService<CommercialAccessStore>(), f.Translator, f.Platform, environment);
        await vm.LoadAsync(); Assert.True(vm.ShowBilling); Assert.False(vm.BillingBusy); Assert.True(vm.CanBuyBilling);
        var row = Assert.Single(vm.BillingOffers);
        Assert.Contains("0.00 EUR", row.Terms); Assert.Contains("39.99 EUR", row.Terms);
        Assert.DoesNotContain("Billing_", row.Terms); Assert.Equal(f.Translator["Billing_IdentityPresent"], vm.BillingIdentityText);
        await vm.BuyBillingCommand.ExecuteAsync(row);
        var dialog = Assert.Single(f.Platform.ConfirmationDialogs);
        Assert.Contains(row.Terms, dialog.Message); Assert.Contains(f.Translator["Billing_ReviewTerms"], dialog.Message);
        Assert.Contains(f.Translator["Market_EcbFreeOriginal"], row.Terms);
        Assert.Contains(f.Translator["Market_EcbFreeOriginal"], dialog.Message);
        Assert.Equal(0, environment.Purchases); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        vm.DeactivateBilling(); Assert.False(vm.CanBuyBilling); Assert.False(vm.CanManageBilling);
    }

    /// <summary>A retired completed read cannot publish prices or reenable controls on a newly loaded page.</summary>
    [Fact]
    public async Task Retired_store_read_cannot_publish_onto_a_later_page()
    {
        using var f = new FlowFixture(); var environment = new Store();
        var pending = new TaskCompletionSource<ProductBillingPresentation>(TaskCreationOptions.RunContinuationsAsynchronously);
        environment.Load = _ => pending.Task;
        var vm = new ProductPlansViewModel(f.Services.GetRequiredService<CommercialAccessStore>(), f.Translator, f.Platform, environment);
        var first = vm.LoadAsync(); Assert.True(vm.BillingBusy); Assert.Empty(vm.BillingOffers);
        vm.DeactivateBilling(); environment.Load = null; await vm.LoadAsync();
        Assert.Single(vm.BillingOffers); Assert.True(vm.CanBuyBilling);
        pending.SetResult(new(ProductBillingStatus.Pending, null, null, [], true, false)); await first;
        Assert.Single(vm.BillingOffers); Assert.True(vm.CanBuyBilling);
        Assert.Equal(f.Translator["Billing_Status_Ready"], vm.BillingStatusText);
        vm.DeactivateBilling();
    }

    /// <summary>Uncertain native foreground work offers original continuation, without displaying a new Buy.</summary>
    [Fact]
    public async Task Native_foreground_retirement_preserves_continuation_without_another_buy()
    {
        using var f = new FlowFixture(); var environment = new Store { RetirePurchase = true };
        var vm = new ProductPlansViewModel(f.Services.GetRequiredService<CommercialAccessStore>(), f.Translator, f.Platform, environment);
        await vm.LoadAsync(); f.Platform.Confirmations.Enqueue(true);
        await vm.BuyBillingCommand.ExecuteAsync(Assert.Single(vm.BillingOffers));
        Assert.Equal(1, environment.Purchases); Assert.Empty(vm.BillingOffers);
        Assert.False(vm.CanBuyBilling); Assert.True(vm.CanContinueBilling);
        Assert.Empty(f.Platform.Failures); vm.DeactivateBilling();
    }

    /// <summary>An unavailable read has no native payment to continue and offers a fresh read instead.</summary>
    [Fact]
    public async Task Interrupted_status_read_does_not_invent_payment_and_retry_uses_only_a_read()
    {
        using var f = new FlowFixture(); var environment = new Store { Load = _ => Task.FromException<ProductBillingPresentation>(new OperationCanceledException()) };
        var vm = new ProductPlansViewModel(f.Services.GetRequiredService<CommercialAccessStore>(), f.Translator, f.Platform, environment);
        await vm.LoadAsync(); Assert.False(vm.CanBuyBilling); Assert.False(vm.CanContinueBilling); Assert.True(vm.CanRetryBilling);
        Assert.Equal(f.Translator["Billing_Status_VerificationUnavailable"], vm.BillingStatusText);
        environment.Load = null; await vm.RetryBillingCommand.ExecuteAsync(null);
        Assert.True(vm.CanBuyBilling); Assert.False(vm.CanRetryBilling); Assert.False(vm.CanContinueBilling); Assert.Equal(0, environment.Purchases);
        vm.DeactivateBilling();
    }

    /// <summary>Private loss exposes only explicit existing-store restoration; Cancel and accepted restoration remain separate from a new payment.</summary>
    [Theory]
    [InlineData("en")][InlineData("fa")][InlineData("de")][InlineData("es")][InlineData("fr")][InlineData("it")]
    public async Task Private_recovery_requires_original_confirmation_and_never_buys(string language)
    {
        using var f = new FlowFixture(); f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(l => l.CultureName == language));
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var environment = new Store { Load = _ => Task.FromResult(new ProductBillingPresentation(ProductBillingStatus.RecoveryRequired, null, null, [], true, false)) };
        var vm = new ProductPlansViewModel(f.Services.GetRequiredService<CommercialAccessStore>(), f.Translator, f.Platform, environment);
        await vm.LoadAsync(); Assert.False(vm.CanBuyBilling); Assert.False(vm.CanContinueBilling); Assert.True(vm.CanRestoreBilling);
        Assert.Equal(f.Translator["Billing_Status_RecoveryRequired"], vm.BillingStatusText);
        await vm.RestoreBillingCommand.ExecuteAsync(null); Assert.Equal(0, environment.Restores); Assert.Equal(0, environment.Purchases);
        var dialog = Assert.Single(f.Platform.ConfirmationDialogs);
        Assert.Equal(f.Translator["Billing_RestoreExplanation"], dialog.Message); Assert.DoesNotContain("Billing_", dialog.Message);
        f.Platform.Confirmations.Enqueue(true); await vm.RestoreBillingCommand.ExecuteAsync(null);
        Assert.Equal(1, environment.Restores); Assert.Equal(0, environment.Purchases); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        vm.DeactivateBilling();
    }

    private sealed class Store : IProductBillingEnvironment
    {
        public bool Available => true;
        internal int Purchases;
        internal int Restores;
        internal bool RetirePurchase;
        internal Func<CancellationToken, Task<ProductBillingPresentation>>? Load;
        public Task<ProductBillingPresentation> LoadAsync(CancellationToken ct) => Load?.Invoke(ct) ?? Task.FromResult(Ready());
        public Task<ProductBillingPresentation> PurchaseAsync(NativeBillingOffer offer, CancellationToken ct)
        { Purchases++; return RetirePurchase ? Task.FromException<ProductBillingPresentation>(new OperationCanceledException()) : Task.FromResult(Ready()); }
        public Task<ProductBillingPresentation> RestoreAsync(CancellationToken ct) { Restores++; return Task.FromResult(Ready()); }
        public Task<ProductBillingPresentation> ContinueAsync(CancellationToken ct) => Task.FromResult(Ready());
        public Task ManageAsync(CancellationToken ct) => Task.CompletedTask;
        private static ProductBillingPresentation Ready() => new(ProductBillingStatus.Ready, null, null,
            [new("fictional-native-handle", "Fictional Pro", "Fictional native catalog only",
                new("fictional-native-handle", BillingProduct.ProYearly, BillingOfferKind.ProTrial,
                    [new(0, "EUR", "0.00 EUR", "P14D", 1, false), new(39990000, "EUR", "39.99 EUR", "P1Y", 0, true)], true), "fictional-native-offer")], true, true);
    }
}
