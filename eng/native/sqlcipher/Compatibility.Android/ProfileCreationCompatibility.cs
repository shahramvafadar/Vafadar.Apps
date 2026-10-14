using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Storage;
using Vafadar.Data;
using Vafadar.Core.Settings;
using Vafadar.Localization;
using Vafadar.Maui.Hosting;
using Vafadar.Zanance.App.Profiles;
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
    internal static async Task<string> RunAsync(string folder, bool create, bool retirement = false)
    {
        if (create) { if (Directory.Exists(folder)) throw new IOException("Use a fresh profile-creation fixture."); Directory.CreateDirectory(folder); }
        var main = Path.Combine(folder, ZananceApp.DatabaseFileName);
        using var services = new ServiceCollection().AddZananceData(main).BuildServiceProvider();
        var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        var keys = new AndroidGenerationKeyStore(folder); var durability = new AndroidGenerationFileDurability(folder);
        var preferences = new MauiSettingsStore(Preferences.Default);
        var identityPath = Path.Combine(folder, "identities.sqlite");
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
        var profiles = new ProfileService(location, store, preferences, runtime, Translator.Instance, profileRetirement: retirementHost);
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
        runtime.Session = new SqlCipherProfileSession<ZananceDbContext>(location, Resolve, async (id, original, owner, ct) =>
        {
            if (profiles.All.Count != 1 || !profiles.Current.IsMain) throw new IOException("Catalogue published before preparation.");
            var identities = new SqliteDatabaseIdentityCatalogue(identityPath, owner.Validate);
            await identities.RegisterAsync(id, ct); await durability.SynchronizeFileAsync(identityPath, ct); await durability.SynchronizeDirectoryAsync(ct);
            using var privateServices = new ServiceCollection().AddZananceData(original).BuildServiceProvider();
            var privateLocation = privateServices.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
            using var privateOwner = await privateLocation.Access.AcquireExclusiveAsync(ct);
            using var prepared = await SqlCipherNewProfile.OpenAsync(privateLocation, identities, id, original, true, keys, durability,
                BuildSchema, privateOwner, owner.Validate, 16 * 1024 * 1024, "4.19.0", ct);
            if (profiles.All.Count != 1 || location.Path == prepared.Path || File.Exists(original)) throw new IOException("New profile changed active publication or created plaintext.");
        });
        using (var owner = await location.Access.AcquireExclusiveAsync())
        {
            if (create)
            {
                preferences.Remove("profiles.work.catalogue"); preferences.Remove("profiles.list"); preferences.Remove("profiles.current"); preferences.Remove("profiles.main.name");
                var identities = new SqliteDatabaseIdentityCatalogue(identityPath, owner.Validate); await identities.CreateNewAsync([""], durability);
                using var prepared = await SqlCipherNewProfile.OpenAsync(location, identities, "", main, true, keys, durability,
                    BuildSchema, owner, () => { }, 16 * 1024 * 1024, "4.19.0");
            }
            if (!create && retirement) await retirementHost!.ResumePendingAsync(profiles, location, owner, CancellationToken.None);
            await runtime.Session.StartAsync(main, owner);
        }
        if (create)
        {
            await store.SaveAccountAsync(new Account { Name = "Fictitious retained main", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1), OpeningBalance = 250 });
            var endpoint = location.Capture();
            var work = await profiles.CreateReviewedAsync(profiles.ReadWorkReview(), "Fictitious prepared work");
            if (location.Capture() != endpoint || !profiles.Current.IsMain || profiles.All.Count != 2) throw new IOException("Creation changed active finance context.");
            if (!await profiles.SwitchToAsync(work) || (await store.GetAccountsAsync()).Count != 0) throw new IOException("New profile was not empty.");
            await store.SaveAccountAsync(new Account { Name = "Fictitious retained work", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1), OpeningBalance = 880 });
            if (!await profiles.SwitchToAsync(profiles.All.Single(p => p.IsMain))) throw new IOException("Return to original failed.");
            await new AndroidSettingsPersistence(Preferences.Default).PersistAsync();
            if (retirement)
            {
                var refused = false;
                try { await profiles.DeleteAsync(work); }
                catch (IOException error) when (error.Message == RetirementPersistence.Failure) { refused = true; }
                if (!refused || profiles.All.Count != 1 || !File.Exists(Path.Combine(folder, "zanance-" + work.Id + ".db") + ".restore-history.sqlite"))
                    throw new IOException("Native retirement did not retain files after catalogue persistence failure.");
            }
        }
        if ((await store.GetAccountsAsync()).Single().OpeningBalance != 250) throw new IOException("Main financial packet changed.");
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
            return create ? "{\"ActualCataloguePersisted\":true,\"NativeFailureRetainedProfileFiles\":true,\"MainBalance\":250}"
                : "{\"ColdCatalogueRetirementResumed\":true,\"OnlyMainIdentityRemains\":true,\"MainBalance\":250,\"ActualNativeSettings\":true}";
        }
        return "{\"CatalogueAfterEncryptedPreparation\":true,\"ActiveProfileUnchangedDuringCreation\":true,\"RealSwitchAndFinancialPacketsPreserved\":true}";
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
