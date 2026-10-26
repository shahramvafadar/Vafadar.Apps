using System.Data;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Zanance.Data.CompiledModel;
using Vafadar.Zanance.Performance;

namespace Vafadar.Zanance.Data.Tests;

public sealed partial class HomeLedgerBlockStoreTests
{
    [Fact]
    public async Task Schema_probe_refuses_pending_migrations_without_changing_original_bytes()
    {
        await using var seeded = await SeedAsync();
        await using var original = await CopyAsync(seeded);
        await NormalizeProbeControlAsync(original);
        var path = directory.Combine("legacy-probe.db");
        await using (var writer = new SqliteConnection($"Data Source={path};Pooling=False"))
        {
            await writer.OpenAsync(Ct); original.BackupDatabase(writer);
        }
        var bytes = SHA256.HashData(await File.ReadAllBytesAsync(path, Ct));
        await using var source = new SqliteConnection($"Data Source={path};Mode=ReadOnly;Pooling=False");
        await Assert.ThrowsAsync<InvalidDataException>(() => SchemaProbeAdmission.RequireSettledAsync(source, Ct));
        Assert.Equal(ConnectionState.Closed, source.State);
        Assert.Equal(bytes, SHA256.HashData(await File.ReadAllBytesAsync(path, Ct)));
        await source.OpenAsync(Ct);
        await SqliteSnapshotVerifier.VerifyAsync(original, source, Ct);
    }

    [Fact]
    public async Task Schema_probe_preserves_borrowed_current_handle_and_all_original_rows()
    {
        await using var db = await SeedAsync();
        await using var before = await CopyAsync(db);
        var source = (SqliteConnection)db.Database.GetDbConnection();
        Assert.Equal(ConnectionState.Open, source.State);
        await SchemaProbeAdmission.RequireSettledAsync(source, Ct);
        Assert.Equal(ConnectionState.Open, source.State);
        await SqliteSnapshotVerifier.VerifyAsync(before, source, Ct);
    }

    [Fact]
    public async Task Schema_probe_never_creates_a_missing_source()
    {
        var path = directory.Combine("missing-probe.db");
        await using var connection = new SqliteConnection($"Data Source={path};Pooling=False");
        await Assert.ThrowsAsync<InvalidDataException>(() => SchemaProbeAdmission.RequireSettledAsync(connection, Ct));
        Assert.False(File.Exists(path));
        Assert.Equal(ConnectionState.Closed, connection.State);
    }

    [Fact]
    public async Task Schema_probe_cancelled_before_open_keeps_original_handle_closed()
    {
        await using var db = await SeedAsync();
        var source = (SqliteConnection)db.Database.GetDbConnection();
        Assert.Equal(ConnectionState.Closed, source.State);
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => SchemaProbeAdmission.RequireSettledAsync(source, cancelled.Token));
        Assert.Equal(ConnectionState.Closed, source.State);
        Assert.Equal(2, await db.Entries.CountAsync(Ct));
    }

    [Fact]
    public async Task Schema_probe_repair_requires_the_authenticated_full_original_control_before_writing()
    {
        var seeded = await SeedAsync(); await seeded.DisposeAsync();
        var services = provider!;
        var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        using var owner = location.Access.AcquireExclusive();
        await using var db = await services.GetRequiredService<ILocalDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(owner, Ct);
        await using var before = await CopyAsync(db);
        await using var wrong = new SqliteConnection("Data Source=:memory:;Pooling=False");
        await wrong.OpenAsync(Ct); before.BackupDatabase(wrong);
        await NormalizeProbeControlAsync(wrong);
        using (var edit = wrong.CreateCommand())
        {
            edit.CommandText = "UPDATE Entries SET Note='different authenticated control'";
            await edit.ExecuteNonQueryAsync(Ct);
        }
        var request = directory.Combine("refused-probe-repair.json");
        await Assert.ThrowsAsync<InvalidDataException>(() => SchemaProbeAdmission.RepairAsync(db, before, wrong,
            request, Guid.NewGuid().ToString("N"), "exact-owned-probe", owner.Validate, SaveRequestAsync, Ct));
        Assert.False(File.Exists(request));
        Assert.Equal(ReadBlockSchemaRetirement.Current, (await db.Database.GetAppliedMigrationsAsync(Ct)).Last());
        await SqliteSnapshotVerifier.VerifyAsync(before, (SqliteConnection)db.Database.GetDbConnection(), Ct);
    }

    [Fact]
    public async Task Schema_probe_repair_restores_every_original_schema_header_and_typed_row()
    {
        var seeded = await SeedAsync(); await seeded.DisposeAsync();
        var services = provider!;
        var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        using var owner = location.Access.AcquireExclusive();
        await using var db = await services.GetRequiredService<ILocalDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(owner, Ct);
        await using var before = await CopyAsync(db);
        await using var control = new SqliteConnection("Data Source=:memory:;Pooling=False");
        await control.OpenAsync(Ct); before.BackupDatabase(control);
        await NormalizeProbeControlAsync(control);
        var result = await SchemaProbeAdmission.RepairAsync(db, before, control, directory.Combine("owned-probe-repair.json"),
            Guid.NewGuid().ToString("N"), "exact-owned-probe", owner.Validate, SaveRequestAsync, Ct);
        Assert.True(result.CompleteOriginalSourceVerified);
        Assert.Equal(0, result.FinancialCommands);
        Assert.Equal(ReadBlockSchemaRetirement.Target, (await db.Database.GetAppliedMigrationsAsync(Ct)).Last());
        await SqliteSnapshotVerifier.VerifyAsync(control, (SqliteConnection)db.Database.GetDbConnection(), Ct);
        Assert.Equal(2, await db.Entries.CountAsync(Ct));
    }

    /// <summary>Builds an independent original control in memory; never changes the live test source.</summary>
    private static async Task NormalizeProbeControlAsync(SqliteConnection connection)
    {
        await using var model = new ZananceDbContext(new DbContextOptionsBuilder<ZananceDbContext>()
            .UseSqlite(connection, contextOwnsConnection: false).UseModel(ZananceDbContextModel.Instance).Options);
        await model.GetService<IMigrator>().MigrateAsync(ReadBlockSchemaRetirement.Target, Ct);
    }
}
