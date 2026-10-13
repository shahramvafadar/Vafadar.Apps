using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Core.Settings;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.App.Startup;
using Vafadar.Zanance.Core;
using Vafadar.Zanance.Data;

namespace Vafadar.SqlCipher.Compatibility.Android;

/// <summary>Runs the actual application startup composition with every profile and a non-main selected original.</summary>
internal static class ProtectedStartupCompatibility
{
    internal static async Task<string> RunAsync(string folder, bool create)
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
            await GenerationMigrationCompatibility.CreateOriginalAsync(main);
            await GenerationMigrationCompatibility.CreateOriginalAsync(work);
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
        if (profiles.All.Count != 2 || profiles.Current.Id != legacy) throw new IOException("Fictitious captured catalogue is invalid.");
        var identityPath = Path.Combine(folder, "identities.sqlite");
        var actual = new AndroidGenerationKeyStore(folder);
        IDatabaseGenerationKeyStore keys = create ? actual : new ExistingKeys(actual);
        var durability = new AndroidGenerationFileDurability(folder);
        var resolutions = 0;
        var session = new SqlCipherProfileSession<ZananceDbContext>(location, async (original, owner, ct) =>
        {
            resolutions++;
            var identities = new SqliteDatabaseIdentityCatalogue(identityPath, owner.Validate);
            var identity = (await identities.ReadAsync(ct)).Single(row => row.LegacyProfileId == legacy);
            var history = new SqliteProfileRestoreHistory(original + ".restore-history.sqlite", identity.InitialCandidateKey, owner.Validate);
            return await SqlCipherProfileGeneration.OpenCurrentAsync(location, identities, legacy, original, history,
                _ => throw new IOException("Initial startup cannot invent a restore."), keys, durability, owner,
                () => { }, 16 * 1024 * 1024, "4.19.0", ct);
        });
        var publicationCount = 0; location.Changed += (_, _) => publicationCount++;
        var startup = new ProtectedDatabaseStartup(location, profiles, session, identityPath, create, keys, durability,
            (_, _) => throw new IOException("Initial startup cannot invent a restore."), 16 * 1024 * 1024, "4.19.0");
        await startup.PrepareAsync();
        await startup.PrepareAsync();
        if (resolutions != 1 || publicationCount != 1 || !location.IsProtected || File.Exists(main) || File.Exists(work))
            throw new IOException("Startup repeated publication or left a captured profile plaintext.");
        await using (var db = await services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync())
            if ((await db.Entries.SingleAsync()).Amount != 880 || (await db.Accounts.SingleAsync()).OpeningBalance != 1000)
                throw new IOException("Startup selected the wrong financial packet.");
        using (var owner = await location.Access.AcquireExclusiveAsync()) session.Close(owner);
        return "{\"ActualApplicationStartup\":true,\"AllProfilesBeforeSinglePublication\":true,\"SelectedWorkAmount\":880,\"NoRepeatedKeyResolution\":true,\"ColdExistingKeysOnly\":" + (!create ? "true" : "false") + "}";
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
