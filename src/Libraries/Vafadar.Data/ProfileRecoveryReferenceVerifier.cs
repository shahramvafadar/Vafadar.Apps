using Microsoft.Data.Sqlite;
using System.Security.Cryptography;
using Vafadar.Backup.Security;

namespace Vafadar.Data;

/// <summary>Authenticates retained native database, preference and safety references before recovered-key enrollment.</summary>
/// <remarks>The caller owns drained application access and captured private roots. Unknown database names,
/// absent selected generations and unreferenced keys require reconciliation, never replacement or guessed ownership.</remarks>
public sealed class ProfileRecoveryReferenceVerifier(SqliteDatabaseIdentityCatalogue identities,
    IProfileRetirementReferenceFiles files, IDatabaseGenerationFileDurability durability,
    IReadOnlyList<string> preferenceKeys, string cipherVersion, int maximumDatabaseBytes, Action validateAccess)
{
    /// <summary>Checks all existing references for one captured binding with its owned recovered key, without writes or publication.</summary>
    public async Task VerifyAsync(DatabaseKeyBinding binding, string originalPath, ReadOnlyMemory<byte> key, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(binding);
        if (!Path.IsPathFullyQualified(originalPath) || Path.GetExtension(originalPath) != ".db" || key.Length != 32 || maximumDatabaseBytes <= 0)
            throw new IOException("Recovery requires a captured original and bounded existing data key.");
        void Check() { ct.ThrowIfCancellationRequested(); validateAccess(); }
        Check(); var rows = await identities.ReadAsync(ct).ConfigureAwait(false);
        var identity = rows.Single(row => row.ProfileId == binding.ProfileId && row.InstallationId == binding.InstallationId);
        var original = Path.GetFileName(originalPath); var historyName = original + ".restore-history.sqlite";
        var historyFile = await files.CaptureAsync("database", historyName, ct).ConfigureAwait(false);
        var history = new SqliteProfileRestoreHistory(originalPath + ".restore-history.sqlite", identity.InitialCandidateKey, Check);
        var references = await history.ReadAsync(ct).ConfigureAwait(false); Check();
        var known = new HashSet<string>(StringComparer.Ordinal);
        Add(original); Add(original + ".generation.sqlite"); Add(original + ".new-profile.sqlite"); Add(historyName);
        foreach (var reference in references)
        {
            var candidate = original + "." + reference.CandidateGeneration.ToString("N") + ".cipher.db";
            Add(candidate); Add(candidate + ".rollback.db"); Add(candidate + ".before-restore.cipher.db");
            Add(candidate + ".preferences.cipher.db"); Add(candidate + ".restore.sqlite");
        }
        var names = (await files.ListNamesAsync("database", ct).ConfigureAwait(false)).Order(StringComparer.Ordinal).ToArray();
        if (names.Any(name => name.StartsWith(original + ".", StringComparison.Ordinal) && !known.Contains(name)))
            throw new IOException("Unknown recovery database namespace requires reconciliation.");
        var captures = new List<ProfileRetirementArtifact>(); var authenticated = false;
        var ownReference = references.SingleOrDefault(row => row.CandidateGeneration == binding.GenerationId);
        if (ownReference is not null)
        {
            var candidateName = original + "." + binding.GenerationId.ToString("N") + ".cipher.db";
            if (ownReference.Sequence != 0)
            {
                var journalName = candidateName + ".restore.sqlite";
                captures.Add(await files.CaptureAsync("database", journalName, ct).ConfigureAwait(false));
                var capturedIntent = new DatabaseGenerationCheckpoint(binding.ProfileId, ownReference.OriginalGeneration, ownReference.CandidateGeneration);
                var state = await new SqliteDatabaseGenerationJournal(Path.Combine(Path.GetDirectoryName(originalPath)!, journalName), capturedIntent).ReadAsync(ct).ConfigureAwait(false);
                if (ownReference.Selected && !state.CandidateSelected) throw new IOException("Recovery history differs from its selected journal.");
                if (state.CandidateSelected && (!names.Contains(candidateName + ".before-restore.cipher.db", StringComparer.Ordinal)
                    || !names.Contains(candidateName + ".preferences.cipher.db", StringComparer.Ordinal)))
                    throw new IOException("Selected restore recovery references are missing.");
            }
            if (ownReference.Selected && !names.Contains(candidateName, StringComparer.Ordinal))
                throw new IOException("Selected recovery generation is missing.");
            foreach (var suffix in new[] { "", ".rollback.db", ".before-restore.cipher.db", ".preferences.cipher.db" })
            {
                var name = candidateName + suffix;
                if (!names.Contains(name, StringComparer.Ordinal)) continue;
                Check(); var captured = await files.CaptureAsync("database", name, ct).ConfigureAwait(false);
                if (captured.Length is <= 0 || captured.Length > maximumDatabaseBytes) throw new IOException("Recovery database exceeds its bound.");
                var path = Path.Combine(Path.GetDirectoryName(originalPath)!, name);
                await VerifyDatabaseAsync(path, key, ct).ConfigureAwait(false);
                if (suffix == ".preferences.cipher.db")
                    _ = await new SqlCipherRestorePreferences(path, binding, new CapturedKey(binding, key), durability, cipherVersion, preferenceKeys)
                        .ReadAsync(Check, ct).ConfigureAwait(false);
                captures.Add(captured); authenticated = true;
                foreach (var sidecar in new[] { "-wal", "-shm", "-journal" })
                    if (names.Contains(name + sidecar, StringComparer.Ordinal))
                        captures.Add(await files.CaptureAsync("database", name + sidecar, ct).ConfigureAwait(false));
            }
        }
        foreach (var root in new[] { "safety", "portable" })
        {
            var safetyNames = (await files.ListNamesAsync(root, ct).ConfigureAwait(false)).Order(StringComparer.Ordinal).ToArray();
            foreach (var name in safetyNames.Where(name => name.EndsWith(".vbs", StringComparison.OrdinalIgnoreCase)))
            {
                Check(); var captured = await files.CaptureAsync(root, name, ct).ConfigureAwait(false);
                if (captured.Length is <= 80 or > DeviceSafetyEncryption.MaximumPackageBytes + 80) throw new IOException("Recovery safety envelope exceeds its bound.");
                var header = await files.ReadAsync(root, name, 80, prefix: true, ct).ConfigureAwait(false);
                try
                {
                    if (header.Length != 80 || !DeviceSafetyEncryption.IsEncrypted(header)) throw new IOException("Unknown safety envelope ownership.");
                    var owner = new DatabaseKeyBinding(new Guid(header.AsSpan(4, 16), bigEndian: true), new Guid(header.AsSpan(20, 16), bigEndian: true), new Guid(header.AsSpan(36, 16), bigEndian: true));
                    if (owner != binding) continue; // Unauthenticated foreign headers never cause foreign key access.
                }
                finally { CryptographicOperations.ZeroMemory(header); }
                var envelope = await files.ReadAsync(root, name, DeviceSafetyEncryption.MaximumPackageBytes + 80, prefix: false, ct).ConfigureAwait(false);
                byte[]? decoded = null;
                try { decoded = DeviceSafetyEncryption.Open(envelope, key.Span); captures.Add(captured); authenticated = true; }
                finally { CryptographicOperations.ZeroMemory(envelope); if (decoded is not null) CryptographicOperations.ZeroMemory(decoded); }
            }
            if (!(await files.ListNamesAsync(root, ct).ConfigureAwait(false)).Order(StringComparer.Ordinal).SequenceEqual(safetyNames))
                throw new IOException("Safety references changed during recovery.");
        }
        if (!authenticated) throw new IOException("Recovered key has no authenticated retained reference.");
        foreach (var captured in captures.Append(historyFile))
        {
            Check(); if (await files.CaptureAsync(captured.Root, captured.Name, ct).ConfigureAwait(false) != captured)
                throw new IOException("Recovery reference changed during authentication.");
        }
        if (!(await files.ListNamesAsync("database", ct).ConfigureAwait(false)).Order(StringComparer.Ordinal).SequenceEqual(names)
            || !(await identities.ReadAsync(ct).ConfigureAwait(false)).SequenceEqual(rows)
            || !(await history.ReadAsync(ct).ConfigureAwait(false)).SequenceEqual(references)) throw new IOException("Recovery inventory changed.");
        Check();

        void Add(string name)
        {
            foreach (var suffix in new[] { "", "-journal", "-wal", "-shm" }) known.Add(name + suffix);
            if (name.EndsWith(".sqlite", StringComparison.Ordinal))
                foreach (var suffix in new[] { "", "-journal", "-wal", "-shm" }) known.Add(name + ".creating" + suffix);
        }
    }

    private async Task VerifyDatabaseAsync(string path, ReadOnlyMemory<byte> key, CancellationToken ct)
    {
        await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path,
            Password = Convert.ToBase64String(key.Span), Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString());
        await connection.OpenAsync(ct).ConfigureAwait(false);
        using var command = connection.CreateCommand(); command.CommandText = "PRAGMA cipher_version";
        if ((Convert.ToString(await command.ExecuteScalarAsync(ct).ConfigureAwait(false)) ?? "").Split(' ')[0] != cipherVersion)
            throw new InvalidDataException("Recovery requires the reviewed cipher provider.");
        command.CommandText = "SELECT count(*) FROM sqlite_schema"; _ = await command.ExecuteScalarAsync(ct).ConfigureAwait(false);
        command.CommandText = "PRAGMA cipher_integrity_check";
        await using (var authentication = await command.ExecuteReaderAsync(ct).ConfigureAwait(false))
            if (await authentication.ReadAsync(ct).ConfigureAwait(false)) throw new CryptographicException("Retained cipher authentication failed.");
        command.CommandText = "PRAGMA integrity_check";
        await using var integrity = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (!await integrity.ReadAsync(ct).ConfigureAwait(false) || integrity.GetString(0) != "ok" || await integrity.ReadAsync(ct).ConfigureAwait(false))
            throw new InvalidDataException("Retained recovery database integrity failed.");
    }

    private sealed class CapturedKey(DatabaseKeyBinding binding, ReadOnlyMemory<byte> key) : IDatabaseGenerationKeyStore
    {
        public Task PrepareAsync(DatabaseKeyBinding requested, CancellationToken ct) => throw new IOException("Recovery verification cannot create keys.");
        public Task<byte[]> ReadAsync(DatabaseKeyBinding requested, CancellationToken ct)
        { ct.ThrowIfCancellationRequested(); return requested == binding ? Task.FromResult(key.ToArray()) : throw new IOException("Recovery key context differs."); }
    }
}
