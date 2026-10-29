using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original period-review references and complete ledger must belong to one actual file.</summary>
public sealed class PeriodReviewOriginalSourceTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static async Task<Guid> PrepareAsync(FlowFixture f, string currency, string title, Guid? id = null)
    {
        await f.Store.GetSettingsAsync(Ct);
        await using var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(Ct);
        var account = new Account { Name = title, CurrencyCode = currency, Type = AccountType.Checking,
            OpeningDate = new(2026, 1, 1), OpeningBalance = 10000 };
        db.Accounts.Add(account);
        if (id is { } original) db.Entry(account).Property(a => a.Id).CurrentValue = original;
        db.Entries.Add(new LedgerEntry { AccountId = account.Id, Date = new(2026, 9, 15),
            Kind = EntryKind.Expense, Amount = 1000, Title = title, Review = ReviewState.Unreviewed });
        await db.SaveChangesAsync(Ct); return account.Id;
    }

    [Fact]
    public async Task Original_period_review_ledger_retains_its_first_files_references()
    {
        using var first = new FlowFixture(); using var second = new FlowFixture();
        var original = await PrepareAsync(first, "EUR", "Original review expense");
        await PrepareAsync(second, "USD", "Foreign review expense", original);
        var calls = 0;
        var factory = new AccountDetailOriginalBoundaryTests.ControlledFactory(AccountDetailOriginalBoundaryTests.Factory(first))
        { Select = () => Interlocked.Increment(ref calls) <= 2 ? AccountDetailOriginalBoundaryTests.Factory(first) : AccountDetailOriginalBoundaryTests.Factory(second) };
        var store = new ZananceStore(factory, first.Time, first.Services.GetRequiredService<ICommercialWriteAccessSource>());
        // The original three-call failure is preserved; the complete original source now captures one lease.
        var snapshot = await store.ReadPeriodReviewAsync(cancellationToken: Ct);
        var accounts = snapshot.Accounts; var entries = snapshot.Entries;
        Assert.Equal(1, calls);
        Assert.Equal("EUR", Assert.Single(accounts).CurrencyCode);
        Assert.Equal("Original review expense", Assert.Single(entries).Title);
    }

    [Fact]
    public async Task Original_period_review_missing_settings_read_has_no_persisted_side_effect()
    {
        using var f = new FlowFixture();
        var source = await f.Store.ReadPeriodReviewAsync(cancellationToken: Ct);
        Assert.Null(source.Settings); Assert.Empty(source.Accounts); Assert.Empty(source.Entries);
        await using var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(Ct);
        Assert.Empty(await db.Settings.ToListAsync(Ct));
    }

    [Fact]
    public async Task Original_period_review_rejects_mutated_and_persistently_changed_complete_source()
    {
        using var f = new FlowFixture(); await PrepareAsync(f, "EUR", "Original review expense");
        var source = await f.Store.ReadPeriodReviewAsync(cancellationToken: Ct);
        await f.Store.ValidatePeriodReviewAsync(source, Ct);
        source.Entries[0].Title = "Detached mutation";
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.ValidatePeriodReviewAsync(source, Ct));
        source.Entries[0].Title = "Original review expense";
        await using var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(Ct);
        await db.Entries.ExecuteUpdateAsync(set => set.SetProperty(e => e.Amount, 1001), Ct);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.ValidatePeriodReviewAsync(source, Ct));
    }
}
