using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
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

    [Theory, InlineData("active-protected"), InlineData("generation"), InlineData("staged-history"), InlineData("ciphertext"), Trait("AT", "AT-230")]
    public async Task Legacy_deletion_cannot_remove_catalogue_identity_or_retained_protected_artifacts(string state)
    {
        using var f = new FlowFixture(); var runtime = new ProfileRuntime(f); var profiles = Create(f, runtime);
        var profile = profiles.Create("Fictitious retained profile");
        var original = profiles.CaptureStorage().OriginalPaths[profile.Id];
        var artifact = original + (state == "generation" ? ".generation.sqlite" : state == "staged-history"
            ? ".restore-history.sqlite.creating" : "." + Guid.NewGuid().ToString("N") + ".cipher.db");
        await File.WriteAllBytesAsync(artifact, [1, 2, 3], Ct);
        if (state == "active-protected")
            Location(f).MoveToProtected(Location(f).Path, () => throw new IOException("No fixture credential access."), "4.19.0");
        var before = f.Preferences.Get("profiles.work.catalogue"); var notifications = 0;
        profiles.Changed += (_, _) => notifications++;
        await Assert.ThrowsAsync<InvalidOperationException>(() => profiles.DeleteAsync(profile, Ct));
        Assert.Equal(before, f.Preferences.Get("profiles.work.catalogue"));
        Assert.Contains(profile, profiles.All); Assert.Equal(0, notifications);
        Assert.Equal(new byte[] { 1, 2, 3 }, await File.ReadAllBytesAsync(artifact, Ct));
    }

    [Fact, Trait("AT", "AT-224")]
    public void Startup_storage_capture_includes_inactive_originals_without_commercial_or_financial_reads()
    {
        using var f = new FlowFixture(); var runtime = new ProfileRuntime(f);
        var profiles = Create(f, runtime); var work = profiles.Create("Fictitious inactive work");
        var guarded = new ProfileService(Location(f), f.Store, f.Preferences, runtime, f.Translator, new RejectPolicyRead());
        var capture = guarded.CaptureStorage();
        Assert.Equal(2, capture.OriginalPaths.Count);
        Assert.Equal(Location(f).Path, capture.CurrentOriginalPath);
        Assert.Equal(Location(f).Path, capture.OriginalPaths[""]);
        Assert.EndsWith("zanance-" + work.Id + ".db", capture.OriginalPaths[work.Id]);
        Assert.False(File.Exists(capture.OriginalPaths[work.Id]));
        capture.Validate();
        Assert.Equal(0, runtime.ExclusiveRuns);
    }

    [Fact, Trait("AT", "AT-224")]
    public void Changed_device_catalogue_invalidates_the_captured_bootstrap_before_publication()
    {
        using var f = new FlowFixture(); var runtime = new ProfileRuntime(f); var profiles = Create(f, runtime);
        var work = profiles.Create("Fictitious work"); var capture = profiles.CaptureStorage();
        profiles.Rename(work, "Fictitious renamed work");
        Assert.Throws<InvalidOperationException>(capture.Validate);
    }

    private sealed class RejectPolicyRead : IProfileWorkAccessSource
    {
        public ProfileWorkAccess Capture(string cataloguePath) => throw new IOException("Startup consulted commercial availability.");
    }

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

    [Fact, Trait("AT", "AT-204")]
    public async Task Inactive_deletion_waits_for_contexts_and_preserves_catalogue_and_file_until_drain()
    {
        using var f = new FlowFixture(); var profiles = Create(f, new ProfileRuntime(f)); var main = profiles.Current;
        var other = profiles.Create("Fictitious waiting deletion"); Assert.True(await profiles.SwitchToAsync(other));
        var otherPath = Location(f).Path; Assert.True(await profiles.SwitchToAsync(main));
        var factory = f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(); var context = await factory.CreateDbContextAsync(Ct);
        var deletion = profiles.DeleteAsync(other, Ct);
        Assert.False(deletion.IsCompleted); Assert.True(File.Exists(otherPath)); Assert.Equal(2, profiles.All.Count);
        await context.DisposeAsync(); await deletion;
        Assert.False(File.Exists(otherPath)); Assert.Single(profiles.All); Assert.True(File.Exists(Location(f).Path));
    }

    [Fact, Trait("AT", "AT-204")]
    public async Task Catalogue_change_while_deletion_waits_rejects_old_review_without_removing_files()
    {
        using var f = new FlowFixture(); var profiles = Create(f, new ProfileRuntime(f)); var main = profiles.Current;
        var other = profiles.Create("Fictitious retained deletion"); Assert.True(await profiles.SwitchToAsync(other));
        var otherPath = Location(f).Path; Assert.True(await profiles.SwitchToAsync(main));
        var review = profiles.ReadWorkReview(); var owner = await Location(f).Access.AcquireExclusiveAsync(Ct);
        var deletion = profiles.DeleteReviewedAsync(review, other, Ct); Assert.False(deletion.IsCompleted);
        profiles.Rename(other, "Fictitious renamed while waiting"); owner.Dispose();
        await Assert.ThrowsAsync<InvalidOperationException>(() => deletion);
        Assert.True(File.Exists(otherPath)); Assert.Equal(2, profiles.All.Count);
    }

    [Fact, Trait("AT", "AT-204")]
    public async Task Cancelled_deletion_waiter_preserves_original_and_does_not_leak_maintenance()
    {
        using var f = new FlowFixture(); var profiles = Create(f, new ProfileRuntime(f)); var other = profiles.Create("Fictitious cancelled deletion");
        using var context = f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContext();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        var deletion = profiles.DeleteAsync(other, cancellation.Token); cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => deletion);
        Assert.Equal(2, profiles.All.Count); context.Dispose(); await profiles.DeleteAsync(other, Ct); Assert.Single(profiles.All);
    }

    [Fact, Trait("AT", "AT-204")]
    public void Synchronous_delete_refuses_busy_context_instead_of_blocking_or_mutating_catalogue()
    {
        using var f = new FlowFixture(); var profiles = Create(f, new ProfileRuntime(f)); var other = profiles.Create("Fictitious sync deletion");
        using var context = f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContext();
        Assert.Throws<InvalidOperationException>(() => profiles.Delete(other)); Assert.Equal(2, profiles.All.Count);
        context.Dispose(); profiles.Delete(other); Assert.Single(profiles.All);
    }

    [Fact, Trait("AT", "AT-203")]
    public async Task Switch_waits_for_existing_context_before_moving_the_actual_profile_path()
    {
        using var f = new FlowFixture(); var runtime = new ProfileRuntime(f); var profiles = Create(f, runtime);
        var target = profiles.Create("Fictitious drained destination"); var originalPath = Location(f).Path;
        var factory = f.Services.GetRequiredService<Microsoft.EntityFrameworkCore.IDbContextFactory<ZananceDbContext>>();
        var current = await factory.CreateDbContextAsync(Ct);
        var switching = profiles.SwitchToAsync(target);
        Assert.False(switching.IsCompleted); Assert.Equal(originalPath, Location(f).Path); Assert.True(profiles.Current.IsMain);
        Assert.Throws<InvalidOperationException>(() => factory.CreateDbContext());
        await current.DisposeAsync(); Assert.True(await switching);
        Assert.Equal(target.Id, profiles.Current.Id); Assert.NotEqual(originalPath, Location(f).Path);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact, Trait("AT", "AT-203")]
    public async Task Work_queued_during_refused_authentication_opens_only_the_restored_previous_profile()
    {
        using var f = new FlowFixture(); var runtime = new ProfileRuntime(f); var profiles = Create(f, runtime);
        var main = profiles.Current; var target = profiles.Create("Fictitious refused destination");
        Assert.True(await profiles.SwitchToAsync(target)); await f.Store.UpdateSettingsAsync(s => s.AppLockEnabled = true, Ct);
        Assert.True(await profiles.SwitchToAsync(main)); runtime.AuthenticationAllowed = false;
        var factory = f.Services.GetRequiredService<Microsoft.EntityFrameworkCore.IDbContextFactory<ZananceDbContext>>();
        Task<ZananceDbContext>? queued = null;
        runtime.DuringAuthentication = () => { queued = factory.CreateDbContextAsync(Ct); Assert.False(queued.IsCompleted); };
        var originalPath = Location(f).Path;
        Assert.False(await profiles.SwitchToAsync(target));
        await using var reopened = await queued!;
        Assert.Equal(originalPath, new SqliteConnectionStringBuilder(reopened.Database.GetDbConnection().ConnectionString).DataSource);
        Assert.Equal(main.Id, profiles.Current.Id);
    }

    [Fact, Trait("AT", "AT-220")]
    public async Task New_profile_is_not_published_until_preparation_finishes_and_existing_context_drains()
    {
        using var f = new FlowFixture(); var runtime = new ProfileRuntime(f); var profiles = Create(f, runtime);
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        runtime.DuringPreparation = async () => { Assert.Single(profiles.All); ready.SetResult(); await release.Task; };
        var factory = f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        await using var existing = await factory.CreateDbContextAsync(Ct);
        var originalPath = Location(f).Path;
        var create = profiles.CreateReviewedAsync(profiles.ReadWorkReview(), "Fictitious prepared", Ct);
        Assert.False(create.IsCompleted); Assert.Single(profiles.All); Assert.False(ready.Task.IsCompleted);
        await existing.DisposeAsync(); await ready.Task.WaitAsync(Ct);
        Assert.Single(profiles.All); Assert.Equal(originalPath, Location(f).Path);
        release.SetResult(); var profile = await create;
        Assert.Contains(profile, profiles.All); Assert.True(profiles.Current.IsMain);
        Assert.Equal(originalPath, Location(f).Path);
    }

    [Theory, InlineData(false), InlineData(true), Trait("AT", "AT-220")]
    public async Task Failed_preparation_or_changed_catalogue_never_publishes_the_new_profile(bool changedCatalogue)
    {
        using var f = new FlowFixture(); var runtime = new ProfileRuntime(f); var profiles = Create(f, runtime);
        var originalPath = Location(f).Path;
        runtime.DuringPreparation = () =>
        {
            if (changedCatalogue) { profiles.Rename(profiles.Current, "Fictitious changed review"); return Task.CompletedTask; }
            throw new IOException("Fictitious preparation failure.");
        };
        var review = profiles.ReadWorkReview();
        if (changedCatalogue) await Assert.ThrowsAsync<InvalidOperationException>(() => profiles.CreateReviewedAsync(review, "Fictitious rejected", Ct));
        else await Assert.ThrowsAsync<IOException>(() => profiles.CreateReviewedAsync(review, "Fictitious rejected", Ct));
        Assert.Single(profiles.All); Assert.True(profiles.Current.IsMain); Assert.Equal(originalPath, Location(f).Path);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    private sealed class ProfileRuntime(FlowFixture fixture) : IProfileRuntime
    {
        internal int ExclusiveRuns, LockReloads, AuthenticationCalls, UndoDismissals;
        internal bool AuthenticationAllowed = true, FailMigration;
        internal Action? DuringAuthentication;
        internal Func<Task>? DuringPreparation;
        private bool _exclusive;
        public async Task RunExclusiveAsync(Func<Task> action) { Assert.False(_exclusive); ExclusiveRuns++; _exclusive = true; try { await action(); } finally { _exclusive = false; } }
        public Task MigrateAsync(Vafadar.Data.DatabaseAccessGate.ExclusiveLease owner)
        {
            Assert.True(_exclusive);
            if (FailMigration) { throw new IOException("Fictitious migration failure."); }
            return fixture.Services.MigrateLocalDatabaseForMaintenanceAsync<ZananceDbContext>(owner, Ct);
        }
        public async Task PrepareNewDatabaseAsync(LocalDatabaseLocation<ZananceDbContext> location, string profileId, string originalPath,
            DatabaseAccessGate.ExclusiveLease owner, CancellationToken ct)
        {
            Assert.True(_exclusive); _ = location.CapturePublication(owner); ct.ThrowIfCancellationRequested();
            if (DuringPreparation is not null) await DuringPreparation();
        }
        public Task<bool> AuthenticateAsync(string reason) { Assert.True(_exclusive); AuthenticationCalls++; DuringAuthentication?.Invoke(); return Task.FromResult(AuthenticationAllowed); }
        public Task ReloadLockAsync() { Assert.False(_exclusive); LockReloads++; return Task.CompletedTask; }
        public void DismissUndo() => UndoDismissals++;
    }
}
