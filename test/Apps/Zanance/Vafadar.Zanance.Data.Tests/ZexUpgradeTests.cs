using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Vafadar.Backup;
using Vafadar.Core.Hosting;
using Vafadar.Core.Settings;
using Vafadar.Data;
using Vafadar.Testing;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.Data.Tests;

/// <summary>
/// ZEX-S0901: a database and a backup of the version before enhancement ZEX upgrade to the current schema with every
/// balance, goal, budget and setting unchanged, and the new columns get their documented defaults (02 §13).
/// </summary>
public sealed class ZexUpgradeTests : IDisposable
{
    /// <summary>The last migration of the version before enhancement ZEX.</summary>
    private const string BaseVersion = "20260929205530_BudgetPeriods";

    private static readonly DateOnly Day = new(2026, 9, 30);
    private readonly TemporaryDirectory _directory = new();
    private readonly Guid _checking = Guid.CreateVersion7();
    private readonly Guid _card = Guid.CreateVersion7();
    private readonly Guid _loan = Guid.CreateVersion7();
    private readonly Guid _car = Guid.CreateVersion7();
    private readonly Guid _goal = Guid.CreateVersion7();
    private readonly Guid _housing = Guid.CreateVersion7();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose()
    {
        SqliteTestPools.Clear(_directory);
        _directory.Dispose();
    }

    [Fact]
    public async Task Upgrading_a_database_of_the_base_version_keeps_every_number_and_sets_the_new_defaults()
    {
        await using var services = Install("device");
        await CreateBaseVersionAsync(services);

        services.MigrateLocalDatabase<ZananceDbContext>();

        await AssertUpgradedAsync(services);
    }

    [Fact]
    public async Task A_backup_of_the_base_version_restores_into_the_new_version_with_the_same_numbers()
    {
        await using var old = Install("old", oldVersion: true);
        await CreateBaseVersionAsync(old);
        var package = await old.GetRequiredService<IBackupService>().CreatePackageAsync("correct horse", Ct);

        await using var fresh = Install("new");
        fresh.MigrateLocalDatabase<ZananceDbContext>();
        await fresh.GetRequiredService<IBackupService>().RestorePackageAsync(package, "correct horse", Ct);

        await AssertUpgradedAsync(fresh);
    }

    private async Task AssertUpgradedAsync(ServiceProvider services)
    {
        var store = services.GetRequiredService<ZananceStore>();
        var accounts = await store.GetAccountsAsync(cancellationToken: Ct);
        var entries = await store.GetEntriesAsync(cancellationToken: Ct);
        var settings = await store.GetSettingsAsync(Ct);
        var goals = services.GetRequiredService<GoalStore>();

        // Balances as before: 2,000 − 120 on checking, −300 card, −4,000 loan, 8,000 car.
        Assert.Equal(188_000, LedgerCalculator.Balance(accounts.Single(a => a.Id == _checking), entries, Day));
        Assert.Equal(-30_000, LedgerCalculator.Balance(accounts.Single(a => a.Id == _card), entries, Day));
        Assert.Equal(800_000, LedgerCalculator.Balance(accounts.Single(a => a.Id == _car), entries, Day));

        // ZEX-P17: usable for payments by type; the asset account stays a valued asset (ZEX-P11).
        Assert.True(accounts.Single(a => a.Id == _checking).UsableForPayments);
        Assert.False(accounts.Single(a => a.Id == _card).UsableForPayments);
        Assert.False(accounts.Single(a => a.Id == _loan).UsableForPayments);
        Assert.Equal(AccountType.Asset, accounts.Single(a => a.Id == _car).Type);

        // The goal becomes money set aside with its target and earmark.
        var goal = Assert.Single(await goals.GetGoalsAsync(Ct));
        Assert.Equal((GoalType.Earmark, 150_000L), (goal.Type, goal.TargetAmount));
        Assert.Equal(50_000, (await goals.GetAllocationsAsync(goal.Id, Ct)).Sum(a => a.Amount));

        // Settings: the default currency is the old report currency; freshness and valuation have their defaults.
        Assert.Equal(("USD", "USD", 30, true), (settings.ReportCurrencyCode, settings.DefaultCurrencyCode, settings.RateFreshnessDays, settings.ValuationCurrencyEnabled));
        Assert.True((await store.GetCategoriesAsync(Ct)).Single(c => c.Id == _housing).IsEssential);
        Assert.Equal(40_000, (await store.GetBudgetAsync(2026, 9, Core.Budgets.PeriodCalendar.Gregorian, "EUR", Ct))?.TotalLimit);
    }

