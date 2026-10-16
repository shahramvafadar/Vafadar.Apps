using Microsoft.Data.Sqlite;
using System.Security.Cryptography;

namespace Vafadar.Data;

/// <summary>Actual keyed snapshot, encrypted rollback, selection and exact cleanup effects for a captured generation migration.</summary>
/// <remarks>Caller holds the exclusive profile/access lease and closes all other database contexts/pools.
/// Native provider activation, profile catalogue, recovery UX and startup publication remain caller obligations.</remarks>
public sealed class SqlCipherGenerationMigrationSession : IDatabaseGenerationMigrationSession
{
    private readonly DatabaseGenerationCheckpoint identity;
    private readonly DatabaseKeyBinding binding;
    private readonly string original;
    private readonly string candidate;
    private readonly string rollback;
    private readonly SqliteDatabaseGenerationJournal journal;
    private readonly IDatabaseGenerationKeyStore keys;
    private readonly IDatabaseGenerationFileDurability durability;
    private readonly Action validate;
    private readonly int maximumBytes;
    private readonly string cipherVersion;

    /// <summary>Captures explicit original ownership and derives candidate/rollback names from the bound generation.</summary>
    public SqlCipherGenerationMigrationSession(DatabaseGenerationCheckpoint identity, Guid installationId,
        string originalPath, SqliteDatabaseGenerationJournal journal, IDatabaseGenerationKeyStore keys,
        IDatabaseGenerationFileDurability durability, Action validateContext, int maximumBytes, string cipherVersion)
    {
        ArgumentNullException.ThrowIfNull(identity); ArgumentNullException.ThrowIfNull(journal);
        ArgumentNullException.ThrowIfNull(keys); ArgumentNullException.ThrowIfNull(durability);
        ArgumentNullException.ThrowIfNull(validateContext); ArgumentException.ThrowIfNullOrWhiteSpace(originalPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(cipherVersion); ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumBytes);
        if (!Path.IsPathFullyQualified(originalPath) || Path.GetExtension(originalPath) != ".db")
            throw new ArgumentException("Capture one absolute owned profile database path.");
        this.identity = identity; binding = new(identity.ProfileId, installationId, identity.CandidateGeneration);
        original = Path.GetFullPath(originalPath);
        candidate = original + "." + identity.CandidateGeneration.ToString("N") + ".cipher.db";
        rollback = candidate + ".rollback.db";
        this.journal = journal; this.keys = keys; this.durability = durability; validate = validateContext;
        this.maximumBytes = maximumBytes; this.cipherVersion = cipherVersion;
        ValidateContext();
    }

