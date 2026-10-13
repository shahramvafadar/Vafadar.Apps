using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Core.Settings;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.App.Startup;
using Vafadar.Zanance.Core;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.CompiledModel;

namespace Vafadar.SqlCipher.Compatibility.Android;

/// <summary>Checks new-install startup without a plaintext original and cold missing-identity refusal.</summary>
internal static class FreshStartupCompatibility
{
    internal static async Task<string> RunAsync(string folder, bool create)
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
        var actual = new AndroidGenerationKeyStore(folder);
        IDatabaseGenerationKeyStore keys = create ? actual : new ExistingKeys(actual);
        var durability = new AndroidGenerationFileDurability(folder);
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
        location.Changed += (_, _) => publications++;
        var startup = new ProtectedDatabaseStartup(location, profiles, session, identityPath,
            create ? ProtectedStorageInitialization.NewInstallation : ProtectedStorageInitialization.ExistingInstallation,
            keys, durability, (_, _) => throw new IOException("Fresh startup cannot invent a restore."),
            16 * 1024 * 1024, "4.19.0", BuildSchema);
        await startup.PrepareAsync(); await startup.PrepareAsync();
        if (File.Exists(original) || !location.IsProtected || resolutions != 1 || publications != 1 || schemaBuilds != (create ? 1 : 0))
            throw new IOException("Fresh/cold startup created plaintext or repeated initialization.");
        var store = services.GetRequiredService<ZananceStore>();
        if (create)
        {
            if ((await store.GetAccountsAsync()).Count != 0) throw new IOException("Fresh startup manufactured an account.");
            await store.SaveAccountAsync(new Account { Name = "Fictitious retained account", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1), OpeningBalance = 321 });
        }
        if ((await store.GetAccountsAsync()).Single().OpeningBalance != 321) throw new IOException("Cold startup changed retained finance.");
        using (var owner = await location.Access.AcquireExclusiveAsync()) session.Close(owner);
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
        return "{\"ActualFreshStartup\":true,\"NoPlaintextOriginal\":true,\"NoManufacturedAccount\":true,\"RetainedOpeningBalance\":321,\"MissingIdentityRefused\":" + (!create ? "true" : "false") + "}";
    }

    private sealed class ExistingKeys(IDatabaseGenerationKeyStore inner) : IDatabaseGenerationKeyStore
    {
        public Task PrepareAsync(DatabaseKeyBinding binding, CancellationToken ct) => throw new IOException("Cold startup prepared a key.");
        public Task<byte[]> ReadAsync(DatabaseKeyBinding binding, CancellationToken ct) => inner.ReadAsync(binding, ct);
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
