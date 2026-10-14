using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Storage;
using Vafadar.Backup;
using Vafadar.Core.Hosting;
using Vafadar.Core.Settings;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Maui.Hosting;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.App.Features.Backup;
using Vafadar.Zanance.App.Startup;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Data;

namespace Vafadar.SqlCipher.Compatibility.Android;

/// <summary>Checks actual composed receiver continuation before selection and after native preference persistence.</summary>
internal static class RetainedRestoreCompatibility
{
    internal static async Task<string> RunAsync(string folder, bool create, bool afterPreferences, bool validatedDelivery = false)
    {
        SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlcipher());
        if (create) { if (Directory.Exists(folder)) throw new IOException("Use a fresh retained restore fixture."); Directory.CreateDirectory(folder); }
        var original = Path.Combine(folder, "zanance.db");
        using var services = new ServiceCollection().AddZananceData(original, protectionRequired: true).BuildServiceProvider();
        var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        var settings = new ScopedSettings("native.retained." + Path.GetFileName(folder) + ".");
        var nativeKeys = new AndroidGenerationKeyStore(folder);
        var keys = new Keys(nativeKeys, create);
        var durability = new AndroidGenerationFileDurability(folder);
        var persistence = new Persistence(new AndroidSettingsPersistence(Preferences.Default));
        var profiles = new ProfileService(location, services.GetRequiredService<ZananceStore>(), settings, new Runtime(), Translator.Instance);
        var identityPath = Path.Combine(folder, "identities.sqlite");
        var host = new ProtectedProfileStorage(location, () => profiles, identityPath, keys, durability,
            () => { if (!Directory.Exists(folder)) throw new IOException("Owned retained fixture disappeared."); },
            settings, persistence, ["localization.language"], 16 * 1024 * 1024, "4.19.0");
        var startup = host.CreateStartup(create ? ProtectedStorageInitialization.NewInstallation : ProtectedStorageInitialization.ExistingInstallation);
        await startup.PrepareAsync();
        var store = services.GetRequiredService<ZananceStore>();
        if (create)
        {
            await store.SaveAccountAsync(new Account { Name = "Fictitious retained restore", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1), OpeningBalance = 321 });
            settings.Set("localization.language", "en"); settings.Set("device.fictitious.security", "keep");
            await persistence.PersistAsync();
            var sources = new IBackupSource[] { services.GetRequiredService<IBackupSource>(), new SettingsBackupSource(settings, "display-settings.json", ["localization.language"]) };
            var environment = new StaticAppEnvironment("pro.vafadar.test", "Test", new Version(1, 0), "Test", "Native");
            var package = await new BackupService(sources, environment, settings, TimeProvider.System, new BackupOptions()).CreatePackageAsync();
            try
            {
                var account = (await store.GetAccountsAsync()).Single(); account.OpeningBalance = 777; await store.SaveAccountAsync(account);
                settings.Set("localization.language", "de"); await persistence.PersistAsync();
                if (validatedDelivery)
                {
                    var adapter = new ProtectedBackupRestoreReceiver(location, host);
                    var restore = new BackupService(sources, environment, settings, TimeProvider.System, new BackupOptions(), adapter);
                    var damaged = package.ToArray(); damaged[0] ^= 1;
                    try
                    {
                        var rejected = false;
                        try { await restore.RestorePackageAsync(damaged); } catch (BackupException) { rejected = true; }
                        if (!rejected) throw new IOException("Malformed ZIP was accepted.");
                    }
                    finally { CryptographicOperations.ZeroMemory(damaged); }
                    foreach (var invalidPreferences in new[] { false, true })
                    {
                        var badSources = invalidPreferences
                            ? new IBackupSource[] { sources[0], new InvalidSource("display-settings.json", "[]"u8.ToArray()) }
                            : new IBackupSource[] { new InvalidSource("database.sqlite", "not a SQLite snapshot"u8.ToArray()) };
                        var bad = await new BackupService(badSources, environment, settings, TimeProvider.System, new BackupOptions()).CreatePackageAsync();
                        try
                        {
                            var rejected = false;
                            try { await restore.RestorePackageAsync(bad); }
                            catch (Exception error) when (error is IOException or InvalidDataException or System.Text.Json.JsonException or BackupException) { rejected = true; }
                            if (!rejected) throw new IOException("Malformed borrowed source was accepted.");
                        }
                        finally { CryptographicOperations.ZeroMemory(bad); }
                    }
                    DatabaseProfileIdentity validatedIdentity;
                    using (var inspected = await location.Access.AcquireExclusiveAsync())
                    {
                        validatedIdentity = (await new SqliteDatabaseIdentityCatalogue(identityPath, inspected.Validate).ReadAsync()).Single();
                        var unchanged = await new SqliteProfileRestoreHistory(original + ".restore-history.sqlite", validatedIdentity.InitialCandidateKey, inspected.Validate).ReadAsync();
                        if (unchanged.Count != 1 || keys.Preparations != 1 || settings.Get("localization.language") != "de")
                            throw new IOException("Rejected package created an intent, key or preference effect.");
                    }
                    if ((await store.GetAccountsAsync()).Single().OpeningBalance != 777) throw new IOException("Rejected package changed current finance.");
                    keys.RefuseRead = validatedIdentity.InitialCandidateKey;
                    var deliveryFailed = false;
                    try { await restore.RestorePackageAsync(package); } catch (IOException) { deliveryFailed = true; }
                    using var retained = await location.Access.AcquireExclusiveAsync();
                    var validatedHistory = await new SqliteProfileRestoreHistory(original + ".restore-history.sqlite", validatedIdentity.InitialCandidateKey, retained.Validate).ReadAsync();
                    if (!deliveryFailed || validatedHistory.Count != 2 || validatedHistory[^1].Selected || keys.Preparations != 2)
                        throw new IOException("Validated delivery did not retain its exact interrupted intent.");
                    host.Session.Close(retained);
                    return "{\"ActualComposedReceiver\":true,\"InterruptedBeforePublication\":true,\"ExactlyTwoPreparedKeys\":true,\"InvalidPackagesCreatedNoIntentOrKey\":true,\"CurrentFinancePreserved\":true}";
                }
                using var owner = await location.Access.AcquireExclusiveAsync();
                var identity = (await new SqliteDatabaseIdentityCatalogue(identityPath, owner.Validate).ReadAsync()).Single();
                var before = location.Path;
                var receiver = await host.PrepareRestoreAsync(owner);
                keys.RefuseRead = afterPreferences ? null : identity.InitialCandidateKey;
                persistence.Fail = afterPreferences;
                var failed = false;
                try { await new BackupService(sources, environment, settings, TimeProvider.System, new BackupOptions(), receiver).RestorePackageAsync(package); }
                catch (IOException) { failed = true; }
                if (!failed || location.Path != before) throw new IOException("Interrupted restore published financial access.");
                var history = new SqliteProfileRestoreHistory(original + ".restore-history.sqlite", identity.InitialCandidateKey, owner.Validate);
                var last = (await history.ReadAsync())[^1];
                var candidate = original + "." + last.CandidateGeneration.ToString("N") + ".cipher.db";
                var checkpoint = await new SqliteDatabaseGenerationJournal(candidate + ".restore.sqlite", new(identity.ProfileId, last.OriginalGeneration, last.CandidateGeneration)).ReadAsync();
                if (last.Selected || checkpoint.CandidateSelected != afterPreferences || keys.Preparations != 2)
                    throw new IOException("Interrupted checkpoint differs from its original key/selection authority.");
                host.Session.Close(owner);
                return "{\"ActualComposedReceiver\":true,\"InterruptedBeforePublication\":true,\"ExactlyTwoPreparedKeys\":true}";
            }
            finally { CryptographicOperations.ZeroMemory(package); }
        }
        if ((await store.GetAccountsAsync()).Single().OpeningBalance != 321 || settings.Get("localization.language") != "en"
            || settings.Get("device.fictitious.security") != "keep" || persistence.Writes != 1 || keys.Preparations != 0)
            throw new IOException("Cold retained restore changed financial, preference or key authority.");
        var writes = persistence.Writes;
        await startup.PrepareAsync();
        if (persistence.Writes != writes) throw new IOException("Applied preferences were persisted again.");
        using var closed = await location.Access.AcquireExclusiveAsync(); host.Session.Close(closed);
        return "{\"ActualComposedColdStartup\":true,\"ExistingKeysOnly\":true,\"NativePreferencesPersistedOnce\":true,\"DeviceChoicePreserved\":true,\"RestoredBalance\":321}";
    }

    private sealed class InvalidSource(string name, byte[] content) : IBackupSource
    {
        public string Name => name;
        public Task WriteAsync(Stream destination, CancellationToken ct) => destination.WriteAsync(content, ct).AsTask();
        public Task RestoreAsync(Stream source, CancellationToken ct) => throw new IOException("Rejected packages must never invoke a legacy source.");
    }
    private sealed class Keys(IDatabaseGenerationKeyStore inner, bool allowPreparation) : IDatabaseGenerationKeyStore
    {
        internal int Preparations;
        internal DatabaseKeyBinding? RefuseRead;
        public async Task PrepareAsync(DatabaseKeyBinding binding, CancellationToken ct)
        { if (!allowPreparation) throw new IOException("Cold retained restore cannot prepare keys."); Preparations++; await inner.PrepareAsync(binding, ct); }
        public Task<byte[]> ReadAsync(DatabaseKeyBinding binding, CancellationToken ct) => binding == RefuseRead
            ? throw new IOException("Injected interruption before previous-generation read.") : inner.ReadAsync(binding, ct);
    }
    private sealed class Persistence(ISettingsPersistence native) : ISettingsPersistence
    {
        internal int Writes; internal bool Fail;
        public async Task PersistAsync(CancellationToken ct = default)
        { await native.PersistAsync(ct); Writes++; if (Fail) throw new IOException("Injected interruption after native preference persistence."); }
    }
    private sealed class ScopedSettings(string prefix) : ISettingsStore
    {
        public string? Get(string key) => Preferences.Default.Get<string?>(prefix + key, null);
        public void Set(string key, string? value) { if (value is null) Remove(key); else Preferences.Default.Set(prefix + key, value); }
        public void Remove(string key) => Preferences.Default.Remove(prefix + key);
    }
    private sealed class Runtime : IProfileRuntime
    {
        public Task RunExclusiveAsync(Func<Task> action) => throw new IOException("Unexpected profile action.");
        public Task MigrateAsync(DatabaseAccessGate.ExclusiveLease owner) => throw new IOException("Unexpected migration.");
        public Task<bool> AuthenticateAsync(string reason) => throw new IOException("Unexpected authentication.");
        public Task ReloadLockAsync() => throw new IOException("Unexpected lock reload.");
        public void DismissUndo() => throw new IOException("Unexpected Undo effect.");
    }
}
