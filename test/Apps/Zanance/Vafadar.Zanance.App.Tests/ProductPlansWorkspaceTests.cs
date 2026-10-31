using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Commerce;
using Vafadar.Zanance.App.Features.Commerce.Billing;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original plan/access source and native consent boundaries, without an actual store, identity or payment.</summary>
public sealed class ProductPlansWorkspaceTests
{
    private sealed class Access : ICommercialWriteAccessSource
    {
        internal bool Fail;
        internal int Calls;
        public CommercialWriteAccess Capture(string path)
        { Calls++; if (Fail) throw new IOException("Controlled original cached-source failure."); return CommercialWriteAccess.Inactive; }
    }
    private static ProductPlansViewModel Create(FlowFixture f, Access access, IDbContextFactory<ZananceDbContext>? factory = null, IProductBillingEnvironment? billing = null) =>
        new(new CommercialAccessStore(factory ?? f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(), access), f.Translator, f.Platform, billing);

    [Fact]
    public async Task Visible_original_failure_requires_explicit_retry_not_another_appearance()
    {
        using var f = new FlowFixture(); var access = new Access { Fail = true }; var vm = Create(f, access);
        await vm.LoadAsync(); Assert.True(vm.Loading.HasFailed); var calls = access.Calls;
        access.Fail = false; await vm.LoadAsync();
        Assert.True(vm.Loading.HasFailed); Assert.Equal(calls, access.Calls); Assert.False(vm.CanInteract);
        await vm.ReloadCommand.ExecuteAsync(null); Assert.True(vm.Loading.IsReady);
    }

    [Fact]
    public async Task Departed_pending_source_cannot_publish_or_reenable_original_navigation()
    {
        using var f = new FlowFixture(); var factory = new PendingFactory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
        var vm = Create(f, new(), factory); var first = vm.LoadAsync();
        await factory.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        vm.DeactivateBilling(); factory.Release.TrySetResult(); await first;
        Assert.False(vm.CanInteract); Assert.Empty(vm.Rows); Assert.False(vm.ContinueCommand.CanExecute(null));
    }

