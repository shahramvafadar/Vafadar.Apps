using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.Core;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.App.Security;

namespace Vafadar.SqlCipher.Compatibility.Android;

/// <summary>Runs the original profile flow/factory/store under the reviewed engine in isolated fictitious directories.</summary>
internal static class ProfileMaintenanceCompatibility
{
    internal static async Task<string> RunAsync(string folder)
    {
        if (Directory.Exists(folder)) throw new IOException("Use a fresh independent profile fixture.");
        Directory.CreateDirectory(folder); var mainPath = Path.Combine(folder, ZananceApp.DatabaseFileName);
        using var services = new ServiceCollection().AddZananceData(mainPath).BuildServiceProvider();
        await services.MigrateLocalDatabaseAsync<ZananceDbContext>();
        var store = services.GetRequiredService<ZananceStore>();
        await store.SaveAccountAsync(new Account { Name = "Fictitious retained main", CurrencyCode = "EUR", OpeningBalance = 1250, OpeningDate = new(2026, 1, 1) });
        var preferences = new Vafadar.Core.Settings.InMemorySettingsStore();
        var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        var runtime = new Runtime(services); var profiles = new ProfileService(location, store, preferences, runtime, Translator.Instance);
        var main = profiles.Current; var work = profiles.Create("Fictitious native destination");
        var factory = services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        var active = await factory.CreateDbContextAsync();
        var switching = profiles.SwitchToAsync(work);
        if (switching.IsCompleted || location.Path != mainPath || !profiles.Current.IsMain) throw new IOException("Profile moved before its active context drained.");
        var refused = false;
        try { using var unexpected = factory.CreateDbContext(); } catch (InvalidOperationException) { refused = true; }
        if (!refused) throw new IOException("Pending profile maintenance admitted a synchronous context.");
        await active.DisposeAsync();
        if (!await switching || profiles.Current.Id != work.Id) throw new IOException("Owned destination migration/settings did not finish.");
        if ((await store.GetAccountsAsync()).Count != 0 || (await store.GetEntriesAsync()).Count != 0) throw new IOException("Opening a profile posted financial data.");
        await store.UpdateSettingsAsync(settings => settings.AppLockEnabled = true);
        if (!await profiles.SwitchToAsync(main)) throw new IOException("Original profile did not reopen.");
        runtime.AuthenticationAllowed = false;
        Task<ZananceDbContext>? queued = null;
        runtime.DuringAuthentication = () =>
        {
            queued = factory.CreateDbContextAsync();
            if (queued.IsCompleted) throw new IOException("Profile authentication admitted unrelated work.");
        };
        if (await profiles.SwitchToAsync(work)) throw new IOException("Refused fixture authentication committed a profile.");
        await using var reopened = await queued!;
        if (new SqliteConnectionStringBuilder(reopened.Database.GetDbConnection().ConnectionString).DataSource != mainPath ||
            (await reopened.Accounts.SingleAsync()).OpeningBalance != 1250 || profiles.Current.Id != main.Id ||
            (await reopened.Entries.CountAsync()) != 0) throw new IOException("Queued work opened the wrong profile or changed money.");
        var inactivePath = Path.Combine(folder, "zanance-" + work.Id + ".db");
        var deletion = profiles.DeleteAsync(work);
        if (deletion.IsCompleted || !File.Exists(inactivePath) || profiles.All.Count != 2)
            throw new IOException("Inactive deletion did not wait for actual contexts.");
        await reopened.DisposeAsync(); await deletion;
        if (File.Exists(inactivePath) || profiles.All.Count != 1 || !File.Exists(mainPath))
            throw new IOException("Inactive deletion changed the original main or failed to remove its owned file.");
        await CheckProtectedSwitchAsync(folder, services, profiles, runtime, mainPath);
        return "{\"OriginalProfileFlow\":true,\"DrainBeforePathChange\":true,\"OwnedMigrationAndSettings\":true,\"RefusedAuthenticationRestoresQueuedWork\":true,\"NoFinancialPosting\":true,\"InactiveDeletionDrainsAndRetainsMain\":true,\"ProtectedSwitchAndKeyedRollback\":true}";
    }