    /// <summary>The exact encrypted path selected only after successful coordination and final keyed reopen.</summary>
    public string CandidatePath => candidate;
    /// <summary>The encrypted pre-migration rollback file, retained independently from future ledger writes.</summary>
    public string RollbackPath => rollback;
    /// <inheritdoc />
    public void ValidateContext()
    {
        validate();
        var directory = Path.GetDirectoryName(original)!;
        if (!Directory.Exists(directory) || (File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("Captured migration directory is missing or redirected.");
    }
    /// <inheritdoc />
    public Task<DatabaseGenerationCheckpoint> ReadCheckpointAsync(CancellationToken cancellationToken)
    { ValidateContext(); return journal.ReadAsync(cancellationToken); }

    /// <inheritdoc />
    public async Task PrepareKeyAsync(DatabaseGenerationCheckpoint checkpoint, CancellationToken cancellationToken)
    {
        await RequirePhaseAsync(checkpoint, DatabaseGenerationPhase.Original, cancellationToken).ConfigureAwait(false);
        // A candidate cannot precede durable KeyPrepared. Never create a fresh key for unknown existing ciphertext.
        if (File.Exists(candidate) || File.Exists(rollback)) throw new IOException("Candidate exists before key preparation.");
        await keys.PrepareAsync(binding, cancellationToken).ConfigureAwait(false);
        ValidateContext();
        var key = await ReadKeyAsync(cancellationToken).ConfigureAwait(false);
        CryptographicOperations.ZeroMemory(key);
    }

    /// <inheritdoc />
    public async Task CreateAndVerifyCandidateAsync(DatabaseGenerationCheckpoint checkpoint, CancellationToken cancellationToken)
    {
        await RequirePhaseAsync(checkpoint, DatabaseGenerationPhase.KeyPrepared, cancellationToken).ConfigureAwait(false);
        RequireFile(original);
        var sourceBytes = checked(new FileInfo(original).Length + (File.Exists(original + "-wal") ? new FileInfo(original + "-wal").Length : 0));
        if (sourceBytes > maximumBytes || durability.AvailableBytes < checked(sourceBytes * 3 + 16 * 1024 * 1024))
            throw new IOException("Insufficient bounded memory or disk space for encrypted migration and rollback.");
        await using var snapshot = await CaptureOriginalAsync(cancellationToken).ConfigureAwait(false);
        var key = await ReadKeyAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Only the journal-bound, not-yet-selected generation can be recreated after interrupted export.
            // The same protected DEK is reused; no financial relationship or key is guessed.
            foreach (var target in new[] { candidate, rollback })
            {
                if (File.Exists(target))
                {
                    try { await VerifyAgainstAsync(snapshot, target, key, cancellationToken).ConfigureAwait(false); continue; }
                    catch (Exception error) when (error is SqliteException or InvalidDataException or IOException)
                    {
                        await RequirePhaseAsync(checkpoint, DatabaseGenerationPhase.KeyPrepared, cancellationToken).ConfigureAwait(false);
                        DeleteOwnedGeneration(target);
                    }
                }
                await RequirePhaseAsync(checkpoint, DatabaseGenerationPhase.KeyPrepared, cancellationToken).ConfigureAwait(false);
                await snapshot.ExportEncryptedAsync(target, Convert.ToBase64String(key), cancellationToken).ConfigureAwait(false);
                await VerifyAgainstAsync(snapshot, target, key, cancellationToken).ConfigureAwait(false);
            }
            foreach (var target in new[] { candidate, rollback })
                await durability.SynchronizeFileAsync(target, cancellationToken).ConfigureAwait(false);
            ValidateContext();
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }

    /// <inheritdoc />
    public async Task ReopenCandidateAsync(DatabaseGenerationCheckpoint checkpoint, CancellationToken cancellationToken)
    {
        RequireIdentity(checkpoint); ValidateContext();
        if (checkpoint.Phase < DatabaseGenerationPhase.CandidateVerified || await journal.ReadAsync(cancellationToken).ConfigureAwait(false) != checkpoint)
            throw new InvalidOperationException("Candidate is not durably verified or selected.");
        var key = await ReadKeyAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // A completed candidate has hosted ordinary app writers. Native hot-journal rollback
            // after their process loss requires existing-only ReadWrite under drained ownership;
            // pre-selection candidates and independent rollback snapshots remain read-only.
            await using var opened = await OpenAsync(candidate, key, cancellationToken,
                recoverSelected: checkpoint.Phase == DatabaseGenerationPhase.Completed).ConfigureAwait(false);
            await CheckIntegrityAsync(opened, cancellationToken).ConfigureAwait(false);
            if (checkpoint.Phase == DatabaseGenerationPhase.CandidateVerified)
            {
                await using var snapshot = await CaptureOriginalAsync(cancellationToken).ConfigureAwait(false);
                await SqliteSnapshotVerifier.VerifyAsync(snapshot.Connection, opened, cancellationToken).ConfigureAwait(false);
                await VerifyAgainstAsync(snapshot, rollback, key, cancellationToken).ConfigureAwait(false);
            }
            else if (checkpoint.Phase == DatabaseGenerationPhase.Selected)
            {
                // Writers remain gated until Completed, so rollback must still contain the exact selected packet.
                await using var safety = await OpenAsync(rollback, key, cancellationToken).ConfigureAwait(false);
                await SqliteSnapshotVerifier.VerifyAsync(opened, safety, cancellationToken).ConfigureAwait(false);
            }
            ValidateContext();
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }

    /// <inheritdoc />
    public Task PersistCheckpointAsync(DatabaseGenerationCheckpoint expected, DatabaseGenerationCheckpoint next, CancellationToken cancellationToken)
    { RequireIdentity(expected); RequireIdentity(next); ValidateContext(); return journal.CompareAndCommitAsync(expected, next, cancellationToken); }

    /// <inheritdoc />
    public async Task CleanupOriginalAsync(DatabaseGenerationCheckpoint checkpoint, CancellationToken cancellationToken)
    {
        await RequirePhaseAsync(checkpoint, DatabaseGenerationPhase.Selected, cancellationToken).ConfigureAwait(false);
        // Recheck actual keyed candidate and encrypted rollback inside this destructive effect, not only in its caller.
        await ReopenCandidateAsync(checkpoint, cancellationToken).ConfigureAwait(false);
        foreach (var suffix in new[] { "", "-wal", "-shm", "-journal" })
        {
            cancellationToken.ThrowIfCancellationRequested(); ValidateContext();
            if (File.Exists(original + suffix)) { RequireFile(original + suffix); File.Delete(original + suffix); }
        }
        await durability.SynchronizeDirectoryAsync(cancellationToken).ConfigureAwait(false);
        ValidateContext();
    }

    private async Task<SqliteMemorySnapshot> CaptureOriginalAsync(CancellationToken cancellationToken)
    {
        await using var source = await OpenAsync(original, null, cancellationToken).ConfigureAwait(false);
        using var content = new MemoryStream();
        try
        {
            await SqlCipherPortableSnapshot.WriteAsync(source, content, maximumBytes, cancellationToken).ConfigureAwait(false);
            ValidateContext(); content.Position = 0;
            return await SqliteMemorySnapshot.OpenAsync(content, maximumBytes, cancellationToken).ConfigureAwait(false);
        }
        finally { CryptographicOperations.ZeroMemory(content.GetBuffer().AsSpan()); }
    }

    private async Task VerifyAgainstAsync(SqliteMemorySnapshot snapshot, string target, byte[] key, CancellationToken cancellationToken)
    {
        await using var opened = await OpenAsync(target, key, cancellationToken).ConfigureAwait(false);
        await SqliteSnapshotVerifier.VerifyAsync(snapshot.Connection, opened, cancellationToken).ConfigureAwait(false);
        ValidateContext();
    }

    private async Task<SqliteConnection> OpenAsync(string path, byte[]? key, CancellationToken cancellationToken, bool recoverSelected = false)
    {
        ValidateContext(); RequireFile(path);
        var header = new byte[16];
        using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) file.ReadExactly(header);
        if ((key is null) != header.AsSpan().SequenceEqual("SQLite format 3\0"u8))
            throw new InvalidDataException("Captured generation has the wrong plaintext/cipher format.");
        if (recoverSelected)
        {
            if (path != candidate || key is null) throw new InvalidOperationException("Only the captured completed candidate can recover native writer state.");
            foreach (var suffix in new[] { "-journal", "-wal", "-shm" })
                if (File.Exists(path + suffix)) RequireFile(path + suffix);
        }
        // D-196: SQLCipher ATTACH inherits source access flags: exporting into writable memory requires ReadWrite.
        // Main is read under the exporter's snapshot transaction; never ReadWriteCreate or ledger writes.
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        { DataSource = path, Password = key is null ? "" : Convert.ToBase64String(key),
            Mode = key is null || recoverSelected ? SqliteOpenMode.ReadWrite : SqliteOpenMode.ReadOnly, Pooling = false }.ToString());
        try
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            ValidateContext();
            if (key is not null)
            {
                // Opening with the retained key permits SQLite's own recovery, not application
                // mutation. Verification keeps its connection query-only after native open.
                using var readOnly = connection.CreateCommand(); readOnly.CommandText = "PRAGMA query_only = ON";
                await readOnly.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
            using var check = connection.CreateCommand(); check.CommandText = "PRAGMA cipher_version";
            if ((Convert.ToString(await check.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)) ?? "").Split(' ')[0] != cipherVersion)
                throw new InvalidDataException("Captured connection is not the reviewed cipher provider.");
            return connection;
        }
        catch { await connection.DisposeAsync().ConfigureAwait(false); throw; }
    }

    private static async Task CheckIntegrityAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand(); command.CommandText = "PRAGMA integrity_check";
        if (!string.Equals(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) as string, "ok", StringComparison.Ordinal))
            throw new InvalidDataException("Selected generation integrity failed.");
        foreach (var sql in new[] { "PRAGMA cipher_integrity_check", "PRAGMA foreign_key_check" })
        {
            command.CommandText = sql;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) throw new InvalidDataException("Selected generation integrity failed.");
        }
    }

    private async Task<byte[]> ReadKeyAsync(CancellationToken cancellationToken)
    {
        ValidateContext(); cancellationToken.ThrowIfCancellationRequested();
        var key = await keys.ReadAsync(binding, cancellationToken).ConfigureAwait(false);
        try { ValidateContext(); cancellationToken.ThrowIfCancellationRequested(); if (key.Length != 32) throw new CryptographicException("Captured generation key is invalid."); return key; }
        catch { CryptographicOperations.ZeroMemory(key); throw; }
    }

    private async Task RequirePhaseAsync(DatabaseGenerationCheckpoint checkpoint, DatabaseGenerationPhase phase, CancellationToken cancellationToken)
    {
        RequireIdentity(checkpoint); ValidateContext(); cancellationToken.ThrowIfCancellationRequested();
        if (checkpoint.Phase != phase || await journal.ReadAsync(cancellationToken).ConfigureAwait(false) != checkpoint)
            throw new InvalidOperationException("Migration effect does not match its durable checkpoint.");
        ValidateContext();
    }

    private void RequireIdentity(DatabaseGenerationCheckpoint checkpoint)
    {
        if (checkpoint.ProfileId != identity.ProfileId || checkpoint.OriginalGeneration != identity.OriginalGeneration || checkpoint.CandidateGeneration != identity.CandidateGeneration)
            throw new InvalidOperationException("Captured migration identity changed.");
    }

    private void DeleteOwnedGeneration(string path)
    {
        if (path != candidate && path != rollback) throw new IOException("Cannot remove an unrelated generation.");
        foreach (var suffix in new[] { "", "-wal", "-shm", "-journal" })
            if (File.Exists(path + suffix)) { ValidateContext(); RequireFile(path + suffix); File.Delete(path + suffix); }
    }

    private static void RequireFile(string path)
    {
        if (!File.Exists(path) || (File.GetAttributes(path) & (FileAttributes.ReparsePoint | FileAttributes.Directory)) != 0)
            throw new IOException("Owned migration file is missing or redirected.");
    }
}
