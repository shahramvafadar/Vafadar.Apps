using Microsoft.Data.Sqlite;
using System.Security.Cryptography;

namespace Vafadar.Data;

/// <summary>Creates a captured new profile from an in-memory schema without a plaintext financial database on disk.</summary>
/// <remarks>A separate new-profile journal authorizes unselected empty-schema retry. Missing existing migration
/// state is never treated as new. Publication, profile catalogue commit and key lifetime belong to the host.</remarks>
public static class SqlCipherNewProfile
{
    /// <summary>Initializes an explicitly new profile or resumes its existing captured intent, then returns a verified key owner.</summary>
    public static async Task<SqlCipherProfileGeneration> OpenAsync<TContext>(LocalDatabaseLocation<TContext> location,
        SqliteDatabaseIdentityCatalogue identities, string legacyProfile, string originalPath, bool initializeNewProfile,
        IDatabaseGenerationKeyStore keys, IDatabaseGenerationFileDurability durability,
        Func<SqliteConnection, CancellationToken, Task> buildEmptySchema, DatabaseAccessGate.ExclusiveLease owner,
        Action validateAccess, int maximumBytes, string version, CancellationToken ct = default) where TContext : LocalDbContext
    {
        ArgumentNullException.ThrowIfNull(location); ArgumentNullException.ThrowIfNull(identities);
        ArgumentNullException.ThrowIfNull(keys); ArgumentNullException.ThrowIfNull(durability);
        ArgumentNullException.ThrowIfNull(buildEmptySchema); ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(validateAccess); ArgumentException.ThrowIfNullOrWhiteSpace(originalPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(version); ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumBytes);
        void Check() { ct.ThrowIfCancellationRequested(); location.Access.RequireDrainedOwner(owner); validateAccess(); }
        Check();
        if (!Path.IsPathFullyQualified(originalPath) || Path.GetExtension(originalPath) != ".db" || Path.GetDirectoryName(originalPath) != Path.GetDirectoryName(location.Path))
            throw new ArgumentException("New profile requires its captured private original identity path.");
        var identity = (await identities.ReadAsync(ct).ConfigureAwait(false)).Single(row => row.LegacyProfileId == legacyProfile);
        Check(); var captured = identity.InitialMigration; var binding = identity.InitialCandidateKey;
        var finalJournalPath = originalPath + ".generation.sqlite";
        var intentPath = originalPath + ".new-profile.sqlite";
        var candidate = originalPath + "." + binding.GenerationId.ToString("N") + ".cipher.db";
        var rollback = candidate + ".rollback.db";
        var history = new SqliteProfileRestoreHistory(originalPath + ".restore-history.sqlite", binding, Check);
        if (!File.Exists(finalJournalPath))
        {
            foreach (var suffix in new[] { "-journal", "-wal", "-shm" })
                if (File.Exists(finalJournalPath + suffix) || Directory.Exists(finalJournalPath + suffix))
                    throw new IOException("Interrupted existing journal cannot authorize a new profile.");
            foreach (var suffix in new[] { "", "-journal", "-wal", "-shm" })
                if (File.Exists(originalPath + suffix) || Directory.Exists(originalPath + suffix))
                    throw new IOException("An existing original cannot be initialized as a new empty profile.");
            var journal = new SqliteDatabaseGenerationJournal(intentPath, captured);
            if (!File.Exists(intentPath))
            {
                if (!initializeNewProfile) throw new IOException("Captured new-profile intent is missing.");
                foreach (var target in new[] { candidate, rollback })
                    foreach (var suffix in new[] { "", "-journal", "-wal", "-shm" })
                        if (File.Exists(target + suffix) || Directory.Exists(target + suffix)) throw new IOException("Unknown ciphertext cannot authorize a fresh empty profile.");
                await journal.CreateNewAsync(durability, ct).ConfigureAwait(false);
            }
            if (!File.Exists(originalPath + ".restore-history.sqlite"))
                await history.CreateInitialIntentAsync(captured, journal, durability, ct).ConfigureAwait(false);
            var ancestry = await history.ReadAsync(ct).ConfigureAwait(false);
            if (ancestry.Count != 1 || ancestry[0].Selected)
                throw new IOException("New-profile intent requires its original pending ancestry.");
            var state = await journal.ReadAsync(ct).ConfigureAwait(false); Check();
            async Task Advance()
            {
                var next = state.Advance(state.ProfileId, state.CandidateGeneration, state.Phase + 1);
                await journal.CompareAndCommitAsync(state, next, ct).ConfigureAwait(false);
                await durability.SynchronizeFileAsync(intentPath, ct).ConfigureAwait(false);
                await durability.SynchronizeDirectoryAsync(ct).ConfigureAwait(false);
                if (await journal.ReadAsync(ct).ConfigureAwait(false) != next) throw new IOException("New profile checkpoint readback differs.");
                state = next; Check();
            }
            if (state.Phase == DatabaseGenerationPhase.Original)
            {
                if (File.Exists(candidate) || File.Exists(rollback)) throw new IOException("New profile ciphertext preceded durable key preparation.");
                await keys.PrepareAsync(binding, ct).ConfigureAwait(false); Check(); await Advance().ConfigureAwait(false);
            }
            if (state.Phase == DatabaseGenerationPhase.KeyPrepared)
            {
                // Only this durable new-profile intent can reset an unpublished empty-schema attempt.
                foreach (var target in new[] { candidate, rollback })
                    foreach (var suffix in new[] { "", "-journal", "-wal", "-shm" })
                    {
                        Check(); var file = target + suffix;
                        if (Directory.Exists(file) || (File.Exists(file) && (File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0)) throw new IOException("New profile staging is redirected.");
                        File.Delete(file);
                    }
                await using var memory = new SqliteConnection("Data Source=:memory:;Pooling=False");
                await memory.OpenAsync(ct).ConfigureAwait(false);
                using (var command = memory.CreateCommand())
                {
                    command.CommandText = "PRAGMA cipher_version";
                    if ((Convert.ToString(await command.ExecuteScalarAsync(ct).ConfigureAwait(false)) ?? "").Split(' ')[0] != version)
                        throw new InvalidDataException("New profile requires the reviewed native cipher.");
                }
                await buildEmptySchema(memory, ct).ConfigureAwait(false); Check();
                using var portable = new MemoryStream();
                try
                {
                    await SqlCipherPortableSnapshot.WriteAsync(memory, portable, maximumBytes, Check, ct).ConfigureAwait(false);
                    portable.Position = 0;
                    await SqlCipherRestoreStaging.PrepareAsync(portable, candidate, binding, new PreparedKeys(keys), durability,
                        (_, _) => Task.CompletedTask, Check, maximumBytes, version, ct).ConfigureAwait(false);
                    portable.Position = 0;
                    await SqlCipherRestoreStaging.PrepareAsync(portable, rollback, binding, new PreparedKeys(keys), durability,
                        (_, _) => Task.CompletedTask, Check, maximumBytes, version, ct).ConfigureAwait(false);
                }
                finally { CryptographicOperations.ZeroMemory(portable.GetBuffer().AsSpan()); }
                await Advance().ConfigureAwait(false);
            }
            if (state.Phase is DatabaseGenerationPhase.CandidateVerified or DatabaseGenerationPhase.Selected)
            {
                var key = await keys.ReadAsync(binding, ct).ConfigureAwait(false);
                try
                {
                    if (key.Length != 32) throw new CryptographicException("New profile key is invalid.");
                    await using var created = await OpenVerifiedAsync(candidate, key, version, ct).ConfigureAwait(false);
                    await using var safety = await OpenVerifiedAsync(rollback, key, version, ct).ConfigureAwait(false);
                    await SqliteSnapshotVerifier.VerifyAsync(safety, created, ct).ConfigureAwait(false); Check();
                }
                finally { CryptographicOperations.ZeroMemory(key); }
                if (state.Phase == DatabaseGenerationPhase.CandidateVerified) await Advance().ConfigureAwait(false);
                await Advance().ConfigureAwait(false);
            }
            if (state.Phase != DatabaseGenerationPhase.Completed) throw new IOException("New profile initialization did not complete.");
            Check(); File.Move(intentPath, finalJournalPath, overwrite: false);
            await durability.SynchronizeDirectoryAsync(ct).ConfigureAwait(false); Check();
        }
        return await SqlCipherProfileGeneration.OpenCurrentAsync(location, identities, legacyProfile, originalPath, history,
            _ => throw new InvalidOperationException("New profile initializer cannot resolve later restored generations."),
            keys, durability, owner, validateAccess, maximumBytes, version, ct).ConfigureAwait(false);
    }

    private static async Task<SqliteConnection> OpenVerifiedAsync(string path, byte[] key, string version, CancellationToken ct)
    {
        if (!File.Exists(path) || (File.GetAttributes(path) & (FileAttributes.ReparsePoint | FileAttributes.Directory)) != 0)
            throw new IOException("Captured new profile is missing or redirected.");
        using (var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        { var header = new byte[16]; input.ReadExactly(header); if (header.AsSpan().SequenceEqual("SQLite format 3\0"u8)) throw new InvalidDataException("New profile must remain encrypted."); }
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        { DataSource = path, Password = Convert.ToBase64String(key), Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString());
        try
        {
            await connection.OpenAsync(ct).ConfigureAwait(false);
            using var command = connection.CreateCommand(); command.CommandText = "PRAGMA cipher_version";
            if ((Convert.ToString(await command.ExecuteScalarAsync(ct).ConfigureAwait(false)) ?? "").Split(' ')[0] != version)
                throw new InvalidDataException("New profile requires the reviewed native cipher.");
            return connection;
        }
        catch { await connection.DisposeAsync().ConfigureAwait(false); throw; }
    }

    private sealed class PreparedKeys(IDatabaseGenerationKeyStore inner) : IDatabaseGenerationKeyStore
    {
        /// <inheritdoc />
        public async Task PrepareAsync(DatabaseKeyBinding binding, CancellationToken ct)
        { var key = await inner.ReadAsync(binding, ct).ConfigureAwait(false); CryptographicOperations.ZeroMemory(key); }
        /// <inheritdoc />
        public Task<byte[]> ReadAsync(DatabaseKeyBinding binding, CancellationToken ct) => inner.ReadAsync(binding, ct);
    }
}
