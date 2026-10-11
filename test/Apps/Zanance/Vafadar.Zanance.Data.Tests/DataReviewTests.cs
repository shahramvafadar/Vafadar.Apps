using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Testing;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.Data.Tests;

/// <summary>Regression tests for data defects found in the full code review of 2026-09-29.</summary>
public sealed class DataReviewTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();
    private readonly ServiceProvider _services;
    private readonly ZananceStore _store;

    public DataReviewTests()
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
    public async Task Renaming_a_saved_filter_keeps_one_filter_and_replaces_one_with_the_new_name()
    {
        var groceries = new SavedFilter { Name = "Groceries", Search = "market" };
        var other = new SavedFilter { Name = "Travel", Search = "train" };
        await _store.SaveSavedFilterAsync(groceries, Ct);
        await _store.SaveSavedFilterAsync(other, Ct);

        groceries.Name = "Travel";
        await _store.SaveSavedFilterAsync(groceries, Ct);

        var filter = Assert.Single(await _store.GetSavedFiltersAsync(Ct));
        Assert.Equal(groceries.Id, filter.Id);
        Assert.Equal("market", filter.Search);
    }

    [Fact]
    public async Task Undoing_the_deletion_of_a_purchase_links_its_refunds_again()
    {
        var account = await NewAccountAsync();
        var purchase = new LedgerEntry { Kind = EntryKind.Expense, AccountId = account.Id, Amount = 1_000, Date = new DateOnly(2026, 10, 3) };
        await _store.SaveEntryAsync(purchase, Ct);
        var refund = EntryActions.CreateRefund(purchase, 400, account.Id, new DateOnly(2026, 10, 4));
        await _store.SaveEntryAsync(refund, Ct);

        var deleted = await _store.DeleteEntryAsync(purchase.Id, Ct);
        Assert.Empty(await _store.GetRefundsAsync(purchase.Id, Ct));

        await _store.RestoreEntriesAsync(deleted, Ct);

        Assert.Equal(refund.Id, Assert.Single(await _store.GetRefundsAsync(purchase.Id, Ct)).Id);
    }

    [Fact]
    public async Task An_imported_refund_without_its_purchase_is_rejected()
    {
        var account = await NewAccountAsync();
        var orphan = new LedgerEntry { Kind = EntryKind.Refund, AccountId = account.Id, Amount = 300, Date = new DateOnly(2026, 10, 4), RefundOfId = Guid.NewGuid() };

        var result = await _store.ImportAsync([orphan], Ct);

        Assert.Equal([LedgerError.RefundOriginalMissing], result.Errors[orphan.Id]);
        Assert.Empty(await _store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task An_import_takes_a_refund_with_its_purchase_from_the_same_file_and_a_repeated_row_once()
    {
        var account = await NewAccountAsync();
        var purchase = new LedgerEntry { Kind = EntryKind.Expense, AccountId = account.Id, Amount = 1_000, Date = new DateOnly(2026, 10, 3) };
        var refund = new LedgerEntry { Kind = EntryKind.Refund, AccountId = account.Id, Amount = 300, Date = new DateOnly(2026, 10, 4), RefundOfId = purchase.Id };

        var result = await _store.ImportAsync([refund, purchase, purchase], Ct);

        Assert.Empty(result.Errors);
        Assert.Equal(2, result.Imported);
        Assert.Equal(1, result.Skipped);
        Assert.Equal(2, (await _store.GetEntriesAsync(cancellationToken: Ct)).Count);
    }

    [Fact]
    public async Task Imported_refunds_together_may_not_exceed_their_purchase()
    {
        var account = await NewAccountAsync();
        var purchase = new LedgerEntry { Kind = EntryKind.Expense, AccountId = account.Id, Amount = 1_000, Date = new DateOnly(2026, 10, 3) };
        await _store.SaveEntryAsync(purchase, Ct);
        var first = new LedgerEntry { Kind = EntryKind.Refund, AccountId = account.Id, Amount = 700, Date = new DateOnly(2026, 10, 4), RefundOfId = purchase.Id };
        var second = new LedgerEntry { Kind = EntryKind.Refund, AccountId = account.Id, Amount = 700, Date = new DateOnly(2026, 10, 5), RefundOfId = purchase.Id };

        var result = await _store.ImportAsync([first, second], Ct);

        Assert.Contains(LedgerError.RefundExceedsPurchase, result.Errors[second.Id]);
        Assert.Single(await _store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Settings_read_at_the_same_time_are_created_once()
    {
        var reads = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => _store.GetSettingsAsync(Ct)));

        Assert.Single(reads.Select(s => s.Id).Distinct());
    }

    private async Task<Account> NewAccountAsync()
    {
        var account = new Account { Name = "Checking", CurrencyCode = "EUR", OpeningDate = new DateOnly(2026, 10, 1) };
        await _store.SaveAccountAsync(account, Ct);
        return account;
    }
}
