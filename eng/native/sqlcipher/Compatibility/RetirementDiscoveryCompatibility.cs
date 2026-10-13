using System.Security.Cryptography;
using Vafadar.Data;

namespace Vafadar.SqlCipher.Compatibility;

/// <summary>Discovers references in an already migrated real two-profile fixture without repeating migration or deleting finance.</summary>
internal static class RetirementDiscoveryCompatibility
{
    /// <summary>Uses existing native keys and actual identity/history metadata to capture only the retained work profile.</summary>
    internal static async Task<string> RunAsync(string folder, Func<string, IDatabaseGenerationKeyStore> createKeys,
        Func<string, IDatabaseGenerationFileDurability> createDurability,
        Func<IReadOnlyDictionary<string, string>, IProfileRetirementReferenceFiles> createFiles)
    {
        SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlcipher());
        var legacy = (await File.ReadAllTextAsync(Path.Combine(folder, "startup-profile.txt"))).Trim();
        var identities = new SqliteDatabaseIdentityCatalogue(Path.Combine(folder, "identities.sqlite"), () => { });
        var rows = await identities.ReadAsync(); var target = rows.Single(row => row.LegacyProfileId == legacy);
        var main = rows.Single(row => row.LegacyProfileId == "");
        var roots = new Dictionary<string, string> { ["database"] = folder, ["keys"] = folder,
            ["portable"] = Path.Combine(folder, "retirement-portable"), ["safety"] = Path.Combine(folder, "retirement-safety") };
        Directory.CreateDirectory(roots["portable"]); Directory.CreateDirectory(roots["safety"]);
        var files = createFiles(roots); var keys = createKeys(folder); var native = createDurability(folder);
        var original = Path.Combine(folder, "zanance-" + legacy + ".db");
        var history = await new SqliteProfileRestoreHistory(original + ".restore-history.sqlite", target.InitialCandidateKey, () => { }).ReadAsync();
        var last = history[^1]; var candidate = Path.GetFileName(original) + "." + last.CandidateGeneration.ToString("N") + ".cipher.db";
        var before = await files.CaptureAsync("database", candidate);
        var owned = await new DeviceKeySafetyCopyProtection(target.ProfileId, target.InstallationId, keys, () => { })
            .ProtectAsync(new byte[] { 1, 2, 3 }, CancellationToken.None);
        var foreign = await new DeviceKeySafetyCopyProtection(main.ProfileId, main.InstallationId, keys, () => { })
            .ProtectAsync(new byte[] { 4, 5, 6 }, CancellationToken.None);
        var suffix = Guid.NewGuid().ToString("N"); var ownedName = "safety-owned-" + suffix + ".vbs";
        var foreignName = "safety-main-" + suffix + ".vbs";
        await File.WriteAllBytesAsync(Path.Combine(roots["safety"], ownedName), owned);
        await File.WriteAllBytesAsync(Path.Combine(roots["safety"], foreignName), foreign);
        var request = new ProfileRetirementCheckpoint(legacy, target.ProfileId, target.InstallationId, Guid.NewGuid());
        var journal = new SqliteProfileRetirementJournal(Path.Combine(folder, "retirement-discovery-" + request.OperationId.ToString("N") + ".sqlite"), request, () => { });
        await journal.CreateNewAsync(native);
        await ProfileRetirementReferenceDiscovery.CaptureAsync(journal, identities, original, "pro.vafadar.zanance", files, keys, native, () => { });
        var inventory = await journal.ReadArtifactsAsync();
        if (!inventory.Any(item => item.Name == candidate) || !inventory.Any(item => item.Name == ownedName)
            || inventory.Any(item => item.Name == foreignName || item.Name.StartsWith(main.ProfileId.ToString("N") + "-", StringComparison.Ordinal))
            || inventory.Any(item => item.Name.StartsWith("zanance.db.", StringComparison.Ordinal)))
            throw new IOException("Actual native profile discovery mixed ownership or missed retained references.");
        if (await files.CaptureAsync("database", candidate) != before || !(await identities.ReadAsync()).SequenceEqual(rows)
            || await journal.ReadAsync() != request) throw new IOException("Reference discovery changed retained finance/identity or advanced retirement.");
        var key = await keys.ReadAsync(main.InitialCandidateKey, CancellationToken.None);
        try { if (key.Length != 32) throw new IOException("Discovery damaged the retained main key."); }
        finally { CryptographicOperations.ZeroMemory(key); }
        return "{\"ActualEncryptedProfileCaptured\":true,\"OwnedSafetyAuthenticated\":true,\"ForeignSafetyExcluded\":true,\"NoFinancialDeletion\":true,\"CapturedCount\":" + inventory.Count + "}";
    }
}
