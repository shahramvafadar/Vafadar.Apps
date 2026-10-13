using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.Core;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Data;

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
        return "{\"OriginalProfileFlow\":true,\"DrainBeforePathChange\":true,\"OwnedMigrationAndSettings\":true,\"RefusedAuthenticationRestoresQueuedWork\":true,\"NoFinancialPosting\":true,\"InactiveDeletionDrainsAndRetainsMain\":true}";
    }

    private sealed class Runtime(IServiceProvider services) : IProfileRuntime
    {
        internal bool AuthenticationAllowed = true;
        internal Action? DuringAuthentication;
        public Task RunExclusiveAsync(Func<Task> action) => services.GetRequiredService<AutoPostProcessor>().RunExclusiveAsync(action);
        public Task MigrateAsync(DatabaseAccessGate.ExclusiveLease owner) => services.MigrateLocalDatabaseForMaintenanceAsync<ZananceDbContext>(owner);
        // Authentication is the explicit decision port: this fixture accepts/refuses it, never invokes owner/device credentials.
        public Task<bool> AuthenticateAsync(string reason) { DuringAuthentication?.Invoke(); return Task.FromResult(AuthenticationAllowed); }
        public Task ReloadLockAsync() => Task.CompletedTask;
        public void DismissUndo() { }
    }
}