    [Fact]
    public async Task Changed_disclosure_retires_the_original_native_restore_confirmation()
    {
        using var f = new FlowFixture(); var billing = new Store(); var vm = Create(f, new(), billing: billing); await vm.LoadAsync();
        var pending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        f.Platform.PendingConfirmation = () => pending.Task;
        var action = vm.RestoreBillingCommand.ExecuteAsync(null); Assert.Single(f.Platform.ConfirmationDialogs);
        f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(x => x.CultureName == "de")); vm.RefreshDisplay();
        pending.SetResult(true); await action;
        Assert.Equal(0, billing.Restores); Assert.True(vm.CanRestoreBilling);
    }

    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task Replaced_original_root_cannot_navigate_manage_or_reactivate(bool permanentlyRetired)
    {
        using var f = new FlowFixture(); var access = new Access(); var billing = new Store(); var vm = Create(f, access, billing: billing);
        var owns = true; vm.BindRootOwner(() => owns); await vm.LoadAsync(); var captures = access.Calls;
        if (permanentlyRetired) vm.RetireRoot(); else owns = false;
        await vm.ContinueCommand.ExecuteAsync(null); await vm.ManageBillingCommand.ExecuteAsync(null); await vm.LoadAsync();
        Assert.False(vm.CanInteract); Assert.False(vm.CanBuyBilling); Assert.Empty(f.Platform.Routes);
        Assert.Equal(0, billing.Manages); Assert.Equal(captures, access.Calls);
    }

    [Fact]
    public async Task Actual_access_change_during_native_consent_covers_source_without_restoration()
    {
        using var f = new FlowFixture(); var access = new Access(); var billing = new Store(); var vm = Create(f, access, billing: billing); await vm.LoadAsync();
        var pending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously); f.Platform.PendingConfirmation = () => pending.Task;
        var action = vm.RestoreBillingCommand.ExecuteAsync(null); access.Fail = true; pending.SetResult(true); await action;
        Assert.Equal(0, billing.Restores); Assert.True(vm.Loading.HasFailed); Assert.Empty(vm.BillingOffers); Assert.False(vm.CanInteract);
    }

    [Fact]
    public async Task Changed_original_access_refuses_return_and_external_source_without_handoff()
    {
        using var f = new FlowFixture(); var access = new Access(); var vm = Create(f, access); await vm.LoadAsync();
        access.Fail = true; await vm.ContinueCommand.ExecuteAsync(null); var opened = 0;
        await vm.OpenReferenceAsync(() => { opened++; return Task.CompletedTask; });
        Assert.Empty(f.Platform.Routes); Assert.Equal(0, opened); Assert.True(vm.Loading.HasFailed);
    }

    [Fact]
    public async Task Retired_native_confirmation_cannot_restore_for_a_new_actual_frame()
    {
        using var f = new FlowFixture(); var billing = new Store(); var vm = Create(f, new(), billing: billing); await vm.LoadAsync();
        var pending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously); f.Platform.PendingConfirmation = () => pending.Task;
        var action = vm.RestoreBillingCommand.ExecuteAsync(null); vm.DeactivateBilling(); await vm.LoadAsync();
        Assert.True(vm.CanRestoreBilling); Assert.True(vm.CanLeave);
        pending.SetResult(true); await action; Assert.Equal(0, billing.Restores); Assert.True(vm.CanRestoreBilling);
    }

    /// <summary>A late old native result cannot release a newer frame's still-pending review, regardless of the old answer.</summary>
    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task Retired_native_result_cannot_clear_a_new_frames_pending_review(bool oldAccepted)
    {
        using var f = new FlowFixture(); var billing = new Store(); var vm = Create(f, new(), billing: billing);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); await vm.LoadAsync();
        var oldAnswer = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        f.Platform.PendingConfirmation = () => oldAnswer.Task;
        var oldAction = vm.RestoreBillingCommand.ExecuteAsync(null);
        vm.DeactivateBilling(); await vm.LoadAsync(); Assert.True(vm.CanRestoreBilling);
        var newAnswer = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        f.Platform.PendingConfirmation = () => newAnswer.Task;
        var newAction = vm.RestoreBillingCommand.ExecuteAsync(null);
        Assert.Equal(2, f.Platform.ConfirmationDialogs.Count); Assert.False(vm.CanLeave);
        oldAnswer.SetResult(oldAccepted); await oldAction;
        Assert.False(vm.CanLeave); Assert.False(vm.CanRestoreBilling); Assert.Equal(0, billing.Restores);
        newAnswer.SetResult(false); await newAction;
        Assert.True(vm.CanLeave); Assert.True(vm.CanRestoreBilling); Assert.Equal(0, billing.Restores);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); vm.DeactivateBilling();
    }

    [Fact]
    public async Task Original_uncertain_restore_failure_exposes_continuation_without_inventing_acceptance()
    {
        using var f = new FlowFixture(); var billing = new Store { Restore = _ => Task.FromException<ProductBillingPresentation>(new IOException("Controlled ambiguous continuation.")) };
        var vm = Create(f, new(), billing: billing); await vm.LoadAsync(); f.Platform.Confirmations.Enqueue(true);
        await vm.RestoreBillingCommand.ExecuteAsync(null);
        Assert.Equal(1, billing.Restores); Assert.True(vm.CanContinueBilling); Assert.False(vm.CanBuyBilling);
        Assert.Equal(f.Translator["Billing_Status_Pending"], vm.BillingStatusText);
        Assert.Equal(0, billing.Purchases);
    }

    [Fact]
    public async Task Billing_read_allows_back_and_late_failure_cannot_replace_new_frame()
    {
        using var f = new FlowFixture(); var pending = new TaskCompletionSource<ProductBillingPresentation>(TaskCreationOptions.RunContinuationsAsynchronously);
        var billing = new Store { Load = _ => pending.Task }; var vm = Create(f, new(), billing: billing);
        var first = vm.LoadAsync(); Assert.True(vm.CanLeave); Assert.True(vm.BillingBusy);
        vm.DeactivateBilling(); billing.Load = null; await vm.LoadAsync();
        pending.SetException(new IOException("Controlled retired observation.")); await first;
        Assert.True(vm.CanRestoreBilling); Assert.Equal(f.Translator["Billing_Status_Ready"], vm.BillingStatusText);
    }

    [Fact]
    public async Task Original_submitted_restore_blocks_navigation_and_management_despite_cleared_shared_busy()
    {
        using var f = new FlowFixture(); var pending = new TaskCompletionSource<ProductBillingPresentation>(TaskCreationOptions.RunContinuationsAsynchronously);
        var billing = new Store { Restore = _ => pending.Task }; var vm = Create(f, new(), billing: billing); await vm.LoadAsync();
        f.Platform.Confirmations.Enqueue(true); var action = vm.RestoreBillingCommand.ExecuteAsync(null);
        vm.IsBusy = false; Assert.False(vm.CanLeave); await vm.ContinueCommand.ExecuteAsync(null); await vm.ManageBillingCommand.ExecuteAsync(null);
        Assert.Empty(f.Platform.Routes); Assert.Equal(0, billing.Manages); Assert.Equal(1, billing.Restores);
        pending.SetResult(Store.Ready()); await action; Assert.True(vm.CanLeave);
    }

    [Fact]
    public async Task Current_external_reference_and_return_are_read_only_with_actual_source_unchanged()
    {
        using var f = new FlowFixture(); var before = await GoalChooserFlowTests.SnapshotAsync(f); var vm = Create(f, new()); await vm.LoadAsync();
        var calls = 0; await vm.OpenReferenceAsync(() => { calls++; return Task.CompletedTask; });
        await vm.ContinueCommand.ExecuteAsync(null); Assert.Equal(1, calls); Assert.Equal("..", Assert.Single(f.Platform.Routes).Route);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.Empty(f.Platform.ConfirmationDialogs);
    }

    [Fact]
    public async Task Original_native_disclosure_checks_actual_translator_even_without_page_refresh_callback()
    {
        using var f = new FlowFixture(); var billing = new Store(); var vm = Create(f, new(), billing: billing); await vm.LoadAsync();
        var pending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously); f.Platform.PendingConfirmation = () => pending.Task;
        var action = vm.RestoreBillingCommand.ExecuteAsync(null);
        f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(x => x.CultureName == "de"));
        pending.SetResult(true); await action; Assert.Equal(0, billing.Restores);
    }

    [Fact]
    public async Task Original_native_review_refuses_a_second_restore_and_direct_retired_read_command()
    {
        using var f = new FlowFixture(); var access = new Access(); var billing = new Store(); var vm = Create(f, access, billing: billing); await vm.LoadAsync();
        var pending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously); f.Platform.PendingConfirmation = () => pending.Task;
        var action = vm.RestoreBillingCommand.ExecuteAsync(null); var captures = access.Calls;
        var second = vm.RestoreBillingCommand.ExecuteAsync(null); await vm.ReloadCommand.ExecuteAsync(null);
        try { Assert.Single(f.Platform.ConfirmationDialogs); Assert.Equal(captures, access.Calls); }
        finally { pending.TrySetResult(false); await action; await second; }
        Assert.Equal(0, billing.Restores);
    }

    /// <summary>Holds the actual factory before source capture; cancellation remains visible to the original read.</summary>
    private sealed class PendingFactory(IDbContextFactory<ZananceDbContext> original) : IDbContextFactory<ZananceDbContext>
    {
        internal TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public ZananceDbContext CreateDbContext() => throw new InvalidOperationException("Original async factory required.");
        public async Task<ZananceDbContext> CreateDbContextAsync(CancellationToken ct = default)
        { Entered.TrySetResult(); await Release.Task.WaitAsync(ct); return await original.CreateDbContextAsync(ct); }
    }
    /// <summary>Records only requests to the existing interface; it cannot contact a store or grant an entitlement.</summary>
    private sealed class Store : IProductBillingEnvironment
    {
        internal int Restores;
        internal int Purchases;
        internal int Manages;
        internal Func<CancellationToken, Task<ProductBillingPresentation>>? Load;
        internal Func<CancellationToken, Task<ProductBillingPresentation>>? Restore;
        public bool Available => true;
        internal static ProductBillingPresentation Ready() => new(ProductBillingStatus.Ready, null, null, [], true, false);
        public Task<ProductBillingPresentation> LoadAsync(CancellationToken ct) => Load?.Invoke(ct) ?? Task.FromResult(Ready());
        public Task<ProductBillingPresentation> RestoreAsync(CancellationToken ct) { Restores++; return Restore?.Invoke(ct) ?? Task.FromResult(Ready()); }
        public Task<ProductBillingPresentation> PurchaseAsync(NativeBillingOffer offer, CancellationToken ct) { Purchases++; throw new InvalidOperationException("No payment in this source cohort."); }
        public Task<ProductBillingPresentation> ContinueAsync(CancellationToken ct) => Task.FromResult(Ready());
        public Task ManageAsync(CancellationToken ct) { Manages++; return Task.CompletedTask; }
    }
}
