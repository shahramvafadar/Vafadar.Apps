namespace Vafadar.Data;

/// <summary>Prepares every captured existing local profile before the host admits ordinary database work.</summary>
/// <remarks>The host holds its real application maintenance owner and covers UI. This does not create missing
/// financial databases, adopt a native provider, publish contexts or delete retained encrypted rollback/key files.</remarks>
public static class SqlCipherProfileBootstrap
{
    /// <summary>Creates/resumes explicit first-install metadata or reads existing metadata, then verifies all captured profiles.</summary>
    public static async Task PrepareAsync<TContext>(LocalDatabaseLocation<TContext> location,
        DatabaseAccessGate.ExclusiveLease owner, string identityPath, IReadOnlyDictionary<string, string> originalPaths,
        bool initializeInstallation, IDatabaseGenerationKeyStore keys, IDatabaseGenerationFileDurability durability,
        Func<string, ProfileRestoreReference, SqlCipherPackageRestoreReceiver> createReceiver, Action validateAccess,
        int maximumBytes, string cipherVersion, CancellationToken ct = default) where TContext : LocalDbContext
    {
        ArgumentNullException.ThrowIfNull(location); ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(originalPaths); ArgumentNullException.ThrowIfNull(keys);
        ArgumentNullException.ThrowIfNull(durability); ArgumentNullException.ThrowIfNull(createReceiver);
        ArgumentNullException.ThrowIfNull(validateAccess); ArgumentException.ThrowIfNullOrWhiteSpace(identityPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(cipherVersion); ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumBytes);
        void Validate() { ct.ThrowIfCancellationRequested(); location.Access.RequireDrainedOwner(owner); validateAccess(); }
        Validate();
        if (!Path.IsPathFullyQualified(identityPath) || !originalPaths.ContainsKey(""))
            throw new ArgumentException("Bootstrap requires an absolute identity path and the captured main profile.");
        var directory = Path.GetDirectoryName(Path.GetFullPath(identityPath));
        var captured = originalPaths.ToDictionary(row => row.Key, row => Path.GetFullPath(row.Value), StringComparer.Ordinal);
        if (Path.GetDirectoryName(location.Path) != directory || originalPaths.Values.Any(path => !Path.IsPathFullyQualified(path)) || captured.Values.Distinct(StringComparer.Ordinal).Count() != captured.Count ||
            captured.Values.Any(path => Path.GetDirectoryName(path) != directory || Path.GetExtension(path) != ".db"))
            throw new ArgumentException("Each captured profile must own a distinct database in the private metadata directory.");
        var identities = new SqliteDatabaseIdentityCatalogue(identityPath, Validate);
        if (initializeInstallation && !File.Exists(identityPath))
        {
            if (!File.Exists(identityPath + ".creating"))
            {
                // Explicit first setup is never inferred from a missing catalogue after encryption began.
                // Retained key envelopes, migration/history records or ciphertext require reconciliation.
                if (Directory.EnumerateFiles(directory!, "*.key").Any())
                    throw new IOException("Existing key envelopes require their original installation metadata.");
                foreach (var original in captured.Values)
                {
                    Validate();
                    if (!File.Exists(original) || (File.GetAttributes(original) & FileAttributes.ReparsePoint) != 0 || File.Exists(original + ".generation.sqlite") || File.Exists(original + ".generation.sqlite.creating") ||
                        File.Exists(original + ".restore-history.sqlite") || File.Exists(original + ".restore-history.sqlite.creating") ||
                        Directory.EnumerateFiles(directory!, Path.GetFileName(original) + ".*.cipher.db*").Any())
                        throw new IOException("Existing or missing financial generation cannot initialize a new installation identity.");
                    using var input = new FileStream(original, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    var header = new byte[16]; input.ReadExactly(header);
                    if (!header.AsSpan().SequenceEqual("SQLite format 3\0"u8)) throw new InvalidDataException("Fresh installation requires captured plaintext originals.");
                }
            }
            await identities.CreateNewAsync(captured.Keys.ToArray(), durability, ct).ConfigureAwait(false);
        }
        var rows = await identities.ReadAsync(ct).ConfigureAwait(false); Validate();
        if (!rows.Select(row => row.LegacyProfileId).Order(StringComparer.Ordinal).SequenceEqual(captured.Keys.Order(StringComparer.Ordinal)))
            throw new InvalidOperationException("Captured profile catalogue differs from durable identity metadata.");
        foreach (var identity in rows)
        {
            Validate(); var original = captured[identity.LegacyProfileId];
            // Disposed factory contexts can leave pooled native handles open on Windows. The complete
            // host lease has drained those contexts; retire only this original before destructive cleanup.
            LocalDatabasePools.ClearFile(original); Validate();
            // The real host owner excludes all normal contexts. This private unpublished location only
            // supplies the existing migration API's local ownership; it never becomes an application factory.
            var candidateLocation = new LocalDatabaseLocation<TContext>(original);
            using var candidateOwner = await candidateLocation.Access.AcquireExclusiveAsync(ct).ConfigureAwait(false);
            void CheckProfile() { Validate(); candidateOwner.Validate(); }
            var journal = new SqliteDatabaseGenerationJournal(original + ".generation.sqlite", identity.InitialMigration);
            if (!File.Exists(original + ".generation.sqlite"))
            {
                // Existing ciphertext cannot be initialized as a fresh original. Staged journal publication
                // resumes only exact Original state; failed/incomplete staging is never overwritten.
                await SqlCipherProfileGeneration.InitializeJournalAsync(candidateLocation, identities, identity.LegacyProfileId,
                    original, candidateOwner, CheckProfile, durability, ct).ConfigureAwait(false);
            }
            var historyPath = original + ".restore-history.sqlite";
            var history = new SqliteProfileRestoreHistory(historyPath, identity.InitialCandidateKey, CheckProfile);
            if (!File.Exists(historyPath)) await history.CreateInitialIntentAsync(identity.InitialMigration, journal, durability, ct).ConfigureAwait(false);
            using var generation = await SqlCipherProfileGeneration.OpenCurrentAsync(candidateLocation, identities, identity.LegacyProfileId,
                original, history, reference => createReceiver(identity.LegacyProfileId, reference), keys, durability,
                candidateOwner, CheckProfile, maximumBytes, cipherVersion, ct).ConfigureAwait(false);
            // Verification owns a temporary retained read only. The host later publishes through its session,
            // using another existing key read; no profile becomes visible midway through the all-profile pass.
            Validate();
        }
        Validate();
    }
}
