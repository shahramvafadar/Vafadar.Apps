using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original balance-review lifetime and atomic SQLite correction, without substitute application algorithms.</summary>
[Trait("AT", "AT-11")]
public sealed class AccountReconciliationFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly DateOnly Day = new(2026, 10, 1);
    private sealed class Access : ICommercialWriteAccessSource
    {
        internal CommercialWriteAccess Current = CommercialWriteAccess.Inactive;
        public CommercialWriteAccess Capture(string databasePath) => Current;
    }
    private static async Task<Account> SeedAsync(FlowFixture f)
    {
        var account = new Account { Name = "Original checking", CurrencyCode = "EUR", OpeningBalance = 10000,
            OpeningBalanceKnown = false, OpeningDate = Day };
        Assert.True(await f.Store.SaveAccountAsync(account, Ct)); return account;
    }

    [Theory]
    [InlineData(11234, 1234, AdjustmentDirection.Increase)]
    [InlineData(-2500, 12500, AdjustmentDirection.Decrease)]
    public async Task Accepted_original_comparison_saves_exact_adjustment_and_account_together(long observed, long amount, AdjustmentDirection direction)
    {
        using var f = new FlowFixture(); var account = await SeedAsync(f); var session = new AccountReconciliationSession(f.Store);
        var review = await session.CompareAsync(account.Id, observed, Day);
        Assert.Equal(10000, review!.Result.Recorded);
        Assert.True((await session.AdjustAsync(() => Task.FromResult(true), () => "  Original reviewed reason  "))!.Succeeded);
        var entry = Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        Assert.Equal(EntryKind.Adjustment, entry.Kind); Assert.Equal(amount, entry.Amount); Assert.Equal(direction, entry.Direction);
        Assert.Equal("Original reviewed reason", entry.Note); Assert.Equal(Day, entry.Date);
        var saved = (await f.Store.GetAccountsAsync(cancellationToken: Ct)).Single();
        Assert.True(saved.OpeningBalanceKnown); Assert.Equal(Day, saved.LastReconciledOn);
        Assert.Equal(observed, LedgerCalculator.Balance(saved, [entry], Day)); Assert.True(session.Committed); Assert.Null(session.Review);
        Assert.Null(await session.AdjustAsync(() => throw new InvalidOperationException("No second consent"), () => "reason"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Input_change_before_or_during_native_consent_invalidates_the_original_result(bool duringConsent)
    {
        using var f = new FlowFixture(); var account = await SeedAsync(f); var session = new AccountReconciliationSession(f.Store);
        await session.CompareAsync(account.Id, 11234, Day);
        if (!duringConsent) session.Invalidate();
        var confirmations = 0;
        Assert.Null(await session.AdjustAsync(() => { confirmations++; session.Invalidate(); return Task.FromResult(true); }, () => "reason"));
        Assert.Equal(duringConsent ? 1 : 0, confirmations);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        Assert.False((await f.Store.GetAccountsAsync(cancellationToken: Ct)).Single().OpeningBalanceKnown);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Cancellation_or_changed_reason_during_consent_does_not_save(bool changedReason)
    {
        using var f = new FlowFixture(); var account = await SeedAsync(f); var session = new AccountReconciliationSession(f.Store);
        await session.CompareAsync(account.Id, 11234, Day); var reason = "original";
        Assert.Null(await session.AdjustAsync(() => { reason = changedReason ? "changed" : reason; return Task.FromResult(changedReason); }, () => reason));
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct)); Assert.False(session.Committed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Changed_account_or_history_requires_a_fresh_comparison_without_metadata_overwrite(bool history)
    {
        using var f = new FlowFixture(); var account = await SeedAsync(f); var review = await f.Store.ReadReconciliationAsync(account.Id, 11234, Day, Ct);
        if (history) Assert.True((await f.Store.SaveEntryAsync(new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Income, Amount = 100, Date = Day }, Ct)).Succeeded);
        else { account.Name = "Later account edit"; Assert.True(await f.Store.SaveAccountAsync(account, Ct)); }
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.SaveReconciliationAsync(review, "original reason", Ct));
        var entries = await f.Store.GetEntriesAsync(cancellationToken: Ct); Assert.DoesNotContain(entries, e => e.Kind == EntryKind.Adjustment);
        var saved = (await f.Store.GetAccountsAsync(cancellationToken: Ct)).Single(); Assert.False(saved.OpeningBalanceKnown); Assert.Null(saved.LastReconciledOn);
        Assert.Equal(history ? account.Name : "Later account edit", saved.Name);
    }

    [Fact]
    public async Task Matching_original_balance_confirms_metadata_without_creating_a_ledger_entry()
    {
        using var f = new FlowFixture(); var account = await SeedAsync(f); var session = new AccountReconciliationSession(f.Store);
        var review = await session.CompareAsync(account.Id, 10000, Day);
        Assert.Equal(0, review!.Result.Difference); Assert.True(session.Committed); Assert.Null(session.Review);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        Assert.Equal(Day, (await f.Store.GetAccountsAsync(cancellationToken: Ct)).Single().LastReconciledOn);
    }

    [Fact]
    public async Task Review_from_another_actual_file_cannot_authorize_an_identically_numbered_account()
    {
        using var f = new FlowFixture(); using var other = new FlowFixture(); var account = await SeedAsync(f);
        await other.Store.SaveAccountAsync(account.CopyForEditing(), Ct);
        var review = await f.Store.ReadReconciliationAsync(account.Id, 11234, Day, Ct);
        await Assert.ThrowsAsync<InvalidOperationException>(() => other.Store.SaveReconciliationAsync(review, "reason", Ct));
        Assert.Empty(await other.Store.GetEntriesAsync(cancellationToken: Ct));
        Assert.False((await other.Store.GetAccountsAsync(cancellationToken: Ct)).Single().OpeningBalanceKnown);
    }

    [Fact]
    public async Task Required_reason_refusal_does_not_consume_review_and_accepted_review_cannot_be_replayed()
    {
        using var f = new FlowFixture(); var account = await SeedAsync(f);
        var review = await f.Store.ReadReconciliationAsync(account.Id, 11234, Day, Ct);
        await Assert.ThrowsAsync<ArgumentException>(() => f.Store.SaveReconciliationAsync(review, "  ", Ct));
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        Assert.True((await f.Store.SaveReconciliationAsync(review, "original", Ct)).Succeeded);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.SaveReconciliationAsync(review, "original", Ct));
        Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Earlier_comparison_preserves_a_later_original_reconciled_date()
    {
        using var f = new FlowFixture(); var account = await SeedAsync(f);
        account.LastReconciledOn = Day.AddDays(10); await f.Store.SaveAccountAsync(account, Ct);
        var review = await f.Store.ReadReconciliationAsync(account.Id, 11234, Day, Ct);
        await f.Store.SaveReconciliationAsync(review, "earlier comparison", Ct);
        Assert.Equal(Day.AddDays(10), (await f.Store.GetAccountsAsync(cancellationToken: Ct)).Single().LastReconciledOn);
    }

    [Fact]
    public async Task Access_activation_after_review_requires_a_fresh_comparison_even_for_a_formerly_inactive_build()
    {
        using var f = new FlowFixture(); var account = await SeedAsync(f); var access = new Access();
        var path = f.Directory.Combine("zanance.db");
        await using var services = new ServiceCollection().AddSingleton<TimeProvider>(f.Time)
            .AddSingleton<ICommercialWriteAccessSource>(access).AddZananceData(path).BuildServiceProvider();
        var store = services.GetRequiredService<ZananceStore>();
        var review = await store.ReadReconciliationAsync(account.Id, 11234, Day, Ct);
        access.Current = new(path, new(ProductPlan.Free, new(EntitlementScopeKind.PersonalProfile, Guid.NewGuid())));
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.SaveReconciliationAsync(review, "original", Ct));
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Native_consent_in_flight_cannot_start_a_second_financial_operation()
    {
        using var f = new FlowFixture(); var account = await SeedAsync(f); var session = new AccountReconciliationSession(f.Store);
        await session.CompareAsync(account.Id, 11234, Day);
        var confirmation = new TaskCompletionSource<bool>();
        var first = session.AdjustAsync(() => confirmation.Task, () => "original");
        Assert.Null(await session.AdjustAsync(() => throw new InvalidOperationException("No duplicate dialog"), () => "second"));
        Assert.Null(await session.CompareAsync(account.Id, 21234, Day));
        confirmation.SetResult(true); Assert.True((await first)!.Succeeded);
        Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Failed_account_write_rolls_back_the_adjustment_and_keeps_original_review_retryable()
    {
        using var f = new FlowFixture(); var account = await SeedAsync(f); var review = await f.Store.ReadReconciliationAsync(account.Id, 11234, Day, Ct);
        await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER refuse_reconciliation BEFORE UPDATE ON Accounts BEGIN SELECT RAISE(ABORT, 'original failure'); END;", Ct);
        await Assert.ThrowsAsync<DbUpdateException>(() => f.Store.SaveReconciliationAsync(review, "reason", Ct));
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        Assert.False((await f.Store.GetAccountsAsync(cancellationToken: Ct)).Single().OpeningBalanceKnown);
        await db.Database.ExecuteSqlRawAsync("DROP TRIGGER refuse_reconciliation;", Ct);
        Assert.True((await f.Store.SaveReconciliationAsync(review, "reason", Ct)).Succeeded);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Retired_application_draft_before_commit_rolls_back_even_after_the_actual_save(bool matching)
    {
        using var f = new FlowFixture(); var account = await SeedAsync(f);
        var review = await f.Store.ReadReconciliationAsync(account.Id, matching ? 10000 : 11234, Day, Ct);
        var checks = 0;
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.SaveReconciliationAsync(review, "reason", Ct,
            isCurrent: () => ++checks < 3));
        Assert.Equal(3, checks); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        var saved = (await f.Store.GetAccountsAsync(cancellationToken: Ct)).Single();
        Assert.False(saved.OpeningBalanceKnown); Assert.Null(saved.LastReconciledOn);
    }

    [Fact]
    public async Task Failed_post_commit_notification_retains_accepted_outcome_and_never_reposts()
    {
        using var f = new FlowFixture(); var account = await SeedAsync(f); var session = new AccountReconciliationSession(f.Store);
        await session.CompareAsync(account.Id, 11234, Day);
        f.Store.Changed += (_, _) => throw new InvalidOperationException("Original observer failure");
        await Assert.ThrowsAsync<CommittedWriteNotificationException<SaveResult>>(() => session.AdjustAsync(() => Task.FromResult(true), () => "reason"));
        Assert.True(session.Committed); Assert.Null(session.Review);
        Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        Assert.True((await f.Store.GetAccountsAsync(cancellationToken: Ct)).Single().OpeningBalanceKnown);
    }

    [Theory]
    [InlineData(long.MinValue)]
    [InlineData(long.MaxValue)]
    public async Task Unrepresentable_correction_is_refused_before_consent(long observed)
    {
        using var f = new FlowFixture(); var account = await SeedAsync(f);
        // Max minus a negative opening and Min minus a positive opening must not wrap into another amount.
        if (observed == long.MaxValue) { account.OpeningBalance = -10000; await f.Store.SaveAccountAsync(account, Ct); }
        await Assert.ThrowsAsync<OverflowException>(() => f.Store.ReadReconciliationAsync(account.Id, observed, Day, Ct));
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }
}
