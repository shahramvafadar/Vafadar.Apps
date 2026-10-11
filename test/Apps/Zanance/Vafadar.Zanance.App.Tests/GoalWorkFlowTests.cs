using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Reminders;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Reminders;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-147: complete goal reads, scoped contribution writes and actual reminder/lock application flows.</summary>
[Trait("AT", "AT-147")]
public sealed class GoalWorkFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly Guid ScopeId = Guid.Parse("31313131-4242-5353-6464-757575757575");
    private static readonly QuotaScope Scope = new(QuotaScopeKind.PersonalProfile, ScopeId);
    private sealed class AccessSource : ICommercialWriteAccessSource
    {
        internal CommercialWriteAccess Current = CommercialWriteAccess.Inactive;
        public CommercialWriteAccess Capture(string databasePath) => Current;
    }
    private sealed class NativeScheduler : IReminderScheduler
    {
        internal IReadOnlyList<ReminderNotification> Pending = [];
        internal Func<Task>? OnReplace;
        internal int Replacements;
        public bool IsSupported => true;
        public Task<bool> AreEnabledAsync() => Task.FromResult(true);
        public Task<bool> RequestPermissionAsync() => throw new InvalidOperationException("No permission request expected.");
        public void SetSnoozeActions(string oneHour, string tomorrow) { }
        public event EventHandler<string>? Tapped { add { } remove { } }
        public event EventHandler<SnoozeRequest>? SnoozeRequested { add { } remove { } }
        public Task RemoveAsync(int notificationId) => Task.CompletedTask;
        public Task ShowAsync(ReminderNotification notification) => throw new InvalidOperationException("No immediate alert expected.");
        public async Task ReplaceAllAsync(IReadOnlyList<ReminderNotification> notifications)
        {
            Pending = notifications; Replacements++;
            if (OnReplace is { } changed) { OnReplace = null; await changed(); }
        }
    }
    private sealed record Seed(Goal[] Items, Account Account, AccessSource Access, GoalStore Goals,
        PlanStore Plans, ResourceChoiceStore Choices, NativeScheduler Native, ReminderService Service);

    private static async Task<Seed> SeedAsync(FlowFixture f)
    {
        var day = DateOnly.FromDateTime(f.Time.Now.DateTime);
        var account = new Account { Name = "Owned goal funding", CurrencyCode = "EUR", OpeningDate = day.AddDays(-2), OpeningBalance = 10000 };
        await f.Store.SaveAccountAsync(account, Ct); var writer = f.Services.GetRequiredService<GoalStore>();
        var items = Enumerable.Range(0, 6).Select(i => new Goal
        {
            Name = "Owned retained goal " + i, CurrencyCode = "EUR", TargetAmount = 50000,
            Type = i == 0 ? GoalType.AccountBalance : GoalType.Earmark, AccountId = i == 0 ? account.Id : null,
            Protect = i == 1, Priority = i == 1 ? GoalPriority.High : GoalPriority.Low,
            State = i == 3 ? GoalState.Paused : i == 4 ? GoalState.Completed : i == 5 ? GoalState.Archived : GoalState.Active,
        }).ToArray();
        foreach (var goal in items)
            await writer.SaveGoalWithContributionPlanAsync(goal, new ContributionPlan
            { GoalId = goal.Id, Amount = 1000, ReminderEnabled = true,
                Rule = new() { Start = day.AddDays(1), Frequency = Frequency.Weekly } }, Ct);
        foreach (var goal in items.Skip(1).Take(2))
            await writer.AddAllocationAsync(new() { GoalId = goal.Id, AccountId = account.Id, Amount = 8000, Date = day }, Ct);
        await f.Store.GetSettingsAsync(Ct);
        var access = new AccessSource(); var factory = f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        var goals = new GoalStore(factory, access); var plans = new PlanStore(factory, f.Store, access);
        var native = new NativeScheduler();
        return new(items, account, access, goals, plans, new(factory, access), native,
            new(native, f.Store, plans, goals, f.Services.GetRequiredService<HoldingStore>(), f.Preferences,
                f.Translator, new DateFormatter(f.Localization), f.Localization, f.Time));
    }
    private static void Enable(FlowFixture f, Seed s, ProductPlan plan = ProductPlan.Free) =>
        s.Access.Current = new(f.Directory.Combine("zanance.db"), new(plan, new(EntitlementScopeKind.PersonalProfile, ScopeId)));
    private static Task<string> RowsAsync(FlowFixture f, bool choices = true) => GoalChooserFlowTests.SnapshotAsync(f, choices);
    private static async Task ChooseAsync(Seed s, params Guid[] ids) =>
        await s.Choices.SaveAsync(await s.Choices.ReadAsync(QuotaKind.Goals, Ct), ids, Ct);
    private static async Task SqlAsync(FlowFixture f, string sql)
    {
        await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        await db.Database.ExecuteSqlRawAsync(sql, Ct);
    }

    [Theory]
    [InlineData("missing"), InlineData("empty"), InlineData("balance"), InlineData("advanced"), InlineData("paused"), InlineData("plus"), InlineData("inactive")]
    public async Task Work_projection_keeps_every_original_rule_earmark_and_complete_funding_input(string choice)
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f);
        if (choice != "inactive") Enable(f, s);
        if (choice is "empty" or "balance" or "advanced" or "paused")
            await ChooseAsync(s, choice == "empty" ? [] : [s.Items[choice == "balance" ? 0 : choice == "advanced" ? 1 : 3].Id]);
        if (choice == "plus") Enable(f, s, ProductPlan.Plus);
        var before = await RowsAsync(f); var snapshot = await s.Goals.GetWorkSnapshotAsync(Ct);
        Assert.Equal(6, snapshot.Goals.Count); Assert.Equal(6, snapshot.ContributionPlans.Count); Assert.Equal(2, snapshot.Allocations.Count);
        Assert.Equal(choice is "inactive" ? 6 : choice == "plus" ? 3 : choice == "balance" ? 1 : 0,
            snapshot.Goals.Count(goal => snapshot.Work.CanUseGoal(goal.Id)));
        Assert.Equal(choice == "inactive" ? 6 : choice == "plus" ? 3 : 0, snapshot.Goals.Count(goal => snapshot.Work.CanRemind(goal.Id)));
        var progress = GoalProgressService.Evaluate(snapshot.Goals, snapshot.Allocations, await f.Store.GetAccountsAsync(cancellationToken: Ct),
            await f.Store.GetEntriesAsync(cancellationToken: Ct), snapshot.ContributionPlans, DateOnly.FromDateTime(f.Time.Now.DateTime));
        Assert.Equal(8000, progress.Single(p => p.Goal.Id == s.Items[1].Id).Current);
        Assert.Equal(2000, progress.Single(p => p.Goal.Id == s.Items[2].Id).Current);
        await s.Goals.ValidateWorkSnapshotAsync(snapshot, Ct); Assert.Equal(before, await RowsAsync(f));
    }

    [Theory]
    [InlineData("free"), InlineData("plus"), InlineData("inactive")]
    public async Task Actual_native_delivery_filters_paid_activity_before_cap_and_never_mutates_original_data(string access)
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f);
        if (access != "inactive") Enable(f, s, access == "plus" ? ProductPlan.Plus : ProductPlan.Free);
        var before = await RowsAsync(f); await s.Service.RefreshAsync();
        if (access == "free") Assert.Empty(s.Native.Pending);
        else
        {
            Assert.NotEmpty(s.Native.Pending);
            Assert.All(s.Native.Pending, n =>
            {
                var link = GoalReminderLink.Parse(n.Link)!; Assert.Equal(3, link.Targets.Count);
                Assert.Equal(s.Items.Take(3).Select(goal => goal.Id).Order(), link.Targets.Select(target => target.GoalId).Order());
                Assert.Equal(f.Translator["App_Name"], n.Title); Assert.Equal(f.Translator["Reminder_GoalGeneric"], n.Body);
                Assert.Equal(9, n.NotifyAt!.Value.Hour); Assert.False(n.CanSnooze);
            });
        }
        Assert.Equal(before, await RowsAsync(f));
    }

    [Theory]
    [InlineData(false), InlineData(true)]
    public async Task Retired_persisted_goal_revision_during_native_publication_clears_the_queue_even_after_ABA(bool aba)
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); Enable(f, s); await ChooseAsync(s, s.Items[0].Id);
        Enable(f, s, ProductPlan.Plus); var before = await RowsAsync(f, false);
        s.Native.OnReplace = () => SqlAsync(f, aba
            ? "UPDATE ResourceSelections SET IdentitySet='', Revision='11111111-2222-3333-4444-555555555555'; UPDATE ResourceSelections SET IdentitySet='" + s.Items[0].Id.ToString("N") + "', Revision='22222222-3333-4444-5555-666666666666'"
            : "UPDATE ResourceSelections SET Revision='33333333-4444-5555-6666-777777777777'");
        await s.Service.RefreshAsync(); Assert.Equal(2, s.Native.Replacements); Assert.Empty(s.Native.Pending);
        Assert.Equal(before, await RowsAsync(f, false));
    }

    [Theory]
    [InlineData(false), InlineData(true)]
    public async Task Changed_rights_or_activation_retires_the_captured_goal_read(bool initiallyInactive)
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f);
        if (!initiallyInactive) Enable(f, s, ProductPlan.Plus);
        var read = await s.Goals.GetWorkSnapshotAsync(Ct);
        if (initiallyInactive) Enable(f, s); else s.Access.Current = CommercialWriteAccess.Inactive;
        await Assert.ThrowsAsync<InvalidOperationException>(() => s.Goals.ValidateWorkSnapshotAsync(read, Ct));
    }

    [Fact]
    public async Task Another_actual_file_cannot_validate_or_route_the_same_restored_goal_identity()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); var original = await s.Goals.GetWorkSnapshotAsync(Ct);
        using var other = new FlowFixture(); var store = other.Services.GetRequiredService<GoalStore>();
        var restored = new Goal { Name = "Restored identity", Type = GoalType.Earmark, CurrencyCode = "EUR" };
        await store.SaveGoalAsync(restored, Ct);
        await using (var db = await other.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Goals SET Id={s.Items[0].Id} WHERE Id={restored.Id}", Ct);
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.ValidateWorkSnapshotAsync(original, Ct));
        var security = AppLockTests.Create(other); await security.Lock.StartAsync();
        var router = new AppLinkRouter(security.Lock, other.Platform, other.Services.GetRequiredService<PlanStore>(), store, other.Store);
        await router.OpenAsync(GoalReminderLink.Format(original.ReminderScope, [new(s.Items[0].Id, new(2026, 10, 10))]));
        Assert.Empty(other.Platform.Routes);
    }

    [Theory]
    [InlineData(false), InlineData(true)]
    public async Task Original_goal_members_resolve_after_unlock_and_open_history_without_posting_money(bool group)
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); var read = await s.Goals.GetWorkSnapshotAsync(Ct);
        await AppLockTests.EnableDeviceAsync(f); var security = AppLockTests.Create(f); await security.Lock.StartAsync();
        var router = new AppLinkRouter(security.Lock, f.Platform, s.Plans, s.Goals, f.Store);
        var targets = group ? s.Items.Take(2).Select(goal => new GoalReminderTarget(goal.Id, new(2026, 10, 10)))
            : [new GoalReminderTarget(s.Items[0].Id, new(2026, 10, 10))];
        await router.OpenAsync(GoalReminderLink.Format(read.ReminderScope, targets)); Assert.Empty(f.Platform.Routes);
        Enable(f, s); await ChooseAsync(s, []); var before = await RowsAsync(f);
        await security.Lock.UnlockedAsync(); var navigation = Assert.Single(f.Platform.Routes);
        Assert.Equal(group ? AppRoutes.GoalsRoute : AppRoutes.GoalDetailRoute, navigation.Route);
        if (!group) Assert.Equal(s.Items[0].Id, navigation.Parameters!["id"]);
        Assert.Equal(before, await RowsAsync(f));
    }

    [Theory]
    [InlineData(false), InlineData(true)]
    public async Task New_paid_contribution_to_a_historical_goal_rejects_before_pin_or_protection_changes(bool paired)
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); Enable(f, s, ProductPlan.Plus);
        var goal = s.Items[4]; await s.Goals.SaveContributionPlanAsync(goal.Id, null, Ct);
        goal.HomePin = 3; goal.Protect = true; var before = await RowsAsync(f);
        var draft = new ContributionPlan { GoalId = goal.Id, Amount = 2222, Rule = new() { Start = new(2026, 10, 10), Frequency = Frequency.Monthly } };
        var rejected = await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => paired
            ? s.Goals.SaveGoalWithContributionPlanAsync(goal, draft, Ct) : s.Goals.SaveContributionPlanAsync(goal.Id, draft, Ct));
        Assert.Equal(QuotaKind.Goals, rejected.Quota); Assert.Equal(3, goal.HomePin); Assert.True(goal.Protect);
        Assert.Equal(before, await RowsAsync(f));
    }

    [Fact]
    public async Task A_new_goal_and_its_paid_contribution_join_the_same_actual_writer_without_a_ledger_post()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); Enable(f, s, ProductPlan.Plus);
        var goal = new Goal { Name = "Explicit new paid goal", CurrencyCode = "EUR", Type = GoalType.Earmark };
        var draft = new ContributionPlan { GoalId = goal.Id, Amount = 2222, ReminderEnabled = true,
            Rule = new() { Start = new(2026, 10, 10), Frequency = Frequency.Monthly } };
        await s.Goals.SaveGoalWithContributionPlanAsync(goal, draft, Ct);
        Assert.NotNull(await s.Goals.GetGoalAsync(goal.Id, Ct));
        Assert.Equal(2222, (await s.Goals.GetContributionPlansAsync(Ct)).Single(plan => plan.GoalId == goal.Id).Amount);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct)); Assert.Equal(2, (await s.Goals.GetAllocationsAsync(cancellationToken: Ct)).Count);
    }

    [Fact]
    public async Task Retained_correction_and_disabling_reminders_remain_available_with_an_explicit_empty_choice()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); Enable(f, s); await ChooseAsync(s, []);
        var plan = (await s.Goals.GetContributionPlansAsync(Ct)).Single(p => p.GoalId == s.Items[1].Id);
        plan.Amount = 3333; plan.ReminderEnabled = false;
        await s.Goals.SaveContributionPlanAsync(plan.GoalId, plan, Ct);
        var saved = (await s.Goals.GetContributionPlansAsync(Ct)).Single(p => p.GoalId == plan.GoalId);
        Assert.Equal(3333, saved.Amount); Assert.False(saved.ReminderEnabled);
        Assert.Empty((await s.Choices.ReadAsync(QuotaKind.Goals, Ct)).SelectedIds);
        Assert.Equal(2, (await s.Goals.GetAllocationsAsync(cancellationToken: Ct)).Count);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Starting_only_a_reminder_still_requires_a_usable_original_goal()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); Enable(f, s, ProductPlan.Plus);
        var goal = s.Items[4]; await s.Goals.SaveContributionPlanAsync(goal.Id, null, Ct); var before = await RowsAsync(f);
        var plan = new ContributionPlan { GoalId = goal.Id, ReminderEnabled = true,
            Rule = new() { Start = new(2026, 10, 10), Frequency = Frequency.Monthly } };
        var error = await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => s.Goals.SaveContributionPlanAsync(goal.Id, plan, Ct));
        Assert.Equal(CommercialFeature.ContributionReviewReminders, error.Feature); Assert.Equal(goal.Id, error.ResourceId);
        Assert.Equal(before, await RowsAsync(f));
    }

    [Theory]
    [InlineData(false), InlineData(true)]
    public async Task Malformed_or_foreign_persisted_goal_choice_blocks_new_work_without_rewriting_original_inputs(bool foreign)
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); Enable(f, s); await ChooseAsync(s, s.Items[0].Id);
        await SqlAsync(f, foreign ? "UPDATE ResourceSelections SET ScopeId='99999999-8888-7777-6666-555555555555'"
            : "UPDATE ResourceSelections SET IdentitySet='Malformed owned choice'");
        var before = await RowsAsync(f); var read = await s.Goals.GetWorkSnapshotAsync(Ct);
        Assert.All(read.Goals, goal => Assert.False(read.Work.CanUseGoal(goal.Id)));
        Assert.Equal(6, read.ContributionPlans.Count); Assert.Equal(2, read.Allocations.Count);
        Assert.Equal(before, await RowsAsync(f));
    }

    [Fact]
    public async Task Native_publication_drops_its_queue_when_cached_rights_retire_during_the_await()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); Enable(f, s, ProductPlan.Plus); var before = await RowsAsync(f);
        s.Native.OnReplace = () => { Enable(f, s); return Task.CompletedTask; };
        await s.Service.RefreshAsync(); Assert.Empty(s.Native.Pending); Assert.Equal(2, s.Native.Replacements);
        Assert.Equal(before, await RowsAsync(f));
    }

    [Theory]
    [InlineData("state"), InlineData("rule"), InlineData("funding")]
    public async Task Changed_goal_inputs_during_native_publication_retire_the_captured_queue(string change)
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); Enable(f, s, ProductPlan.Plus);
        s.Native.OnReplace = async () =>
        {
            await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
            var changed = change switch
            {
                "state" => await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Goals SET State=3 WHERE Id={s.Items[0].Id}", Ct),
                "rule" => await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE ContributionPlans SET ReminderEnabled=0 WHERE GoalId={s.Items[0].Id}", Ct),
                _ => await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE GoalAllocations SET Amount=9999 WHERE GoalId={s.Items[1].Id}", Ct),
            };
            Assert.True(changed > 0);
        };
        await s.Service.RefreshAsync(); Assert.Empty(s.Native.Pending); Assert.Equal(2, s.Native.Replacements);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Theory]
    [InlineData(false), InlineData(true)]
    public async Task Shared_membership_and_host_rights_never_extend_personal_Pro_to_new_goal_activity(bool member)
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f);
        var context = new CapabilityContext(ProductPlan.Pro, new(EntitlementScopeKind.SharedSpace, ScopeId), new(ScopeId, member, false));
        var work = GoalWorkPolicy.Resolve(context, new(QuotaScopeKind.SharedSpace, ScopeId), s.Items);
        Assert.All(s.Items, goal => { Assert.False(work.CanUseGoal(goal.Id)); Assert.False(work.CanContribute(goal.Id)); Assert.False(work.CanRemind(goal.Id)); });
    }

    [Fact]
    public async Task Goal_policy_captures_original_eligibility_and_rejects_a_foreign_choice_scope()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f);
        var context = new CapabilityContext(ProductPlan.Free, new(EntitlementScopeKind.PersonalProfile, ScopeId));
        var selection = new ResourceSelection(QuotaKind.Goals, Scope, [s.Items[0].Id]);
        var work = GoalWorkPolicy.Resolve(context, Scope, s.Items, selection); s.Items[0].State = GoalState.Archived;
        Assert.True(work.CanUseGoal(s.Items[0].Id)); Assert.False(work.CanContribute(s.Items[0].Id)); Assert.False(work.CanRemind(s.Items[0].Id));
        Assert.Throws<ArgumentException>(() => GoalWorkPolicy.Resolve(context, Scope, s.Items,
            new(QuotaKind.Goals, new(QuotaScopeKind.PersonalProfile, Guid.NewGuid()), [])));
    }

    [Theory]
    [InlineData(""), InlineData("goals"), InlineData("goal|invalid"), InlineData("goal-reminder-v1|invalid|invalid")]
    public void Unscoped_or_malformed_goal_payloads_are_not_guessed(string input) => Assert.Null(GoalReminderLink.Parse(input));

    [Fact]
    public void Scoped_goal_payload_rejects_duplicate_oversized_and_invalid_original_members()
    {
        var id = Guid.NewGuid(); var scope = new string('a', 64); var target = new GoalReminderTarget(id, new(2026, 10, 10));
        var formatted = GoalReminderLink.Format(scope, [target]); var parsed = GoalReminderLink.Parse(formatted)!;
        Assert.Equal(scope, parsed.Scope); Assert.Equal(target, Assert.Single(parsed.Targets));
        Assert.Null(GoalReminderLink.Parse(formatted + ";" + id.ToString("N") + "@2026-10-10"));
        Assert.Null(GoalReminderLink.Parse(formatted.Replace("2026-10-10", "2026-02-30", StringComparison.Ordinal)));
        Assert.Null(GoalReminderLink.Parse(formatted + new string('x', 1500)));
        Assert.Throws<ArgumentException>(() => GoalReminderLink.Format(scope, Enumerable.Repeat(target, ReminderPlanner.MaxPending + 1)));
        Assert.Throws<ArgumentException>(() => GoalReminderLink.Format(scope, [target with { GoalId = Guid.Empty }]));
    }
}
