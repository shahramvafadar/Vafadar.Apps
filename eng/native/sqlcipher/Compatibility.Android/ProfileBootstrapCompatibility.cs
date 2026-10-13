using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.Data;

namespace Vafadar.SqlCipher.Compatibility.Android;

/// <summary>Prepares two original financial profiles with actual native metadata, key storage and migration effects.</summary>
internal static class ProfileBootstrapCompatibility
{
    /// <summary>Initializes every captured original or verifies a cold all-profile pass with existing keys only.</summary>
    internal static async Task<string> RunAsync(string folder, bool create)
    {
        var scope = Path.Combine(folder, "profile-scope.txt");
        if (create)
        {
            if (Directory.Exists(folder)) throw new IOException("Use a fresh all-profile fixture.");
            Directory.CreateDirectory(folder); await File.WriteAllTextAsync(scope, Guid.NewGuid().ToString("N"));
        }
        var legacy = await File.ReadAllTextAsync(scope);
        var paths = new Dictionary<string, string> { [""] = Path.Combine(folder, "main.db"), [legacy] = Path.Combine(folder, "work.db") };
        if (create)
        {
            foreach (var path in paths.Values) await GenerationMigrationCompatibility.CreateOriginalAsync(path);
            using var work = new ServiceCollection().AddZananceData(paths[legacy]).BuildServiceProvider();
            await using var db = await work.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync();
            (await db.Entries.SingleAsync()).Amount = 880; await db.SaveChangesAsync();
        }
        using var services = new ServiceCollection().AddZananceData(paths[""]).BuildServiceProvider();
        var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        using var owner = await location.Access.AcquireExclusiveAsync();
        var identityPath = Path.Combine(folder, "identities.sqlite");
        var actual = new AndroidGenerationKeyStore(folder);
        IDatabaseGenerationKeyStore keys = create ? actual : new ExistingKeys(actual);
        await SqlCipherProfileBootstrap.PrepareAsync(location, owner, identityPath, paths, create, keys,
            new AndroidGenerationFileDurability(folder), (_, _) => throw new IOException("Initial profiles cannot invent restores."),
            () => { }, 16 * 1024 * 1024, "4.19.0");
        if (location.Path != paths[""]) throw new IOException("Bootstrap published an intermediate profile.");
        var identities = new SqliteDatabaseIdentityCatalogue(identityPath, owner.Validate);
        var rows = await identities.ReadAsync();
        var fingerprint = string.Join("\n", rows.Select(row => row.ToString()));
        if (create) await File.WriteAllTextAsync(Path.Combine(folder, "fingerprint.txt"), fingerprint);
        else if (fingerprint != await File.ReadAllTextAsync(Path.Combine(folder, "fingerprint.txt"))) throw new IOException("Cold bootstrap changed stable identities.");
        foreach (var identity in rows)
        {
            var original = paths[identity.LegacyProfileId]; var isolated = new LocalDatabaseLocation<ZananceDbContext>(original);
            using var isolatedOwner = await isolated.Access.AcquireExclusiveAsync();
            var history = new SqliteProfileRestoreHistory(original + ".restore-history.sqlite", identity.InitialCandidateKey, isolatedOwner.Validate);
            using var generation = await SqlCipherProfileGeneration.OpenCurrentAsync(isolated, identities, identity.LegacyProfileId,
                original, history, _ => throw new IOException("Initial profile verification cannot restore."), keys,
                new AndroidGenerationFileDurability(folder), isolatedOwner, () => { }, 16 * 1024 * 1024, "4.19.0");
            generation.Publish(location, owner);
            await using var db = await services.GetRequiredService<ILocalDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(owner);
            if ((await db.Entries.SingleAsync()).Amount != (identity.LegacyProfileId == "" ? 250 : 880) ||
                (await db.Accounts.SingleAsync()).OpeningBalance != 1000 || File.Exists(original) || !File.Exists(generation.Path + ".rollback.db"))
                throw new IOException("All-profile migration changed financial data or retained rollback.");
        }
        return "{\"AllCapturedProfilesPrepared\":true,\"DistinctFinancialPacketsPreserved\":true,\"NoIntermediatePublication\":true,\"StableBindingsAndEncryptedRollback\":true}";
    }

    private sealed class ExistingKeys(IDatabaseGenerationKeyStore inner) : IDatabaseGenerationKeyStore
    {
        /// <inheritdoc />
        public Task PrepareAsync(DatabaseKeyBinding binding, CancellationToken ct) => throw new IOException("Cold all-profile verification must not prepare keys.");
        /// <inheritdoc />
        public Task<byte[]> ReadAsync(DatabaseKeyBinding binding, CancellationToken ct) => inner.ReadAsync(binding, ct);
    }
}
