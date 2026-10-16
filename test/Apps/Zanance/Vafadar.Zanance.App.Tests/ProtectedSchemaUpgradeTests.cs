using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Vafadar.Zanance.App.Startup;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.CompiledModel;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual original schema-upgrade callback on ordinary isolated SQLite; native protected publication is separately checked in the running app.</summary>
[Trait("AT", "AT-294")]
public sealed class ProtectedSchemaUpgradeTests
{
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
        await ProtectedProfileStorage.UpgradeAsync(connection, ct);
        Assert.Empty(await db.Database.GetPendingMigrationsAsync(ct));
        Assert.Equal(1L, await db.Database.SqlQueryRaw<long>("SELECT COUNT(*) AS \"Value\" FROM sqlite_schema WHERE type='table' AND name='HouseholdLocalOwnership'").SingleAsync(ct));
        Assert.Equal("original unchanged", await db.Database.SqlQueryRaw<string>("SELECT Value FROM FictitiousUpgradeSentinel WHERE Id=1").SingleAsync(ct));
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
}