    private static async Task CheckProtectedSwitchAsync(string folder, IServiceProvider services, ProfileService profiles, Runtime runtime, string mainPath)
    {
        runtime.AuthenticationAllowed = true; runtime.DuringAuthentication = null;
        var main = profiles.Current; var destination = profiles.Create("Fictitious protected destination");
        if (!await profiles.SwitchToAsync(destination)) throw new IOException("Protected fixture destination preparation failed.");
        await services.GetRequiredService<ZananceStore>().UpdateSettingsAsync(settings => settings.AppLockEnabled = true);
        if (!await profiles.SwitchToAsync(main)) throw new IOException("Protected fixture original preparation failed.");
        var keys = new AndroidGenerationKeyStore(folder);
        var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        var identityPath = Path.Combine(folder, "profile-identities.sqlite");
        using (var owner = await location.Access.AcquireExclusiveAsync())
        {
            var identities = new SqliteDatabaseIdentityCatalogue(identityPath, owner.Validate);
            await identities.CreateNewAsync(["", destination.Id]);
            foreach (var original in new[] { mainPath, Path.Combine(folder, "zanance-" + destination.Id + ".db") })
            {
                var legacy = original == mainPath ? "" : destination.Id;
                location.MoveTo(original, owner);
                await SqlCipherProfileGeneration.InitializeJournalAsync(location, identities, legacy, original, owner, () => { });
                using var generation = await SqlCipherProfileGeneration.OpenInitialAsync(location, identities, legacy, original,
                    keys, new AndroidGenerationFileDurability(folder), owner, () => { }, 16 * 1024 * 1024, "4.19.0");
                await new SqliteProfileRestoreHistory(original + ".restore-history.sqlite", generation.Binding, owner.Validate).CreateNewAsync();
            }
            location.MoveTo(mainPath, owner);
        }
        var opened = new List<SqlCipherProfileGeneration>();
        var session = new SqlCipherProfileSession<ZananceDbContext>(location, async (original, owner, ct) =>
        {
            var identities = new SqliteDatabaseIdentityCatalogue(identityPath, owner.Validate);
            var legacy = original == mainPath ? "" : destination.Id;
            var identity = (await identities.ReadAsync(ct)).Single(row => row.LegacyProfileId == legacy);
            var history = new SqliteProfileRestoreHistory(original + ".restore-history.sqlite", identity.InitialCandidateKey, owner.Validate);
            var generation = await SqlCipherProfileGeneration.OpenCurrentAsync(location, identities, legacy, original, history,
                _ => throw new IOException("Initial profile switch must not invent a restore."), new ExistingKeys(keys),
                new AndroidGenerationFileDurability(folder), owner, () => { }, 16 * 1024 * 1024, "4.19.0", ct);
            opened.Add(generation); return generation;
        });
        runtime.ProtectedSession = session;
        using (var owner = await location.Access.AcquireExclusiveAsync()) await session.StartAsync(mainPath, owner);
        try
        {
            var factory = services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
            await using var active = await factory.CreateDbContextAsync();
            var switching = profiles.SwitchToAsync(destination);
            if (switching.IsCompleted) throw new IOException("Protected profile moved before actual context disposal.");
            runtime.AuthenticationAllowed = false; Task<ZananceDbContext>? queued = null;
            runtime.DuringAuthentication = () => { queued = factory.CreateDbContextAsync(); if (queued.IsCompleted) throw new IOException("Protected authentication admitted queued work."); };
            await active.DisposeAsync();
            if (await switching) throw new IOException("Protected refused authentication committed a switch.");
            await using (var reopened = await queued!)
            {
                if (location.Path != opened[0].Path || (await reopened.Accounts.SingleAsync()).OpeningBalance != 1250 ||
                    (await reopened.Entries.CountAsync()) != 0 || !profiles.Current.IsMain)
                    throw new IOException("Protected rollback lost its key factory or financial profile.");
            }
            RequireDisposed(opened[^1]);
            using (opened[0].CreateConnection()) { }
            runtime.AuthenticationAllowed = true; runtime.DuringAuthentication = null;
            if (!await profiles.SwitchToAsync(destination) || !await profiles.SwitchToAsync(main))
                throw new IOException("Verified protected destination/current profile did not switch.");
            foreach (var retired in opened.SkipLast(1)) RequireDisposed(retired);
            using (var owner = await location.Access.AcquireExclusiveAsync())
            {
                await using var activeOwned = await services.GetRequiredService<ILocalDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(owner);
                var refused = false; try { session.Close(owner); } catch (InvalidOperationException) { refused = true; }
                if (!refused) throw new IOException("Session cleared a key before native context disposal.");
                await activeOwned.DisposeAsync(); session.Close(owner); RequireDisposed(opened[^1]);
            }
        }
        finally { foreach (var generation in opened) generation.Dispose(); }
    }

    private static void RequireDisposed(SqlCipherProfileGeneration generation)
    {
        try { using var unexpected = generation.CreateConnection(); }
        catch (ObjectDisposedException) { return; }
        throw new IOException("Retired session generation retained usable key construction.");
    }

    private sealed class ExistingKeys(IDatabaseGenerationKeyStore inner) : IDatabaseGenerationKeyStore
    {
        public Task PrepareAsync(DatabaseKeyBinding binding, CancellationToken ct) => throw new IOException("Profile reopen must not prepare keys.");
        public Task<byte[]> ReadAsync(DatabaseKeyBinding binding, CancellationToken ct) => inner.ReadAsync(binding, ct);
    }

    private sealed class Runtime(IServiceProvider services) : IProfileRuntime
    {
        internal bool AuthenticationAllowed = true;
        internal Action? DuringAuthentication;
        internal SqlCipherProfileSession<ZananceDbContext>? ProtectedSession;
        public Task RunExclusiveAsync(Func<Task> action) => services.GetRequiredService<AutoPostProcessor>().RunExclusiveAsync(action);
        public Task MigrateAsync(DatabaseAccessGate.ExclusiveLease owner) => services.MigrateLocalDatabaseForMaintenanceAsync<ZananceDbContext>(owner);
        public async Task OpenDatabaseAsync(LocalDatabaseLocation<ZananceDbContext> location, string originalPath, DatabaseAccessGate.ExclusiveLease owner)
        {
            if (ProtectedSession is not null) await ProtectedSession.BeginSwitchAsync(originalPath, owner);
            else location.MoveTo(originalPath, owner);
            await MigrateAsync(owner);
        }
        public void FinishDatabaseTransition(bool accepted, DatabaseAccessGate.ExclusiveLease owner) => ProtectedSession?.FinishSwitch(accepted, owner);
        // Authentication is the explicit decision port: this fixture accepts/refuses it, never invokes owner/device credentials.
        public Task<bool> AuthenticateAsync(string reason) { DuringAuthentication?.Invoke(); return Task.FromResult(AuthenticationAllowed); }
        public Task ReloadLockAsync() => Task.CompletedTask;
        public void DismissUndo() { }
    }
}
