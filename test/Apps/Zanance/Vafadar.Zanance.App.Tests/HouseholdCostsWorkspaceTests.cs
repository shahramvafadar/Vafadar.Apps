using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Sharing;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;
using Vafadar.Zanance.Data.Sharing;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original cost workspace lifetime without fabricating private household membership or financial data.</summary>
public sealed class HouseholdCostsWorkspaceTests
{
    private static HouseholdCostsViewModel Create(FlowFixture f, PersonalSyncFlowTests.EnvironmentPort environment,
        IDbContextFactory<ZananceDbContext>? factory = null) => new(
        new HouseholdCostStore(factory ?? f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(),
            f.Services.GetRequiredService<ICommercialWriteAccessSource>()), environment, f.Platform, f.Localization,
        new DateFormatter(f.Localization), f.Translator, f.Time);

    [Fact]
    public async Task Original_visible_source_failure_requires_explicit_Retry_instead_of_another_appearance_read()
    {
        using var f = new FlowFixture();
        using var environment = new PersonalSyncFlowTests.EnvironmentPort { FailCapture = true };
        var vm = Create(f, environment); await vm.ActivateAsync();
        Assert.NotNull(vm.Error); Assert.False(vm.IsReady); var captures = environment.Captures;
        await vm.ActivateAsync();
        Assert.Equal(captures, environment.Captures); Assert.NotNull(vm.Error);
        vm.Deactivate();
    }

    [Fact]
    public async Task Original_pending_source_read_allows_Back_and_cannot_publish_a_departed_result()
    {
        using var f = new FlowFixture(); using var environment = new PersonalSyncFlowTests.EnvironmentPort();
        var factory = new PendingFactory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
        var vm = Create(f, environment, factory); var pending = vm.ActivateAsync();
        await factory.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        try { Assert.True(vm.CanLeave); }
        finally { vm.Deactivate(); factory.Release.TrySetResult(); await pending; }
        Assert.False(vm.IsReady); Assert.Empty(vm.Costs); Assert.Null(vm.Error);
    }

    /// <summary>Suspends only the original factory boundary; it grants no private role, key or writable source.</summary>
    private sealed class PendingFactory(IDbContextFactory<ZananceDbContext> original) : IDbContextFactory<ZananceDbContext>
    {
        internal int Opens;
        internal TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public ZananceDbContext CreateDbContext() => throw new InvalidOperationException("Use the original asynchronous read.");
        public async Task<ZananceDbContext> CreateDbContextAsync(CancellationToken ct = default)
        {
            Opens++; Entered.TrySetResult(); await Release.Task.WaitAsync(ct);
            ct.ThrowIfCancellationRequested(); return await original.CreateDbContextAsync(ct);
        }
    }

    [Fact]
    public async Task Original_pending_read_coalesces_appearance_even_if_shared_busy_feedback_is_cleared()
    {
        using var f = new FlowFixture(); using var environment = new PersonalSyncFlowTests.EnvironmentPort();
        var factory = new PendingFactory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
        var vm = Create(f, environment, factory); var first = vm.ActivateAsync();
        await factory.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        vm.IsBusy = false; var second = vm.ActivateAsync();
        try
        {
            Assert.True(vm.HasPendingOperation); Assert.False(vm.CanRead);
            Assert.Equal(1, factory.Opens); Assert.Equal(1, environment.Captures);
        }
        finally { vm.Deactivate(); factory.Release.TrySetResult(); await Task.WhenAll(first, second); }
        Assert.False(vm.IsReady); Assert.Null(vm.Error);
    }

    [Fact]
    public async Task Original_explicit_source_Retry_retains_literal_unsubmitted_cost_inputs_on_failure()
    {
        using var f = new FlowFixture();
        using var environment = new PersonalSyncFlowTests.EnvironmentPort { FailCapture = true };
        var vm = Create(f, environment); await vm.ActivateAsync();
        vm.Title = "  Original unsubmitted cost  "; vm.AmountText = "123.4500";
        var captures = environment.Captures;
        await vm.ReloadCommand.ExecuteAsync(null);
        Assert.Equal(captures + 1, environment.Captures); Assert.NotNull(vm.Error);
        Assert.Equal("  Original unsubmitted cost  ", vm.Title); Assert.Equal("123.4500", vm.AmountText);
        Assert.True(vm.CanRead); Assert.False(vm.CanWrite); vm.Deactivate();
    }

