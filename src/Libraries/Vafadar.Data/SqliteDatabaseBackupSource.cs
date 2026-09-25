using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vafadar.Backup;

namespace Vafadar.Data;

/// <summary>
/// Includes an app's SQLite database in backups.
/// </summary>
/// <remarks>
/// <para>
/// Backups use SQLite's online backup API, which produces a consistent snapshot even while the app is using the
/// database (including WAL mode). Copying the database file directly could capture a half-written state.
/// </para>
/// <para>
/// A restore first checks the snapshot with <c>PRAGMA integrity_check</c>, then copies it into the live database and
/// finally applies EF Core migrations, so a backup from an older app version is upgraded to the current schema.
/// </para>
/// </remarks>
public sealed class SqliteDatabaseBackupSource<TContext>(IDbContextFactory<TContext> contextFactory) : IBackupSource
    where TContext : DbContext
{
    /// <summary>The name of the database entry in backup packages.</summary>
    public const string EntryName = "database.sqlite";

    /// <inheritdoc />
    public string Name => EntryName;

    /// <inheritdoc />
    public async Task WriteAsync(Stream destination, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(destination);
        var snapshotPath = CreateTemporaryPath();

        try
        {
            await using (var context = await contextFactory.CreateDbContextAsync(cancellationToken))
            await using (var snapshot = OpenUnpooled(snapshotPath))
            {
                await context.Database.OpenConnectionAsync(cancellationToken);
                await snapshot.OpenAsync(cancellationToken);
                ((SqliteConnection)context.Database.GetDbConnection()).BackupDatabase(snapshot);
            }

            await using var file = new FileStream(snapshotPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
            await file.CopyToAsync(destination, cancellationToken);
        }
        finally
        {
            TryDelete(snapshotPath);
        }
    }

    /// <inheritdoc />
    public async Task RestoreAsync(Stream source, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        var snapshotPath = CreateTemporaryPath();

        try
        {
            await using (var file = new FileStream(snapshotPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            {
                await source.CopyToAsync(file, cancellationToken);
            }

            await using (var snapshot = OpenUnpooled(snapshotPath))
            {
                await snapshot.OpenAsync(cancellationToken);
                await EnsureIntegrityAsync(snapshot, cancellationToken);

                await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
                await context.Database.OpenConnectionAsync(cancellationToken);
                snapshot.BackupDatabase((SqliteConnection)context.Database.GetDbConnection());
            }

            SqliteConnection.ClearAllPools();

            await using var migrationContext = await contextFactory.CreateDbContextAsync(cancellationToken);
            await migrationContext.Database.MigrateAsync(cancellationToken);
        }
        finally
        {
            TryDelete(snapshotPath);
        }
    }

    private static async Task EnsureIntegrityAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA integrity_check;";
            var result = await command.ExecuteScalarAsync(cancellationToken) as string;

            if (!string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase))
            {
                throw new BackupException(BackupError.Corrupted, $"The database in the backup is damaged ({result}).");
            }
        }
        catch (SqliteException ex)
        {
            throw new BackupException(BackupError.Corrupted, "The backup does not contain a valid database.", ex);
        }
    }

    private static SqliteConnection OpenUnpooled(string path) =>
        new(new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString());

    private static string CreateTemporaryPath() =>
        Path.Combine(Path.GetTempPath(), $"vafadar-{Guid.NewGuid():N}.sqlite");

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // A leftover temporary file is harmless; the OS cleans the temp folder.
        }
    }
}
