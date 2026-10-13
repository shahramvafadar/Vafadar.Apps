using Microsoft.Data.Sqlite;
using System.Security.Cryptography;

namespace Vafadar.Data;

/// <summary>Imports a bounded portable image into a fresh device-keyed encrypted candidate before generation selection.</summary>
/// <remarks>Caller holds exclusive profile/access ownership, supplies actual schema upgrade and later durable selection.
/// This receiver never overwrites live data, publishes a selector or creates a plaintext temporary file.</remarks>
public static class SqlCipherRestoreStaging
{
    /// <summary>Validates complete portable content, prepares its new local key, verifies encrypted import, upgrades and reopens it.</summary>
    /// <remarks>The callback upgrades only this candidate with the actual application migrations. A failed candidate remains
    /// caller-owned/unselected; original data and generation selection are untouched.</remarks>
    public static async Task PrepareAsync(Stream portable, string candidatePath, DatabaseKeyBinding binding,
        IDatabaseGenerationKeyStore keys, IDatabaseGenerationFileDurability durability,
        Func<SqliteConnection, CancellationToken, Task> upgradeCandidate, Action validateAccess,
        int maximumBytes, string cipherVersion, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(portable); ArgumentNullException.ThrowIfNull(binding);
        ArgumentNullException.ThrowIfNull(keys); ArgumentNullException.ThrowIfNull(durability);
        ArgumentNullException.ThrowIfNull(upgradeCandidate); ArgumentNullException.ThrowIfNull(validateAccess);
        ArgumentException.ThrowIfNullOrWhiteSpace(candidatePath); ArgumentException.ThrowIfNullOrWhiteSpace(cipherVersion);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumBytes);
        if (!Path.IsPathFullyQualified(candidatePath)) throw new ArgumentException("Restore requires a captured absolute candidate path.");
        var path = Path.GetFullPath(candidatePath);
        validateAccess(); cancellationToken.ThrowIfCancellationRequested();
        if (!Directory.Exists(Path.GetDirectoryName(path)) || (File.GetAttributes(Path.GetDirectoryName(path)!) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("Trusted restore directory is missing or redirected.");
        foreach (var suffix in new[] { "", "-wal", "-shm", "-journal" })
            if (File.Exists(path + suffix) || Directory.Exists(path + suffix)) throw new IOException("Restore candidate already exists.");
        await using var source = await SqliteMemorySnapshot.OpenAsync(portable, maximumBytes, cancellationToken).ConfigureAwait(false);
        validateAccess(); cancellationToken.ThrowIfCancellationRequested();
        if (durability.AvailableBytes < checked((long)maximumBytes * 2 + 16 * 1024 * 1024))
            throw new IOException("Insufficient space for bounded encrypted restore staging.");
        await keys.PrepareAsync(binding, cancellationToken).ConfigureAwait(false);
        validateAccess(); cancellationToken.ThrowIfCancellationRequested();
        var key = await keys.ReadAsync(binding, cancellationToken).ConfigureAwait(false);
        try
        {
            validateAccess(); cancellationToken.ThrowIfCancellationRequested();
            if (key.Length != 32) throw new CryptographicException("Restore key is invalid.");
            await source.ExportEncryptedAsync(path, Convert.ToBase64String(key), cancellationToken).ConfigureAwait(false);
            validateAccess(); cancellationToken.ThrowIfCancellationRequested();
            await using (var candidate = await OpenAsync(path, key, cipherVersion, cancellationToken).ConfigureAwait(false))
            {
                // Verify all original rows/schema/header before migrations legitimately alter the restored format.
                await SqliteSnapshotVerifier.VerifyAsync(source.Connection, candidate, cancellationToken).ConfigureAwait(false);
                validateAccess(); cancellationToken.ThrowIfCancellationRequested();
                await upgradeCandidate(candidate, cancellationToken).ConfigureAwait(false);
                validateAccess(); cancellationToken.ThrowIfCancellationRequested();
            }
            await using (var reopened = await OpenAsync(path, key, cipherVersion, cancellationToken).ConfigureAwait(false))
            {
                using var check = reopened.CreateCommand(); check.CommandText = "PRAGMA integrity_check";
                if (!string.Equals(await check.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) as string, "ok", StringComparison.Ordinal))
                    throw new InvalidDataException("Upgraded restore integrity failed.");
                foreach (var command in new[] { "PRAGMA cipher_integrity_check", "PRAGMA foreign_key_check" })
                {
                    check.CommandText = command;
                    await using var reader = await check.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                    if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) throw new InvalidDataException("Upgraded restore integrity failed.");
                }
            }
            validateAccess(); cancellationToken.ThrowIfCancellationRequested();
            await durability.SynchronizeFileAsync(path, cancellationToken).ConfigureAwait(false);
            validateAccess(); cancellationToken.ThrowIfCancellationRequested();
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }

    private static async Task<SqliteConnection> OpenAsync(string path, byte[] key, string version, CancellationToken cancellationToken)
    {
        if (!File.Exists(path) || (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("Captured restore candidate is missing or redirected.");
        using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            var header = new byte[16]; file.ReadExactly(header);
            if (header.AsSpan().SequenceEqual("SQLite format 3\0"u8)) throw new InvalidDataException("Restore candidate must be encrypted.");
        }
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        { DataSource = path, Password = Convert.ToBase64String(key), Mode = SqliteOpenMode.ReadWrite, Pooling = false, ForeignKeys = true }.ToString());
        try
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            using var check = connection.CreateCommand(); check.CommandText = "PRAGMA cipher_version";
            if ((Convert.ToString(await check.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)) ?? "").Split(' ')[0] != version)
                throw new InvalidDataException("Restore requires reviewed cipher identity.");
            return connection;
        }
        catch { await connection.DisposeAsync().ConfigureAwait(false); throw; }
    }
}
