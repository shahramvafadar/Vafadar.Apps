using Microsoft.Extensions.DependencyInjection;
using Vafadar.Core.Settings;
using Vafadar.Data;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual device catalogue writer and original authentication flow with real independent SQLite.</summary>
public sealed class ProfileWorkFlowTests
{
    private const string PacketKey = "profiles.work.catalogue";
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact, Trait("AT", "AT-158")]
    public void Inactive_read_and_bounded_missing_choice_preserve_original_catalogue_without_writes()
    {
        using var f = new Fixture(); f.AddOthers(3); var raw = f.Preferences.Get(PacketKey);
        var review = f.Service.ReadWorkReview(); Assert.Equal(4, review.Profiles.Count); Assert.Equal(4, review.SelectedIds.Count);
        Assert.Null(review.Maximum); Assert.False(review.CanChoose); Assert.Equal(raw, f.Preferences.Get(PacketKey));
        f.Enable(ProductPlan.Free); review = f.Service.ReadWorkReview();
        Assert.True(review.RequiresSelection); Assert.False(review.CanCreate); Assert.Empty(review.SelectedIds);
        Assert.Equal(4, review.Profiles.Count); Assert.Equal(raw, f.Preferences.Get(PacketKey)); Assert.True(f.Service.Current.IsMain);
    }

    [Fact, Trait("AT", "AT-158")]
    public async Task Explicit_later_choice_preserves_all_original_rows_and_does_not_switch_or_write_money()
    {
        using var f = new Fixture(); f.AddOthers(3);
        await f.Flow.Store.SaveAccountAsync(new Account { Name = "Fictitious retained", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1) }, Ct);
        var accounts = await f.Flow.Store.GetAccountsAsync(cancellationToken: Ct); f.Enable(ProductPlan.Free);
        var review = f.Service.ReadWorkReview(); var last = review.Profiles[^1]; var changes = 0; f.Service.Changed += (_, _) => changes++;
        Assert.True(f.Service.SaveWorkChoice(review, [last.Id])); var next = f.Service.ReadWorkReview();
        Assert.True(next.CanUse(last.Id)); Assert.False(next.CanUse("")); Assert.Equal(review.Profiles, next.Profiles);
        Assert.True(f.Service.Current.IsMain); Assert.Equal(0, f.Runtime.ExclusiveRuns); Assert.Equal(1, changes);
        Assert.False(f.Service.SaveWorkChoice(next, [last.Id])); Assert.Equal(1, changes);
        Assert.Equal(accounts.Select(a => a.Id), (await f.Flow.Store.GetAccountsAsync(cancellationToken: Ct)).Select(a => a.Id));
        Assert.Empty(await f.Flow.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact, Trait("AT", "AT-158")]
    public void Explicit_empty_choice_allows_one_new_profile_and_joins_only_that_original_slot_atomically()
    {
        using var f = new Fixture(); f.AddOthers(2); f.Enable(ProductPlan.Free);
        Assert.True(f.Service.SaveWorkChoice(f.Service.ReadWorkReview(), [])); var review = f.Service.ReadWorkReview();
        Assert.True(review.CanCreate); var original = review.Profiles.ToArray(); var created = f.Service.CreateReviewed(review, "Fictitious new");
        var next = f.Service.ReadWorkReview(); Assert.Equal(created.Id, Assert.Single(next.SelectedIds)); Assert.False(next.CanCreate);
        Assert.Equal(original.Length + 1, next.Profiles.Count); Assert.All(original, p => Assert.Contains(p, next.Profiles));
        Assert.Throws<CommercialWriteRejectedException>(() => f.Service.Create("Fictitious extra"));
        Assert.True(f.Service.Current.IsMain); Assert.Equal(0, f.Runtime.ExclusiveRuns);
    }

    [Fact, Trait("AT", "AT-158")]
    public void Free_original_main_occupies_its_device_slot_without_a_manufactured_Guid()
    {
        using var f = new Fixture(); f.Enable(ProductPlan.Free); var review = f.Service.ReadWorkReview();
        Assert.Equal("", Assert.Single(review.SelectedIds)); Assert.False(review.CanCreate); Assert.False(review.RequiresSelection);
        Assert.Null(f.Preferences.Get(PacketKey)); Assert.Throws<CommercialWriteRejectedException>(() => f.Service.Create("Fictitious extra"));
        Assert.Single(f.Service.All); Assert.Null(f.Preferences.Get(PacketKey));
    }

    [Fact, Trait("AT", "AT-158")]
    public void Original_unselected_rename_preserves_choice_and_rejects_same_id_stale_name()
    {
        using var f = new Fixture(); f.AddOthers(2); f.Enable(ProductPlan.Free); f.Service.SaveWorkChoice(f.Service.ReadWorkReview(), [""]);
        var review = f.Service.ReadWorkReview(); var other = review.Profiles[1]; f.Service.RenameReviewed(review, other, "Fictitious corrected");
        var next = f.Service.ReadWorkReview(); Assert.Equal("", Assert.Single(next.SelectedIds)); Assert.False(next.CanUse(other.Id));
        Assert.Equal("Fictitious corrected", next.Profiles.Single(p => p.Id == other.Id).Name);
        Assert.Throws<ArgumentException>(() => f.Service.Rename(other, "Fictitious stale")); Assert.True(f.Service.Current.IsMain);
    }

    [Fact, Trait("AT", "AT-158")]
    public async Task Removing_selected_inactive_original_releases_only_its_slot_and_keeps_other_files()
    {
        using var f = new Fixture(); f.AddOthers(2); var other = f.Service.All[1]; await f.Service.SwitchToAsync(other);
        var owned = f.Location.Path; await f.Service.SwitchToAsync(f.Service.All[0]); f.Enable(ProductPlan.Free);
        f.Service.SaveWorkChoice(f.Service.ReadWorkReview(), [other.Id]); f.Service.DeleteReviewed(f.Service.ReadWorkReview(), other);
        Assert.False(File.Exists(owned)); Assert.True(File.Exists(f.Location.Path)); var next = f.Service.ReadWorkReview();
        Assert.Empty(next.SelectedIds); Assert.True(next.CanCreate); Assert.Equal(2, next.Profiles.Count); Assert.True(f.Service.Current.IsMain);
    }

    [Theory, InlineData("choice"), InlineData("name"), InlineData("legacy"), InlineData("rights"), Trait("AT", "AT-158")]
    public void A_complete_original_review_rejects_retired_inputs_without_an_extra_mutation(string change)
    {
        using var f = new Fixture(); f.AddOthers(2); f.Enable(ProductPlan.Free); var review = f.Service.ReadWorkReview();
        switch (change)
        {
            case "choice": f.Service.SaveWorkChoice(review, [""]); break;
            case "name": f.Service.RenameReviewed(review, review.Profiles[1], "Fictitious changed"); break;
            case "legacy": f.Preferences.Set("profiles.main.name", "Fictitious external"); break;
            case "rights": f.Enable(ProductPlan.Plus); break;
        }
        var raw = f.Preferences.Get(PacketKey); Assert.Throws<InvalidOperationException>(() => f.Service.SaveWorkChoice(review, [review.Profiles[1].Id]));
        Assert.Equal(raw, f.Preferences.Get(PacketKey)); Assert.Equal(3, f.Service.All.Count);
    }

    [Fact, Trait("AT", "AT-158")]
    public void Choice_ABA_and_another_actual_catalogue_cannot_reuse_an_original_review()
    {
        using var f = new Fixture(); f.AddOthers(2); f.Enable(ProductPlan.Free); f.Service.SaveWorkChoice(f.Service.ReadWorkReview(), [""]);
        var original = f.Service.ReadWorkReview(); f.Service.SaveWorkChoice(original, [original.Profiles[1].Id]);
        f.Service.SaveWorkChoice(f.Service.ReadWorkReview(), [""]);
        Assert.Throws<InvalidOperationException>(() => f.Service.SaveWorkChoice(original, [""]));
        var foreignLocation = new LocalDatabaseLocation<ZananceDbContext>(f.Flow.Directory.Combine(Path.Combine("other", "zanance.db")));
        var foreign = new ProfileService(foreignLocation, f.Flow.Store, f.Preferences, f.Runtime, f.Flow.Translator, f.Source);
        Assert.Throws<InvalidOperationException>(() => foreign.ValidateWorkReview(f.Service.ReadWorkReview()));
    }

    [Theory, InlineData("scope"), InlineData("path"), InlineData("missing"), InlineData("revision"), Trait("AT", "AT-158")]
    public void Foreign_or_malformed_choice_retains_original_catalogue_and_requires_explicit_repair(string corruption)
    {
        using var f = new Fixture(); f.AddOthers(2); f.Enable(ProductPlan.Free); f.Service.SaveWorkChoice(f.Service.ReadWorkReview(), [""]);
        var fields = f.Preferences.Get(PacketKey)!.Split('|');
        if (corruption == "scope") fields[4] = Guid.NewGuid().ToString("N");
        else if (corruption == "path") fields[3] = "=" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(f.Flow.Directory.Combine("foreign.db")));
        else if (corruption == "revision") fields[7] = "malformed";
        else fields[5] = "-";
        f.Preferences.Set(PacketKey, string.Join('|', fields)); var raw = f.Preferences.Get(PacketKey); var review = f.Service.ReadWorkReview();
        Assert.True(review.RequiresSelection); Assert.False(review.CanCreate); Assert.Equal(3, review.Profiles.Count);
        Assert.Equal(raw, f.Preferences.Get(PacketKey));
        if (corruption == "revision") { f.Service.RenameReviewed(review, review.Profiles[1], "Fictitious retained correction"); review = f.Service.ReadWorkReview(); Assert.True(review.RequiresSelection); }
        Assert.True(f.Service.SaveWorkChoice(review, [review.Profiles[^1].Id]));
        Assert.False(f.Service.ReadWorkReview().RequiresSelection);
    }

    [Fact, Trait("AT", "AT-158")]
    public void Failed_single_preference_publication_leaves_names_choice_and_originals_unchanged()
    {
        using var f = new Fixture(); f.AddOthers(2); f.Enable(ProductPlan.Free); f.Service.SaveWorkChoice(f.Service.ReadWorkReview(), []);
        var review = f.Service.ReadWorkReview(); var raw = f.Preferences.Get(PacketKey); var changes = 0; f.Service.Changed += (_, _) => changes++;
        f.Preferences.FailSet = true; Assert.Throws<IOException>(() => f.Service.CreateReviewed(review, "Fictitious failure"));
        Assert.Equal(raw, f.Preferences.Get(PacketKey)); Assert.Equal(review.Profiles, f.Service.All); Assert.Equal(0, changes);
    }

    [Fact, Trait("AT", "AT-158")]
    public async Task Unselected_original_remains_openable_for_viewing_and_backup()
    {
        using var f = new Fixture(); f.AddOthers(2); f.Enable(ProductPlan.Free); f.Service.SaveWorkChoice(f.Service.ReadWorkReview(), [""]);
        var review = f.Service.ReadWorkReview(); var other = review.Profiles[1]; Assert.False(review.CanUse(other.Id));
        Assert.True(await f.Service.SwitchReviewedAsync(review, other)); Assert.Equal(other.Id, f.Service.Current.Id);
        Assert.Equal(other.Id, ProfileService.CurrentBackupSet(f.Preferences)); Assert.True(File.Exists(f.Location.Path));
        Assert.True(await f.Service.SwitchToAsync(f.Service.All[0])); Assert.Null(ProfileService.CurrentBackupSet(f.Preferences));
    }

    [Fact, Trait("AT", "AT-158")]
    public async Task Authentication_await_rechecks_complete_original_catalogue_and_rolls_back_the_path()
    {
        using var f = new Fixture(); f.AddOthers(1); var other = f.Service.All[1]; await f.Service.SwitchToAsync(other);
        await f.Flow.Store.UpdateSettingsAsync(s => s.AppLockEnabled = true, Ct); await f.Service.SwitchToAsync(f.Service.All[0]);
        var review = f.Service.ReadWorkReview(); var path = f.Location.Path;
        // Authentication temporarily opens the destination, so simulate the outside catalogue write through its own current review.
        f.Runtime.DuringAuthentication = () => f.Service.Rename(f.Service.All[0], "Fictitious changed during auth");
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Service.SwitchReviewedAsync(review, other));
        Assert.Equal(path, f.Location.Path); Assert.True(f.Service.Current.IsMain); Assert.Null(ProfileService.CurrentBackupSet(f.Preferences));
    }

    [Fact, Trait("AT", "AT-158")]
    public void Two_service_writers_cannot_join_the_same_reviewed_free_slot()
    {
        using var f = new Fixture(); f.AddOthers(1); f.Enable(ProductPlan.Free); f.Service.SaveWorkChoice(f.Service.ReadWorkReview(), []);
        var other = new ProfileService(f.Location, f.Flow.Store, f.Preferences, f.Runtime, f.Flow.Translator, f.Source);
        var a = f.Service.ReadWorkReview(); var b = other.ReadWorkReview(); var created = f.Service.CreateReviewed(a, "Fictitious first");
        Assert.Throws<InvalidOperationException>(() => other.CreateReviewed(b, "Fictitious second"));
        Assert.Equal(created.Id, Assert.Single(f.Service.ReadWorkReview().SelectedIds)); Assert.Equal(3, f.Service.All.Count);
    }

    [Theory, InlineData(ProductPlan.Free, 1), InlineData(ProductPlan.Plus, -1), InlineData(ProductPlan.Pro, -1), Trait("AT", "AT-158")]
    public void Pure_device_policy_preserves_original_main_spelling_and_personal_capacity(ProductPlan plan, int expected)
    {
        var device = new QuotaScope(QuotaScopeKind.Device, Guid.NewGuid()); var personal = new EntitlementScope(EntitlementScopeKind.PersonalProfile, Guid.NewGuid());
        var id = Guid.NewGuid().ToString("N"); var result = LocalProfileWorkPolicy.Resolve(device, new(plan, personal), ["", id], [id]);
        Assert.Equal(expected == -1 ? null : expected, result.Maximum); Assert.Contains("", result.Ids);
        Assert.Equal(plan == ProductPlan.Free ? new[] { id } : new[] { "", id }, result.SelectedIds);
        Assert.Throws<ArgumentException>(() => LocalProfileWorkPolicy.Resolve(new(QuotaScopeKind.PersonalProfile, device.Id), new(plan, personal), ["", id]));
        Assert.Throws<ArgumentException>(() => LocalProfileWorkPolicy.Resolve(device, new(plan, new(EntitlementScopeKind.SharedSpace, Guid.NewGuid())), ["", id]));
        Assert.Throws<ArgumentException>(() => LocalProfileWorkPolicy.Resolve(device, new(plan, personal), ["", id, id]));
        Assert.Throws<ArgumentException>(() => LocalProfileWorkPolicy.Resolve(device, new(plan, personal), ["", "../unsafe"]));
    }

    [Fact, Trait("AT", "AT-158")]
    public async Task Actual_form_keeps_saved_original_availability_separate_from_unsaved_choices_and_saves_explicitly()
    {
        using var f = new Fixture(); f.AddOthers(2); f.Enable(ProductPlan.Free);
        var vm = new Vafadar.Zanance.App.Features.Profiles.ProfilesViewModel(f.Service, f.Flow.Translator, f.Flow.Platform, f.Flow.Platform);
        var raw = f.Preferences.Get(PacketKey); Assert.False(vm.CanInteract); await vm.LoadAsync();
        Assert.True(vm.CanChoose); Assert.False(vm.CanAdd); Assert.Equal(3, vm.Profiles.Count); Assert.Equal(raw, f.Preferences.Get(PacketKey));
        var later = vm.Profiles[^1]; vm.ToggleCommand.Execute(later); Assert.True(later.IsSelected); Assert.True(vm.IsDirty);
        Assert.Equal(f.Flow.Translator["Profile_WorkRetained"], later.WorkText); Assert.NotEqual(later.AccessibilityText, later.ChoiceDescription);
        vm.ToggleCommand.Execute(vm.Profiles[0]); Assert.True(vm.OverCapacity); Assert.False(vm.CanSave); Assert.NotNull(vm.CapacityError);
        vm.ToggleCommand.Execute(vm.Profiles[0]); await vm.SaveChoiceCommand.ExecuteAsync(null);
        Assert.Equal(later.Profile.Id, Assert.Single(f.Service.ReadWorkReview().SelectedIds)); Assert.False(vm.IsDirty);
        Assert.Equal(f.Flow.Translator["Profile_WorkAvailable"], vm.Profiles[^1].WorkText); Assert.True(f.Service.Current.IsMain);
        Assert.Empty(await f.Flow.Store.GetEntriesAsync(cancellationToken: Ct)); Assert.Equal(0, f.Flow.Platform.ProfileOpened);
    }

    [Fact, Trait("AT", "AT-158")]
    public async Task Failed_original_Save_retains_exact_draft_until_explicit_reload_and_discard()
    {
        using var f = new Fixture(); f.AddOthers(2); f.Enable(ProductPlan.Free);
        var vm = new Vafadar.Zanance.App.Features.Profiles.ProfilesViewModel(f.Service, f.Flow.Translator, f.Flow.Platform, f.Flow.Platform);
        await vm.LoadAsync(); var originalRows = vm.Profiles.ToArray(); vm.ToggleCommand.Execute(originalRows[^1]);
        f.Service.SaveWorkChoice(f.Service.ReadWorkReview(), [""]); var raw = f.Preferences.Get(PacketKey);
        await vm.SaveChoiceCommand.ExecuteAsync(null); Assert.False(vm.CanInteract); Assert.True(vm.IsDirty); Assert.True(originalRows[^1].IsSelected);
        Assert.Equal(originalRows, vm.Profiles); Assert.Equal(raw, f.Preferences.Get(PacketKey)); Assert.NotNull(vm.Error);
        f.Flow.Platform.Confirmations.Enqueue(false); await vm.ReloadCommand.ExecuteAsync(null);
        Assert.True(vm.IsDirty); Assert.Equal(originalRows, vm.Profiles);
        f.Flow.Platform.Confirmations.Enqueue(true); await vm.ReloadCommand.ExecuteAsync(null);
        Assert.True(vm.CanInteract); Assert.False(vm.IsDirty); Assert.True(vm.Profiles[0].IsSelected); Assert.False(vm.Profiles[^1].IsSelected);
        Assert.Equal(raw, f.Preferences.Get(PacketKey));
    }

    [Fact, Trait("AT", "AT-158")]
    public async Task Actual_Add_guard_excludes_duplicate_requests_and_rechecks_the_original_after_the_native_prompt()
    {
        using var f = new Fixture(); f.AddOthers(1);
        var vm = new Vafadar.Zanance.App.Features.Profiles.ProfilesViewModel(f.Service, f.Flow.Translator, f.Flow.Platform, f.Flow.Platform);
        await vm.LoadAsync(); var prompt = new TaskCompletionSource<string?>(); var entered = new TaskCompletionSource(); var calls = 0;
        f.Flow.Platform.PendingInput = () => { calls++; entered.TrySetResult(); return prompt.Task; };
        var pending = vm.AddCommand.ExecuteAsync(null); await entered.Task.WaitAsync(TimeSpan.FromSeconds(10), Ct);
        Assert.False(vm.CanLeave); Assert.False(vm.CanAdd); await vm.AddCommand.ExecuteAsync(null); Assert.Equal(1, calls);
        f.Service.Rename(f.Service.All[0], "Fictitious changed"); var raw = f.Preferences.Get(PacketKey); prompt.SetResult("Fictitious new"); await pending;
        Assert.Equal(2, f.Service.All.Count); Assert.Equal(raw, f.Preferences.Get(PacketKey)); Assert.NotNull(vm.Error); Assert.False(vm.CanInteract);
    }

    [Fact, Trait("AT", "AT-158")]
    public async Task Actual_second_delete_confirmation_cannot_delete_a_changed_same_id_original()
    {
        using var f = new Fixture(); f.AddOthers(1);
        var vm = new Vafadar.Zanance.App.Features.Profiles.ProfilesViewModel(f.Service, f.Flow.Translator, f.Flow.Platform, f.Flow.Platform);
        await vm.LoadAsync(); var row = vm.Profiles[1]; f.Flow.Platform.Choices.Enqueue(f.Flow.Translator["Common_Delete"]);
        var second = new TaskCompletionSource<bool>(); var entered = new TaskCompletionSource(); var count = 0;
        f.Flow.Platform.PendingConfirmation = () => { if (++count == 1) return Task.FromResult(true); entered.TrySetResult(); return second.Task; };
        var pending = vm.ChooseCommand.ExecuteAsync(row); await entered.Task.WaitAsync(TimeSpan.FromSeconds(10), Ct);
        f.Service.Rename(row.Profile, "Fictitious corrected"); var raw = f.Preferences.Get(PacketKey); second.SetResult(true); await pending;
        Assert.Equal(2, f.Service.All.Count); Assert.Equal(raw, f.Preferences.Get(PacketKey)); Assert.NotNull(vm.Error);
        Assert.Equal("Fictitious corrected", f.Service.All[1].Name); Assert.True(f.Service.Current.IsMain);
    }

    [Fact, Trait("AT", "AT-158")]
    public async Task Committed_choice_with_failed_publication_retries_only_a_read_and_cannot_repeat_Save()
    {
        using var f = new Fixture(); f.AddOthers(2); f.Enable(ProductPlan.Free);
        var vm = new Vafadar.Zanance.App.Features.Profiles.ProfilesViewModel(f.Service, f.Flow.Translator, f.Flow.Platform, f.Flow.Platform);
        await vm.LoadAsync(); vm.ToggleCommand.Execute(vm.Profiles[^1]);
        f.Service.Changed += (_, _) => f.Source.FailCapture = true;
        await vm.SaveChoiceCommand.ExecuteAsync(null); var committed = f.Preferences.Get(PacketKey);
        Assert.True(vm.Loading.HasFailed); Assert.False(vm.IsDirty); Assert.False(vm.CanSave);
        await vm.SaveChoiceCommand.ExecuteAsync(null); Assert.Equal(committed, f.Preferences.Get(PacketKey));
        f.Source.FailCapture = false; await vm.ReloadCommand.ExecuteAsync(null);
        Assert.True(vm.CanInteract); Assert.Equal(committed, f.Preferences.Get(PacketKey)); Assert.True(vm.Profiles[^1].IsSelected);
        Assert.Empty(await f.Flow.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Theory, InlineData("en"), InlineData("fa"), InlineData("de"), InlineData("es"), InlineData("fr"), InlineData("it"), Trait("AT", "AT-158")]
    public async Task Six_live_languages_retain_original_names_draft_id_and_saved_work_state_without_writes(string language)
    {
        using var f = new Fixture(); f.AddOthers(1); f.Enable(ProductPlan.Free);
        var vm = new Vafadar.Zanance.App.Features.Profiles.ProfilesViewModel(f.Service, f.Flow.Translator, f.Flow.Platform, f.Flow.Platform);
        await vm.LoadAsync(); var rows = vm.Profiles.ToArray(); var later = rows[1]; vm.ToggleCommand.Execute(later); var raw = f.Preferences.Get(PacketKey);
        f.Flow.Localization.SetLanguage(f.Flow.Localization.SupportedLanguages.First(l => l.CultureName == language)); vm.RefreshDisplay();
        Assert.Equal(rows, vm.Profiles); Assert.True(later.IsSelected); Assert.True(vm.IsDirty); Assert.Equal("Fictitious original 0", later.Name);
        Assert.Equal(f.Flow.Translator["Profile_Main"], rows[0].Name); Assert.Equal(f.Flow.Translator["Profile_WorkRetained"], later.WorkText);
        Assert.Contains(later.Name, later.ChoiceDescription); Assert.DoesNotContain("Profile_WorkHint", vm.AllowanceText);
        Assert.Equal(raw, f.Preferences.Get(PacketKey)); Assert.True(f.Service.Current.IsMain);
    }

    private sealed class Fixture : IDisposable
    {
        internal readonly FlowFixture Flow = new();
        internal readonly FaultPreferences Preferences = new();
        internal readonly WorkSource Source = new();
        internal readonly Runtime Runtime;
        internal readonly ProfileService Service;
        internal LocalDatabaseLocation<ZananceDbContext> Location => Flow.Services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        internal Fixture() { Runtime = new(Flow); Service = new(Location, Flow.Store, Preferences, Runtime, Flow.Translator, Source); }
        internal void AddOthers(int count) { for (var i = 0; i < count; i++) Service.Create("Fictitious original " + i); }
        internal void Enable(ProductPlan plan) => Source.Access = new(Path.Combine(Path.GetDirectoryName(Location.Path)!, "zanance.db"),
            Source.Device, new(plan, Source.Personal));
        public void Dispose() => Flow.Dispose();
    }
    private sealed class FaultPreferences : ISettingsStore
    {
        private readonly InMemorySettingsStore _inner = new(); internal bool FailSet;
        public string? Get(string key) => _inner.Get(key);
        public void Set(string key, string? value) { if (FailSet) throw new IOException("Fictitious preference failure."); _inner.Set(key, value); }
        public void Remove(string key) => _inner.Remove(key);
    }
    private sealed class WorkSource : IProfileWorkAccessSource
    {
        internal ProfileWorkAccess Access = ProfileWorkAccess.Inactive;
        internal readonly QuotaScope Device = new(QuotaScopeKind.Device, Guid.NewGuid());
        internal readonly EntitlementScope Personal = new(EntitlementScopeKind.PersonalProfile, Guid.NewGuid());
        internal bool FailCapture;
        public ProfileWorkAccess Capture(string cataloguePath) => FailCapture ? throw new IOException("Fictitious cached access failure.") : Access;
    }
    private sealed class Runtime(FlowFixture flow) : IProfileRuntime
    {
        internal int ExclusiveRuns;
        internal Action? DuringAuthentication;
        public async Task RunExclusiveAsync(Func<Task> action) { ExclusiveRuns++; await action(); }
        public Task MigrateAsync(Vafadar.Data.DatabaseAccessGate.ExclusiveLease owner) => flow.Services.MigrateLocalDatabaseForMaintenanceAsync<ZananceDbContext>(owner, Ct);
        public Task<bool> AuthenticateAsync(string reason) { DuringAuthentication?.Invoke(); return Task.FromResult(true); }
        public Task ReloadLockAsync() => Task.CompletedTask;
        public void DismissUndo() { }
    }
}
