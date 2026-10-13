using System.Security.Cryptography;
using Microsoft.Data.Sqlite;

namespace Vafadar.Data;

/// <summary>Durably selects an already verified restored generation after preserving the current data in encrypted rollback.</summary>
/// <remarks>Caller holds exclusive profile/access ownership and retains both original and rollback. This does not
/// publish application contexts, restore other package sources, prune keys or complete the package transaction.</remarks>
public static class SqlCipherRestoreSelection
{
    /// <summary>Resumes actual journal selection; after Selected it reopens only candidate/rollback with their existing key.</summary>
    public static async Task<DatabaseGenerationCheckpoint> SelectAsync(DatabaseGenerationCheckpoint captured,
        SqliteDatabaseGenerationJournal journal, string candidatePath, DatabaseKeyBinding binding,
        SqliteConnection currentProtected, IDatabaseGenerationKeyStore keys, IDatabaseGenerationFileDurability durability,
        Action validateAccess, int maximumBytes, string cipherVersion, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(captured); ArgumentNullException.ThrowIfNull(journal);
        ArgumentNullException.ThrowIfNull(binding); ArgumentNullException.ThrowIfNull(currentProtected);
        ArgumentNullException.ThrowIfNull(keys); ArgumentNullException.ThrowIfNull(durability); ArgumentNullException.ThrowIfNull(validateAccess);
        ArgumentException.ThrowIfNullOrWhiteSpace(candidatePath); ArgumentException.ThrowIfNullOrWhiteSpace(cipherVersion);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumBytes);
        if (!Path.IsPathFullyQualified(candidatePath) || captured.ProfileId != binding.ProfileId || captured.CandidateGeneration != binding.GenerationId)
            throw new InvalidOperationException("Restore selection requires captured path and matching key generation.");
        var path = Path.GetFullPath(candidatePath); var rollbackPath = path + ".before-restore.cipher.db";
        void Check() { cancellationToken.ThrowIfCancellationRequested(); validateAccess(); }
        Check();
        var state = await journal.ReadAsync(cancellationToken).ConfigureAwait(false); Check();
        if (state.ProfileId != captured.ProfileId || state.OriginalGeneration != captured.OriginalGeneration || state.CandidateGeneration != captured.CandidateGeneration || state.Phase < captured.Phase)
            throw new InvalidOperationException("Restore selection identity or checkpoint changed.");
        // Never prepare a key here: staging has already produced it, including on Selected restart.
        var key = await keys.ReadAsync(binding, cancellationToken).ConfigureAwait(false);
        try
        {
            Check(); if (key.Length != 32) throw new CryptographicException("Restored selection key is invalid.");
            await using (var candidate = await OpenAsync(path, key, cipherVersion, cancellationToken).ConfigureAwait(false))
                await IntegrityAsync(candidate, cancellationToken).ConfigureAwait(false);
            Check();
            if (state.Phase is DatabaseGenerationPhase.Selected or DatabaseGenerationPhase.Completed)
            {
                await using var rollback = await OpenAsync(rollbackPath, key, cipherVersion, cancellationToken).ConfigureAwait(false);
                await IntegrityAsync(rollback, cancellationToken).ConfigureAwait(false); Check();
                return state;
            }
            // D-200: use captured configured identity, not SQLite's open-time native filename alias.
            var currentOptions = new SqliteConnectionStringBuilder(currentProtected.ConnectionString);
            if (currentProtected.State != System.Data.ConnectionState.Open ||
                string.IsNullOrEmpty(currentOptions.Password) ||
                Path.GetFullPath(currentOptions.DataSource) == path || Path.GetDirectoryName(Path.GetFullPath(currentOptions.DataSource)) != Path.GetDirectoryName(path))
                throw new InvalidOperationException("Restore rollback requires the captured open private current generation.");
            ProtectedSqliteAccess.Require(currentProtected);
            async Task Advance()
            {
                Check(); var next = state.Advance(state.ProfileId, state.CandidateGeneration, state.Phase + 1);
                await journal.CompareAndCommitAsync(state, next, cancellationToken).ConfigureAwait(false); Check();
                if (await journal.ReadAsync(cancellationToken).ConfigureAwait(false) != next) throw new IOException("Restore selection readback differs.");
                state = next; Check();
            }
            if (state.Phase == DatabaseGenerationPhase.Original) await Advance().ConfigureAwait(false);
            using var portable = new MemoryStream();
            try
            {
                void CheckCurrent() { Check(); ProtectedSqliteAccess.Require(currentProtected); }
                await SqlCipherPortableSnapshot.WriteAsync(currentProtected, portable, maximumBytes, CheckCurrent, cancellationToken).ConfigureAwait(false);
                portable.Position = 0;
                await using var original = await SqliteMemorySnapshot.OpenAsync(portable, maximumBytes, cancellationToken).ConfigureAwait(false);
                CheckCurrent();
                if (!File.Exists(rollbackPath))
                {
                    if (state.Phase != DatabaseGenerationPhase.KeyPrepared) throw new IOException("Verified restore rollback is missing.");
                    await original.ExportEncryptedAsync(rollbackPath, Convert.ToBase64String(key), cancellationToken).ConfigureAwait(false);
                }
                await using (var rollback = await OpenAsync(rollbackPath, key, cipherVersion, cancellationToken).ConfigureAwait(false))
                    await SqliteSnapshotVerifier.VerifyAsync(original.Connection, rollback, cancellationToken).ConfigureAwait(false);
                CheckCurrent();
                await durability.SynchronizeFileAsync(rollbackPath, cancellationToken).ConfigureAwait(false);
                await durability.SynchronizeFileAsync(path, cancellationToken).ConfigureAwait(false); CheckCurrent();
                if (state.Phase == DatabaseGenerationPhase.KeyPrepared) await Advance().ConfigureAwait(false);
                if (state.Phase != DatabaseGenerationPhase.CandidateVerified) throw new InvalidOperationException("Unsupported restore selection phase.");
                await using (var candidate = await OpenAsync(path, key, cipherVersion, cancellationToken).ConfigureAwait(false))
                    await IntegrityAsync(candidate, cancellationToken).ConfigureAwait(false);
                CheckCurrent(); await Advance().ConfigureAwait(false);
                await using (var selected = await OpenAsync(path, key, cipherVersion, cancellationToken).ConfigureAwait(false))
                    await IntegrityAsync(selected, cancellationToken).ConfigureAwait(false);
                Check(); return state;
            }
            finally { CryptographicOperations.ZeroMemory(portable.GetBuffer().AsSpan()); }
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }

