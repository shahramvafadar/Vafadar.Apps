using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Storage;
using System.Security.Cryptography;
using System.Text.Json;
using Vafadar.Backup;
using Vafadar.Core.Hosting;
using Vafadar.Core.Settings;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Maui.Hosting;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.App.Startup;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.CompiledModel;

namespace Vafadar.SqlCipher.Compatibility.Android;

/// <summary>Runs actual portable package restore, retained ancestry and cold native recovery on independent financial data.</summary>
internal static class RecoveryRestoreChainCompatibility
{
    private static readonly string[] PreferenceKeys = ["fixture.restore.culture"];
    private const int Bound = 16 * 1024 * 1024;

    /// <summary>Builds two real restores or recovers/resumes the same interrupted second restore after process restart.</summary>
    internal static async Task<string> RunAsync(string folder, bool create, bool repairFixtureCatalogue = false)
    {
        SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlcipher());
        if (create) { if (Directory.Exists(folder)) throw new IOException("Use a fresh restore-chain fixture."); Directory.CreateDirectory(folder); }
        var original = Path.Combine(folder, "zanance.db");
        using var services = new ServiceCollection().AddZananceData(original).BuildServiceProvider();
        var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        var settings = new MauiSettingsStore(Preferences.Default); var persistence = new AndroidSettingsPersistence(Preferences.Default);
        using var owner = await location.Access.AcquireExclusiveAsync();
        var durability = new AndroidGenerationFileDurability(folder); var native = new AndroidGenerationKeyStore(folder);
        var identities = new SqliteDatabaseIdentityCatalogue(Path.Combine(folder, "identities.sqlite"), owner.Validate);
        if (create)
        {
            settings.Remove("profiles.work.catalogue"); settings.Remove("profiles.list"); settings.Remove("profiles.current"); settings.Remove("profiles.main.name");
            await identities.CreateNewAsync([""], durability); settings.Set(PreferenceKeys[0], "en-GB");
        }
        var initial = (await identities.ReadAsync()).Single().InitialCandidateKey;
        var secret = Enumerable.Range(1, 32).Select(value => (byte)value).ToArray(); // Independent fictitious fixture only.
        try
        {
            var publicPath = Path.Combine(folder, "recovery-public.json"); DatabaseRecoveryIdentity recoveryIdentity;
            if (create)
            {
                using var material = DatabaseRecoveryMaterial.Create(initial.InstallationId); recoveryIdentity = material.Identity;
                await File.WriteAllTextAsync(publicPath, JsonSerializer.Serialize(new[] { recoveryIdentity.InstallationId.ToString("N"), recoveryIdentity.RecoveryId.ToString("N"), Convert.ToBase64String(recoveryIdentity.ExportPublicKey()) }, RetirementJsonContext.Default.StringArray));
                await new SqlCipherRecoveryVault(Path.Combine(folder, "recovery-private.cipher.db"), recoveryIdentity, "4.19.0", durability, owner.Validate).CreateNewAsync(material, secret);
            }
            else
            {
                var values = JsonSerializer.Deserialize(await File.ReadAllTextAsync(publicPath), RetirementJsonContext.Default.StringArray)!;
                recoveryIdentity = new(Guid.ParseExact(values[0], "N"), Guid.ParseExact(values[1], "N"), Convert.FromBase64String(values[2]));
            }
            var vault = new SqlCipherRecoveryVault(Path.Combine(folder, "recovery-private.cipher.db"), recoveryIdentity, "4.19.0", durability, owner.Validate);
            var catalogue = new SqliteRecoveryEnvelopeCatalogue(Path.Combine(folder, "recovery-envelopes.sqlite"), recoveryIdentity, durability, owner.Validate);
            if (create) await catalogue.CreateNewAsync();
            var keys = new RecoveryGenerationKeyStore(native, catalogue);
            var roots = new Dictionary<string, string> { ["database"] = folder, ["keys"] = folder, ["safety"] = Path.Combine(folder, "safety"), ["portable"] = Path.Combine(folder, "portable") };
            if (create) { Directory.CreateDirectory(roots["safety"]); Directory.CreateDirectory(roots["portable"]); }
            var verifier = new ProfileRecoveryReferenceVerifier(identities, new AndroidProfileRetirementFiles(roots, owner.Validate), durability, PreferenceKeys, "4.19.0", Bound, owner.Validate);
            var history = new SqliteProfileRestoreHistory(original + ".restore-history.sqlite", initial, owner.Validate);
            SqlCipherProfileGeneration? active = null;
            try
            {
                if (create)
                {
                    active = await SqlCipherNewProfile.OpenAsync(location, identities, "", original, true, keys, durability, BuildSchema, owner, owner.Validate, Bound, "4.19.0");
                    active.Publish(location, owner);
                    await using (var db = await services.GetRequiredService<ILocalDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(owner))
                    { db.Accounts.Add(Account(250)); await db.SaveChangesAsync(); }
                    for (var index = 1; index <= 2; index++)
                    {
                        var previous = (await history.ReadAsync())[^1]; var target = new DatabaseKeyBinding(initial.ProfileId, initial.InstallationId, Guid.NewGuid());
                        var intent = new DatabaseGenerationCheckpoint(initial.ProfileId, previous.CandidateGeneration, target.GenerationId);
                        var path = Candidate(target.GenerationId); var journal = new SqliteDatabaseGenerationJournal(path + ".restore.sqlite", intent);
                        await journal.CreateNewAsync(durability); await history.ReserveAsync(intent);
                        await using var source = new SqliteConnection("Data Source=:memory:;Pooling=False"); await source.OpenAsync(); await BuildSchema(source, default);
                        await using (var db = new ZananceDbContext(Options(source))) { db.Accounts.Add(Account(index == 1 ? 730 : 990)); await db.SaveChangesAsync(); }
                        var imported = new InMemorySettingsStore(); imported.Set(PreferenceKeys[0], index == 1 ? "de-DE" : "fa-IR");
                        var environment = new StaticAppEnvironment("pro.vafadar.zanance", "Zanance", new Version(1, 0), "Independent fixture", "Fixture");
                        var producer = new BackupService([new Source(source), new SettingsBackupSource(imported, "display-settings.json", PreferenceKeys)], environment, imported, TimeProvider.System, new());
                        var package = await producer.CreatePackageAsync();
                        await using var current = await services.GetRequiredService<ILocalDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(owner);
                        await current.Database.OpenConnectionAsync(); var fail = index == 2;
                        var receiver = Receiver(intent, target, _ => Task.FromResult((SqliteConnection)current.Database.GetDbConnection()), async ct =>
                        { await persistence.PersistAsync(ct); if (fail) throw new InterruptedPersistence(); });
                        try
                        {
                            var consumer = new BackupService([], [], environment, settings, TimeProvider.System, new(), receiver);
                            try { await consumer.RestorePackageAsync(package); if (fail) throw new IOException("Expected native persistence interruption."); }
                            catch (InterruptedPersistence) when (fail) { }
                        }
                        finally { CryptographicOperations.ZeroMemory(package); }
                        if (fail)
                        {
                            if ((await history.ReadAsync())[^1].Selected || (await journal.ReadAsync()).Phase != DatabaseGenerationPhase.Selected)
                                throw new IOException("Interrupted restore advanced history or lost selection.");
                            break;
                        }
                        await current.DisposeAsync();
                        var next = await SqlCipherProfileGeneration.OpenCurrentAsync(location, identities, "", original, history, _ => receiver,
                            keys, durability, owner, owner.Validate, Bound, "4.19.0");
                        next.Publish(location, owner); active.Dispose(); active = next;
                    }
                    foreach (var binding in await catalogue.ReadBindingsAsync()) File.Delete(Path.Combine(folder, $"{binding.ProfileId:N}-{binding.GenerationId:N}.key"));
                    await durability.SynchronizeDirectoryAsync(default);
                    return "{\"TwoActualPortableRestores\":true,\"SecondJournalSelectedHistoryPending\":true,\"ThreeDeviceEnvelopesRemoved\":true}";
                }
                var profiles = new ProfileService(location, services.GetRequiredService<ZananceStore>(), settings, new NoTransitions(), Translator.Instance);
                if (repairFixtureCatalogue)
                {
                    // The original fixture used the wrong preference name. Repair only known fictitious
                    // catalogue rows in this independent package; preserve the existing three generations.
                    if ((await identities.ReadAsync()).Count != 1 || (await history.ReadAsync()).Count != 3
                        || profiles.All.Any(profile => !profile.IsMain && !(profile.Name?.StartsWith("Fictitious", StringComparison.Ordinal) ?? false)))
                        throw new IOException("Recorded fixture catalogue repair has no authority.");
                    settings.Remove("profiles.work.catalogue"); await persistence.PersistAsync(default);
                }
                await new ProtectedProfileRecovery(location, profiles, Path.Combine(folder, "identities.sqlite"), (input, ct) => vault.OpenAsync(input, ct), catalogue, keys,
                    (binding, path, key, lease, ct) => verifier.VerifyAsync(binding, path, key, ct)).RecoverAsync(secret, owner);
                var latest = (await history.ReadAsync())[^1];
                var captured = new DatabaseGenerationCheckpoint(initial.ProfileId, latest.OriginalGeneration, latest.CandidateGeneration);
                var bindingLatest = new DatabaseKeyBinding(initial.ProfileId, initial.InstallationId, latest.CandidateGeneration);
                var resume = Receiver(captured, bindingLatest, _ => throw new IOException("Selected restart cannot access an old connection."), persistence.PersistAsync);
                active = await SqlCipherProfileGeneration.OpenCurrentAsync(location, identities, "", original, history, _ => resume,
                    keys, durability, owner, owner.Validate, Bound, "4.19.0"); active.Publish(location, owner);
                await using (var db = await services.GetRequiredService<ILocalDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(owner))
                    if ((await db.Accounts.SingleAsync()).OpeningBalance != 990) throw new IOException("Cold latest restored finance differs.");
                var references = await history.ReadAsync();
                if (references.Count != 3 || references.Any(row => !row.Selected) || settings.Get(PreferenceKeys[0]) != "fa-IR") throw new IOException("Cold ancestry or actual preference replay differs.");
                foreach (var row in references)
                {
                    var binding = new DatabaseKeyBinding(initial.ProfileId, initial.InstallationId, row.CandidateGeneration);
                    var key = await keys.ReadAsync(binding, default);
                    try
                    {
                        await verifier.VerifyAsync(binding, original, key);
                        foreach (var packet in row.Sequence == 0 ? new[] { (Candidate(row.CandidateGeneration), 250L) }
                            : new[] { (Candidate(row.CandidateGeneration), row.Sequence == 1 ? 730L : 990L), (Candidate(row.CandidateGeneration) + ".before-restore.cipher.db", row.Sequence == 1 ? 250L : 730L) })
                        {
                            await using var retained = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = packet.Item1, Password = Convert.ToBase64String(key), Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString());
                            await retained.OpenAsync(); await using var db = new ZananceDbContext(Options(retained));
                            if ((await db.Accounts.SingleAsync()).OpeningBalance != packet.Item2) throw new IOException("Retained financial ancestry differs.");
                        }
                    }
                    finally { CryptographicOperations.ZeroMemory(key); }
                }
                return "{\"ThreeRecoveredGenerationsVerified\":true,\"InterruptedSecondRestoreResumed\":true,\"LatestBalance\":990,\"ActualNativePreferencesReplayed\":true,\"AllRollbackAndCapsuleReferencesRetained\":true}";
            }
            finally { active?.Dispose(); }

