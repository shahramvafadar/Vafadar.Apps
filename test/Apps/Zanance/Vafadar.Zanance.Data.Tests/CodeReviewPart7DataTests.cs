using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Testing;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.Data.Tests;

/// <summary>Regression tests of the code review, part 7: recording (the data side of the transaction list).</summary>
public sealed class CodeReviewPart7DataTests : IDisposable
{
    private static readonly DateOnly Day = new(2026, 10, 3);
    private readonly TemporaryDirectory _directory = new();
    private readonly ServiceProvider _services;
    private readonly ZananceStore _store;

    public CodeReviewPart7DataTests()
    {
        _services = new ServiceCollection().AddZananceData(_directory.Combine("zanance.db")).BuildServiceProvider();
        _services.MigrateLocalDatabase<ZananceDbContext>();
        _store = _services.GetRequiredService<ZananceStore>();
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose()
    {
        _services.Dispose();
        SqliteTestPools.Clear(_directory);
        _directory.Dispose();
    }

    [Fact]
    public async Task A_bulk_delete_removes_the_selection_with_its_groups_in_one_change_and_can_be_undone()
    {
        // CR07: the list deleted a selection entry by entry – one transaction and one change notification per entry.
        var checking = await NewAccountAsync("Checking");
        var savings = await NewAccountAsync("Savings");
        var coffee = new LedgerEntry { Kind = EntryKind.Expense, AccountId = checking.Id, Amount = 350, Date = Day };
        var lunch = new LedgerEntry { Kind = EntryKind.Expense, AccountId = checking.Id, Amount = 1_200, Date = Day };
        var kept = new LedgerEntry { Kind = EntryKind.Income, AccountId = checking.Id, Amount = 100_000, Date = Day };
        var transfer = new LedgerEntry { Kind = EntryKind.Transfer, AccountId = checking.Id, ToAccountId = savings.Id, Amount = 5_000, Date = Day };
        var fee = EntryActions.SyncTransferFee(transfer, null, 150, null)!;
        Assert.True((await _store.SaveEntriesAsync([coffee, lunch, kept, transfer, fee], [], Ct)).Succeeded);

        var changes = 0;
        _store.Changed += (_, _) => changes++;

        // The transfer is selected without its fee, and one id twice: the fee goes with it, nothing is deleted twice.
        var deleted = await _store.DeleteEntriesAsync([coffee.Id, lunch.Id, transfer.Id, coffee.Id], Ct);

        Assert.Equal(4, deleted.Count);
        Assert.Equal(1, changes);
        Assert.Equal(kept.Id, Assert.Single(await _store.GetEntriesAsync(cancellationToken: Ct)).Id);

        await _store.RestoreEntriesAsync(deleted, Ct);
        Assert.Equal(5, (await _store.GetEntriesAsync(cancellationToken: Ct)).Count);
    }

    [Fact]
    public async Task A_bulk_delete_of_nothing_changes_nothing()
    {
        var changes = 0;
        _store.Changed += (_, _) => changes++;

        Assert.Empty(await _store.DeleteEntriesAsync([Guid.NewGuid()], Ct));
        Assert.Equal(0, changes);
    }

    private async Task<Account> NewAccountAsync(string name)
    {
        var account = new Account { Name = name, CurrencyCode = "EUR", OpeningDate = new DateOnly(2026, 10, 1) };
        await _store.SaveAccountAsync(account, Ct);
        return account;
    }
}