    private static async Task<SqliteConnection> OpenAsync(string path, byte[] key, string version, CancellationToken ct)
    {
        if (!File.Exists(path) || (File.GetAttributes(path) & (FileAttributes.ReparsePoint | FileAttributes.Directory)) != 0)
            throw new IOException("Captured restore generation is missing or redirected.");
        using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        { var header = new byte[16]; file.ReadExactly(header); if (header.AsSpan().SequenceEqual("SQLite format 3\0"u8)) throw new InvalidDataException("Restore generations must remain encrypted."); }
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        { DataSource = path, Password = Convert.ToBase64String(key), Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString());
        try
        {
            await connection.OpenAsync(ct).ConfigureAwait(false);
            using var check = connection.CreateCommand(); check.CommandText = "PRAGMA cipher_version";
            if ((Convert.ToString(await check.ExecuteScalarAsync(ct).ConfigureAwait(false)) ?? "").Split(' ')[0] != version)
                throw new InvalidDataException("Restore selection requires reviewed cipher identity.");
            return connection;
        }
        catch { await connection.DisposeAsync().ConfigureAwait(false); throw; }
    }

    private static async Task IntegrityAsync(SqliteConnection connection, CancellationToken ct)
    {
        using var check = connection.CreateCommand(); check.CommandText = "PRAGMA integrity_check";
        if (!string.Equals(await check.ExecuteScalarAsync(ct).ConfigureAwait(false) as string, "ok", StringComparison.Ordinal)) throw new InvalidDataException("Restore generation integrity failed.");
        foreach (var sql in new[] { "PRAGMA cipher_integrity_check", "PRAGMA foreign_key_check" })
        { check.CommandText = sql; await using var reader = await check.ExecuteReaderAsync(ct).ConfigureAwait(false); if (await reader.ReadAsync(ct).ConfigureAwait(false)) throw new InvalidDataException("Restore generation integrity failed."); }
    }
}
