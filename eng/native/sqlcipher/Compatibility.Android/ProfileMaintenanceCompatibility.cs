using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.Core;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.CompiledModel;
using Vafadar.Zanance.App.Security;
using System.Security.Cryptography;

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
        var keys = new AndroidGenerationKeyStore(folder); var retained = new List<byte[]>();
        try
        {
            foreach (var original in new[] { mainPath, Path.Combine(folder, "zanance-" + destination.Id + ".db") })
            {
                using var portable = new MemoryStream();
                await using (var source = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = original, Mode = SqliteOpenMode.ReadWrite, Pooling = false }.ToString()))
                {
                    await source.OpenAsync(); await SqlCipherPortableSnapshot.WriteAsync(source, portable, 16 * 1024 * 1024);
                }
                portable.Position = 0; var binding = new DatabaseKeyBinding(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
                var target = original + ".protected.db";
                await SqlCipherRestoreStaging.PrepareAsync(portable, target, binding, keys, new AndroidGenerationFileDurability(folder),
                    async (connection, ct) =>
                    {
                        var options = new DbContextOptionsBuilder<ZananceDbContext>().UseSqlite(connection, contextOwnsConnection: false).UseModel(ZananceDbContextModel.Instance).Options;
                        await using var db = new ZananceDbContext(options); await db.Database.MigrateAsync(ct);
                    }, () => { }, 16 * 1024 * 1024, "4.19.0");
                var key = await keys.ReadAsync(binding, CancellationToken.None); retained.Add(key);
                runtime.Protected[original] = (target, key);
            }
            var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
            using (var owner = await location.Access.AcquireExclusiveAsync()) await runtime.OpenDatabaseAsync(location, mainPath, owner);
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
                if (location.Path != mainPath + ".protected.db" || (await reopened.Accounts.SingleAsync()).OpeningBalance != 1250 ||
                    (await reopened.Entries.CountAsync()) != 0 || !profiles.Current.IsMain)
                    throw new IOException("Protected rollback lost its key factory or financial profile.");
            }
            runtime.AuthenticationAllowed = true; runtime.DuringAuthentication = null;
            if (!await profiles.SwitchToAsync(destination) || !await profiles.SwitchToAsync(main))
                throw new IOException("Verified protected destination/current profile did not switch.");
        }
        finally { foreach (var key in retained) CryptographicOperations.ZeroMemory(key); }
    }

    private sealed class Runtime(IServiceProvider services) : IProfileRuntime
    {
        internal bool AuthenticationAllowed = true;
        internal Action? DuringAuthentication;
        internal readonly Dictionary<string, (string Path, byte[] Key)> Protected = new(StringComparer.Ordinal);
        public Task RunExclusiveAsync(Func<Task> action) => services.GetRequiredService<AutoPostProcessor>().RunExclusiveAsync(action);
        public Task MigrateAsync(DatabaseAccessGate.ExclusiveLease owner) => services.MigrateLocalDatabaseForMaintenanceAsync<ZananceDbContext>(owner);
        public async Task OpenDatabaseAsync(LocalDatabaseLocation<ZananceDbContext> location, string originalPath, DatabaseAccessGate.ExclusiveLease owner)
        {
            if (Protected.TryGetValue(originalPath, out var target))
                location.MoveToProtected(target.Path, () => new SqliteConnection(new SqliteConnectionStringBuilder
                { DataSource = target.Path, Password = Convert.ToBase64String(target.Key), Mode = SqliteOpenMode.ReadWrite, Pooling = false }.ToString()), "4.19.0", owner);
            else location.MoveTo(originalPath, owner);
            await MigrateAsync(owner);
        }
        // Authentication is the explicit decision port: this fixture accepts/refuses it, never invokes owner/device credentials.
        public Task<bool> AuthenticateAsync(string reason) { DuringAuthentication?.Invoke(); return Task.FromResult(AuthenticationAllowed); }
        public Task ReloadLockAsync() => Task.CompletedTask;
        public void DismissUndo() { }
    }
}
