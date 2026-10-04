using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Testing;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Settings;

namespace Vafadar.Zanance.Data.Tests;

/// <summary>Data side of enhancement ZEX phase 4: the new columns keep their values and the essential defaults.</summary>
public sealed class ZexPhase4DataTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();
    private readonly ServiceProvider _services;
    private readonly ZananceStore _store;

    public ZexPhase4DataTests()
    {
        _services = new ServiceCollection().AddZananceData(_directory.Combine("zanance.db")).BuildServiceProvider();
        _services.MigrateLocalDatabase<ZananceDbContext>();
        _store = _services.GetRequiredService<ZananceStore>();
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose()
    {
        _services.Dispose();
        SqliteConnection.ClearAllPools();
        _directory.Dispose();
    }

    [Fact]
    public async Task Default_categories_mark_essential_spending()
    {
        await _store.EnsureDefaultCategoriesAsync(Ct);
        var categories = await _store.GetCategoriesAsync(Ct);

        Assert.True(categories.Single(c => c.SystemKey == "Housing").IsEssential);
        Assert.True(categories.Single(c => c.SystemKey == "Food").IsEssential);
        Assert.False(categories.Single(c => c.SystemKey == "Leisure").IsEssential);
    }

    [Fact]
    public async Task Reconciliation_date_due_dates_aggregates_and_the_estimate_are_kept()
    {
        var lent = new Account { Name = "Sara", Type = AccountType.Lent, CurrencyCode = "EUR", OpeningDate = new DateOnly(2026, 1, 1), LastReconciledOn = new DateOnly(2026, 9, 30), DueDate = new DateOnly(2026, 12, 1) };
        await _store.SaveAccountAsync(lent, Ct);
        var cash = new Account { Name = "Cash", Type = AccountType.Cash, CurrencyCode = "EUR", OpeningDate = new DateOnly(2026, 1, 1), OpeningBalance = 1_000_00 };
        await _store.SaveAccountAsync(cash, Ct);
        await _store.EnsureDefaultCategoriesAsync(Ct);
        var food = (await _store.GetCategoriesAsync(Ct)).Single(c => c.SystemKey == "Food").Id;
        var entry = new LedgerEntry
        {
            Kind = EntryKind.Expense, AccountId = cash.Id, Amount = 412_00, Date = new DateOnly(2026, 9, 30), CategoryId = food,
            IsAggregated = true, AggregatedFrom = new DateOnly(2026, 9, 1), AggregatedTo = new DateOnly(2026, 9, 30),
        };
        Assert.True((await _store.SaveEntryAsync(entry, Ct)).Succeeded);
        var settings = await _store.GetSettingsAsync(Ct);
        settings.EssentialEstimate = 20_00;
        settings.EssentialEstimatePeriod = EstimatePeriod.Week;
        settings.EssentialEstimateCurrency = "EUR";
        settings.ReviewProgress = "2026-09:Plans";
        await _store.SaveSettingsAsync(settings, Ct);

        var account = (await _store.GetAccountsAsync(cancellationToken: Ct)).Single(a => a.Id == lent.Id);
        var saved = (await _store.GetEntriesAsync(cancellationToken: Ct)).Single();
        var reloaded = await _store.GetSettingsAsync(Ct);

        Assert.Equal((new DateOnly(2026, 9, 30), new DateOnly(2026, 12, 1)), (account.LastReconciledOn, account.DueDate));
        Assert.Equal((true, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30)), (saved.IsAggregated, saved.AggregatedFrom, saved.AggregatedTo));
        Assert.Equal((20_00L, EstimatePeriod.Week, "EUR", "2026-09:Plans"), (reloaded.EssentialEstimate!.Value, reloaded.EssentialEstimatePeriod, reloaded.EssentialEstimateCurrency, reloaded.ReviewProgress));
    }
}