    [Theory]
    [InlineData("disclosure")]
    [InlineData("draft")]
    [InlineData("root")]
    [InlineData("search")]
    public async Task Original_discard_consent_cannot_start_a_source_read_after_its_reviewed_frame_changes(string change)
    {
        using var f = new FlowFixture();
        using var environment = new PersonalSyncFlowTests.EnvironmentPort { FailCapture = true };
        var factory = new PendingFactory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
        factory.Release.TrySetResult();
        var vm = Create(f, environment, factory); await vm.ActivateAsync();
        var ownsRoot = true; vm.BindRootOwner(() => ownsRoot);
        vm.EditingCost = true; vm.Title = "Original draft"; vm.AmountText = "123.4500";
        environment.FailCapture = false;
        f.Platform.PendingConfirmation = () =>
        {
            if (change == "disclosure") f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(row => row.CultureName == "de"));
            else if (change == "root") ownsRoot = false;
            else if (change == "search") vm.MovementSearch = "Newer literal search";
            else vm.Title = "Newer draft";
            return Task.FromResult(true);
        };
        await vm.ReloadCommand.ExecuteAsync(null);
        Assert.Single(f.Platform.ConfirmationDialogs); Assert.Equal(0, factory.Opens);
        Assert.Equal("123.4500", vm.AmountText); vm.Deactivate();
    }

    [Fact]
    public async Task Original_replaced_root_cannot_capture_another_source_or_reactivate_its_retired_page()
    {
        using var f = new FlowFixture();
        using var environment = new PersonalSyncFlowTests.EnvironmentPort { FailCapture = true };
        var vm = Create(f, environment); var current = true; vm.BindRootOwner(() => current);
        await vm.ActivateAsync(); var captures = environment.Captures;
        current = false; await vm.ReloadCommand.ExecuteAsync(null); await vm.ActivateAsync();
        Assert.Equal(captures, environment.Captures); Assert.False(vm.CanRead);
        vm.RetireRoot(); current = true; await vm.ActivateAsync();
        Assert.Equal(captures, environment.Captures); Assert.False(vm.CanInteract); Assert.False(vm.CanRead);
    }

    [Fact]
    public async Task Original_movement_query_owner_is_checked_before_opening_any_factory()
    {
        using var f = new FlowFixture();
        var factory = new PendingFactory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
        var store = new HouseholdCostStore(factory, f.Services.GetRequiredService<ICommercialWriteAccessSource>());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.FindMovementsAsync(new(2026, 10, 30),
            Vafadar.Zanance.Core.Ledger.EntryKind.Expense, "Original query", TestContext.Current.CancellationToken,
            () => throw new OperationCanceledException("Original query retired.")));
        Assert.Equal(0, factory.Opens);
    }

    [Theory]
    [InlineData("contact")]
    [InlineData("cost")]
    [InlineData("settlement")]
    [InlineData("delete-cost")]
    [InlineData("delete-settlement")]
    public async Task Original_retired_writer_opens_no_context_and_never_reports_acceptance(string action)
    {
        using var f = new FlowFixture(); var ct = TestContext.Current.CancellationToken;
        var original = f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        HouseholdCostReview review;
        // Actual empty local cost facts are not protected household membership or a
        // writable role. This test exercises refusal before any such grant is opened.
        await using (var db = await original.CreateDbContextAsync(ct))
            review = new(DatabaseFileIdentity.ConfiguredPath(db), HouseholdCostState.Read(db), canWrite: false);
        var factory = new PendingFactory(original);
        var store = new HouseholdCostStore(factory, f.Services.GetRequiredService<ICommercialWriteAccessSource>());
        var accepted = 0;
        Task Effect() => action switch
        {
            "contact" => store.SaveContactAsync(review, new() { Name = "Never persisted" }, ct, () => false, () => accepted++),
            "cost" => store.SaveCostAsync(review, new() { Title = "Never persisted", CurrencyCode = "EUR" }, [], ct, () => false, () => accepted++),
            "settlement" => store.SaveSettlementAsync(review, new() { CurrencyCode = "EUR" }, ct, () => false, () => accepted++),
            "delete-cost" => store.DeleteCostAsync(review, Guid.NewGuid(), ct, () => false, () => accepted++),
            _ => store.DeleteSettlementAsync(review, Guid.NewGuid(), ct, () => false, () => accepted++),
        };
        await Assert.ThrowsAsync<InvalidOperationException>(Effect);
        Assert.Equal(0, factory.Opens); Assert.Equal(0, accepted);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: ct));
    }

    [Theory]
    [InlineData("keep")]
    [InlineData("discard")]
    [InlineData("changed-root")]
    [InlineData("failed-return")]
    public async Task Original_dirty_departure_uses_current_native_consent_and_retains_a_cancelled_or_failed_return(string outcome)
    {
        using var f = new FlowFixture();
        using var environment = new PersonalSyncFlowTests.EnvironmentPort { FailCapture = true };
        var vm = Create(f, environment); var root = true; vm.BindRootOwner(() => root);
        await vm.ActivateAsync(); environment.FailCapture = false;
        vm.EditingCost = true; vm.Title = "Unsubmitted original cost"; vm.AmountText = "123.4500";
        Assert.False(vm.CanLeave); var destinations = 0;
        f.Platform.PendingConfirmation = () =>
        {
            if (outcome == "changed-root") root = false;
            return Task.FromResult(outcome != "keep");
        };
        await vm.RequestLeaveAsync(() =>
        {
            Assert.True(vm.CanLeave); destinations++;
            if (outcome == "failed-return") throw new IOException("Original native destination did not open.");
            return Task.CompletedTask;
        });
        Assert.Single(f.Platform.ConfirmationDialogs);
        Assert.Equal(outcome is "discard" or "failed-return" ? 1 : 0, destinations);
        Assert.True(vm.HasEditor); Assert.Equal("123.4500", vm.AmountText);
        Assert.Equal("Unsubmitted original cost", vm.Title); Assert.False(vm.CanLeave);
        vm.Deactivate();
    }

    [Fact]
    public async Task Original_dirty_departure_offers_Discard_and_Keep_editing_without_a_Save_action()
    {
        using var f = new FlowFixture();
        using var environment = new PersonalSyncFlowTests.EnvironmentPort { FailCapture = true };
        f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(row => row.CultureName == "en"));
        var vm = Create(f, environment); await vm.ActivateAsync(); environment.FailCapture = false;
        vm.EditingCost = true; f.Platform.Confirmations.Enqueue(false);
        await vm.RequestLeaveAsync(() => throw new InvalidOperationException("Keep editing cannot navigate."));
        var dialog = Assert.Single(f.Platform.ConfirmationDialogs);
        Assert.Equal("Discard", dialog.Accept); Assert.Equal("Keep editing", dialog.Cancel);
        Assert.True(vm.HasEditor); vm.Deactivate();
    }

    [Theory]
    [InlineData("entry-title")]
    [InlineData("entry-note")]
    [InlineData("entry-tags")]
    [InlineData("account-name")]
    public void Original_linked_cash_metadata_is_part_of_the_frozen_household_source(string field)
    {
        var account = new Vafadar.Zanance.Core.Accounts.Account
            { Name = "Original cash source", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 1) };
        var entry = new Vafadar.Zanance.Core.Ledger.LedgerEntry
            { AccountId = account.Id, Kind = Vafadar.Zanance.Core.Ledger.EntryKind.Expense, Date = new(2026, 10, 30), Amount = 1234, Title = "Original cash title" };
        var contact = new Vafadar.Zanance.Core.Sharing.HouseholdContact { Name = "Original participant" };
        var cost = new Vafadar.Zanance.Core.Sharing.HouseholdCost
            { Title = "Original allocation", CurrencyCode = "EUR", Amount = 1234, Date = entry.Date,
              Kind = Vafadar.Zanance.Core.Sharing.HouseholdCostKind.Expense,
              PaymentKind = Vafadar.Zanance.Core.Sharing.HouseholdPaymentKind.SharedAccount, EntryId = entry.Id };
        var state = new HouseholdCostState([contact], [cost], [new() { CostId = cost.Id, ContactId = contact.Id, Amount = 1234 }], [],
            new Dictionary<Guid, Vafadar.Zanance.Core.Ledger.LedgerEntry> { [entry.Id] = entry },
            new Dictionary<Guid, Vafadar.Zanance.Core.Accounts.Account> { [account.Id] = account });
        state.DemandValid(); var frozen = state.Fingerprint();
        switch (field)
        {
            case "entry-title": entry.Title = "Changed cash title"; break;
            case "entry-note": entry.Note = "Changed cash note"; break;
            case "entry-tags": entry.Tags.Add("changed-cash-tag"); break;
            default: account.Name = "Changed cash source"; break;
        }
        state.DemandValid(); Assert.NotEqual(frozen, state.Fingerprint());
    }
}
