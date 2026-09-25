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
        await _store.RestoreEntryAsync(deleted!, Ct);

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

    private async Task<Account> NewAccountAsync()
    {
        var account = new Account { Name = "Checking", CurrencyCode = "EUR", OpeningDate = new DateOnly(2026, 10, 1) };
        await _store.SaveAccountAsync(account, Ct);
        return account;
    }
}
