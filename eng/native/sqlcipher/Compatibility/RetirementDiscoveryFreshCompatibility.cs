using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;
using Vafadar.Backup;
using Vafadar.Data;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.CompiledModel;

namespace Vafadar.SqlCipher.Compatibility;

/// <summary>Creates an independently owned actual encrypted model for native reference discovery, without startup/UI replay.</summary>
internal static class RetirementDiscoveryFreshCompatibility
{
    /// <summary>Verifies database/key/local-backup/safety capture using actual platform effects and the original EF schema.</summary>
    internal static async Task<string> RunAsync(string folder, Func<string, IDatabaseGenerationKeyStore> makeKeys,
        Func<string, IDatabaseGenerationFileDurability> makeDurability,
        Func<IReadOnlyDictionary<string, string>, IProfileRetirementReferenceFiles> makeFiles, bool resume = false)
    {
        SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlcipher());
        if (resume ? !Directory.Exists(folder) : Directory.Exists(folder)) throw new IOException("Native discovery requires its explicit fresh or retained directory.");
        if (!resume) Directory.CreateDirectory(folder);
        var roots = new Dictionary<string, string>();
        foreach (var root in new[] { "database", "keys", "portable", "safety" })
        {
            roots.Add(root, Path.Combine(folder, root));
            if (!resume) Directory.CreateDirectory(roots[root]);
            else if (!Directory.Exists(roots[root])) throw new IOException("Retained discovery root is missing.");
        }
        var identities = new SqliteDatabaseIdentityCatalogue(Path.Combine(roots["database"], "identities.sqlite"), () => { });
        var native = makeDurability(roots["database"]); var keys = makeKeys(roots["keys"]); var files = makeFiles(roots);
        if (!resume) await identities.CreateNewAsync(["", Guid.NewGuid().ToString("N")], native);
        var rows = await identities.ReadAsync();
        if (rows.Count != 2) throw new IOException("Retained discovery requires the exact two original identities.");
        var target = rows.Single(row => row.LegacyProfileId.Length > 0); var main = rows.Single(row => row.LegacyProfileId == "");
        var legacy = target.LegacyProfileId;
        var original = Path.Combine(roots["database"], "owned.db"); var candidate = original + "." + target.CandidateGeneration.ToString("N") + ".cipher.db";
        if (!resume) await keys.PrepareAsync(target.InitialCandidateKey, CancellationToken.None);
        var key = await keys.ReadAsync(target.InitialCandidateKey, CancellationToken.None);
        try
        {
            await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            { DataSource = candidate, Password = Convert.ToBase64String(key), Pooling = false }.ToString());
            await connection.OpenAsync();
            using var command = connection.CreateCommand(); command.CommandText = "PRAGMA cipher_version";
            if (!(Convert.ToString(await command.ExecuteScalarAsync()) ?? "").StartsWith("4.19.0", StringComparison.Ordinal))
                throw new IOException("Native discovery requires the reviewed cipher.");
            // The original data registration sets EF's synchronous compiled-model initialization switch.
            // Without it Mono's static constructor waits for a worker waiting on that same constructor.
            _ = new ServiceCollection().AddZananceData(candidate);
            var options = new DbContextOptionsBuilder<ZananceDbContext>().UseSqlite(connection).UseModel(ZananceDbContextModel.Instance).Options;
            await using var model = new ZananceDbContext(options); await model.Database.MigrateAsync();
        }
        finally { CryptographicOperations.ZeroMemory(key); }
        await native.SynchronizeFileAsync(candidate, CancellationToken.None);
        await new SqliteProfileRestoreHistory(original + ".restore-history.sqlite", target.InitialCandidateKey, () => { }).CreateNewAsync(native);
        var own = await new DeviceKeySafetyCopyProtection(target.ProfileId, target.InstallationId, keys, () => { }).ProtectAsync(new byte[] { 1, 2, 3 }, CancellationToken.None);
        var foreign = await new DeviceKeySafetyCopyProtection(main.ProfileId, main.InstallationId, keys, () => { }).ProtectAsync(new byte[] { 4, 5, 6 }, CancellationToken.None);
        await File.WriteAllBytesAsync(Path.Combine(roots["safety"], "safety-owned.vbs"), own);
        await File.WriteAllBytesAsync(Path.Combine(roots["safety"], "safety-main.vbs"), foreign);
        var backup = BackupFileName.Create("pro.vafadar.zanance~" + legacy, DateTimeOffset.UnixEpoch);
        await File.WriteAllTextAsync(Path.Combine(roots["portable"], backup), "Fictitious local portable fixture.");
        var before = await files.CaptureAsync("database", Path.GetFileName(candidate));
        var request = new ProfileRetirementCheckpoint(legacy, target.ProfileId, target.InstallationId, Guid.NewGuid());
        var journal = new SqliteProfileRetirementJournal(Path.Combine(roots["database"], "retirement.sqlite"), request, () => { });
        await journal.CreateNewAsync(native);
        await ProfileRetirementReferenceDiscovery.CaptureAsync(journal, identities, original, "pro.vafadar.zanance", files, keys, native, () => { });
        var captured = await journal.ReadArtifactsAsync();
        if (!captured.Any(item => item.Name == Path.GetFileName(candidate)) || !captured.Any(item => item.Name == "safety-owned.vbs")
            || !captured.Any(item => item.Name == backup) || captured.Any(item => item.Name == "safety-main.vbs")
            || captured.Any(item => item.Name.StartsWith(main.ProfileId.ToString("N") + "-", StringComparison.Ordinal))
            || await files.CaptureAsync("database", Path.GetFileName(candidate)) != before
            || !(await identities.ReadAsync()).SequenceEqual(rows) || await journal.ReadAsync() != request)
            throw new IOException("Native reference capture missed ownership or changed retained finance/identity.");
        return "{\"ActualEncryptedModelCaptured\":true,\"OwnedSafetyAuthenticated\":true,\"ForeignSafetyExcluded\":true,\"LocalPortableCaptured\":true,\"NoFinancialDeletion\":true}";
    }
}
