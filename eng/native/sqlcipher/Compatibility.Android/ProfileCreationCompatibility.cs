using Microsoft.Data.Sqlite;
using System.Security.Cryptography;
using System.Text.Json;
using Vafadar.SqlCipher.Compatibility;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Storage;
using Vafadar.Data;
using Vafadar.Core.Settings;
using Vafadar.Localization;
using Vafadar.Maui.Hosting;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.App.Startup;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.Core;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.CompiledModel;

namespace Vafadar.SqlCipher.Compatibility.Android;

/// <summary>Exercises actual asynchronous profile creation and switching with native encrypted files and retained catalogue preferences.</summary>
internal static class ProfileCreationCompatibility
{
    /// <summary>Prepares a new original through the product flow or verifies both originals after a cold restart.</summary>
    internal static async Task<string> RunAsync(string folder, bool create, bool retirement = false, bool creationResume = false, bool retirementRecovery = false, bool recoveredRetirement = false, bool allProfileRecovery = false)
    {
        if (create) { if (Directory.Exists(folder)) throw new IOException("Use a fresh profile-creation fixture."); Directory.CreateDirectory(folder); }
        var main = Path.Combine(folder, ZananceApp.DatabaseFileName);
        using var services = new ServiceCollection().AddZananceData(main).BuildServiceProvider();
        var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        IDatabaseGenerationKeyStore keys = new AndroidGenerationKeyStore(folder);
        if (!create && creationResume) keys = new ExistingKeys(keys);
        var durability = new AndroidGenerationFileDurability(folder);
        var preferences = new MauiSettingsStore(Preferences.Default);
        var identityPath = Path.Combine(folder, "identities.sqlite");
        SqliteRecoveryEnvelopeCatalogue? recoveryCatalogue = null;
        DatabaseRecoveryIdentity? recoveryIdentity = null;
        var runtime = new Runtime(services);
        var store = services.GetRequiredService<ZananceStore>();
        ProtectedProfileRetirementHost? retirementHost = null;
        var persistence = new RetirementPersistence(new AndroidSettingsPersistence(Preferences.Default), create && retirement);
        if (retirement)
        {
            var roots = new Dictionary<string, string> { ["database"] = folder, ["keys"] = folder,
                ["portable"] = Path.Combine(folder, "retirement-portable"), ["safety"] = Path.Combine(folder, "retirement-safety") };
            if (create) { Directory.CreateDirectory(roots["portable"]); Directory.CreateDirectory(roots["safety"]); }
            var files = new AndroidProfileRetirementFiles(roots, () => { });
            retirementHost = new(folder, identityPath, persistence, keys, durability, files,
                (state, journal, ct) => files.RemoveAsync(state, journal, durability, ct), (journal, ct) => files.VerifyRemovedAsync(journal, ct));
        }
        ProfileService profiles = null!;
        var creationHost = creationResume ? new ProtectedProfileCreationHost(new AndroidSettingsPersistence(Preferences.Default), PrepareNew) : null;
        profiles = new ProfileService(location, store, preferences, runtime, Translator.Instance, profileRetirement: retirementHost, profileCreation: creationHost);
        async Task BuildSchema(SqliteConnection connection, CancellationToken ct)
        {
            var options = new DbContextOptionsBuilder<ZananceDbContext>().UseSqlite(connection, contextOwnsConnection: false).UseModel(ZananceDbContextModel.Instance).Options;
            await using var db = new ZananceDbContext(options); await db.Database.MigrateAsync(ct);
        }
        async Task<SqlCipherProfileGeneration> Resolve(string original, DatabaseAccessGate.ExclusiveLease owner, CancellationToken ct)
        {
            var row = profiles.All.Single(profile => (profile.IsMain ? main : Path.Combine(folder, "zanance-" + profile.Id + ".db")) == original);
            return await SqlCipherNewProfile.OpenAsync(location, new(identityPath, owner.Validate), row.Id, original, false,
                keys, durability, BuildSchema, owner, () => { }, 16 * 1024 * 1024, "4.19.0", ct);
        }
        async Task PrepareNew(string id, string original, DatabaseAccessGate.ExclusiveLease owner, CancellationToken ct)
        {
            if (!profiles.Current.IsMain || profiles.All.Any(p => !p.IsMain && p.Id != id)) throw new IOException("Preparation differs from original catalogue.");
            var identities = new SqliteDatabaseIdentityCatalogue(identityPath, owner.Validate);
            await identities.RegisterAsync(id, ct); await durability.SynchronizeFileAsync(identityPath, ct); await durability.SynchronizeDirectoryAsync(ct);
            using var privateServices = new ServiceCollection().AddZananceData(original).BuildServiceProvider();
            var privateLocation = privateServices.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
            using var privateOwner = await privateLocation.Access.AcquireExclusiveAsync(ct);
            using var prepared = await SqlCipherNewProfile.OpenAsync(privateLocation, identities, id, original, true, keys, durability,
                BuildSchema, privateOwner, owner.Validate, 16 * 1024 * 1024, "4.19.0", ct);
            if (location.Path == prepared.Path || File.Exists(original)) throw new IOException("New profile changed active publication or created plaintext.");
            if (create && creationResume) throw new IOException("Injected interruption after native profile preparation.");
        }
        runtime.Session = new SqlCipherProfileSession<ZananceDbContext>(location, Resolve, PrepareNew);
        using (var owner = await location.Access.AcquireExclusiveAsync())
        {
            if (create)
            {
                preferences.Remove("profiles.work.catalogue"); preferences.Remove("profiles.list"); preferences.Remove("profiles.current"); preferences.Remove("profiles.main.name"); preferences.Remove("profiles.creation.pending");
                var identities = new SqliteDatabaseIdentityCatalogue(identityPath, owner.Validate); await identities.CreateNewAsync([""], durability);
                using var prepared = await SqlCipherNewProfile.OpenAsync(location, identities, "", main, true, keys, durability,
                    BuildSchema, owner, () => { }, 16 * 1024 * 1024, "4.19.0");
            }
            if (retirementRecovery)
            {
                var installation = (await new SqliteDatabaseIdentityCatalogue(identityPath, owner.Validate).ReadAsync()).Single(row => row.LegacyProfileId.Length == 0);
                var publicPath = Path.Combine(folder, "recovery-public.json");
                if (create)
                {
                    using var material = DatabaseRecoveryMaterial.Create(installation.InstallationId);
                    recoveryIdentity = material.Identity;
                    var secret = Enumerable.Range(1, 32).Select(value => (byte)value).ToArray();
                    try { await new SqlCipherRecoveryVault(Path.Combine(folder, "recovery-private.cipher.db"), recoveryIdentity, "4.19.0", durability, owner.Validate).CreateNewAsync(material, secret); }
                    finally { CryptographicOperations.ZeroMemory(secret); }
                    await File.WriteAllTextAsync(publicPath, JsonSerializer.Serialize(new[] { recoveryIdentity.InstallationId.ToString("N"), recoveryIdentity.RecoveryId.ToString("N"), Convert.ToBase64String(recoveryIdentity.ExportPublicKey()) }, RetirementJsonContext.Default.StringArray));
                    await durability.SynchronizeFileAsync(publicPath, default); await durability.SynchronizeDirectoryAsync(default);
                }
                else
                {
                    var values = JsonSerializer.Deserialize(await File.ReadAllTextAsync(publicPath), RetirementJsonContext.Default.StringArray) ?? throw new IOException("Missing fixture recovery public identity.");
                    if (values.Length != 3 || Guid.ParseExact(values[0], "N") != installation.InstallationId) throw new IOException("Recovery installation differs.");
                    recoveryIdentity = new(installation.InstallationId, Guid.ParseExact(values[1], "N"), Convert.FromBase64String(values[2]));
                }
                recoveryCatalogue = new(Path.Combine(folder, "recovery-envelopes.sqlite"), recoveryIdentity, durability, () => { });
                if (create)
                {
                    await recoveryCatalogue.CreateNewAsync();
                    var key = await keys.ReadAsync(installation.InitialCandidateKey, default);
                    try { await recoveryCatalogue.CaptureAsync(installation.InitialCandidateKey, key); }
                    finally { CryptographicOperations.ZeroMemory(key); }
                }
                keys = new RecoveryGenerationKeyStore(keys, recoveryCatalogue);
                var roots = new Dictionary<string, string> { ["database"] = folder, ["keys"] = folder, ["portable"] = Path.Combine(folder, "retirement-portable"), ["safety"] = Path.Combine(folder, "retirement-safety") };
                var nativeFiles = new AndroidProfileRetirementFiles(roots, () => { });
                retirementHost = new(folder, identityPath, persistence, keys, durability, nativeFiles,
                    (state, journal, ct) => nativeFiles.RemoveAsync(state, journal, durability, ct), (journal, ct) => nativeFiles.VerifyRemovedAsync(journal, ct), recoveryCatalogue);
                profiles = new(location, store, preferences, runtime, Translator.Instance, profileRetirement: retirementHost);
            }
            if (!create && allProfileRecovery)
            {
                var recoveryService = new ProtectedProfileRecovery(location, profiles, identityPath,
                    (secret, ct) => new SqlCipherRecoveryVault(Path.Combine(folder, "recovery-private.cipher.db"), recoveryIdentity!, "4.19.0", durability, () => { }).OpenAsync(secret, ct),
                    recoveryCatalogue!, (IDatabaseGenerationKeyRecovery)keys,
                    async (binding, original, candidate, capturedOwner, ct) =>
                    {
                        var identities = new SqliteDatabaseIdentityCatalogue(identityPath, capturedOwner.Validate);
                        var profile = (await identities.ReadAsync(ct)).Single(row => row.ProfileId == binding.ProfileId);
                        var privateLocation = new LocalDatabaseLocation<ZananceDbContext>(original);
                        using var privateOwner = await privateLocation.Access.AcquireExclusiveAsync(ct);
                        using var verified = await SqlCipherNewProfile.OpenAsync(privateLocation, identities, profile.LegacyProfileId, original, false,
                            new VerifiedKeys(binding, candidate), durability, (_, _) => throw new IOException("Recovery cannot create replacement schema."),
                            privateOwner, capturedOwner.Validate, 16 * 1024 * 1024, "4.19.0", ct);
                        if (verified.Binding != binding) throw new IOException("Recovery verification differs from captured binding.");
                    });
                var secret = Enumerable.Range(1, 32).Select(value => (byte)value).ToArray();
                try { await recoveryService.RecoverAsync(secret, owner); }
                finally { CryptographicOperations.ZeroMemory(secret); }
            }
            if (!create && retirement && !allProfileRecovery) await retirementHost!.ResumePendingAsync(profiles, location, owner, CancellationToken.None);
            if ((create || !creationResume) && !(!create && allProfileRecovery)) await runtime.Session.StartAsync(main, owner);
        }
        if (!create && allProfileRecovery)
        {
            await new ProtectedDatabaseStartup(location, profiles, runtime.Session, identityPath, ProtectedStorageInitialization.ExistingInstallation,
                keys, durability, (_, _) => throw new IOException("Fresh recovery fixture has no later restore."), 16 * 1024 * 1024, "4.19.0").PrepareAsync();
        }
        if (!create && creationResume)
        {
            var startup = new ProtectedDatabaseStartup(location, profiles, runtime.Session, identityPath,
                ProtectedStorageInitialization.ExistingInstallation, keys, durability,
                (_, _) => throw new IOException("Fresh creation cannot use a later restore receiver."),
                16 * 1024 * 1024, "4.19.0", creation: creationHost);
            await startup.PrepareAsync();
            await startup.PrepareAsync(); // The completed callback must not repeat preparation or key publication.
        }
        if (create)
        {
            await store.SaveAccountAsync(new Account { Name = "Fictitious retained main", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1), OpeningBalance = 250 });
            if (creationResume)
            {
                try { await profiles.CreateReviewedAsync(profiles.ReadWorkReview(), "Fictitious resumed work"); throw new IOException("Expected native interruption did not occur."); }
                catch (IOException error) when (error.Message == "Injected interruption after native profile preparation.") { }
                using var retainedOwner = await location.Access.AcquireExclusiveAsync();
                var request = profiles.ReadCreation(retainedOwner) ?? throw new IOException("Creation intent was lost.");
                var target = request.Split('|')[2];
                var rows = await new SqliteDatabaseIdentityCatalogue(identityPath, retainedOwner.Validate).ReadAsync();
                if (profiles.All.Count != 1 || rows.Count != 2 || !rows.Any(row => row.LegacyProfileId == target)) throw new IOException("Unpublished creation identity differs.");
                runtime.Session.Close(retainedOwner);
                return "{\"CreationIntentRetained\":true,\"PreparedProfileUnpublished\":true,\"MainBalance\":250}";
            }
            var endpoint = location.Capture();
            var work = await profiles.CreateReviewedAsync(profiles.ReadWorkReview(), "Fictitious prepared work");
            if (location.Capture() != endpoint || !profiles.Current.IsMain || profiles.All.Count != 2) throw new IOException("Creation changed active finance context.");
            if (!await profiles.SwitchToAsync(work) || (await store.GetAccountsAsync()).Count != 0) throw new IOException("New profile was not empty.");
            await store.SaveAccountAsync(new Account { Name = "Fictitious retained work", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1), OpeningBalance = 880 });
            if (recoveredRetirement)
            {
                using var recoveryOwner = await location.Access.AcquireExclusiveAsync();
                var catalogue = new SqliteDatabaseIdentityCatalogue(identityPath, recoveryOwner.Validate);
                var target = (await catalogue.ReadAsync()).Single(row => row.LegacyProfileId == work.Id);
                var key = await keys.ReadAsync(target.InitialCandidateKey, default);
                try
                {
                    await ((IDatabaseGenerationKeyRecovery)keys).ImportRecoveredAsync(target.InitialCandidateKey, key, async (candidate, ct) =>
                    {
                        using var verified = await SqlCipherNewProfile.OpenAsync(location, catalogue, work.Id,
                            Path.Combine(folder, "zanance-" + work.Id + ".db"), false, new VerifiedKeys(target.InitialCandidateKey, candidate), durability,
                            BuildSchema, recoveryOwner, recoveryOwner.Validate, 16 * 1024 * 1024, "4.19.0", ct);
                        if (verified.Binding != target.InitialCandidateKey) throw new IOException("Verified retirement recovery binding differs.");
                    }, default);
                }
                finally { CryptographicOperations.ZeroMemory(key); }
            }
            if (!await profiles.SwitchToAsync(profiles.All.Single(p => p.IsMain))) throw new IOException("Return to original failed.");
            await new AndroidSettingsPersistence(Preferences.Default).PersistAsync();
            if (retirement && !allProfileRecovery)
            {
                var refused = false;
                try { await profiles.DeleteAsync(work); }
                catch (IOException error) when (error.Message == RetirementPersistence.Failure) { refused = true; }
                if (!refused || profiles.All.Count != 1 || !File.Exists(Path.Combine(folder, "zanance-" + work.Id + ".db") + ".restore-history.sqlite"))
                    throw new IOException("Native retirement did not retain files after catalogue persistence failure.");
            }
        }
        if (allProfileRecovery)
        {
            if ((await store.GetAccountsAsync()).Single().OpeningBalance != 250 || profiles.All.Count != 2 || !profiles.Current.IsMain)
                throw new IOException("All-profile recovery changed main finance/catalogue.");
            if (!create)
            {
                if (!await profiles.SwitchToAsync(profiles.All.Single(p => !p.IsMain)) || (await store.GetAccountsAsync()).Single().OpeningBalance != 880)
                    throw new IOException("Inactive profile recovery lost original money.");
                if (!await profiles.SwitchToAsync(profiles.All.Single(p => p.IsMain))) throw new IOException("Return to recovered main failed.");
            }
            using var recoveryOwner = await location.Access.AcquireExclusiveAsync();
            runtime.Session.Close(recoveryOwner);
            if (create)
            {
                foreach (var row in await new SqliteDatabaseIdentityCatalogue(identityPath, recoveryOwner.Validate).ReadAsync())
                    File.Delete(Path.Combine(folder, $"{row.ProfileId:N}-{row.CandidateGeneration:N}.key"));
                await durability.SynchronizeDirectoryAsync(default);
                return "{\"ActualTwoProfileFixture\":true,\"BothDeviceEnvelopesMissing\":true}";
            }
            return "{\"AllProfileRecoveryVerifiedBeforeStartup\":true,\"ActualCoveredStartup\":true,\"MainBalance\":250,\"WorkBalance\":880,\"NoExtraProfiles\":true}";
        }
        if ((await store.GetAccountsAsync()).Single().OpeningBalance != 250) throw new IOException("Main financial packet changed.");
        if (!create && creationResume)
        {
            var resumed = profiles.All.Single(profile => !profile.IsMain);
            if (resumed.Name != "Fictitious resumed work" || !await profiles.SwitchToAsync(resumed) || (await store.GetAccountsAsync()).Count != 0) throw new IOException("Resumed profile was not empty.");
            if (!await profiles.SwitchToAsync(profiles.All.Single(p => p.IsMain))) throw new IOException("Return to main failed.");
            using var resumedOwner = await location.Access.AcquireExclusiveAsync();
            if (profiles.ReadCreation(resumedOwner) is not null || (await new SqliteDatabaseIdentityCatalogue(identityPath, resumedOwner.Validate).ReadAsync()).Count != 2) throw new IOException("Creation completion or identity count differs.");
            runtime.Session.Close(resumedOwner);
            return "{\"ActualCoveredStartup\":true,\"ColdCreationResumed\":true,\"ExactlyTwoIdentities\":true,\"NewProfileEmpty\":true,\"MainBalance\":250}";
        }
        if (!create && !retirement)
        {
            if (!await profiles.SwitchToAsync(profiles.All.Single(p => !p.IsMain)) || (await store.GetAccountsAsync()).Single().OpeningBalance != 880)
                throw new IOException("Cold new-profile switch lost financial data.");
        }
        using (var owner = await location.Access.AcquireExclusiveAsync()) runtime.Session.Close(owner);
        if (retirement)
        {
            if (!profiles.Current.IsMain || profiles.All.Count != 1) throw new IOException("Retirement changed main catalogue selection.");
            var rows = await new SqliteDatabaseIdentityCatalogue(identityPath, () => { }).ReadAsync();
            if (rows.Count != (create ? 2 : 1)) throw new IOException("Retirement identity barrier differs from actual completion.");
            if (!create && retirementRecovery)
            {
                var mainIdentity = rows.Single();
                var secret = Enumerable.Range(1, 32).Select(value => (byte)value).ToArray();
                try
                {
                    using var unlocked = await new SqlCipherRecoveryVault(Path.Combine(folder, "recovery-private.cipher.db"), recoveryIdentity!, "4.19.0", durability, () => { }).OpenAsync(secret);
                    var key = await recoveryCatalogue!.RecoverAsync(mainIdentity.InitialCandidateKey, unlocked);
                    CryptographicOperations.ZeroMemory(key);
                }
                finally { CryptographicOperations.ZeroMemory(secret); }
                return "{\"ColdCatalogueRetirementResumed\":true,\"OnlyMainIdentityRemains\":true,\"MainBalance\":250,\"ActualNativeSettings\":true,\"RecoveryMappingsRetired\":true,\"MainRecoveryPreserved\":true}";
            }
            return create ? "{\"ActualCataloguePersisted\":true,\"NativeFailureRetainedProfileFiles\":true,\"MainBalance\":250}"
                : "{\"ColdCatalogueRetirementResumed\":true,\"OnlyMainIdentityRemains\":true,\"MainBalance\":250,\"ActualNativeSettings\":true}";
        }
        return "{\"CatalogueAfterEncryptedPreparation\":true,\"ActiveProfileUnchangedDuringCreation\":true,\"RealSwitchAndFinancialPacketsPreserved\":true}";
    }

    private sealed class VerifiedKeys(DatabaseKeyBinding expected, ReadOnlyMemory<byte> key) : IDatabaseGenerationKeyStore
    {
        public Task PrepareAsync(DatabaseKeyBinding binding, CancellationToken ct) => throw new IOException("Verification cannot prepare keys.");
        public Task<byte[]> ReadAsync(DatabaseKeyBinding binding, CancellationToken ct) => binding == expected ? Task.FromResult(key.ToArray()) : throw new IOException("Verification belongs to another generation.");
    }

    private sealed class ExistingKeys(IDatabaseGenerationKeyStore inner) : IDatabaseGenerationKeyStore
    {
        public Task PrepareAsync(DatabaseKeyBinding binding, CancellationToken ct) => throw new IOException("Cold creation tried preparing a replacement key.");
        public Task<byte[]> ReadAsync(DatabaseKeyBinding binding, CancellationToken ct) => inner.ReadAsync(binding, ct);
    }

    private sealed class RetirementPersistence(ISettingsPersistence native, bool fail) : ISettingsPersistence
    {
        internal const string Failure = "Injected failure after actual native catalogue persistence.";
        public async Task PersistAsync(CancellationToken cancellationToken = default)
        { await native.PersistAsync(cancellationToken); if (fail) throw new IOException(Failure); }
    }

    private sealed class Runtime(IServiceProvider services) : IProfileRuntime
    {
        internal SqlCipherProfileSession<ZananceDbContext> Session = null!;
        /// <inheritdoc />
        public Task RunExclusiveAsync(Func<Task> action) => services.GetRequiredService<AutoPostProcessor>().RunExclusiveAsync(action);
        /// <inheritdoc />
        public Task MigrateAsync(DatabaseAccessGate.ExclusiveLease owner) => services.MigrateLocalDatabaseForMaintenanceAsync<ZananceDbContext>(owner);
        /// <inheritdoc />
        public Task PrepareNewDatabaseAsync(LocalDatabaseLocation<ZananceDbContext> location, string id, string original, DatabaseAccessGate.ExclusiveLease owner, CancellationToken ct) => Session.PrepareNewAsync(id, original, owner, ct);
        /// <inheritdoc />
        public async Task OpenDatabaseAsync(LocalDatabaseLocation<ZananceDbContext> location, string original, DatabaseAccessGate.ExclusiveLease owner) { await Session.BeginSwitchAsync(original, owner); await MigrateAsync(owner); }
        /// <inheritdoc />
        public void FinishDatabaseTransition(bool accepted, DatabaseAccessGate.ExclusiveLease owner) => Session.FinishSwitch(accepted, owner);
        /// <inheritdoc />
        public Task<bool> AuthenticateAsync(string reason) => throw new IOException("Fictitious unlocked profiles must not request authentication.");
        /// <inheritdoc />
        public Task ReloadLockAsync() => Task.CompletedTask;
        /// <inheritdoc />
        public void DismissUndo() { }
    }
}
