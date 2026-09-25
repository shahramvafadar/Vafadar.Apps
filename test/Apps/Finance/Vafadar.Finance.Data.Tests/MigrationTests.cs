using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Finance.Core.Accounts;
using Vafadar.Finance.Core.Ledger;
using Vafadar.Testing;

namespace Vafadar.Finance.Data.Tests;

public sealed class MigrationTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        _directory.Dispose();
    }

    [Fact]
    [Trait("AT", "AT-60")]
    public async Task Upgrading_from_the_first_schema_keeps_every_account_and_entry()
    {
        await using var services = new ServiceCollection().AddFinanceData(_directory.Combine("finance.db")).BuildServiceProvider();
        var factory = services.GetRequiredService<IDbContextFactory<FinanceDbContext>>();
        var migrations = (await factory.CreateDbContextAsync(Ct)).Database.GetMigrations().ToList();
        Assert.True(migrations.Count > 1, "Needs at least one migration after the first.");

        // A database as the first released version created it.
        await using (var db = await factory.CreateDbContextAsync(Ct))
        {
            await db.GetService<IMigrator>().MigrateAsync(migrations[0], Ct);
        }

        var store = services.GetRequiredService<FinanceStore>();
        var account = new Account { Name = "Checking", CurrencyCode = "EUR", OpeningBalance = 1_000, OpeningDate = new DateOnly(2026, 1, 1) };
        await using (var db = await factory.CreateDbContextAsync(Ct))
        {
            db.Accounts.Add(account);
            db.Entries.Add(new LedgerEntry { Kind = EntryKind.Expense, AccountId = account.Id, Amount = 250, Date = new DateOnly(2026, 2, 1) });
            await db.SaveChangesAsync(Ct);
        }

        services.MigrateLocalDatabase<FinanceDbContext>();

        await using (var db = await factory.CreateDbContextAsync(Ct))
        {
            Assert.Empty(await db.Database.GetPendingMigrationsAsync(Ct));
        }

        var accounts = await store.GetAccountsAsync(cancellationToken: Ct);
        var entries = await store.GetEntriesAsync(cancellationToken: Ct);
        Assert.Equal(750, LedgerCalculator.Balance(accounts.Single(), entries, new DateOnly(2026, 12, 31)));
    }
}
