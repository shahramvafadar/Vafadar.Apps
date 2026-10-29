using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data.Auditing;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Goals;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>D-640: complete real SQLite source, native lifetime continuations and accepted goal draft receipts.</summary>
[Collection("Entry validation units")]
public sealed class GoalEditorContinuationTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private sealed class Access : ICommercialWriteAccessSource
    {
        internal CommercialWriteAccess Current = CommercialWriteAccess.Inactive;
        public CommercialWriteAccess Capture(string path) => Current;
    }
    private sealed class Notifications(bool supported = false) : IGoalEditorNotifications
    {
        internal readonly TaskCompletionSource Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource<bool>? Answer;
        public bool IsSupported => supported;
        public Task<bool> EnableAsync()
        {
            if (!supported) throw new InvalidOperationException("No notification permission belongs to this continuation.");
            Entered.TrySetResult(); return Answer?.Task ?? Task.FromResult(true);
        }
    }
    private static GoalEditorViewModel Editor(FlowFixture f, GoalStore? goals = null, IGoalEditorNotifications? notifications = null)
    {
        var dates = new DateFormatter(f.Localization); var holdings = f.Services.GetRequiredService<HoldingStore>();
        return new(f.Store, goals ?? f.Services.GetRequiredService<GoalStore>(), f.Translator, f.Localization, dates, f.Time,
            holdings, new HoldingText(f.Translator, f.Localization, dates), f.Platform, notifications ?? new Notifications());
    }
    private static async Task<Goal> SeedAsync(FlowFixture f)
    {
        var settings = await f.Store.GetSettingsAsync(Ct); settings.Mode = ExperienceMode.Advanced;
        await f.Store.SaveSettingsAsync(settings, Ct);
        var goal = new Goal { Name = "D640 original goal", Type = GoalType.Earmark, CurrencyCode = "EUR", TargetAmount = 15000 };
        await f.Services.GetRequiredService<GoalStore>().SaveGoalAsync(goal, Ct); return goal;
    }
    private static GoalStore Controlled(FlowFixture f, AccountDetailOriginalBoundaryTests.ControlledFactory factory) =>
        new(factory, f.Services.GetRequiredService<ICommercialWriteAccessSource>());

    /// <summary>The new reader and validator do not initialize preferences, categories or financial history.</summary>
    [Fact]
    public async Task Empty_original_editor_read_and_validation_never_initialize_storage()
    {
        using var f = new FlowFixture(); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var goals = f.Services.GetRequiredService<GoalStore>();
        var source = await goals.ReadEditorSnapshotAsync(null, new(2026, 10, 9), cancellationToken: Ct);
        Assert.Null(source.Inputs.Settings); Assert.Empty(source.Inputs.Accounts); Assert.Empty(source.Inputs.Entries);
        await goals.ValidateEditorSnapshotAsync(source, Ct);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    /// <summary>Complete detached source facts cannot be changed after review and then validated as original.</summary>
    [Fact]
    public async Task Mutated_detached_preferences_refuse_validation_without_a_write()
    {
        using var f = new FlowFixture(); await SeedAsync(f); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var goals = f.Services.GetRequiredService<GoalStore>();
        var source = await goals.ReadEditorSnapshotAsync(null, new(2026, 10, 9), cancellationToken: Ct);
        source.Settings.DefaultCurrencyCode = "USD";
        await Assert.ThrowsAsync<InvalidOperationException>(() => goals.ValidateEditorSnapshotAsync(source, Ct));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    /// <summary>The original writer also validates complete non-goal references rather than only the goal choice.</summary>
    [Fact]
    public async Task Changed_original_category_refuses_the_entire_new_goal_writer()
    {
        using var f = new FlowFixture(); var category = new Category { Name = "D640 original category", Kind = CategoryKind.Expense };
        await f.Store.SaveCategoryAsync(category, Ct); var goals = f.Services.GetRequiredService<GoalStore>();
        var source = await goals.ReadEditorSnapshotAsync(null, new(2026, 10, 9), cancellationToken: Ct);
        category.Name = "D640 authoritative changed category"; await f.Store.SaveCategoryAsync(category, Ct);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var draft = new Goal { Name = "D640 must not commit", CurrencyCode = "EUR", Type = GoalType.Earmark, TargetAmount = 10000 };
        await Assert.ThrowsAsync<InvalidOperationException>(() => goals.SaveGoalWithContributionPlanAsync(draft, null, Ct,
            review: source.Goals, originalSource: source));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    /// <summary>A failed original refresh retains every raw field and Retry does not replay a writer.</summary>
    [Fact]
    public async Task Failed_refresh_and_explicit_retry_keep_raw_goal_money_and_note()
    {
        using var f = new FlowFixture(); var goal = await SeedAsync(f); var refuse = false;
        var factory = new AccountDetailOriginalBoundaryTests.ControlledFactory(AccountDetailOriginalBoundaryTests.Factory(f))
        { Before = _ => refuse ? Task.FromException(new IOException("D640 original read refusal.")) : Task.CompletedTask };
        var vm = Editor(f, Controlled(f, factory)); await vm.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = goal.Id });
        vm.Name = "  D640 raw goal  "; vm.AmountText = " 123.4500 "; vm.Note = "D640 retained\nsecond line";
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        refuse = true; await vm.ReloadCommand.ExecuteAsync(null); Assert.False(vm.IsReady); Assert.False(vm.CanSave);
        refuse = false; await vm.ReloadCommand.ExecuteAsync(null);
        Assert.True(vm.IsReady); Assert.True(vm.IsDirty);
        Assert.Equal(("  D640 raw goal  ", " 123.4500 ", "D640 retained\nsecond line"), (vm.Name, vm.AmountText, vm.Note));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    /// <summary>Native appearance shares a pending read but does not implicitly retry a completed failure.</summary>
    [Fact]
    public async Task Initial_appearance_keeps_failed_source_until_visible_retry()
    {
        using var f = new FlowFixture(); var goal = await SeedAsync(f); var refuse = true;
        var factory = new AccountDetailOriginalBoundaryTests.ControlledFactory(AccountDetailOriginalBoundaryTests.Factory(f))
        { Before = _ => refuse ? Task.FromException(new IOException("D640 fast original failure.")) : Task.CompletedTask };
        var vm = Editor(f, Controlled(f, factory)); await vm.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = goal.Id });
        Assert.False(vm.IsReady); refuse = false; vm.SetViewActive(true); await vm.EnsureLoadedAsync();
        Assert.False(vm.IsReady); Assert.Equal(f.Translator["Goal_EditTitle"], vm.Title);
        await vm.ReloadCommand.ExecuteAsync(null); Assert.True(vm.IsReady); Assert.Equal(goal.Name, vm.Name);
    }

    /// <summary>The same native form can resume its cancelled reader without replaying source preparation.</summary>
    [Fact]
    public async Task Same_goal_form_resumes_only_its_new_pending_owner()
    {
        using var f = new FlowFixture(); var goal = await SeedAsync(f); var first = true;
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var wait = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var factory = new AccountDetailOriginalBoundaryTests.ControlledFactory(AccountDetailOriginalBoundaryTests.Factory(f))
        { Before = async ct => { if (!first) return; first = false; entered.TrySetResult(); await wait.Task.WaitAsync(ct); } };
        var vm = Editor(f, Controlled(f, factory)); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var pending = vm.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = goal.Id }); await entered.Task.WaitAsync(Ct);
        vm.SetViewActive(false); await pending; Assert.False(vm.IsReady);
        vm.SetViewActive(true); await vm.EnsureLoadedAsync();
        Assert.True(vm.IsReady); Assert.Equal(goal.Name, vm.Name); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    /// <summary>Narrow actual parent authority pins the original file before the first child context can be adopted.</summary>
    [Fact]
    public async Task Original_parent_file_refuses_first_foreign_goal_editor_context()
    {
        using var f = new FlowFixture(); using var foreign = new FlowFixture(); var goal = await SeedAsync(f);
        var parent = await f.Services.GetRequiredService<GoalStore>().ReadListSnapshotAsync(Ct);
        var factory = new AccountDetailOriginalBoundaryTests.ControlledFactory(AccountDetailOriginalBoundaryTests.Factory(f))
        { Select = () => AccountDetailOriginalBoundaryTests.Factory(foreign) };
        var vm = Editor(f, Controlled(f, factory));
        var before = await GoalChooserFlowTests.SnapshotAsync(foreign);
        await vm.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = goal.Id, ["goalEditorOrigin"] = parent.CreateEditorOrigin() });
        Assert.False(vm.IsReady); Assert.Empty(vm.Name); Assert.False(vm.CanSave);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(foreign));
    }

    /// <summary>Reentrant native retirement cannot uncover or continue publishing the goal's remaining raw fields.</summary>
    [Fact]
    public async Task Retirement_during_name_publication_does_not_publish_amount_or_ready()
    {
        using var f = new FlowFixture(); var goal = await SeedAsync(f); var vm = Editor(f);
        vm.PropertyChanged += (_, args) => { if (args.PropertyName == nameof(vm.Name)) vm.SetViewActive(false); };
        await vm.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = goal.Id });
        Assert.False(vm.IsReady); Assert.Empty(vm.AmountText); Assert.False(vm.CanSave);
    }

    /// <summary>A native discard answer cannot close input that changed while that actual dialog was open.</summary>
    [Fact]
    public async Task Late_discard_answer_preserves_newer_goal_text_and_current_form()
    {
        using var f = new FlowFixture(); var goal = await SeedAsync(f); var vm = Editor(f);
        await vm.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = goal.Id }); vm.Note = "D640 first unsaved note";
        f.Platform.PendingConfirmation = () => { vm.Note = "D640 later unsaved note"; return Task.FromResult(true); };
        var before = await GoalChooserFlowTests.SnapshotAsync(f); await vm.CancelCommand.ExecuteAsync(null);
        Assert.Empty(f.Platform.Routes); Assert.Equal("D640 later unsaved note", vm.Note); Assert.True(vm.IsDirty);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    /// <summary>Accepted receipts precede both late input and notification failure; newer text needs its own discard.</summary>
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Accepted_goal_keeps_later_text_and_never_replays_save(bool notificationFails)
    {
        using var f = new FlowFixture(); var goal = await SeedAsync(f); var vm = Editor(f);
        await vm.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = goal.Id }); vm.Name = "D640 accepted metadata";
        f.Services.GetRequiredService<GoalStore>().Changed += (_, _) =>
        { vm.Note = "D640 text delivered after acceptance"; if (notificationFails) throw new IOException("D640 accepted publication failure."); };
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.Equal("D640 accepted metadata", (await f.Services.GetRequiredService<GoalStore>().GetGoalAsync(goal.Id, Ct))!.Name);
        Assert.False(vm.CanSave); Assert.True(vm.IsDirty); Assert.Equal(f.Translator["Goal_SavedDraftChanged"], notificationFails ? vm.SaveError!.Split("\n\n").Last() : vm.SaveError);
        Assert.Empty(f.Platform.Routes); var accepted = await GoalChooserFlowTests.SnapshotAsync(f);
        await vm.SaveCommand.ExecuteAsync(null); Assert.Equal(accepted, await GoalChooserFlowTests.SnapshotAsync(f));
        f.Platform.Confirmations.Enqueue(false); await vm.CancelCommand.ExecuteAsync(null); Assert.Empty(f.Platform.Routes);
        f.Platform.Confirmations.Enqueue(true); await vm.CancelCommand.ExecuteAsync(null); Assert.Single(f.Platform.Routes);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    /// <summary>An accepted writer may finish after native root retirement without sending that root a late alert or route.</summary>
    [Fact]
    public async Task Accepted_goal_after_root_retirement_has_no_late_native_effect()
    {
        using var f = new FlowFixture(); var goal = await SeedAsync(f); var vm = Editor(f); var root = true;
        vm.BindRootOwner(() => root); await vm.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = goal.Id }); vm.Note = "D640 accepted original root";
        f.Services.GetRequiredService<GoalStore>().Changed += (_, _) => { root = false; throw new IOException("D640 retired publication."); };
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.Equal("D640 accepted original root", (await f.Services.GetRequiredService<GoalStore>().GetGoalAsync(goal.Id, Ct))!.Note);
        Assert.Empty(f.Platform.Routes); Assert.Empty(f.Platform.Alerts); Assert.Empty(f.Platform.Failures); Assert.False(vm.CanSave);
    }

    /// <summary>The actual original audit must still own both goal and contribution plan before their transaction commits.</summary>
    [Fact]
    public async Task Retirement_at_original_audit_rolls_back_goal_and_plan_and_preserves_draft()
    {
        using var f = new FlowFixture(); var goal = await SeedAsync(f); var vm = Editor(f);
        await vm.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = goal.Id }); vm.Name = "D640 pending audit"; vm.ContributionText = "10.00";
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var reached = false;
        f.Time.BeforeRead = () =>
        {
            if (!new StackTrace().GetFrames().Any(frame => frame.GetMethod()?.DeclaringType == typeof(AuditingSaveChangesInterceptor))) return;
            reached = true; f.Time.BeforeRead = null; vm.SetViewActive(false);
        };
        try { await vm.SaveCommand.ExecuteAsync(null); } finally { f.Time.BeforeRead = null; }
        Assert.True(reached); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.Equal("D640 pending audit", vm.Name); Assert.Equal("10.00", vm.ContributionText); Assert.Empty(f.Platform.Routes);
    }

    /// <summary>Complete original ledger payload guards include tags independently of the goal work reference stamp.</summary>
    [Fact]
    public async Task Detached_original_ledger_tag_mutation_cannot_validate_as_original()
    {
        using var f = new FlowFixture(); var goal = await SeedAsync(f);
        var account = new Account { Name = "D640 tag source", Type = AccountType.Checking, CurrencyCode = "EUR", OpeningDate = new(2026, 10, 1) };
        await f.Store.SaveAccountAsync(account, Ct);
        await using (var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(Ct))
        {
            db.Entries.Add(new LedgerEntry { Kind = EntryKind.Income, AccountId = account.Id, Amount = 1234,
                Date = new(2026, 10, 2), Tags = ["D640 original tag"] });
            await db.SaveChangesAsync(Ct);
        }
        var goals = f.Services.GetRequiredService<GoalStore>(); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var source = await goals.ReadEditorSnapshotAsync(goal.Id, new(2026, 10, 9), cancellationToken: Ct);
        Assert.Equal("D640 original tag", Assert.Single(source.Inputs.Entries).Tags.Single());
        source.Inputs.Entries.Single().Tags.Add("D640 altered detached tag");
        await Assert.ThrowsAsync<InvalidOperationException>(() => goals.ValidateEditorSnapshotAsync(source, Ct));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    /// <summary>A real local-day transition refuses a stale preview and offers explicit source review without losing text.</summary>
    [Fact]
    public async Task Local_day_change_requires_explicit_source_reload_and_preserves_raw_draft()
    {
        using var f = new FlowFixture(); var goal = await SeedAsync(f); var vm = Editor(f);
        await vm.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = goal.Id }); vm.Name = "  D640 next day input  ";
        var before = await GoalChooserFlowTests.SnapshotAsync(f); f.Time.Now = f.Time.Now.AddDays(1);
        await vm.SaveCommand.ExecuteAsync(null); Assert.NotNull(vm.SaveError); Assert.True(vm.CanReload); Assert.Empty(f.Platform.Routes);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        await vm.ReloadCommand.ExecuteAsync(null); Assert.True(vm.IsReady); Assert.Equal("  D640 next day input  ", vm.Name);
        Assert.True(vm.RequiresReview); vm.AcceptReviewCommand.Execute(null); Assert.False(vm.RequiresReview);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    /// <summary>Only the new display owner can publish after a pending original acquisition is cancelled.</summary>
    [Fact]
    public async Task Display_change_retires_pending_source_and_only_new_owner_publishes()
    {
        using var f = new FlowFixture(); var goal = await SeedAsync(f); var first = true;
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var wait = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var factory = new AccountDetailOriginalBoundaryTests.ControlledFactory(AccountDetailOriginalBoundaryTests.Factory(f))
        { Before = async ct => { if (!first) return; first = false; entered.TrySetResult(); await wait.Task.WaitAsync(ct); } };
        var vm = Editor(f, Controlled(f, factory)); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var pending = vm.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = goal.Id }); await entered.Task.WaitAsync(Ct);
        f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(item => item.CultureName == "de")); vm.RefreshDisplay();
        await pending; await vm.EnsureLoadedAsync(); Assert.True(vm.IsReady); Assert.Equal(f.Translator["Goal_EditTitle"], vm.Title);
        Assert.Equal(goal.Name, vm.Name); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    /// <summary>A late real permission-port answer has no effects on a retired original form or its source.</summary>
    [Fact]
    public async Task Retired_original_permission_answer_cannot_refresh_or_notify_another_owner()
    {
        using var f = new FlowFixture(); var goal = await SeedAsync(f);
        var notifications = new Notifications(true) { Answer = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        var vm = Editor(f, notifications: notifications); await vm.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = goal.Id });
        var before = await GoalChooserFlowTests.SnapshotAsync(f); vm.ReminderEnabled = true;
        await notifications.Entered.Task.WaitAsync(Ct); vm.SetViewActive(false); notifications.Answer.SetResult(false);
        for (var i = 0; i < 100 && vm.IsBusy; i++) await Task.Delay(10, Ct);
        Assert.False(vm.IsBusy); Assert.False(vm.IsReady); Assert.Null(vm.ReminderNote);
        Assert.Empty(f.Platform.Failures); Assert.Empty(f.Platform.Alerts); Assert.Empty(f.Platform.Routes);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    /// <summary>Retired cached work rights disable the stale Save while keeping the exact editable draft for explicit reload.</summary>
    [Fact]
    public async Task Changed_cached_goal_rights_disable_save_until_explicit_source_reload()
    {
        using var f = new FlowFixture(); var goal = await SeedAsync(f); var access = new Access();
        var goals = new GoalStore(AccountDetailOriginalBoundaryTests.Factory(f), access); var vm = Editor(f, goals);
        await vm.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = goal.Id }); vm.Note = "  D640 retained rights draft  ";
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        access.Current = new(f.Directory.Combine("zanance.db"), new(ProductPlan.Free,
            new(EntitlementScopeKind.PersonalProfile, Guid.Parse("64646464-3434-4545-6767-898989898989"))));
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.False(vm.CanSave); Assert.True(vm.CanReload); Assert.NotNull(vm.SaveError);
        Assert.Equal("  D640 retained rights draft  ", vm.Note); Assert.True(vm.IsDirty);
        Assert.Empty(f.Platform.Routes); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        await vm.ReloadCommand.ExecuteAsync(null); Assert.True(vm.IsReady);
        Assert.Equal("  D640 retained rights draft  ", vm.Note); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
}
