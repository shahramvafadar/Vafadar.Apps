using System.Security.Cryptography;
using Vafadar.Backup;
using Vafadar.Backup.Security;

namespace Vafadar.Data;

/// <summary>Discovers exact profile database/history/backup/key references before irreversible catalogue removal.</summary>
/// <remarks>The host owns all named roots and exclusive maintenance; cloud backups and shared installation keys
/// are outside this inventory. Unknown profile namespace artifacts or unauthenticated safety ownership fail closed.</remarks>
public static class ProfileRetirementReferenceDiscovery
{
    /// <summary>Reads actual identities/history, authenticates native key/safety bindings, then captures unchanged exact objects.</summary>
    public static async Task CaptureAsync(SqliteProfileRetirementJournal retirement, SqliteDatabaseIdentityCatalogue identities,
        string originalPath, string appId, IProfileRetirementReferenceFiles files, IDatabaseGenerationKeyStore keys,
        IDatabaseGenerationFileDurability durability, Action validateAccess, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(retirement); ArgumentNullException.ThrowIfNull(identities);
        ArgumentNullException.ThrowIfNull(files); ArgumentNullException.ThrowIfNull(keys);
        ArgumentNullException.ThrowIfNull(durability); ArgumentNullException.ThrowIfNull(validateAccess);
        ArgumentException.ThrowIfNullOrWhiteSpace(appId);
        if (!Path.IsPathFullyQualified(originalPath) || Path.GetExtension(originalPath) != ".db")
            throw new ArgumentException("Retirement requires the captured absolute original database path.");
        void Check() { ct.ThrowIfCancellationRequested(); validateAccess(); }
        Check(); var request = await retirement.SynchronizeAndReadAsync(durability, ct).ConfigureAwait(false);
        if (request.Phase != ProfileRetirementPhase.Authorized) throw new InvalidOperationException("Reference capture must precede catalogue removal.");
        var originalIdentities = await identities.ReadAsync(ct).ConfigureAwait(false);
        var identity = originalIdentities.Single(row => row.LegacyProfileId == request.LegacyProfileId);
        if (identity.ProfileId != request.ProfileId || identity.InstallationId != request.InstallationId)
            throw new InvalidOperationException("Retirement differs from the actual profile binding.");
        var original = Path.GetFileName(originalPath); var historyName = original + ".restore-history.sqlite";
        // Native capture verifies the regular private metadata file before the strict SQLite reader opens it.
        _ = await files.CaptureAsync("database", historyName, ct).ConfigureAwait(false); Check();
        var history = await new SqliteProfileRestoreHistory(originalPath + ".restore-history.sqlite", identity.InitialCandidateKey, validateAccess)
            .ReadAsync(ct).ConfigureAwait(false);
        var known = new HashSet<string>(StringComparer.Ordinal);
        AddFamily(known, original); AddMetadata(known, original + ".generation.sqlite");
        AddMetadata(known, original + ".new-profile.sqlite"); AddMetadata(known, historyName);
        foreach (var reference in history)
        {
            var candidate = original + "." + reference.CandidateGeneration.ToString("N") + ".cipher.db";
            AddFamily(known, candidate); AddFamily(known, candidate + ".rollback.db");
            AddFamily(known, candidate + ".before-restore.cipher.db");
            AddFamily(known, candidate + ".preferences.cipher.db"); AddMetadata(known, candidate + ".restore.sqlite");
        }
        var captured = new List<ProfileRetirementArtifact>();
        var observed = new Dictionary<string, string[]>(StringComparer.Ordinal);
        foreach (var root in new[] { "database", "keys", "portable", "safety" })
        {
            Check(); observed.Add(root, (await files.ListNamesAsync(root, ct).ConfigureAwait(false)).Order(StringComparer.Ordinal).ToArray());
        }
        foreach (var name in observed["database"])
        {
            if (known.Contains(name)) captured.Add(await files.CaptureAsync("database", name, ct).ConfigureAwait(false));
            else if (name.StartsWith(original + ".", StringComparison.Ordinal))
                throw new IOException("Unknown profile namespace artifact requires reconciliation before retirement.");
        }
        var keyPrefix = request.ProfileId.ToString("N") + "-";
        foreach (var name in observed["keys"].Where(name => name.StartsWith(keyPrefix, StringComparison.Ordinal)))
        {
            var baseName = name.EndsWith(".key.recovered.receipt.creating", StringComparison.Ordinal) ? name[..^".recovered.receipt.creating".Length]
                : name.EndsWith(".key.recovered.receipt", StringComparison.Ordinal) ? name[..^".recovered.receipt".Length]
                : name.EndsWith(".key.recovered.creating", StringComparison.Ordinal) ? name[..^".recovered.creating".Length]
                : name.EndsWith(".key.recovered", StringComparison.Ordinal) ? name[..^".recovered".Length] : name;
            if (baseName.Length != 69 || !baseName.EndsWith(".key", StringComparison.Ordinal)
                || !Guid.TryParseExact(baseName.AsSpan(33, 32), "N", out var generation) || generation == Guid.Empty)
                throw new IOException("Unknown profile key namespace artifact requires reconciliation.");
            var keyArtifact = await files.CaptureAsync("keys", name, ct).ConfigureAwait(false);
            var key = await keys.ReadAsync(new(request.ProfileId, request.InstallationId, generation), ct).ConfigureAwait(false);
            try
            {
                if (key.Length != 32) throw new CryptographicException("Native retirement key binding differs.");
                if (name != baseName || observed["keys"].Contains(baseName + ".recovered", StringComparer.Ordinal))
                {
                    if (!observed["keys"].Contains(baseName + ".recovered", StringComparer.Ordinal))
                        throw new IOException("Unpublished recovered artifacts require explicit reconciliation.");
                    var binding = new DatabaseKeyBinding(request.ProfileId, request.InstallationId, generation);
                    var originalKey = observed["keys"].Contains(baseName, StringComparer.Ordinal) ? await files.CaptureAsync("keys", baseName, ct).ConfigureAwait(false) : null;
                    var recovered = await files.CaptureAsync("keys", baseName + ".recovered", ct).ConfigureAwait(false);
                    var receipt = await files.ReadAsync("keys", baseName + ".recovered.receipt", KeyRecoveryReceipt.MaximumBytes, prefix: false, ct).ConfigureAwait(false);
                    KeyRecoveryReceipt.Verify(receipt, binding, originalKey, recovered, key);
                    if (name.EndsWith(".creating", StringComparison.Ordinal)) throw new IOException("Staged recovery artifacts require explicit reconciliation.");
                }
                Check();
            }
            finally { CryptographicOperations.ZeroMemory(key); }
            if (await files.CaptureAsync("keys", name, ct).ConfigureAwait(false) != keyArtifact)
                throw new IOException("Native key reference changed during binding authentication.");
            captured.Add(keyArtifact);
        }
        var backupPrefix = appId + "~" + request.LegacyProfileId;
        foreach (var root in new[] { "portable", "safety" })
        {
            foreach (var name in observed[root])
            {
                if (BackupFileName.TryParse(name, out var owner, out _) && owner == backupPrefix)
                    captured.Add(await files.CaptureAsync(root, name, ct).ConfigureAwait(false));
                else if (name.StartsWith(backupPrefix + "_", StringComparison.Ordinal))
                    throw new IOException("Malformed profile backup namespace artifact requires reconciliation.");
                else if (name.EndsWith(".vbs", StringComparison.OrdinalIgnoreCase))
                    await CaptureSafetyAsync(root, name).ConfigureAwait(false);
            }
        }
        foreach (var root in observed.Keys)
        {
            Check(); if (!(await files.ListNamesAsync(root, ct).ConfigureAwait(false)).Order(StringComparer.Ordinal).SequenceEqual(observed[root]))
                throw new IOException("Private reference namespace changed during retirement capture.");
        }
        foreach (var artifact in captured)
        {
            Check(); if (await files.CaptureAsync(artifact.Root, artifact.Name, ct).ConfigureAwait(false) != artifact)
                throw new IOException("Profile reference changed during ownership verification.");
        }
        if (!(await identities.ReadAsync(ct).ConfigureAwait(false)).SequenceEqual(originalIdentities)
            || await retirement.ReadAsync(ct).ConfigureAwait(false) != request) throw new IOException("Profile identity or retirement changed during capture.");
        await retirement.CaptureArtifactsAsync(captured, durability, ct).ConfigureAwait(false); Check();

        async Task CaptureSafetyAsync(string root, string name)
        {
            var artifact = await files.CaptureAsync(root, name, ct).ConfigureAwait(false);
            if (artifact.Length is <= 80 or > DeviceSafetyEncryption.MaximumPackageBytes + 80)
                throw new IOException("Opaque safety reference exceeds its supported envelope bound.");
            var header = await files.ReadAsync(root, name, 80, prefix: true, ct).ConfigureAwait(false);
            DatabaseKeyBinding binding;
            try
            {
                if (header.Length != 80 || !DeviceSafetyEncryption.IsEncrypted(header)) throw new IOException("Opaque safety ownership is unknown.");
                binding = new(new Guid(header.AsSpan(4, 16), bigEndian: true), new Guid(header.AsSpan(20, 16), bigEndian: true),
                    new Guid(header.AsSpan(36, 16), bigEndian: true));
            }
            finally { CryptographicOperations.ZeroMemory(header); }
            // Unverified foreign headers never cause foreign key access or deletion. Matching headers still
            // require full authenticated bytes before they can establish this profile's ownership.
            if (binding.ProfileId != request.ProfileId || binding.InstallationId != request.InstallationId) return;
            var envelope = await files.ReadAsync(root, name, DeviceSafetyEncryption.MaximumPackageBytes + 80, prefix: false, ct).ConfigureAwait(false);
            byte[]? decoded = null;
            try
            {
                var protector = new DeviceKeySafetyCopyProtection(request.ProfileId, request.InstallationId, keys, validateAccess);
                decoded = await protector.OpenAsync(envelope, ct).ConfigureAwait(false); Check();
                if (await files.CaptureAsync(root, name, ct).ConfigureAwait(false) != artifact) throw new IOException("Safety reference changed during authentication.");
                captured.Add(artifact);
            }
            finally { CryptographicOperations.ZeroMemory(envelope); if (decoded is not null) CryptographicOperations.ZeroMemory(decoded); }
        }
    }
    private static void AddFamily(HashSet<string> names, string name)
    { foreach (var suffix in new[] { "", "-journal", "-wal", "-shm" }) names.Add(name + suffix); }
    private static void AddMetadata(HashSet<string> names, string name) { AddFamily(names, name); AddFamily(names, name + ".creating"); }
}
