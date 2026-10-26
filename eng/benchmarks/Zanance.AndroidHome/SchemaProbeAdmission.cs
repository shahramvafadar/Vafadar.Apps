using System.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Vafadar.Data;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.CompiledModel;

namespace Vafadar.Zanance.Performance;

/// <summary>Prevents an unchanged-source diagnostic from accidentally applying pending migrations.</summary>
internal static class SchemaProbeAdmission
{
    /// <summary>Requires the exact current history without creating a source or changing borrowed handle ownership.</summary>
    internal static async Task RequireSettledAsync(SqliteConnection connection, CancellationToken ct)
    {
        var close = connection.State == ConnectionState.Closed;
        if (close && !File.Exists(new SqliteConnectionStringBuilder(connection.ConnectionString).DataSource))
            throw new InvalidDataException("An existing diagnostic source is required.");
        try
        {
            ct.ThrowIfCancellationRequested();
            if (close) await connection.OpenAsync(ct).ConfigureAwait(false);
            await using var db = new ZananceDbContext(new DbContextOptionsBuilder<ZananceDbContext>()
                .UseSqlite(connection, contextOwnsConnection: false).UseModel(ZananceDbContextModel.Instance).Options);
            var applied = (await db.Database.GetAppliedMigrationsAsync(ct).ConfigureAwait(false)).ToArray();
            if (!applied.SequenceEqual(db.Database.GetMigrations(), StringComparer.Ordinal))
                throw new InvalidDataException("An unchanged-source probe refuses pending or unknown migrations; use an explicit owned copy.");
        }
        finally
        {
            if (close) await connection.CloseAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Repairs only declared diagnostic bookkeeping after an authenticated original control proves the complete
    /// expected result in disposable memory. Callers must separately bind ownership, failure and source identity.
    /// </summary>
    internal static async Task<ReadBlockSchemaRetirement.Result> RepairAsync(ZananceDbContext db,
        SqliteConnection before, SqliteConnection originalControl, string requestPath, string comparison,
        string scope, Action demand, Func<string, string, CancellationToken, Task> saveNewDurable, CancellationToken ct)
    {
        demand();
        var source = (SqliteConnection)db.Database.GetDbConnection();
        await SqliteSnapshotVerifier.VerifyAsync(before, source, ct).ConfigureAwait(false);
        await using var expected = new SqliteConnection("Data Source=:memory:;Pooling=False");
        await expected.OpenAsync(ct).ConfigureAwait(false);
        before.BackupDatabase(expected);
        await using (var model = new ZananceDbContext(new DbContextOptionsBuilder<ZananceDbContext>()
            .UseSqlite(expected, contextOwnsConnection: false).UseModel(ZananceDbContextModel.Instance).Options))
            await model.GetService<IMigrator>().MigrateAsync(ReadBlockSchemaRetirement.Target, ct).ConfigureAwait(false);
        // D-558: never infer safety from a migration name or common-row digest alone.
        await SqliteSnapshotVerifier.VerifyAsync(originalControl, expected, ct).ConfigureAwait(false);
        demand();
        var result = await ReadBlockSchemaRetirement.RetireAsync(db, before, requestPath, comparison, scope,
            demand, saveNewDurable, ct).ConfigureAwait(false);
        await SqliteSnapshotVerifier.VerifyAsync(originalControl, source, ct).ConfigureAwait(false);
        demand();
        return result;
    }
}
