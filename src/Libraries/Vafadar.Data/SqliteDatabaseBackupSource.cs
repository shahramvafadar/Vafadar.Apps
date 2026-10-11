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
    public Task WriteAsync(Stream destination, CancellationToken cancellationToken) => WriteAsync(destination, null, cancellationToken);

    /// <summary>Writes a snapshot with an optional actual-context check before native copying and output.</summary>
    /// <remarks>The callback reads only cached authorization; it never moves the database or performs network work.</remarks>
    public async Task WriteAsync(Stream destination, Action<TContext>? verifyAccess, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(destination);
        var snapshotPath = CreateTemporaryPath();

        try
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            verifyAccess?.Invoke(context);
            await using (var snapshot = OpenUnpooled(snapshotPath))
            {
                await context.Database.OpenConnectionAsync(cancellationToken);
                await snapshot.OpenAsync(cancellationToken);
                verifyAccess?.Invoke(context);
                ((SqliteConnection)context.Database.GetDbConnection()).BackupDatabase(snapshot);
            }

            await using var file = new FileStream(snapshotPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
            verifyAccess?.Invoke(context);
            await file.CopyToAsync(destination, cancellationToken);
        }
        finally
        {
            TryDelete(snapshotPath);
        }
    }

    /// <inheritdoc />
    public Task RestoreAsync(Stream source, CancellationToken cancellationToken) => RestoreAsync(source, null, cancellationToken);

    /// <summary>Restores into the initial actual context, checking optional cached access before consuming/copying.</summary>
    /// <remarks>The same context migrates the destination; a profile change during input cannot redirect recovery.</remarks>
    public async Task RestoreAsync(Stream source, Action<TContext>? verifyAccess, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        var snapshotPath = CreateTemporaryPath();

        try
        {
            // D-124: bind the destination before reading the package. The current profile may move during that await.
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            verifyAccess?.Invoke(context);
            await using (var file = new FileStream(snapshotPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            {
                await source.CopyToAsync(file, cancellationToken);
            }

            await using (var snapshot = OpenUnpooled(snapshotPath))
            {
                await snapshot.OpenAsync(cancellationToken);
                await EnsureIntegrityAsync(snapshot, cancellationToken);

                await context.Database.OpenConnectionAsync(cancellationToken);
                verifyAccess?.Invoke(context);
                snapshot.BackupDatabase((SqliteConnection)context.Database.GetDbConnection());
            }

            await context.Database.CloseConnectionAsync();
            // D-137: retire the captured destination pool, even if the current profile moved during input.
            SqliteConnection.ClearPool((SqliteConnection)context.Database.GetDbConnection());

            await context.Database.MigrateAsync(cancellationToken);
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
        // The snapshot holds the user's data, so its journal files go as well.
        foreach (var file in new[] { path, path + "-journal", path + "-wal", path + "-shm" })
        {
            try
            {
                File.Delete(file);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // A leftover file in the app's temp folder is removed by the OS with the app's cache.
            }
        }
    }
}