    // A database as the version before ZEX left it, with rows written in its own columns.
    private async Task CreateBaseVersionAsync(ServiceProvider services)
    {
        var factory = services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        await using var db = await factory.CreateDbContextAsync(Ct);
        await db.GetService<IMigrator>().MigrateAsync(BaseVersion, Ct);
        var connection = (SqliteConnection)db.Database.GetDbConnection();
        await connection.OpenAsync(Ct);

        Insert(connection, "Accounts", new() { ["Id"] = _checking, ["Name"] = "Checking", ["Type"] = (int)AccountType.Checking, ["CurrencyCode"] = "EUR", ["OpeningBalance"] = 200_000, ["OpeningDate"] = "2026-01-01", ["OpeningBalanceKnown"] = 1, ["IncludeInTotals"] = 1 });
        Insert(connection, "Accounts", new() { ["Id"] = _card, ["Name"] = "Card", ["Type"] = (int)AccountType.CreditCard, ["CurrencyCode"] = "EUR", ["OpeningBalance"] = -30_000, ["OpeningDate"] = "2026-01-01", ["OpeningBalanceKnown"] = 1, ["IncludeInTotals"] = 1 });
        Insert(connection, "Accounts", new() { ["Id"] = _loan, ["Name"] = "Loan", ["Type"] = (int)AccountType.Loan, ["CurrencyCode"] = "EUR", ["OpeningBalance"] = -400_000, ["OpeningDate"] = "2026-01-01", ["OpeningBalanceKnown"] = 1, ["IncludeInTotals"] = 0 });
        Insert(connection, "Accounts", new() { ["Id"] = _car, ["Name"] = "Car", ["Type"] = (int)AccountType.Asset, ["CurrencyCode"] = "EUR", ["OpeningBalance"] = 800_000, ["OpeningDate"] = "2026-01-01", ["OpeningBalanceKnown"] = 1, ["IncludeInTotals"] = 1 });
        Insert(connection, "Categories", new() { ["Id"] = _housing, ["Kind"] = 0, ["SystemKey"] = "Housing", ["SpendingType"] = 1 });
        Insert(connection, "Entries", new() { ["Id"] = Guid.CreateVersion7(), ["Kind"] = (int)EntryKind.Expense, ["Date"] = "2026-09-10", ["AccountId"] = _checking, ["Amount"] = 12_000, ["CategoryId"] = _housing, ["Review"] = 0, ["Source"] = 0 });
        Insert(connection, "Goals", new() { ["Id"] = _goal, ["Name"] = "Holiday", ["TargetAmount"] = 150_000, ["CurrencyCode"] = "EUR", ["Priority"] = 1, ["State"] = 0 });
        Insert(connection, "GoalAllocations", new() { ["Id"] = Guid.CreateVersion7(), ["GoalId"] = _goal, ["AccountId"] = _checking, ["Amount"] = 50_000, ["Date"] = "2026-09-01" });
        Insert(connection, "Budgets", new() { ["Id"] = Guid.CreateVersion7(), ["Year"] = 2026, ["Month"] = 9, ["CurrencyCode"] = "EUR", ["TotalLimit"] = 40_000, ["AccountIds"] = "[]" });
        Insert(connection, "Settings", new() { ["Id"] = Guid.CreateVersion7(), ["ReportCurrencyCode"] = "USD", ["MonthStartDay"] = 1, ["OnboardingCompleted"] = 1, ["ReminderTime"] = "09:00:00", ["ReminderDaysBefore"] = 3 });
    }

    // Inserts a row with the given values; every other NOT NULL column without a default gets 0 or an empty text, as
    // the old app would have written its defaults. The column list comes from the table itself.
    private static void Insert(SqliteConnection connection, string table, Dictionary<string, object> values)
    {
        using (var info = connection.CreateCommand())
        {
            info.CommandText = $"PRAGMA table_info({table})";
            using var reader = info.ExecuteReader();
            while (reader.Read())
            {
                var name = reader.GetString(1);
                var notNull = reader.GetInt32(3) == 1;
                if (!values.ContainsKey(name) && notNull && reader.IsDBNull(4))
                {
                    values[name] = reader.GetString(2).Contains("TEXT", StringComparison.OrdinalIgnoreCase) ? string.Empty : 0;
                }
            }
        }

        using var command = connection.CreateCommand();
        command.CommandText = $"INSERT INTO {table} ({string.Join(", ", values.Keys)}) VALUES ({string.Join(", ", values.Keys.Select(k => "$" + k))})";
        foreach (var (key, value) in values)
        {
            command.Parameters.AddWithValue("$" + key, value is Guid id ? id.ToString().ToUpperInvariant() : value is string text ? text : Convert.ToString(value, CultureInfo.InvariantCulture) is { } s && long.TryParse(s, out var number) ? number : value);
        }

        command.ExecuteNonQuery();
    }

    // The old version counted only the tables it had, so its backup summary is replaced for the old install.
    private ServiceProvider Install(string name, bool oldVersion = false)
    {
        var collection = new ServiceCollection()
            .AddZananceData(_directory.Combine(Path.Combine(name, "zanance.db")))
            .AddVafadarBackup()
            .AddSingleton<IAppEnvironment>(new StaticAppEnvironment("pro.vafadar.zanance", "Zanance", new Version(1, 0, 0), name, "Tests"))
            .AddSingleton<ISettingsStore, InMemorySettingsStore>();
        if (oldVersion)
        {
            collection.RemoveAll<IBackupSummaryProvider>();
            collection.AddSingleton<IBackupSummaryProvider, OldVersionSummary>();
        }

        var services = collection.BuildServiceProvider();
        Directory.CreateDirectory(_directory.Combine(name));
        return services;
    }

    private sealed class OldVersionSummary : IBackupSummaryProvider
    {
        public Task<IReadOnlyDictionary<string, string>> GetSummaryAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<string, string>>(new Dictionary<string, string> { [ZananceBackupSummary.Accounts] = "4" });
    }
}
