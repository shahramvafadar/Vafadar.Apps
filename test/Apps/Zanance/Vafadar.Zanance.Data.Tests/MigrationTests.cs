using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Testing;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data.Commerce;
using Vafadar.Backup;
using Vafadar.Zanance.Core.Commerce;

namespace Vafadar.Zanance.Data.Tests;

public sealed class MigrationTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose()
    {
        SqliteTestPools.Clear(_directory);
        _directory.Dispose();
    }

    [Fact]
    [Trait("AT", "AT-60")]
    public async Task Upgrading_from_the_first_schema_keeps_every_account_and_entry()
    {
        await using var services = new ServiceCollection().AddZananceData(_directory.Combine("zanance.db")).BuildServiceProvider();
        var factory = services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        var migrations = (await factory.CreateDbContextAsync(Ct)).Database.GetMigrations().ToList();
        Assert.True(migrations.Count > 1, "Needs at least one migration after the first.");

        // A database as the first released version created it.
        await using (var db = await factory.CreateDbContextAsync(Ct))
        {
            await db.GetService<IMigrator>().MigrateAsync(migrations[0], Ct);
        }

        var store = services.GetRequiredService<ZananceStore>();
        // Rows are written with the columns of the first schema: the current model has columns the old tables lack.
        var accountId = Guid.CreateVersion7();
        var entryId = Guid.CreateVersion7();
        var settingsId = Guid.CreateVersion7();
        await using (var db = await factory.CreateDbContextAsync(Ct))
        {
            await db.Database.ExecuteSqlAsync(
                $"INSERT INTO Settings (Id, ReportCurrencyCode, Mode, BudgetCalendar, WeekStart, ReminderDaysBefore, ReminderTime, NotificationsShowDetails, AppLockEnabled, OnboardingCompleted, CreatedAt, UpdatedAt) VALUES ({settingsId}, 'EUR', 0, 0, 1, 3, '09:00:00', 0, 0, 1, 0, 0)", Ct);
            await db.Database.ExecuteSqlAsync(
                $"INSERT INTO Accounts (Id, Name, Type, CurrencyCode, OpeningBalance, OpeningDate, OpeningBalanceKnown, IncludeInTotals, IsArchived, SortOrder, CreatedAt, UpdatedAt) VALUES ({accountId}, 'Checking', 0, 'EUR', 1000, '2026-01-01', 1, 1, 0, 0, 0, 0)",
                Ct);
            await db.Database.ExecuteSqlAsync(
                $"INSERT INTO Entries (Id, Kind, Date, AccountId, Amount, Review, Source, CreatedAt, UpdatedAt) VALUES ({entryId}, {(int)EntryKind.Expense}, '2026-02-01', {accountId}, 250, 0, 0, 0, 0)",
                Ct);
        }

        services.MigrateLocalDatabase<ZananceDbContext>();

        await using (var db = await factory.CreateDbContextAsync(Ct))
        {
            Assert.Empty(await db.Database.GetPendingMigrationsAsync(Ct));
        }

        var accounts = await store.GetAccountsAsync(cancellationToken: Ct);
        var entries = await store.GetEntriesAsync(cancellationToken: Ct);
        Assert.Equal(750, LedgerCalculator.Balance(accounts.Single(), entries, new DateOnly(2026, 12, 31)));
        Assert.False((await store.GetSettingsAsync(Ct)).ReviewReminderEnabled);
        Assert.Equal(settingsId, (await store.GetSettingsAsync(Ct)).Id);
    }

    [Fact, Trait("AT", "AT-277")]
    public async Task Original_over_free_quota_upgrade_and_portable_restore_keep_data_without_creating_paid_rights()
    {
        var path = _directory.Combine("original-over-quota.db");
        await using var services = new ServiceCollection().AddZananceData(path).BuildServiceProvider();
        var factory = services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        var ids = Enumerable.Range(0, 4).Select(_ => Guid.CreateVersion7()).ToArray();
        var freeQuota = QuotaPolicy.Get(QuotaKind.FinancialAccounts,
            new CapabilityContext(ProductPlan.Free, new(EntitlementScopeKind.PersonalProfile, Guid.CreateVersion7())));
        Assert.Equal(3, freeQuota.Maximum);
        Assert.False(freeQuota.CanAdd(ids.Length));
        await using (var db = await factory.CreateDbContextAsync(Ct))
        {
            await db.GetService<IMigrator>().MigrateAsync(db.Database.GetMigrations().First(), Ct);
            foreach (var id in ids)
                await db.Database.ExecuteSqlAsync($"INSERT INTO Accounts (Id, Name, Type, CurrencyCode, OpeningBalance, OpeningDate, OpeningBalanceKnown, IncludeInTotals, IsArchived, SortOrder, CreatedAt, UpdatedAt) VALUES ({id}, 'Original retained account', 0, 'EUR', 1000, '2026-01-01', 1, 1, 0, 0, 0, 0)", Ct);
            await db.Database.ExecuteSqlAsync($"INSERT INTO Entries (Id, Kind, Date, AccountId, Amount, Review, Source, CreatedAt, UpdatedAt) VALUES ({Guid.CreateVersion7()}, {(int)EntryKind.Expense}, '2026-02-01', {ids[0]}, 250, 0, 0, 0, 0)", Ct);
        }
        await services.MigrateLocalDatabaseAsync<ZananceDbContext>(Ct);
        var source = services.GetRequiredService<ICommercialWriteAccessSource>();
        var access = source.Capture(Path.GetFullPath(path));
        Assert.False(access.Enforced); Assert.Null(access.Context);
        var store = services.GetRequiredService<ZananceStore>();
        var accounts = await store.GetAccountsAsync(cancellationToken: Ct);
        var entries = await store.GetEntriesAsync(cancellationToken: Ct);
        Assert.Equal(ids.Order(), accounts.Select(a => a.Id).Order());
        Assert.Equal(750, LedgerCalculator.Balance(accounts.Single(a => a.Id == ids[0]), entries, new(2026, 12, 31)));
        await store.SaveAccountAsync(new Account { Name = "Explicit fifth test account", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1) }, Ct);
        Assert.Equal(5, (await store.GetAccountsAsync(cancellationToken: Ct)).Count);

        using var portable = new MemoryStream();
        await services.GetRequiredService<IBackupSource>().WriteAsync(portable, Ct); portable.Position = 0;
        var restoredPath = _directory.Combine("restored-over-quota.db");
        await using var restored = new ServiceCollection().AddZananceData(restoredPath).BuildServiceProvider();
        await restored.MigrateLocalDatabaseAsync<ZananceDbContext>(Ct);
        await restored.GetRequiredService<IBackupSource>().RestoreAsync(portable, Ct);
        var restoredAccess = restored.GetRequiredService<ICommercialWriteAccessSource>().Capture(Path.GetFullPath(restoredPath));
        Assert.False(restoredAccess.Enforced); Assert.Null(restoredAccess.Context);
        var recovered = restored.GetRequiredService<ZananceStore>();
        Assert.Equal(5, (await recovered.GetAccountsAsync(cancellationToken: Ct)).Count);
        Assert.Equal(250, Assert.Single(await recovered.GetEntriesAsync(cancellationToken: Ct)).Amount);
        await using (var db = await restored.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
        {
            Assert.False(await db.ResourceSelections.AnyAsync(Ct));
        }
    }
}
