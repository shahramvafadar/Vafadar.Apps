using System.Data;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Backup;
using Vafadar.Core.Hosting;
using Vafadar.Core.Settings;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.App.Startup;
using Vafadar.Zanance.Core;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.CompiledModel;

namespace Vafadar.SqlCipher.Compatibility;

/// <summary>Exercises whole-package restoration and adoption by the actual active session with native platform effects.</summary>
internal static class RestoreSessionCompatibility
{
    internal static async Task<string> ReopenAsync(string folder, Func<string, IDatabaseGenerationKeyStore> createKeys,
        Func<string, IDatabaseGenerationFileDurability> createDurability)
    {
        var original = Path.Combine(folder, ZananceApp.DatabaseFileName);
        using var services = new ServiceCollection().AddZananceData(original, protectionRequired: true).BuildServiceProvider();
        var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        var settings = new InMemorySettingsStore();
        var saved = JsonSerializer.Deserialize<string[]>(await File.ReadAllTextAsync(Path.Combine(folder, "fictitious-display.json")))!;
        settings.Set("localization.language", saved.Single());
        var keys = new ExistingKeys(createKeys(folder)); var durability = createDurability(folder);
        SqlCipherPackageRestoreReceiver Receiver(ProfileRestoreReference reference, DatabaseAccessGate.ExclusiveLease owner,
            DatabaseProfileIdentity identity)
        {
            var captured = new DatabaseGenerationCheckpoint(identity.ProfileId, reference.OriginalGeneration, reference.CandidateGeneration);
            var binding = new DatabaseKeyBinding(identity.ProfileId, identity.InstallationId, reference.CandidateGeneration);
            var candidate = original + "." + binding.GenerationId.ToString("N") + ".cipher.db";
            var history = new SqliteProfileRestoreHistory(original + ".restore-history.sqlite", identity.InitialCandidateKey, owner.Validate);
            return new(captured, new SqliteDatabaseGenerationJournal(candidate + ".restore.sqlite", captured), candidate,
                binding, keys, durability, settings, ["localization.language"], _ => throw new IOException("Cold selected restore opened previous finance."),
                (_, _) => throw new IOException("Cold selected restore repeated upgrade."),
                _ => throw new IOException("Cold generation invoked old publication."), owner.Validate, 16 * 1024 * 1024, "4.19.0",
                _ => throw new IOException("Applied preferences were persisted again."), history);
        }
        var identityPath = Path.Combine(folder, "identities.sqlite");
        DatabaseProfileIdentity capturedIdentity;
        using (var identityOwner = await location.Access.AcquireExclusiveAsync())
            capturedIdentity = (await new SqliteDatabaseIdentityCatalogue(identityPath, identityOwner.Validate).ReadAsync()).Single();
        var profiles = new ProfileService(location, services.GetRequiredService<ZananceStore>(), settings, new Runtime(), Translator.Instance);
        var host = new ProtectedProfileStorage(location, () => profiles, identityPath, keys, durability,
            () => { if (!Directory.Exists(folder)) throw new IOException("Owned restored fixture disappeared."); },
            (path, reference, owner) =>
            {
                if (path != original) throw new IOException("Cold composed receiver belongs to another original.");
                return Receiver(reference, owner, capturedIdentity);
            }, 16 * 1024 * 1024, "4.19.0");
        await host.CreateStartup(ProtectedStorageInitialization.ExistingInstallation).PrepareAsync();
        using var maintenance = await location.Access.AcquireExclusiveAsync();
        await using (var db = await services.GetRequiredService<ILocalDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(maintenance))
            if ((await db.Accounts.SingleAsync()).OpeningBalance != 321 || settings.Get("localization.language") != "fr")
                throw new IOException("Cold restore changed finance or reapplied old user preferences.");
        host.Session.Close(maintenance);
        return "{\"ColdRestoredSession\":true,\"ExistingKeysOnly\":true,\"LaterPreferencesPreserved\":true,\"RestoredBalance\":321}";
    }
    internal static async Task<string> RunAsync(string folder, Func<string, IDatabaseGenerationKeyStore> createKeys,
        Func<string, IDatabaseGenerationFileDurability> createDurability)
    {
        var original = Path.Combine(folder, ZananceApp.DatabaseFileName);
        using var services = new ServiceCollection().AddZananceData(original).BuildServiceProvider();
        var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        var store = services.GetRequiredService<ZananceStore>();
        var settings = new InMemorySettingsStore(); settings.Set("localization.language", "en");
        settings.Set("device.fictitious.security", "keep");
        var profiles = new ProfileService(location, store, settings, new Runtime(), Translator.Instance);
        var identityPath = Path.Combine(folder, "identities.sqlite");
        var keys = createKeys(folder); var durability = createDurability(folder);
        SqlCipherPackageRestoreReceiver? receiver = null;
        var session = new SqlCipherProfileSession<ZananceDbContext>(location, async (path, owner, ct) =>
        {
            var identities = new SqliteDatabaseIdentityCatalogue(identityPath, owner.Validate);
            var identity = (await identities.ReadAsync(ct)).Single();
            var history = new SqliteProfileRestoreHistory(original + ".restore-history.sqlite", identity.InitialCandidateKey, owner.Validate);
            return await SqlCipherProfileGeneration.OpenCurrentAsync(location, identities, "", path, history,
                _ => receiver ?? throw new IOException("Unknown retained restore."), keys, durability, owner,
                () => { }, 16 * 1024 * 1024, "4.19.0", ct);
        });
        var startup = new ProtectedDatabaseStartup(location, profiles, session, identityPath,
            ProtectedStorageInitialization.ExistingInstallation, keys, durability,
            (_, _) => throw new IOException("Unexpected previous restore."), 16 * 1024 * 1024, "4.19.0");
        await startup.PrepareAsync();
        var sources = new IBackupSource[] { services.GetRequiredService<IBackupSource>(),
            new SettingsBackupSource(settings, "display-settings.json", ["localization.language"]) };
        var environment = new StaticAppEnvironment("pro.vafadar.test", "Test", new Version(1, 0), "Test", "Native");
        var portable = new BackupService(sources, environment, settings, TimeProvider.System, new BackupOptions());
        var package = await portable.CreatePackageAsync();
        var account = (await store.GetAccountsAsync()).Single(); var expected = account.OpeningBalance;
        account.OpeningBalance = 777; await store.SaveAccountAsync(account); settings.Set("localization.language", "de");
        using var maintenance = await location.Access.AcquireExclusiveAsync();
        var catalogue = new SqliteDatabaseIdentityCatalogue(identityPath, maintenance.Validate);
        var capturedIdentity = (await catalogue.ReadAsync()).Single();
        var references = new SqliteProfileRestoreHistory(original + ".restore-history.sqlite", capturedIdentity.InitialCandidateKey, maintenance.Validate);
        var latest = (await references.ReadAsync())[^1];
        var next = new DatabaseGenerationCheckpoint(capturedIdentity.ProfileId, latest.CandidateGeneration, Guid.NewGuid());
        var binding = new DatabaseKeyBinding(capturedIdentity.ProfileId, capturedIdentity.InstallationId, next.CandidateGeneration);
        var candidate = original + "." + binding.GenerationId.ToString("N") + ".cipher.db";
        var journalPath = candidate + ".restore.sqlite";
        var journal = new SqliteDatabaseGenerationJournal(journalPath, next);
        await references.ReserveAsync(next); await journal.CreateNewAsync();
        await durability.SynchronizeFileAsync(journalPath, CancellationToken.None);
        ZananceDbContext? borrowed = null; var persisted = 0; var notifications = 0;
        location.Changed += (_, _) => { notifications++; if (notifications == 1) throw new IOException("Fictitious restore observer failure."); };
        receiver = new(next, journal, candidate, binding, keys, durability, settings, ["localization.language"],
            async ct =>
            {
                borrowed = await services.GetRequiredService<ILocalDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(maintenance, ct);
                await borrowed.Database.OpenConnectionAsync(ct); return (SqliteConnection)borrowed.Database.GetDbConnection();
            },
            async (connection, ct) =>
            {
                var options = new DbContextOptionsBuilder<ZananceDbContext>().UseSqlite(connection, contextOwnsConnection: false)
                    .UseModel(ZananceDbContextModel.Instance).Options;
                await using var upgrade = new ZananceDbContext(options); await upgrade.Database.MigrateAsync(ct);
            },
            async ct =>
            {
                if (borrowed is not null)
                {
                    if (borrowed.Database.GetDbConnection().State != ConnectionState.Closed)
                        throw new IOException("Old registered restore connection was still open at publication.");
                    await borrowed.DisposeAsync(); borrowed = null;
                }
                await session.AdoptRestoredAsync(original, candidate, binding, maintenance, ct);
            }, maintenance.Validate, 16 * 1024 * 1024, "4.19.0",
            async ct =>
            {
                persisted++;
                var path = Path.Combine(folder, "fictitious-display.json");
                await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new[] { settings.Get("localization.language") }), ct);
                await durability.SynchronizeFileAsync(path, ct);
            }, references);
        var restore = new BackupService(sources, environment, settings, TimeProvider.System, new BackupOptions(), receiver);
        var failed = false;
        try { await restore.RestorePackageAsync(package); }
        catch (IOException) { failed = true; }
        finally { if (borrowed is not null) { await borrowed.DisposeAsync(); borrowed = null; } }
        if (!failed || location.Path != candidate || settings.Get("localization.language") != "en" || persisted != 1)
            throw new IOException("Selected restore did not retain its installed key and preferences after notification failure.");
        await using (var db = await services.GetRequiredService<ILocalDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(maintenance))
            if ((await db.Accounts.SingleAsync()).OpeningBalance != expected) throw new IOException("Pending restored key no longer opens actual finance.");
        try { session.FinishSwitch(true, maintenance); throw new IOException("Profile switch accepted a restore transition."); }
        catch (InvalidOperationException) { }
        await receiver.ResumeAsync();
        await session.AdoptRestoredAsync(original, candidate, binding, maintenance);
        if (persisted != 1 || notifications != 2 || settings.Get("device.fictitious.security") != "keep")
            throw new IOException("Restore repeated preference writes or changed device state.");
        var displayPath = Path.Combine(folder, "fictitious-display.json");
        settings.Set("localization.language", "fr");
        await File.WriteAllTextAsync(displayPath, JsonSerializer.Serialize(new[] { "fr" }));
        await durability.SynchronizeFileAsync(displayPath, CancellationToken.None);
        session.Close(maintenance);
        return "{\"ActualBackupReceiverAndSession\":true,\"SelectedRestoreRetainedAfterObserverFailure\":true,\"PreviousConnectionClosedBeforeAdoption\":true,\"NoRepeatedPreferencePersistence\":true,\"RestoredBalance\":" + expected + "}";
    }

    private sealed class Runtime : IProfileRuntime
    {
        public Task RunExclusiveAsync(Func<Task> action) => throw new IOException("Unexpected profile action.");
        public Task MigrateAsync(DatabaseAccessGate.ExclusiveLease owner) => throw new IOException("Unexpected migration shortcut.");
        public Task<bool> AuthenticateAsync(string reason) => throw new IOException("Unexpected authentication.");
        public Task ReloadLockAsync() => throw new IOException("Unexpected lock reload.");
        public void DismissUndo() => throw new IOException("Unexpected Undo dismissal.");
    }
    private sealed class ExistingKeys(IDatabaseGenerationKeyStore inner) : IDatabaseGenerationKeyStore
    {
        public Task PrepareAsync(DatabaseKeyBinding binding, CancellationToken ct) => throw new IOException("Cold selected restore prepared a replacement key.");
        public Task<byte[]> ReadAsync(DatabaseKeyBinding binding, CancellationToken ct) => inner.ReadAsync(binding, ct);
    }
}
