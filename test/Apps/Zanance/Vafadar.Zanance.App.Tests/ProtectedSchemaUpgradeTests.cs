using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Vafadar.Zanance.App.Startup;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.CompiledModel;
using Vafadar.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual original schema-upgrade callback on ordinary isolated SQLite; native protected publication is separately checked in the running app.</summary>
[Trait("AT", "AT-294")]
public sealed class ProtectedSchemaUpgradeTests
{
    /// <summary>Native adoption may reverse only its declared suffix on an independent memory clone for complete preservation proof.</summary>
    [Theory, InlineData(false), InlineData(true)]
    public async Task Memory_only_read_transport_reversal_preserves_every_original_schema_and_typed_row(bool withRevision)
    {
        SQLitePCL.Batteries_V2.Init();
        var ct = TestContext.Current.CancellationToken;
        await using var original = new SqliteConnection("Data Source=:memory:"); await original.OpenAsync(ct);
        string target;
        await using (var db = new ZananceDbContext(new DbContextOptionsBuilder<ZananceDbContext>().UseSqlite(original)
            .UseModel(ZananceDbContextModel.Instance).Options))
        {
            var migrations = db.Database.GetMigrations().ToArray();
            target = withRevision ? "20261024115108_HomeFinancialRevision" : migrations[^3];
            await db.GetService<IMigrator>().MigrateAsync(target, ct);
            var account = new Account { Name = "Retained transport control", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 25) };
            db.Accounts.Add(account);
            db.Entries.Add(new LedgerEntry { AccountId = account.Id, Amount = 456, Date = new(2026, 10, 25), Title = "Original complete row", Tags = ["retained"] });
            await db.SaveChangesAsync(ct);
        }
        await using var current = new SqliteConnection("Data Source=:memory:"); await current.OpenAsync(ct);
        original.BackupDatabase(current);
        await ProtectedProfileStorage.UpgradeAsync(current, ct);
        await using (var db = new ZananceDbContext(new DbContextOptionsBuilder<ZananceDbContext>().UseSqlite(current)
            .UseModel(ZananceDbContextModel.Instance).Options))
        {
            Assert.Equal("20261026104025_HomeLedgerDirtyBlockConflictGuards", (await db.Database.GetAppliedMigrationsAsync(ct)).Last());
            await using var read = await db.Database.BeginTransactionAsync(ct);
            var cached = await HomeLedgerReadBlocks.TryReadAsync(db, ct); Assert.NotNull(cached);
            Assert.Equal(456, Assert.Single(cached).Amount);
        }
        await using var normalized = new SqliteConnection("Data Source=:memory:"); await normalized.OpenAsync(ct);
        current.BackupDatabase(normalized);
        await using (var db = new ZananceDbContext(new DbContextOptionsBuilder<ZananceDbContext>().UseSqlite(normalized)
            .UseModel(ZananceDbContextModel.Instance).Options))
            await db.GetService<IMigrator>().MigrateAsync(target, ct);
        await SqliteSnapshotVerifier.VerifyAsync(original, normalized, ct);
        using var proof = current.CreateCommand(); proof.CommandText = "SELECT count(*) FROM HomeLedgerReadBlocks";
        Assert.Equal(1L, await proof.ExecuteScalarAsync(ct)); // Actual upgraded source was never reversed.
    }

    /// <summary>The actual callback upgrades an old supported application schema before new household journal queries can run.</summary>
    [Fact]
    public async Task Old_supported_schema_receives_pending_household_tables_and_preserves_its_original_rows()
    {
        using var fixture = new FlowFixture();
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var db = new ZananceDbContext(new DbContextOptionsBuilder<ZananceDbContext>().UseSqlite(connection).UseModel(ZananceDbContextModel.Instance).Options);
        var ct = TestContext.Current.CancellationToken;
        await db.GetService<IMigrator>().MigrateAsync(db.Database.GetMigrations().First(), ct);
        await db.Database.ExecuteSqlRawAsync("CREATE TABLE FictitiousUpgradeSentinel (Id INTEGER PRIMARY KEY, Value TEXT NOT NULL); INSERT INTO FictitiousUpgradeSentinel VALUES (1, 'original unchanged')", ct);
        // Write only original first-schema columns before the real callback upgrades the file.
        var accountId = Guid.CreateVersion7(); var entryId = Guid.CreateVersion7(); var settingsId = Guid.CreateVersion7();
        await db.Database.ExecuteSqlAsync($"INSERT INTO Settings (Id, ReportCurrencyCode, Mode, BudgetCalendar, WeekStart, ReminderDaysBefore, ReminderTime, NotificationsShowDetails, AppLockEnabled, OnboardingCompleted, CreatedAt, UpdatedAt) VALUES ({settingsId}, 'EUR', 0, 0, 1, 3, '09:00:00', 0, 0, 1, 0, 0)", ct);
        await db.Database.ExecuteSqlAsync($"INSERT INTO Accounts (Id, Name, Type, CurrencyCode, OpeningBalance, OpeningDate, OpeningBalanceKnown, IncludeInTotals, IsArchived, SortOrder, CreatedAt, UpdatedAt) VALUES ({accountId}, 'Fictitious original checking', 0, 'EUR', 1000, '2026-01-01', 1, 1, 0, 0, 0, 0)", ct);
        await db.Database.ExecuteSqlAsync($"INSERT INTO Entries (Id, Kind, Date, AccountId, Amount, Review, Source, CreatedAt, UpdatedAt) VALUES ({entryId}, {(int)EntryKind.Expense}, '2026-02-01', {accountId}, 250, 0, 0, 0, 0)", ct);
        await ProtectedProfileStorage.UpgradeAsync(connection, ct);
        Assert.Empty(await db.Database.GetPendingMigrationsAsync(ct));
        Assert.Equal(1L, await db.Database.SqlQueryRaw<long>("SELECT COUNT(*) AS \"Value\" FROM sqlite_schema WHERE type='table' AND name='HouseholdLocalOwnership'").SingleAsync(ct));
        Assert.Equal("original unchanged", await db.Database.SqlQueryRaw<string>("SELECT Value FROM FictitiousUpgradeSentinel WHERE Id=1").SingleAsync(ct));
        var account = await db.Accounts.AsNoTracking().SingleAsync(ct); var entry = await db.Entries.AsNoTracking().SingleAsync(ct);
        Assert.Equal(accountId, account.Id); Assert.Equal(entryId, entry.Id); Assert.Equal(accountId, entry.AccountId);
        Assert.Equal("EUR", account.CurrencyCode); Assert.Equal(250, entry.Amount);
        Assert.Equal(750, LedgerCalculator.Balance(account, [entry], new DateOnly(2026, 12, 31)));
        var settings = await db.Settings.AsNoTracking().SingleAsync(ct); Assert.Equal(settingsId, settings.Id); Assert.False(settings.ReviewReminderEnabled);

    }

    /// <summary>Unknown/future migration history is refused before trying to reapply a current schema or altering stored rows.</summary>
    [Fact]
    public async Task Unknown_history_is_refused_without_reinterpreting_it_as_an_empty_or_current_schema()
    {
        using var fixture = new FlowFixture();
        await using var connection = new SqliteConnection("Data Source=:memory:"); var ct = TestContext.Current.CancellationToken; await connection.OpenAsync(ct);
        await using var db = new ZananceDbContext(new DbContextOptionsBuilder<ZananceDbContext>().UseSqlite(connection).UseModel(ZananceDbContextModel.Instance).Options);
        await db.GetService<IMigrator>().MigrateAsync(db.Database.GetMigrations().First(), ct);
        await db.Database.ExecuteSqlRawAsync("UPDATE __EFMigrationsHistory SET MigrationId='99991231999999_UnknownSchema'", ct);
        var before = await db.Database.SqlQueryRaw<long>("SELECT COUNT(*) AS \"Value\" FROM sqlite_schema WHERE type='table'").SingleAsync(ct);
        await Assert.ThrowsAsync<InvalidDataException>(() => ProtectedProfileStorage.UpgradeAsync(connection, ct));
        Assert.Equal(before, await db.Database.SqlQueryRaw<long>("SELECT COUNT(*) AS \"Value\" FROM sqlite_schema WHERE type='table'").SingleAsync(ct));
        Assert.Equal("99991231999999_UnknownSchema", await db.Database.SqlQueryRaw<string>("SELECT MigrationId AS Value FROM __EFMigrationsHistory").SingleAsync(ct));
    }
    /// <summary>One startup upgrade derives one closed handle, retains caller ownership and preserves original rows on every supported outcome.</summary>
    [Theory]
    [InlineData("current", false), InlineData("old", false), InlineData("unknown", false)]
    [InlineData("current", true), InlineData("old", true), InlineData("unknown", true)]
    [Trait("AT", "AT-309")]
    public async Task Schema_review_uses_one_original_connection_and_restores_its_callers_open_state(string history, bool alreadyOpen)
    {
        using var fixture = new FlowFixture(); var ct = TestContext.Current.CancellationToken;
        await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        { DataSource = fixture.Directory.Combine("original-schema-review.db"), Pooling = false }.ToString());
        await connection.OpenAsync(ct);
        await using var db = new ZananceDbContext(new DbContextOptionsBuilder<ZananceDbContext>().UseSqlite(connection)
            .UseModel(ZananceDbContextModel.Instance).Options);
        var known = db.Database.GetMigrations().ToArray();
        await db.GetService<IMigrator>().MigrateAsync(history == "current" ? known.Last() : known.First(), ct);
        await db.Database.ExecuteSqlRawAsync("CREATE TABLE FictitiousUpgradeSentinel (Id INTEGER PRIMARY KEY, Value TEXT NOT NULL); INSERT INTO FictitiousUpgradeSentinel VALUES (1, 'original unchanged')", ct);
        if (history == "unknown") await db.Database.ExecuteSqlRawAsync("UPDATE __EFMigrationsHistory SET MigrationId='99991231999999_UnknownSchema'", ct);
        var beforeHistory = (await db.Database.GetAppliedMigrationsAsync(ct)).ToArray();
        var beforeTables = await db.Database.SqlQueryRaw<long>("SELECT COUNT(*) AS Value FROM sqlite_schema WHERE type='table'").SingleAsync(ct);
        if (!alreadyOpen) await connection.CloseAsync();
        var opens = 0;
        void StateChanged(object? sender, System.Data.StateChangeEventArgs args) { if (args.CurrentState == System.Data.ConnectionState.Open) opens++; }
        connection.StateChange += StateChanged;
        try
        {
            if (history == "unknown") await Assert.ThrowsAsync<InvalidDataException>(() => ProtectedProfileStorage.UpgradeAsync(connection, ct));
            else await ProtectedProfileStorage.UpgradeAsync(connection, ct);
        }
        finally { connection.StateChange -= StateChanged; }
        Assert.Equal(alreadyOpen ? System.Data.ConnectionState.Open : System.Data.ConnectionState.Closed, connection.State);
        Assert.Equal(alreadyOpen ? 0 : 1, opens);
        await connection.OpenAsync(ct);
        Assert.Equal("original unchanged", await db.Database.SqlQueryRaw<string>("SELECT Value FROM FictitiousUpgradeSentinel WHERE Id=1").SingleAsync(ct));
        Assert.Equal(history == "unknown" ? beforeHistory : known, (await db.Database.GetAppliedMigrationsAsync(ct)).ToArray());
        if (history == "unknown") Assert.Equal(beforeTables, await db.Database.SqlQueryRaw<long>("SELECT COUNT(*) AS Value FROM sqlite_schema WHERE type='table'").SingleAsync(ct));
    }

    /// <summary>Cancellation cannot leak a newly opened handle or close an original caller-owned one.</summary>
    [Theory, InlineData(false), InlineData(true)]
    [Trait("AT", "AT-309")]
    public async Task Cancelled_schema_review_preserves_original_connection_ownership(bool alreadyOpen)
    {
        using var fixture = new FlowFixture();
        await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        { DataSource = fixture.Directory.Combine("cancelled-schema-review.db"), Pooling = false }.ToString());
        if (alreadyOpen) await connection.OpenAsync(TestContext.Current.CancellationToken);
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ProtectedProfileStorage.UpgradeAsync(connection, cancelled.Token));
        Assert.Equal(alreadyOpen ? System.Data.ConnectionState.Open : System.Data.ConnectionState.Closed, connection.State);
    }
}
