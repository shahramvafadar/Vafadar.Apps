using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Testing;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.Data.Tests;

/// <summary>Data side of enhancement ZEX phase 1: the migration of the new settings and account fields, and the currency lock.</summary>
public sealed class ZexPhase1DataTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose()
    {
        SqliteTestPools.Clear(_directory);
        _directory.Dispose();
    }

    [Fact]
    public async Task Upgrading_copies_the_report_currency_to_the_default_currency_and_marks_money_accounts_usable()
    {
        await using var services = new ServiceCollection().AddZananceData(_directory.Combine("zanance.db")).BuildServiceProvider();
        var factory = services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        var migrations = (await factory.CreateDbContextAsync(Ct)).Database.GetMigrations().ToList();
        var before = migrations[migrations.FindIndex(m => m.EndsWith("_ZexDefaultsAndAccounts", StringComparison.Ordinal)) - 1];

        await using (var db = await factory.CreateDbContextAsync(Ct))
        {
            await db.GetService<IMigrator>().MigrateAsync(before, Ct);
            await InsertAsync(db, "Settings", new() { ["Id"] = Guid.CreateVersion7(), ["ReportCurrencyCode"] = "USD", ["ReminderTime"] = "09:00:00" });
            await InsertAsync(db, "Accounts", new() { ["Id"] = Guid.CreateVersion7(), ["Name"] = "Checking", ["Type"] = (int)AccountType.Checking, ["CurrencyCode"] = "EUR", ["OpeningDate"] = "2026-01-01" });
            await InsertAsync(db, "Accounts", new() { ["Id"] = Guid.CreateVersion7(), ["Name"] = "Loan", ["Type"] = (int)AccountType.Loan, ["CurrencyCode"] = "EUR", ["OpeningDate"] = "2026-01-01" });
        }

        services.MigrateLocalDatabase<ZananceDbContext>();

        var store = services.GetRequiredService<ZananceStore>();
        var settings = await store.GetSettingsAsync(Ct);
        Assert.Equal(("USD", "USD", true, 30), (settings.DefaultCurrencyCode, settings.ReportCurrencyCode, settings.ValuationCurrencyEnabled, settings.RateFreshnessDays));
        var accounts = await store.GetAccountsAsync(cancellationToken: Ct);
        Assert.True(accounts.Single(a => a.Type == AccountType.Checking).UsableForPayments);
        Assert.False(accounts.Single(a => a.Type == AccountType.Loan).UsableForPayments);
    }

    [Fact]
    public async Task A_plan_locks_the_currency_of_an_account_without_entries()
    {
        await using var services = new ServiceCollection().AddZananceData(_directory.Combine("lock.db")).BuildServiceProvider();
        services.MigrateLocalDatabase<ZananceDbContext>();
        var store = services.GetRequiredService<ZananceStore>();
        var plans = services.GetRequiredService<PlanStore>();
        var account = new Account { Name = "Main", CurrencyCode = "EUR", OpeningDate = new DateOnly(2026, 1, 1) };
        await store.SaveAccountAsync(account, Ct);
        Assert.False((await store.GetCurrencyLockAsync(account.Id, Ct)).IsLocked);

        await plans.SaveSchedulesAsync(
        [
            new Schedule
            {
                Name = "Rent",
                Kind = EntryKind.Expense,
                AccountId = account.Id,
                Amount = 950_00,
                Rule = new RecurrenceRule { Frequency = Frequency.Monthly, Start = new DateOnly(2026, 1, 5) },
            },
        ], Ct);

        var currencyLock = await store.GetCurrencyLockAsync(account.Id, Ct);
        Assert.Equal(1, currencyLock.Plans);
        account.CurrencyCode = "USD";
        Assert.False(await store.SaveAccountAsync(account, Ct));
        Assert.Equal("EUR", (await store.GetAccountsAsync(cancellationToken: Ct)).Single().CurrencyCode);
    }

    // Inserts a row with the columns of the schema at hand: every NOT NULL column without a default gets a neutral value.
    private static async Task InsertAsync(ZananceDbContext db, string table, Dictionary<string, object> values)
    {
        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync(Ct);
        var columns = new List<(string Name, string Type)>();
        await using (var info = connection.CreateCommand())
        {
            info.CommandText = $"SELECT name, type FROM pragma_table_info('{table}') WHERE \"notnull\" = 1 AND dflt_value IS NULL";
            await using var reader = await info.ExecuteReaderAsync(Ct);
            while (await reader.ReadAsync(Ct))
            {
                columns.Add((reader.GetString(0), reader.GetString(1)));
            }
        }

        foreach (var (name, type) in columns.Where(c => !values.ContainsKey(c.Name)))
        {
            values[name] = type.Equals("TEXT", StringComparison.OrdinalIgnoreCase) ? string.Empty : 0;
        }

        await using var insert = connection.CreateCommand();
        insert.CommandText = $"INSERT INTO {table} ({string.Join(", ", values.Keys)}) VALUES ({string.Join(", ", values.Keys.Select(k => "@" + k))})";
        foreach (var (name, value) in values)
        {
            var parameter = insert.CreateParameter();
            parameter.ParameterName = "@" + name;
            parameter.Value = value is Guid id ? id.ToString().ToUpperInvariant() : value;
            insert.Parameters.Add(parameter);
        }

        await insert.ExecuteNonQueryAsync(Ct);
    }
}
