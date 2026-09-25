using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Finance.Core.Accounts;
using Vafadar.Finance.Core.Categories;
using Vafadar.Finance.Core.Ledger;
using Vafadar.Testing;

namespace Vafadar.Finance.Data.Tests;

public sealed class FinanceStoreTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();
    private readonly ServiceProvider _services;
    private readonly FinanceStore _store;

    public FinanceStoreTests()
    {
        _services = new ServiceCollection().AddFinanceData(_directory.Combine("finance.db")).BuildServiceProvider();
        _services.MigrateLocalDatabase<FinanceDbContext>();
        _store = _services.GetRequiredService<FinanceStore>();
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose()
    {
        _services.Dispose();
        SqliteConnection.ClearAllPools();
        _directory.Dispose();
    }

    [Fact]
    [Trait("AT", "AT-03")]
    public async Task Saving_the_same_entry_twice_creates_one_entry()
    {
        var account = await NewAccountAsync();
        var entry = new LedgerEntry { Kind = EntryKind.Expense, AccountId = account.Id, Amount = 500, Date = new DateOnly(2026, 10, 1) };

        await Task.WhenAll(_store.SaveEntryAsync(entry, Ct), Task.Delay(1, Ct));
        await _store.SaveEntryAsync(entry, Ct);

        Assert.Single(await _store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Invalid_entries_are_not_saved_and_report_the_reason()
    {
        var account = await NewAccountAsync();
        var result = await _store.SaveEntryAsync(new LedgerEntry { Kind = EntryKind.Expense, AccountId = account.Id, Amount = 0 }, Ct);

        Assert.Contains(LedgerError.AmountMustBePositive, result.Errors);
        Assert.Empty(await _store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Delete_and_undo_restore_the_entry_unchanged()
    {
        var account = await NewAccountAsync();
        var entry = new LedgerEntry { Kind = EntryKind.Income, AccountId = account.Id, Amount = 700, Date = new DateOnly(2026, 10, 2), Note = "line 1\nline 2" };
        await _store.SaveEntryAsync(entry, Ct);
        var created = (await _store.GetEntryAsync(entry.Id, Ct))!.CreatedAt;

        var deleted = await _store.DeleteEntryAsync(entry.Id, Ct);
        Assert.Empty(await _store.GetEntriesAsync(cancellationToken: Ct));
        await _store.RestoreEntriesAsync(deleted, Ct);

        var restored = await _store.GetEntryAsync(entry.Id, Ct);
        Assert.NotNull(restored);
        Assert.Equal("line 1\nline 2", restored.Note);
        Assert.Equal(created, restored.CreatedAt);
    }

    [Fact]
    public async Task Currency_of_an_account_with_entries_cannot_change()
    {
        var account = await NewAccountAsync();
        await _store.SaveEntryAsync(new LedgerEntry { Kind = EntryKind.Income, AccountId = account.Id, Amount = 1, Date = new DateOnly(2026, 10, 1) }, Ct);

        account.CurrencyCode = "USD";

        Assert.False(await _store.SaveAccountAsync(account, Ct));
        Assert.Equal("EUR", (await _store.GetAccountsAsync(cancellationToken: Ct)).Single().CurrencyCode);
    }

    [Fact]
    [Trait("AT", "AT-10")]
    public async Task Archived_account_keeps_its_history_in_past_reports()
    {
        var account = await NewAccountAsync();
        await _store.SaveEntryAsync(new LedgerEntry { Kind = EntryKind.Expense, AccountId = account.Id, Amount = 4_000, Date = new DateOnly(2026, 10, 10) }, Ct);

        account.IsArchived = true;
        Assert.True(await _store.SaveAccountAsync(account, Ct));

        Assert.Empty(await _store.GetAccountsAsync(includeArchived: false, cancellationToken: Ct));
        var all = await _store.GetAccountsAsync(cancellationToken: Ct);
        var entries = await _store.GetEntriesAsync(cancellationToken: Ct);
        var october = LedgerCalculator.Totals(all, entries, new LedgerFilter(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31)));
        Assert.Equal(4_000, october.Single().GrossExpense);
    }

    [Fact]
    public async Task Default_categories_are_created_once()
    {
        await _store.EnsureDefaultCategoriesAsync(Ct);
        await _store.EnsureDefaultCategoriesAsync(Ct);

        var categories = await _store.GetCategoriesAsync(Ct);
        Assert.Equal(DefaultCategories.All.Count, categories.Count);
        Assert.All(categories, c => Assert.NotNull(c.SystemKey));
    }

    [Fact]
    public async Task Defaults_added_later_are_created_without_touching_existing_categories()
    {
        await _store.EnsureDefaultCategoriesAsync(Ct);
        var categories = await _store.GetCategoriesAsync(Ct);
        var food = categories.Single(c => c.SystemKey == "Food");
        food.Name = "Groceries";
        await _store.SaveCategoryAsync(food, Ct);
        var fees = categories.Single(c => c.SystemKey == DefaultCategories.Fees);
        await _services.GetRequiredService<IDbContextFactory<FinanceDbContext>>().CreateDbContext().Categories.Where(c => c.Id == fees.Id).ExecuteDeleteAsync(Ct);

        await _store.EnsureDefaultCategoriesAsync(Ct);

        categories = await _store.GetCategoriesAsync(Ct);
        Assert.Equal(DefaultCategories.All.Count, categories.Count);
        Assert.Equal("Groceries", categories.Single(c => c.SystemKey == "Food").Name);
    }

    [Fact]
    [Trait("AT", "AT-08")]
    public async Task Transfer_and_fee_are_saved_together_and_deleted_together_with_undo()
    {
        var checking = await NewAccountAsync();
        var savings = await NewAccountAsync("Savings");
        var transfer = new LedgerEntry { Kind = EntryKind.Transfer, AccountId = checking.Id, ToAccountId = savings.Id, Amount = 5_000, Date = new DateOnly(2026, 10, 3) };
        var fee = EntryActions.SyncTransferFee(transfer, null, 150, null)!;

        Assert.True((await _store.SaveEntriesAsync([transfer, fee], [], Ct)).Succeeded);
        Assert.Equal(2, (await _store.GetGroupAsync(transfer.GroupId!.Value, Ct)).Count);

        var deleted = await _store.DeleteEntryAsync(transfer.Id, Ct);
        Assert.Equal(2, deleted.Count);
        Assert.Empty(await _store.GetEntriesAsync(cancellationToken: Ct));

        await _store.RestoreEntriesAsync(deleted, Ct);
        Assert.Equal(2, (await _store.GetEntriesAsync(cancellationToken: Ct)).Count);
    }

    [Fact]
    public async Task Removing_a_fee_deletes_it_in_the_same_save()
    {
        var checking = await NewAccountAsync();
        var savings = await NewAccountAsync("Savings");
        var transfer = new LedgerEntry { Kind = EntryKind.Transfer, AccountId = checking.Id, ToAccountId = savings.Id, Amount = 5_000, Date = new DateOnly(2026, 10, 3) };
        var fee = EntryActions.SyncTransferFee(transfer, null, 150, null)!;
        await _store.SaveEntriesAsync([transfer, fee], [], Ct);

        Assert.True((await _store.SaveEntriesAsync([transfer], [fee.Id], Ct)).Succeeded);

        Assert.Single(await _store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task An_invalid_entry_in_a_batch_saves_nothing()
    {
        var checking = await NewAccountAsync();
        var valid = new LedgerEntry { Kind = EntryKind.Expense, AccountId = checking.Id, Amount = 100, Date = new DateOnly(2026, 10, 3) };
        var invalid = new LedgerEntry { Kind = EntryKind.Expense, AccountId = checking.Id, Amount = 0, Date = new DateOnly(2026, 10, 3) };

        var result = await _store.SaveEntriesAsync([valid, invalid], [], Ct);

        Assert.False(result.Succeeded);
        Assert.Empty(await _store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    [Trait("AT", "AT-15")]
    public async Task Refunds_are_limited_to_the_purchase_amount()
    {
        var checking = await NewAccountAsync();
        var purchase = new LedgerEntry { Kind = EntryKind.Expense, AccountId = checking.Id, Amount = 1_000, Date = new DateOnly(2026, 10, 3) };
        await _store.SaveEntryAsync(purchase, Ct);
        await _store.SaveEntryAsync(EntryActions.CreateRefund(purchase, 600, checking.Id, new DateOnly(2026, 10, 4)), Ct);

        var tooMuch = await _store.SaveEntryAsync(EntryActions.CreateRefund(purchase, 500, checking.Id, new DateOnly(2026, 10, 5)), Ct);

        Assert.Contains(LedgerError.RefundExceedsPurchase, tooMuch.Errors);
        Assert.Single(await _store.GetRefundsAsync(purchase.Id, Ct));
    }

    [Fact]
    public async Task Settings_are_created_on_first_use_and_persist()
    {
        var settings = await _store.GetSettingsAsync(Ct);
        settings.ReportCurrencyCode = "USD";
        settings.OnboardingCompleted = true;
        await _store.SaveSettingsAsync(settings, Ct);

        var reloaded = await _store.GetSettingsAsync(Ct);
        Assert.Equal("USD", reloaded.ReportCurrencyCode);
        Assert.True(reloaded.OnboardingCompleted);
    }

    [Fact]
    public async Task Second_settlement_of_the_same_occurrence_is_rejected_by_the_database()
    {
        var account = await NewAccountAsync();
        var schedule = Guid.CreateVersion7();
        var date = new DateOnly(2026, 10, 1);
        await _store.SaveEntryAsync(new LedgerEntry { Kind = EntryKind.Expense, AccountId = account.Id, Amount = 1, Date = date, ScheduleId = schedule, OccurrenceDate = date }, Ct);

        await Assert.ThrowsAnyAsync<Exception>(() => _store.SaveEntryAsync(new LedgerEntry { Kind = EntryKind.Expense, AccountId = account.Id, Amount = 1, Date = date, ScheduleId = schedule, OccurrenceDate = date }, Ct));
        Assert.Single(await _store.GetEntriesAsync(cancellationToken: Ct));
    }

    private async Task<Account> NewAccountAsync(string name = "Checking")
    {
        var account = new Account { Name = name, CurrencyCode = "EUR", OpeningDate = new DateOnly(2026, 10, 1) };
        await _store.SaveAccountAsync(account, Ct);
        return account;
    }
}
