using System.Text;
using Microsoft.Data.Sqlite;
using Vafadar.Testing;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Zanance.App.Features.Profiles;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual profile catalogue, native-independent view model and SQLite path-switch behaviour.</summary>
public sealed class ProfileTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact, Trait("AT", "AT-80")]
    public async Task Switching_is_exclusive_and_preserves_independent_accounts_and_settings()
    {
        using var f = new FlowFixture(); var runtime = new ProfileRuntime(f);
        var profiles = Create(f, runtime); var main = profiles.Current;
        await f.Store.SaveAccountAsync(new Account { Name = "Fictitious personal", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1) }, Ct);
        await f.Store.UpdateSettingsAsync(s => s.ReportCurrencyCode = "EUR", Ct);
        var work = profiles.Create("Fictitious work"); Assert.True(await profiles.SwitchToAsync(work));
        Assert.Empty(await f.Store.GetAccountsAsync(cancellationToken: Ct));
        await f.Store.SaveAccountAsync(new Account { Name = "Fictitious business", CurrencyCode = "USD", OpeningDate = new(2026, 1, 1) }, Ct);
        await f.Store.UpdateSettingsAsync(s => s.ReportCurrencyCode = "USD", Ct);
        Assert.True(await profiles.SwitchToAsync(main));
        Assert.Equal("Fictitious personal", Assert.Single(await f.Store.GetAccountsAsync(cancellationToken: Ct)).Name);
        Assert.Equal("EUR", (await f.Store.GetSettingsAsync(Ct)).ReportCurrencyCode);
        Assert.True(await profiles.SwitchToAsync(work));
        Assert.Equal("Fictitious business", Assert.Single(await f.Store.GetAccountsAsync(cancellationToken: Ct)).Name);
        Assert.Equal("USD", (await f.Store.GetSettingsAsync(Ct)).ReportCurrencyCode);
        Assert.Equal(work.Id, ProfileService.CurrentBackupSet(f.Preferences));
        Assert.Equal(3, runtime.ExclusiveRuns); Assert.Equal(3, runtime.LockReloads); Assert.Equal(0, runtime.AuthenticationCalls);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact, Trait("AT", "AT-80")]
    public async Task Refused_destination_authentication_keeps_the_previous_path_preference_and_profile()
    {
        using var f = new FlowFixture(); var runtime = new ProfileRuntime(f); var profiles = Create(f, runtime);
        var main = profiles.Current; var locked = profiles.Create("Fictitious locked");
        Assert.True(await profiles.SwitchToAsync(locked));
        await f.Store.UpdateSettingsAsync(s => s.AppLockEnabled = true, Ct);
        Assert.True(await profiles.SwitchToAsync(main));
        var path = Location(f).Path; runtime.AuthenticationAllowed = false;
        Assert.False(await profiles.SwitchToAsync(locked));
        Assert.Equal(path, Location(f).Path); Assert.Equal(main.Id, profiles.Current.Id);
        Assert.Null(ProfileService.CurrentBackupSet(f.Preferences)); Assert.Equal(2, runtime.LockReloads);
        Assert.Equal(1, runtime.AuthenticationCalls);
    }

    [Fact, Trait("AT", "AT-80")]
    public async Task Migration_failure_restores_the_previous_database_and_does_not_commit_a_current_profile()
    {
        using var f = new FlowFixture(); var runtime = new ProfileRuntime(f); var profiles = Create(f, runtime);
        var path = Location(f).Path; var profile = profiles.Create("Fictitious migration"); runtime.FailMigration = true;
        await Assert.ThrowsAsync<IOException>(() => profiles.SwitchToAsync(profile));
        Assert.Equal(path, Location(f).Path); Assert.True(profiles.Current.IsMain);
        Assert.Null(ProfileService.CurrentBackupSet(f.Preferences)); Assert.Equal(0, runtime.LockReloads);
    }

    [Fact, Trait("AT", "AT-80")]
    public void Invalid_stored_ids_and_unknown_profiles_cannot_form_database_or_deletion_paths()
    {
        using var f = new FlowFixture(); var runtime = new ProfileRuntime(f); var profiles = Create(f, runtime);
        var invalid = new string('.', 32); f.Preferences.Set("profiles.list", invalid + ":" + Convert.ToBase64String(Encoding.UTF8.GetBytes("Fictitious")));
        f.Preferences.Set("profiles.current", invalid);
        Assert.Single(profiles.All); Assert.Null(ProfileService.CurrentBackupSet(f.Preferences));
        Assert.Equal(Location(f).Path, ProfileService.StartupDatabasePath(Location(f).Path, f.Preferences));
        Assert.Throws<ArgumentException>(() => profiles.Delete(new LocalProfile(Guid.NewGuid().ToString("N"), "Unknown")));
        Assert.Throws<ArgumentException>(() => profiles.Rename(new LocalProfile(invalid, "Unknown"), "New name"));
        Assert.Throws<ArgumentException>(() => profiles.Create(" "));
    }

    [Fact, Trait("AT", "AT-80")]
    public async Task Deletion_is_for_inactive_known_profiles_and_preserves_the_other_database_and_backups()
    {
        using var f = new FlowFixture(); var runtime = new ProfileRuntime(f); var profiles = Create(f, runtime); var main = profiles.Current;
        var other = profiles.Create("Fictitious removal"); Assert.True(await profiles.SwitchToAsync(other));
        var otherPath = Location(f).Path;
        Assert.Throws<InvalidOperationException>(() => profiles.Delete(other));
        Assert.Throws<InvalidOperationException>(() => profiles.Delete(main));
        Assert.True(await profiles.SwitchToAsync(main));
        var backupDir = f.Directory.Combine("backups"); Directory.CreateDirectory(backupDir);
        var owned = Path.Combine(backupDir, $"pro.vafadar.zanance~{other.Id}_fictitious.vbak");
        var retained = Path.Combine(backupDir, "pro.vafadar.zanance_main.vbak");
        await File.WriteAllTextAsync(owned, "Fictitious", Ct); await File.WriteAllTextAsync(retained, "Fictitious", Ct);
        profiles.Delete(other); Assert.False(File.Exists(otherPath)); Assert.False(File.Exists(owned));
        Assert.True(File.Exists(retained)); Assert.True(File.Exists(Location(f).Path)); Assert.Single(profiles.All);
    }

    [Fact, Trait("AT", "AT-80")]
    public async Task Duplicate_names_are_rejected_and_cancelled_add_does_not_switch_or_create_another_profile()
    {
        using var f = new FlowFixture(); var runtime = new ProfileRuntime(f); var profiles = Create(f, runtime);
        profiles.Create("Fictitious work"); var vm = new ProfilesViewModel(profiles, f.Translator, f.Platform, f.Platform);
        f.Platform.Inputs.Enqueue("  Fictitious work "); await vm.AddCommand.ExecuteAsync(null);
        Assert.Equal(f.Translator["Profile_NameTaken"], vm.Error); Assert.Equal(2, profiles.All.Count);
        f.Platform.Inputs.Enqueue(null); await vm.AddCommand.ExecuteAsync(null);
        Assert.Equal(2, profiles.All.Count); Assert.Equal(0, runtime.ExclusiveRuns); Assert.Equal(0, f.Platform.ProfileOpened);
    }

    [Fact, Trait("AT", "AT-80")]
    public async Task Both_deletion_confirmations_are_required_and_main_current_actions_offer_no_delete()
    {
        using var f = new FlowFixture(); var runtime = new ProfileRuntime(f); var profiles = Create(f, runtime);
        var other = profiles.Create("Fictitious removal"); var vm = new ProfilesViewModel(profiles, f.Translator, f.Platform, f.Platform);
        await vm.LoadAsync(); var row = vm.Profiles.Single(x => x.Profile.Id == other.Id);
        f.Platform.Choices.Enqueue(f.Translator["Common_Delete"]); f.Platform.Confirmations.Enqueue(true); f.Platform.Confirmations.Enqueue(false);
        await vm.ChooseCommand.ExecuteAsync(row); Assert.Equal(2, profiles.All.Count);
        f.Platform.Choices.Enqueue(null); await vm.ChooseCommand.ExecuteAsync(vm.Profiles.Single(x => x.IsCurrent));
        Assert.DoesNotContain(f.Translator["Common_Delete"], f.Platform.OfferedActions[^1]);
        f.Platform.Choices.Enqueue(f.Translator["Common_Delete"]); f.Platform.Confirmations.Enqueue(true); f.Platform.Confirmations.Enqueue(true);
        await vm.ChooseCommand.ExecuteAsync(row); Assert.Single(profiles.All); Assert.Single(vm.Profiles);
    }

    [Fact, Trait("AT", "AT-139")]
    public async Task Deleting_an_inactive_profile_preserves_the_current_profiles_native_session_and_rows()
    {
        using var f = new FlowFixture(); var profiles = Create(f, new ProfileRuntime(f)); var main = profiles.Current;
        var other = profiles.Create("Fictitious pool removal"); Assert.True(await profiles.SwitchToAsync(other));
        var otherPath = Location(f).Path; Assert.True(await profiles.SwitchToAsync(main));
        await f.Store.SaveAccountAsync(new Account { Name = "Fictitious retained", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1) }, Ct);
        var connection = new SqliteConnectionStringBuilder { DataSource = Location(f).Path, Mode = SqliteOpenMode.ReadWriteCreate }.ToString();
        SqlitePoolProbe.Mark(connection);
        profiles.Delete(other);
        Assert.Equal("retained", SqlitePoolProbe.Read(connection)); Assert.False(File.Exists(otherPath));
        Assert.Equal("Fictitious retained", Assert.Single(await f.Store.GetAccountsAsync(cancellationToken: Ct)).Name);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct)); Assert.True(profiles.Current.IsMain);
    }

    private static LocalDatabaseLocation<ZananceDbContext> Location(FlowFixture f) => f.Services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
    private static ProfileService Create(FlowFixture f, ProfileRuntime runtime) => new(Location(f), f.Store, f.Preferences, runtime, f.Translator);

    private sealed class ProfileRuntime(FlowFixture fixture) : IProfileRuntime
    {
        internal int ExclusiveRuns, LockReloads, AuthenticationCalls, UndoDismissals;
        internal bool AuthenticationAllowed = true, FailMigration;
        private bool _exclusive;
        public async Task RunExclusiveAsync(Func<Task> action) { Assert.False(_exclusive); ExclusiveRuns++; _exclusive = true; try { await action(); } finally { _exclusive = false; } }
        public Task MigrateAsync()
        {
            Assert.True(_exclusive);
            if (FailMigration) { throw new IOException("Fictitious migration failure."); }
            return fixture.Services.MigrateLocalDatabaseAsync<ZananceDbContext>(Ct);
        }
        public Task<bool> AuthenticateAsync(string reason) { Assert.True(_exclusive); AuthenticationCalls++; return Task.FromResult(AuthenticationAllowed); }
        public Task ReloadLockAsync() { Assert.False(_exclusive); LockReloads++; return Task.CompletedTask; }
        public void DismissUndo() => UndoDismissals++;
    }
}
