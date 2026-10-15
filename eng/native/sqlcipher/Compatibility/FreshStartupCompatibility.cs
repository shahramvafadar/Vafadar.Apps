using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Core.Settings;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.App.Startup;
using Vafadar.Zanance.Core;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.CompiledModel;

namespace Vafadar.SqlCipher.Compatibility;

/// <summary>Checks new-install startup without a plaintext original and cold missing-identity refusal.</summary>
internal static class FreshStartupCompatibility
{
    internal static async Task<string> RunAsync(string folder, bool create,
        Func<string, IDatabaseGenerationKeyStore> createKeys,
        Func<string, IDatabaseGenerationFileDurability> createDurability, bool failPublication = false)
    {
        if (create)
        {
            if (Directory.Exists(folder)) throw new IOException("Use a fresh installation fixture.");
            Directory.CreateDirectory(folder);
        }
        var original = Path.Combine(folder, ZananceApp.DatabaseFileName);
        using var services = new ServiceCollection().AddZananceData(original).BuildServiceProvider();
        var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        var preferences = new InMemorySettingsStore();
        var profiles = new ProfileService(location, services.GetRequiredService<ZananceStore>(), preferences, new Runtime(), Translator.Instance);
        var identityPath = Path.Combine(folder, "identities.sqlite");
        var actual = createKeys(folder);
        var keys = new CountingKeys(create ? actual : new ExistingKeys(actual));
        var durability = createDurability(folder);
        var schemaBuilds = 0; var resolutions = 0; var publications = 0;
        async Task BuildSchema(SqliteConnection connection, CancellationToken ct)
        {
            schemaBuilds++;
            if (File.Exists(original)) throw new IOException("Fresh schema was created on disk before protection.");
            var options = new DbContextOptionsBuilder<ZananceDbContext>().UseSqlite(connection, contextOwnsConnection: false)
                .UseModel(ZananceDbContextModel.Instance).Options;
            await using var db = new ZananceDbContext(options); await db.Database.MigrateAsync(ct);
            if ((await db.Database.GetAppliedMigrationsAsync(ct)).Count() != 32) throw new IOException("Real schema is incomplete.");
        }
        var session = new SqlCipherProfileSession<ZananceDbContext>(location, async (path, owner, ct) =>
        {
            resolutions++;
            var identities = new SqliteDatabaseIdentityCatalogue(identityPath, owner.Validate);
            var identity = (await identities.ReadAsync(ct)).Single();
            var history = new SqliteProfileRestoreHistory(original + ".restore-history.sqlite", identity.InitialCandidateKey, owner.Validate);
            return await SqlCipherProfileGeneration.OpenCurrentAsync(location, identities, "", path, history,
                _ => throw new IOException("Fresh startup cannot invent a restore."), keys, durability, owner,
                () => { }, 16 * 1024 * 1024, "4.19.0", ct);
        });
        location.Changed += (_, _) =>
        {
            publications++;
            if (failPublication && publications == 1) throw new IOException("Fictitious publication observer failure.");
        };
        var completedObservers = 0;
        location.Changed += (_, _) => completedObservers++;
        var startup = new ProtectedDatabaseStartup(location, profiles, session, identityPath,
            create ? ProtectedStorageInitialization.NewInstallation : ProtectedStorageInitialization.ExistingInstallation,
            keys, durability, (_, _) => throw new IOException("Fresh startup cannot invent a restore."),
            16 * 1024 * 1024, "4.19.0", BuildSchema);
        if (failPublication)
        {
            var refused = false;
            try { await startup.PrepareAsync(); }
            catch (IOException) { refused = true; }
            if (!refused || !location.IsProtected) throw new IOException("Actual post-publication failure was not observed.");
            var retainedEndpoint = location.Capture();
            if ((await services.GetRequiredService<ZananceStore>().GetAccountsAsync()).Single().OpeningBalance != 321)
                throw new IOException("Published key owner was retired after observer failure.");
            var reads = keys.Reads;
            preferences.Set("profiles.current", Guid.NewGuid().ToString("N"));
            var changed = false;
            try { await startup.PrepareAsync(); }
            catch (InvalidOperationException) { changed = true; }
            finally { preferences.Remove("profiles.current"); }
            if (!changed || keys.Reads != reads) throw new IOException("Changed retry repeated key work or was accepted.");
            await startup.PrepareAsync();
            if (keys.Reads != reads || !ReferenceEquals(location.Capture(), retainedEndpoint) || completedObservers != 1)
                throw new IOException("Observer retry changed publication/keys or skipped remaining observers.");
        }
        var startupClock = System.Diagnostics.Stopwatch.StartNew();
        await startup.PrepareAsync(); await startup.PrepareAsync();
        startupClock.Stop();
        var startupReads = keys.Reads;
        if (!create && startupReads != 2) throw new IOException("Cold selected profile repeated key verification.");
        if (File.Exists(original) || !location.IsProtected || resolutions != 1 || publications != (failPublication ? 2 : 1)
            || completedObservers != 1 || schemaBuilds != (create ? 1 : 0))
            throw new IOException("Fresh/cold startup created plaintext or repeated initialization.");
        var store = services.GetRequiredService<ZananceStore>();
        if (create)
        {
            if ((await store.GetAccountsAsync()).Count != 0) throw new IOException("Fresh startup manufactured an account.");
            await store.SaveAccountAsync(new Account { Name = "Fictitious retained account", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1), OpeningBalance = 321 });
        }
        if ((await store.GetAccountsAsync()).Single().OpeningBalance != 321) throw new IOException("Cold startup changed retained finance.");
        using (var owner = await location.Access.AcquireExclusiveAsync())
        {
            if (failPublication)
            {
                var rejected = false;
                try { await session.StartAsync(original, owner); }
                catch (InvalidOperationException) { rejected = true; }
                if (!rejected) throw new IOException("Ordinary duplicate session start was accepted.");
            }
            session.Close(owner);
        }
        if (!create)
        {
            var held = identityPath + ".held"; File.Move(identityPath, held, overwrite: false);
            try
            {
                using var rejectedServices = new ServiceCollection().AddZananceData(original).BuildServiceProvider();
                var rejectedLocation = rejectedServices.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
                var rejectedProfiles = new ProfileService(rejectedLocation, rejectedServices.GetRequiredService<ZananceStore>(), preferences, new Runtime(), Translator.Instance);
                var rejectedSession = new SqlCipherProfileSession<ZananceDbContext>(rejectedLocation, (_, _, _) => throw new IOException("Missing identity was published."));
                var rejected = new ProtectedDatabaseStartup(rejectedLocation, rejectedProfiles, rejectedSession, identityPath,
                    ProtectedStorageInitialization.NewInstallation, keys, durability, (_, _) => throw new IOException("Unexpected restore."),
                    16 * 1024 * 1024, "4.19.0", BuildSchema);
                try { await rejected.PrepareAsync(); throw new InvalidOperationException("Missing identity was recreated."); }
                catch (IOException) { }
                if (File.Exists(identityPath) || File.Exists(original) || rejectedLocation.IsProtected)
                    throw new IOException("Missing identity created replacement state.");
            }
            finally { File.Move(held, identityPath, overwrite: false); }
        }
        return "{\"ActualFreshStartup\":true,\"NoPlaintextOriginal\":true,\"NoManufacturedAccount\":true,\"RetainedOpeningBalance\":321,\"MissingIdentityRefused\":" + (!create ? "true" : "false") +
            ",\"StartupKeyReads\":" + startupReads + ",\"StartupMilliseconds\":" + startupClock.Elapsed.TotalMilliseconds.ToString(System.Globalization.CultureInfo.InvariantCulture) +
            ",\"ObserverFailureRetainsKeyAndRetry\":" + (failPublication ? "true" : "false") + "}";
    }

    private sealed class ExistingKeys(IDatabaseGenerationKeyStore inner) : IDatabaseGenerationKeyStore
    {
        public Task PrepareAsync(DatabaseKeyBinding binding, CancellationToken ct) => throw new IOException("Cold startup prepared a key.");
        public Task<byte[]> ReadAsync(DatabaseKeyBinding binding, CancellationToken ct) => inner.ReadAsync(binding, ct);
    }

    private sealed class CountingKeys(IDatabaseGenerationKeyStore inner) : IDatabaseGenerationKeyStore
    {
        internal int Reads;
        public Task PrepareAsync(DatabaseKeyBinding binding, CancellationToken ct) => inner.PrepareAsync(binding, ct);
        public Task<byte[]> ReadAsync(DatabaseKeyBinding binding, CancellationToken ct) { Reads++; return inner.ReadAsync(binding, ct); }
    }

    private sealed class Runtime : IProfileRuntime
    {
        public Task RunExclusiveAsync(Func<Task> action) => throw new IOException("Unexpected profile action.");
        public Task MigrateAsync(DatabaseAccessGate.ExclusiveLease owner) => throw new IOException("Unexpected disk schema shortcut.");
        public Task<bool> AuthenticateAsync(string reason) => throw new IOException("Unexpected authentication.");
        public Task ReloadLockAsync() => throw new IOException("Unexpected lock reload.");
        public void DismissUndo() => throw new IOException("Unexpected Undo dismissal.");
    }
}