            string Candidate(Guid generation) => original + "." + generation.ToString("N") + ".cipher.db";
            SqlCipherPackageRestoreReceiver Receiver(DatabaseGenerationCheckpoint intent, DatabaseKeyBinding binding,
                Func<CancellationToken, Task<SqliteConnection>> open, Func<CancellationToken, Task> persist) =>
                new(intent, new(Candidate(binding.GenerationId) + ".restore.sqlite", intent), Candidate(binding.GenerationId), binding, keys, durability,
                    settings, PreferenceKeys, open, BuildSchema, _ => Task.CompletedTask, owner.Validate, Bound, "4.19.0", persist, history);
        }
        finally { CryptographicOperations.ZeroMemory(secret); }
    }

    private static DbContextOptions<ZananceDbContext> Options(SqliteConnection connection) => new DbContextOptionsBuilder<ZananceDbContext>()
        .UseSqlite(connection, contextOwnsConnection: false).UseModel(ZananceDbContextModel.Instance).Options;
    private static async Task BuildSchema(SqliteConnection connection, CancellationToken ct)
    { await using var db = new ZananceDbContext(Options(connection)); await db.Database.MigrateAsync(ct); }
    private static Account Account(long amount) => new() { Name = "Fictitious restore chain", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1), OpeningBalance = amount };
    private sealed class InterruptedPersistence : Exception;
    private sealed class Source(SqliteConnection connection) : IBackupSource
    {
        public string Name => "database.sqlite";
        public Task WriteAsync(Stream target, CancellationToken ct) => SqlCipherPortableSnapshot.WriteAsync(connection, target, Bound, ct);
        public Task RestoreAsync(Stream source, CancellationToken ct) => throw new IOException("Source-only fixture port.");
    }
    private sealed class NoTransitions : IProfileRuntime
    {
        public Task RunExclusiveAsync(Func<Task> action) => throw new IOException("No profile transition during recovery capture.");
        public Task MigrateAsync(DatabaseAccessGate.ExclusiveLease owner) => throw new IOException("No profile migration during recovery capture.");
        public Task<bool> AuthenticateAsync(string reason) => throw new IOException("No profile authentication in the captured fixture.");
        public Task ReloadLockAsync() => throw new IOException("No lock publication in the captured fixture.");
        public void DismissUndo() => throw new IOException("No financial UI publication in the captured fixture.");
    }
}
