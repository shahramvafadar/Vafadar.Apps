using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Core.Settings;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.App.Startup;
using Vafadar.Zanance.Core;
using Vafadar.Zanance.Data;

namespace Vafadar.SqlCipher.Compatibility;

/// <summary>Runs the actual application startup composition with every profile and a non-main selected original.</summary>
internal static class ProtectedStartupCompatibility
{
    internal static async Task<string> RunAsync(string folder, bool create,
        Func<string, IDatabaseGenerationKeyStore> createKeys,
        Func<string, IDatabaseGenerationFileDurability> createDurability, bool primeRetainedPool = false, bool checkDeletion = false)
    {
        var main = Path.Combine(folder, ZananceApp.DatabaseFileName);
        var scope = Path.Combine(folder, "startup-profile.txt");
        if (create)
        {
            if (Directory.Exists(folder)) throw new IOException("Use a fresh startup fixture.");
            Directory.CreateDirectory(folder);
            await File.WriteAllTextAsync(scope, Guid.NewGuid().ToString("N"));
        }
        var legacy = await File.ReadAllTextAsync(scope);
        var work = Path.Combine(folder, "zanance-" + legacy + ".db");
        if (create)
        {
            await OriginalProfileFixture.CreateOriginalAsync(main);
            await OriginalProfileFixture.CreateOriginalAsync(work);
            using var workServices = new ServiceCollection().AddZananceData(work).BuildServiceProvider();
            await using var db = await workServices.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync();
            (await db.Entries.SingleAsync()).Amount = 880; await db.SaveChangesAsync();
        }
        var preferences = new InMemorySettingsStore();
        preferences.Set("profiles.list", legacy + ":" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("Fictitious work")));
        preferences.Set("profiles.current", legacy);
        // Supply fictitious legacy device preferences; the actual product parser captures their storage map.
        using var services = new ServiceCollection().AddZananceData(work).BuildServiceProvider();
        var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        var profiles = new ProfileService(location, services.GetRequiredService<ZananceStore>(), preferences, new Runtime(), Translator.Instance);
        if (primeRetainedPool)
        {
            if (!File.Exists(work)) throw new IOException("Recorded interrupted original is missing.");
            await using var borrowed = await services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync();
            if ((await borrowed.Entries.SingleAsync()).Amount != 880) throw new IOException("Recorded original differs.");
            // Disposal deliberately leaves the product factory's idle pool for the bootstrap to retire.
        }
        if (profiles.All.Count != 2 || profiles.Current.Id != legacy) throw new IOException("Fictitious captured catalogue is invalid.");
        var identityPath = Path.Combine(folder, "identities.sqlite");
        var actual = createKeys(folder);
        IDatabaseGenerationKeyStore keys = create ? actual : new ExistingKeys(actual);
        var durability = createDurability(folder);
        var resolutions = 0;
        var session = new SqlCipherProfileSession<ZananceDbContext>(location, async (original, owner, ct) =>
        {
            resolutions++;
            var identities = new SqliteDatabaseIdentityCatalogue(identityPath, owner.Validate);
            var selectedLegacy = original == main ? "" : legacy;
            var identity = (await identities.ReadAsync(ct)).Single(row => row.LegacyProfileId == selectedLegacy);
            var history = new SqliteProfileRestoreHistory(original + ".restore-history.sqlite", identity.InitialCandidateKey, owner.Validate);
            return await SqlCipherProfileGeneration.OpenCurrentAsync(location, identities, selectedLegacy, original, history,
                _ => throw new IOException("Initial startup cannot invent a restore."), keys, durability, owner,
                () => { }, 16 * 1024 * 1024, "4.19.0", ct);
        });
        var publicationCount = 0; location.Changed += (_, _) => publicationCount++;
        var startup = new ProtectedDatabaseStartup(location, profiles, session, identityPath,
            create ? ProtectedStorageInitialization.LegacyPlaintextProfiles : ProtectedStorageInitialization.ExistingInstallation, keys, durability,
            (_, _) => throw new IOException("Initial startup cannot invent a restore."), 16 * 1024 * 1024, "4.19.0");
        await startup.PrepareAsync();
        await startup.PrepareAsync();
        if (resolutions != 1 || publicationCount != 1 || !location.IsProtected || File.Exists(main) || File.Exists(work))
            throw new IOException("Startup repeated publication or left a captured profile plaintext.");
        await using (var db = await services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync())
            if ((await db.Entries.SingleAsync()).Amount != 880 || (await db.Accounts.SingleAsync()).OpeningBalance != 1000)
                throw new IOException("Startup selected the wrong financial packet.");
        if (checkDeletion)
        {
            using (var owner = await location.Access.AcquireExclusiveAsync())
            { await session.BeginSwitchAsync(main, owner); session.FinishSwitch(true, owner); }
            preferences.Remove("profiles.current");
            var before = profiles.All.ToArray(); var currentPath = location.Path;
            var unchanged = Directory.EnumerateFiles(folder).ToDictionary(path => path,
                path => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path))));
            var refused = false;
            try { await profiles.DeleteAsync(before.Single(profile => profile.Id == legacy)); }
            catch (InvalidOperationException) { refused = true; }
            if (!refused || !profiles.All.SequenceEqual(before) || location.Path != currentPath)
                throw new IOException("Legacy deletion removed protected profile identity or active endpoint.");
            foreach (var file in unchanged)
                if (!File.Exists(file.Key) || Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(file.Key))) != file.Value)
                    throw new IOException("Refused deletion changed a retained native artifact.");
        }
        using (var owner = await location.Access.AcquireExclusiveAsync()) session.Close(owner);
        return "{\"ActualApplicationStartup\":true,\"AllProfilesBeforeSinglePublication\":true,\"SelectedWorkAmount\":880,\"NoRepeatedKeyResolution\":true,\"ColdExistingKeysOnly\":" + (!create ? "true" : "false") +
            ",\"IncompleteProtectedDeletionRefused\":" + (checkDeletion ? "true" : "false") + "}";
    }

    private sealed class ExistingKeys(IDatabaseGenerationKeyStore inner) : IDatabaseGenerationKeyStore
    {
        public Task PrepareAsync(DatabaseKeyBinding binding, CancellationToken ct) => throw new IOException("Cold startup prepared a replacement key.");
        public Task<byte[]> ReadAsync(DatabaseKeyBinding binding, CancellationToken ct) => inner.ReadAsync(binding, ct);
    }

    private sealed class Runtime : IProfileRuntime
    {
        public Task RunExclusiveAsync(Func<Task> action) => throw new IOException("Unexpected startup profile action.");
        public Task MigrateAsync(DatabaseAccessGate.ExclusiveLease owner) => throw new IOException("Unexpected migration shortcut.");
        public Task<bool> AuthenticateAsync(string reason) => throw new IOException("Unexpected authentication.");
        public Task ReloadLockAsync() => throw new IOException("Unexpected lock reload.");
        public void DismissUndo() => throw new IOException("Unexpected Undo dismissal.");
    }
}
