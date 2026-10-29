using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual published transaction-reader composition must never combine original references with another file's ledger.</summary>
public sealed class TransactionsOriginalSourceBoundaryTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static async Task<Guid> SeedAsync(FlowFixture fixture, string currency, string title, long amount, Guid? accountId = null)
    {
        await fixture.Store.GetSettingsAsync(Ct);
        await using var db = await AccountDetailOriginalBoundaryTests.Factory(fixture).CreateDbContextAsync(Ct);
        var account = new Account { Name = title, Type = AccountType.Checking, CurrencyCode = currency,
            OpeningDate = new(2026, 10, 1), OpeningBalance = 0 };
        if (accountId is { } id) db.Entry(account).Property(a => a.Id).CurrentValue = id;
        db.Accounts.Add(account); db.Entries.Add(new LedgerEntry { AccountId = account.Id, Date = new(2026, 10, 2),
            Kind = EntryKind.Expense, Amount = amount, Title = title });
        await db.SaveChangesAsync(Ct); return account.Id;
    }

    [Fact]
    public async Task Original_transaction_rows_remain_with_their_original_account_currency_and_file()
    {
        using var first = new FlowFixture(); using var second = new FlowFixture();
        var id = await SeedAsync(first, "EUR", "First original row", 1000);
        await SeedAsync(second, "USD", "Foreign replacement row", 9999, id);
        var calls = 0; var factory = new AccountDetailOriginalBoundaryTests.ControlledFactory(AccountDetailOriginalBoundaryTests.Factory(first))
        { Select = () => Interlocked.Increment(ref calls) <= 3 ? AccountDetailOriginalBoundaryTests.Factory(first) : AccountDetailOriginalBoundaryTests.Factory(second) };
        var store = new ZananceStore(factory, first.Time, first.Services.GetRequiredService<ICommercialWriteAccessSource>());
        // The failing published composition is preserved separately; the replacement captures one actual lease.
        var snapshot = await store.ReadTransactionsAsync(cancellationToken: Ct);
        var accounts = snapshot.Resources.Accounts; var entries = snapshot.Entries;
        Assert.Equal(1, calls); Assert.Equal("EUR", Assert.Single(accounts).CurrencyCode);
        Assert.Equal("First original row", Assert.Single(entries).Title);
        Assert.Equal(1000, Assert.Single(entries).Amount);
    }

    [Fact]
    public async Task A_changed_original_ledger_or_detached_payload_cannot_authorize_publication()
    {
        using var fixture = new FlowFixture();
        await SeedAsync(fixture, "EUR", "Original row", 1000);
        var snapshot = await fixture.Store.ReadTransactionsAsync(cancellationToken: Ct);
        await fixture.Store.ValidateTransactionsAsync(snapshot, Ct);
        snapshot.Entries[0].Title = "Changed detached row";
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Store.ValidateTransactionsAsync(snapshot, Ct));
        snapshot.Entries[0].Title = "Original row";
        await using var db = await AccountDetailOriginalBoundaryTests.Factory(fixture).CreateDbContextAsync(Ct);
        await db.Entries.ExecuteUpdateAsync(set => set.SetProperty(e => e.Amount, 1001), Ct);
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Store.ValidateTransactionsAsync(snapshot, Ct));
    }
}
