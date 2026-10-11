using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Testing;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data.Tests.Commerce;

/// <summary>AT-125: retained earmarks, contribution rights and complete goal editor writes on real SQLite.</summary>
[Trait("AT", "AT-125")]
public sealed class GoalContributionWritePolicyTests : IDisposable
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly DateOnly Day = new(2026, 10, 10);
    private static readonly Guid ScopeId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private readonly TemporaryDirectory _directory = new();
    private readonly List<ServiceProvider> _providers = [];

    private static CapabilityContext Context(ProductPlan plan, bool shared = false, bool member = true, bool host = true) =>
        new(plan, new(shared ? EntitlementScopeKind.SharedSpace : EntitlementScopeKind.PersonalProfile, ScopeId),
            shared ? new(ScopeId, member, host) : null);

    private ServiceProvider Provider(string path, AccessSource source, bool retireAfterSave = false)
    {
        var services = new ServiceCollection().AddSingleton<ICommercialWriteAccessSource>(source)
            .AddSingleton<TimeProvider>(new FixedTime()).AddZananceData(path);
        if (retireAfterSave)
        {
            // Decorate the actual production factory: its own SQLite options must remain in use.
            var original = services.Single(d => d.ServiceType == typeof(IDbContextFactory<ZananceDbContext>));
            services.Remove(original);
            services.AddSingleton<IDbContextFactory<ZananceDbContext>>(provider => new RetiringConnections(
                (IDbContextFactory<ZananceDbContext>)ActivatorUtilities.CreateInstance(provider, original.ImplementationType!), source));
        }
        var provider = services.BuildServiceProvider(); provider.MigrateLocalDatabase<ZananceDbContext>();
        _providers.Add(provider); return provider;
    }

    private async Task<Fixture> FixtureAsync(bool retireAfterSave = false)
    {
        var path = _directory.Combine(Guid.NewGuid()+".db"); var source = new AccessSource(path);
        var provider = Provider(path, source, retireAfterSave); var store = provider.GetRequiredService<ZananceStore>();
        var goals = provider.GetRequiredService<GoalStore>();
        var account = new Account { Name = "Original account", CurrencyCode = "EUR", OpeningDate = Day, OpeningBalance = 10000 };
        await store.SaveAccountAsync(account, Ct);
        var goal = new Goal { Name = "Retained earmark", Type = GoalType.Earmark, TargetAmount = 5000, CurrencyCode = "EUR",
            Note = "Complete original note", Protect = true, HomePin = 1 };
        var plan = Plan(goal.Id); plan.Amount = 1000; plan.ReminderEnabled = true;
        await goals.SaveGoalWithContributionPlanAsync(goal, plan, Ct);
        var allocation = new GoalAllocation { GoalId = goal.Id, AccountId = account.Id, Amount = 2500, Date = Day, Note = "Original funding" };
        await goals.AddAllocationAsync(allocation, Ct);
        return new(path, provider, source, store, goals, account, goal, plan, allocation);
    }

    private sealed record Fixture(string Path, ServiceProvider Provider, AccessSource Source, ZananceStore Store, GoalStore Goals,
        Account Account, Goal Goal, ContributionPlan Plan, GoalAllocation Allocation)
    {
        public void Enable(ProductPlan plan = ProductPlan.Free, bool shared = false, bool member = true, bool host = true) =>
            Source.Current = new(Path, Context(plan, shared, member, host));
        public GoalAllocation AllocationDraft(long amount) => new() { GoalId = Goal.Id, AccountId = Account.Id, Amount = amount, Date = Day };
        public Goal BalanceGoal(string name = "New balance goal") => new() { Name = name, CurrencyCode = "EUR", TargetAmount = 9000,
            Type = GoalType.AccountBalance, AccountId = Account.Id, State = GoalState.Active, HomePin = 3 };
    }

    private static ContributionPlan Plan(Guid goalId) => new() { GoalId = goalId, Rule = new() { Start = Day, Frequency = Frequency.Monthly } };

    private static async Task SqlAsync(Fixture f, string sql)
    {
        await using var db = await f.Provider.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        await db.Database.ExecuteSqlRawAsync(sql, Ct);
    }

    public void Dispose()
    {
        foreach (var provider in _providers) provider.Dispose();
        SqliteTestPools.Clear(_directory); _directory.Dispose();
    }

    [Fact]
    public async Task Inactive_registration_saves_complete_draft_and_earmark_without_posting_money()
    {
        var f = await FixtureAsync(); var goal = f.BalanceGoal(); var plan = Plan(goal.Id); plan.Amount = 1000;
        var events = 0; f.Goals.Changed += (_, _) => events++;
        await f.Goals.SaveGoalWithContributionPlanAsync(goal, plan, Ct);
        await f.Goals.AddAllocationAsync(f.AllocationDraft(1000), Ct);
        Assert.Equal(2, events); Assert.Equal(2, (await f.Goals.GetGoalsAsync(Ct)).Count);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        Assert.Equal(10000, (await f.Store.GetAccountsAsync(cancellationToken: Ct)).Single().OpeningBalance);
    }

    [Fact]
    public async Task Free_cannot_add_positive_earmark_and_never_releases_existing_money_silently()
    {
        var f = await FixtureAsync(); f.Enable(); var before = await SnapshotAsync(f.Provider); var events = 0;
        f.Goals.Changed += (_, _) => events++;
        var error = await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => f.Goals.AddAllocationAsync(f.AllocationDraft(1000), Ct));
        Assert.Equal(CommercialFeature.AdvancedGoals, error.Feature);
        Assert.Equal(before, await SnapshotAsync(f.Provider)); Assert.Equal(0, events);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Retained_release_delete_and_plan_removal_remain_available_without_ledger_movements(bool expiredHost)
    {
        var f = await FixtureAsync(); f.Enable(shared: expiredHost, host: !expiredHost);
        var release = f.AllocationDraft(-1000); await f.Goals.AddAllocationAsync(release, Ct);
        Assert.Equal(1500, (await f.Goals.GetAllocationsAsync(f.Goal.Id, Ct)).Sum(a => a.Amount));
        await f.Goals.DeleteAllocationAsync(release.Id, Ct);
        await f.Goals.SaveContributionPlanAsync(f.Goal.Id, null, Ct);
        Assert.Empty(await f.Goals.GetContributionPlansAsync(Ct)); Assert.Single(await f.Goals.GetGoalsAsync(Ct));
        Assert.Single(await f.Goals.GetAllocationsAsync(f.Goal.Id, Ct)); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        await f.Goals.DeleteGoalAsync(f.Goal.Id, Ct);
        Assert.Empty(await f.Goals.GetGoalsAsync(Ct)); Assert.Empty(await f.Goals.GetAllocationsAsync(cancellationToken: Ct));
        Assert.Equal(10000, (await f.Store.GetAccountsAsync(cancellationToken: Ct)).Single().OpeningBalance);
    }

    [Theory]
    [InlineData("positive")]
    [InlineData("release")]
    [InlineData("delete-allocation")]
    [InlineData("new-plan")]
    [InlineData("edit-plan")]
    [InlineData("remove-plan")]
    [InlineData("delete-goal")]
    [InlineData("paired")]
    public async Task Personal_Pro_does_not_replace_membership_for_any_goal_write(string operation)
    {
        var f = await FixtureAsync(); f.Enable(ProductPlan.Pro, shared: true, member: false);
        var before = await SnapshotAsync(f.Provider); var events = 0; f.Goals.Changed += (_, _) => events++;
        var error = await Assert.ThrowsAsync<CommercialWriteRejectedException>(async () =>
        {
            switch (operation)
            {
                case "positive": await f.Goals.AddAllocationAsync(f.AllocationDraft(1000), Ct); break;
                case "release": await f.Goals.AddAllocationAsync(f.AllocationDraft(-1000), Ct); break;
                case "delete-allocation": await f.Goals.DeleteAllocationAsync(f.Allocation.Id, Ct); break;
                case "new-plan": await f.Goals.SaveContributionPlanAsync(Guid.NewGuid(), Plan(Guid.NewGuid()), Ct); break;
                case "edit-plan": await f.Goals.SaveContributionPlanAsync(f.Goal.Id, f.Plan, Ct); break;
                case "remove-plan": await f.Goals.SaveContributionPlanAsync(f.Goal.Id, null, Ct); break;
                case "delete-goal": await f.Goals.DeleteGoalAsync(f.Goal.Id, Ct); break;
                case "paired": await f.Goals.SaveGoalWithContributionPlanAsync(f.Goal, f.Plan, Ct); break;
            }
        });
        Assert.Equal(FeaturePermission.RequiresMembership, error.Permission);
        Assert.Equal(before, await SnapshotAsync(f.Provider)); Assert.Equal(0, events);
    }

    [Fact]
    public async Task Free_basic_goal_keeps_empty_monthly_contribution_dates_in_one_save()
    {
        var f = await FixtureAsync(); await f.Goals.DeleteGoalAsync(f.Goal.Id, Ct); f.Enable();
        var goal = f.BalanceGoal(); var plan = Plan(goal.Id); plan.Rule.Calendar = PeriodCalendar.Persian;
        var events = 0; f.Goals.Changed += (_, _) => events++;
        await f.Goals.SaveGoalWithContributionPlanAsync(goal, plan, Ct);
        Assert.Equal(1, events); Assert.Equal(goal.Id, Assert.Single(await f.Goals.GetGoalsAsync(Ct)).Id);
        var saved = Assert.Single(await f.Goals.GetContributionPlansAsync(Ct));
        Assert.Null(saved.Amount); Assert.False(saved.ReminderEnabled); Assert.Equal(PeriodCalendar.Persian, saved.Rule.Calendar);
        Assert.Equal(goal.Id, saved.GoalId); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Theory]
    [InlineData("fixed")]
    [InlineData("income")]
    [InlineData("cut")]
    [InlineData("categories")]
    [InlineData("assumed-price")]
    [InlineData("custom-dates")]
    [InlineData("reminder")]
    public async Task Free_rejects_new_contribution_tools_before_goal_pin_or_protection_changes(string variation)
    {
        var f = await FixtureAsync(); await f.Goals.DeleteGoalAsync(f.Goal.Id, Ct); f.Enable();
        var goal = f.BalanceGoal(); goal.Protect = true; var plan = Plan(goal.Id);
        switch (variation)
        {
            case "fixed": plan.Amount = 1000; break;
            case "income": plan.Method = ContributionMethod.ShareOfIncome; plan.Percent = 10; break;
            case "cut": plan.Method = ContributionMethod.SpendingCut; plan.Amount = 1000; break;
            case "categories": plan.CategoryIds = [Guid.NewGuid()]; break;
            case "assumed-price": plan.AssumedPricePerUnitMilli = 10000; break;
            case "custom-dates": plan.Rule.SecondDay = 20; break;
            case "reminder": plan.ReminderEnabled = true; break;
        }
        var before = await SnapshotAsync(f.Provider); var events = 0; f.Goals.Changed += (_, _) => events++;
        var error = await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => f.Goals.SaveGoalWithContributionPlanAsync(goal, plan, Ct));
        Assert.Equal(variation == "reminder" ? CommercialFeature.ContributionReviewReminders : CommercialFeature.AdvancedGoals, error.Feature);
        Assert.True(goal.Protect); Assert.Equal(before, await SnapshotAsync(f.Provider)); Assert.Equal(0, events);
    }

    [Fact]
    public async Task Free_corrects_retained_plan_and_disables_reminder_preserving_identity_and_full_metadata()
    {
        var f = await FixtureAsync(); f.Plan.CategoryIds = [Guid.NewGuid()]; f.Plan.Rule.SecondDay = 20;
        f.Plan.AssumedPricePerUnitMilli = 123456; await f.Goals.SaveContributionPlanAsync(f.Goal.Id, f.Plan, Ct); f.Enable();
        var draft = Plan(Guid.NewGuid()); draft.Amount = 2000; draft.CategoryIds = [.. f.Plan.CategoryIds];
        draft.Rule = f.Plan.Rule.Clone(); draft.Rule.Start = Day.AddDays(1); draft.AssumedPricePerUnitMilli = 123456;
        await f.Goals.SaveContributionPlanAsync(f.Goal.Id, draft, Ct);
        var saved = Assert.Single(await f.Goals.GetContributionPlansAsync(Ct));
        Assert.Equal(f.Plan.Id, saved.Id); Assert.Equal(f.Plan.CreatedAt, saved.CreatedAt); Assert.Equal(f.Goal.Id, saved.GoalId);
        Assert.Equal(2000, saved.Amount); Assert.False(saved.ReminderEnabled); Assert.Equal(draft.CategoryIds, saved.CategoryIds);
        Assert.Equal(20, saved.Rule.SecondDay); Assert.Equal(draft.Rule.Start, saved.Rule.Start); Assert.Equal(123456, saved.AssumedPricePerUnitMilli);
        draft.CategoryIds.Clear(); draft.Rule.SecondDay = null;
        Assert.Single(saved.CategoryIds); Assert.Equal(20, saved.Rule.SecondDay);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Free_cannot_add_new_method_or_custom_date_tool_to_active_retained_plan(bool customDates)
    {
        var f = await FixtureAsync(); f.Enable(); var before = await SnapshotAsync(f.Provider);
        var draft = Plan(f.Goal.Id); draft.Amount = 2000;
        if (customDates) draft.Rule.SecondDay = 20; else { draft.Method = ContributionMethod.ShareOfIncome; draft.Percent = 10; }
        await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => f.Goals.SaveContributionPlanAsync(f.Goal.Id, draft, Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

    [Theory]
    [InlineData(ProductPlan.Plus, false)]
    [InlineData(ProductPlan.Pro, false)]
    [InlineData(ProductPlan.Free, true)]
    public async Task Approved_paid_or_exact_shared_right_saves_new_contribution_and_earmark_without_ledger(ProductPlan product, bool shared)
    {
        var f = await FixtureAsync(); f.Enable(product, shared); var goal = f.BalanceGoal(); var plan = Plan(goal.Id); plan.Amount = 1000; plan.ReminderEnabled = true;
        await f.Goals.SaveGoalWithContributionPlanAsync(goal, plan, Ct); await f.Goals.AddAllocationAsync(f.AllocationDraft(1000), Ct);
        Assert.Equal(2, (await f.Goals.GetGoalsAsync(Ct)).Count); Assert.Equal(2, (await f.Goals.GetContributionPlansAsync(Ct)).Count);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Free_cannot_reopen_historical_basic_goal_with_retained_paid_contribution_or_reminder(bool reminderOnly)
    {
        var f = await FixtureAsync(); await f.Goals.DeleteGoalAsync(f.Goal.Id, Ct);
        var goal = f.BalanceGoal(); goal.State = GoalState.Completed; var plan = Plan(goal.Id);
        if (reminderOnly) plan.ReminderEnabled = true; else plan.Amount = 1000;
        await f.Goals.SaveGoalWithContributionPlanAsync(goal, plan, Ct); f.Enable();
        var before = await SnapshotAsync(f.Provider); goal.State = GoalState.Active;
        await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => f.Goals.SaveGoalAsync(goal, Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider));
        f.Enable(ProductPlan.Plus); await f.Goals.SaveGoalAsync(goal, Ct);
        Assert.Equal(GoalState.Active, Assert.Single(await f.Goals.GetGoalsAsync(Ct)).State);
    }

    [Fact]
    public async Task Reopening_basic_history_with_empty_dates_remains_Free()
    {
        var f = await FixtureAsync(); await f.Goals.DeleteGoalAsync(f.Goal.Id, Ct);
        var goal = f.BalanceGoal(); goal.State = GoalState.Archived; await f.Goals.SaveGoalWithContributionPlanAsync(goal, Plan(goal.Id), Ct); f.Enable();
        goal.State = GoalState.Paused; await f.Goals.SaveGoalAsync(goal, Ct);
        Assert.Equal(GoalState.Paused, Assert.Single(await f.Goals.GetGoalsAsync(Ct)).State);
    }

    [Fact]
    public async Task Complete_editor_cannot_exceed_last_Free_goal_slot_or_leave_plan_rows()
    {
        var f = await FixtureAsync(); f.Enable(); var before = await SnapshotAsync(f.Provider);
        var goal = f.BalanceGoal(); var error = await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => f.Goals.SaveGoalWithContributionPlanAsync(goal, Plan(goal.Id), Ct));
        Assert.Equal(QuotaKind.Goals, error.Quota); Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Failed_contribution_insert_or_update_restores_goal_and_old_Home_pins(bool update)
    {
        var f = await FixtureAsync(); var other = f.BalanceGoal("Second pinned goal"); other.HomePin = 2;
        await f.Goals.SaveGoalAsync(other, Ct); var goal = update ? other : f.BalanceGoal("Third pinned goal");
        if (update) await f.Goals.SaveContributionPlanAsync(goal.Id, Plan(goal.Id), Ct);
        var before = await SnapshotAsync(f.Provider); goal.Name = "Changed unsaved name"; goal.HomePin = 3;
        var plan = Plan(goal.Id); plan.Amount = 2000;
        // A new third goal uses an earmark to avoid the separate one-balance-goal-per-account invariant.
        if (!update) { goal.Type = GoalType.Earmark; goal.AccountId = null; }
        await SqlAsync(f, "CREATE TRIGGER RejectContribution BEFORE " + (update ? "UPDATE" : "INSERT") + " ON ContributionPlans BEGIN SELECT RAISE(ABORT, 'Owned contribution failure'); END;");
        var events = 0; f.Goals.Changed += (_, _) => events++;
        await Assert.ThrowsAsync<DbUpdateException>(() => f.Goals.SaveGoalWithContributionPlanAsync(goal, plan, Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider)); Assert.Equal(0, events);
        await SqlAsync(f, "DROP TRIGGER RejectContribution;");
        await f.Goals.SaveGoalWithContributionPlanAsync(goal, plan, Ct); Assert.Equal(1, events);
        var originalPin = (await f.Goals.GetGoalsAsync(Ct)).Single(g => g.Id == f.Goal.Id).HomePin;
        if (update) Assert.Equal(1, originalPin); else Assert.Null(originalPin);
    }

    [Fact]
    public async Task Retired_access_after_SQL_rolls_back_complete_editor_and_old_pins()
    {
        var f = await FixtureAsync(retireAfterSave: true); f.Enable(ProductPlan.Plus);
        var before = await SnapshotAsync(f.Provider); var goal = f.BalanceGoal(); var plan = Plan(goal.Id); plan.Amount = 1000;
        await SqlAsync(f, "CREATE TRIGGER RetireAfterContribution AFTER INSERT ON ContributionPlans BEGIN SELECT RetireGoalAccess(); END;");
        f.Source.RetireAfterSave = true; var events = 0; f.Goals.Changed += (_, _) => events++;
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Goals.SaveGoalWithContributionPlanAsync(goal, plan, Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider)); Assert.Equal(0, events); Assert.Equal(1, f.Source.RetiredWrites);
    }

    [Fact]
    public async Task Failed_goal_delete_restores_original_allocations_plan_and_goal()
    {
        var f = await FixtureAsync(); f.Enable(); var before = await SnapshotAsync(f.Provider); var events = 0; f.Goals.Changed += (_, _) => events++;
        await SqlAsync(f, "CREATE TRIGGER RejectGoalDelete BEFORE DELETE ON Goals BEGIN SELECT RAISE(ABORT, 'Owned goal failure'); END;");
        await Assert.ThrowsAsync<SqliteException>(() => f.Goals.DeleteGoalAsync(f.Goal.Id, Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider)); Assert.Equal(0, events);
    }

    [Fact]
    public async Task Independent_complete_editors_compete_for_one_Free_goal_slot()
    {
        var f = await FixtureAsync(); await f.Goals.DeleteGoalAsync(f.Goal.Id, Ct);
        var secondAccount = new Account { Name = "Second account", CurrencyCode = "EUR", OpeningDate = Day };
        await f.Store.SaveAccountAsync(secondAccount, Ct); var second = Provider(f.Path, f.Source).GetRequiredService<GoalStore>(); f.Enable();
        using var barrier = new Barrier(2); f.Source.Rendezvous = barrier; f.Source.Captures = 0;
        var firstGoal = f.BalanceGoal("First contender"); var secondGoal = f.BalanceGoal("Second contender"); secondGoal.AccountId = secondAccount.Id;
        var events = 0; f.Goals.Changed += (_, _) => Interlocked.Increment(ref events); second.Changed += (_, _) => Interlocked.Increment(ref events);
        async Task<bool> Save(GoalStore store, Goal goal)
        {
            try { await store.SaveGoalWithContributionPlanAsync(goal, Plan(goal.Id), Ct); return true; }
            catch (CommercialWriteRejectedException) { return false; }
        }
        var results = await Task.WhenAll(Task.Run(() => Save(f.Goals, firstGoal), Ct), Task.Run(() => Save(second, secondGoal), Ct));
        Assert.Single(results, r => r); Assert.Equal(1, events);
        var savedGoal = Assert.Single(await f.Goals.GetGoalsAsync(Ct)); Assert.Equal(savedGoal.Id, Assert.Single(await f.Goals.GetContributionPlansAsync(Ct)).GoalId);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Independent_contribution_replacements_keep_one_parent_row_and_original_identity()
    {
        var f = await FixtureAsync(); var second = Provider(f.Path, f.Source).GetRequiredService<GoalStore>(); f.Enable(ProductPlan.Plus);
        using var barrier = new Barrier(2); f.Source.Rendezvous = barrier; f.Source.Captures = 0;
        var first = Plan(Guid.NewGuid()); first.Amount = 1500; var other = Plan(Guid.NewGuid()); other.Amount = 2000;
        await Task.WhenAll(Task.Run(() => f.Goals.SaveContributionPlanAsync(f.Goal.Id, first, Ct), Ct),
            Task.Run(() => second.SaveContributionPlanAsync(f.Goal.Id, other, Ct), Ct));
        var saved = Assert.Single(await f.Goals.GetContributionPlansAsync(Ct));
        Assert.Equal(f.Plan.Id, saved.Id); Assert.Equal(f.Goal.Id, saved.GoalId); Assert.Contains(saved.Amount, new long?[] { 1500, 2000 });
    }

    [Fact]
    public async Task Independent_new_contributions_reuse_the_row_created_by_the_first_writer()
    {
        var f = await FixtureAsync(); await f.Goals.SaveContributionPlanAsync(f.Goal.Id, null, Ct);
        var second = Provider(f.Path, f.Source).GetRequiredService<GoalStore>(); f.Enable(ProductPlan.Plus);
        using var barrier = new Barrier(2); f.Source.Rendezvous = barrier; f.Source.Captures = 0;
        var first = Plan(Guid.NewGuid()); first.Amount = 1500; var other = Plan(Guid.NewGuid()); other.Amount = 2000;
        await Task.WhenAll(Task.Run(() => f.Goals.SaveContributionPlanAsync(f.Goal.Id, first, Ct), Ct),
            Task.Run(() => second.SaveContributionPlanAsync(f.Goal.Id, other, Ct), Ct));
        var saved = Assert.Single(await f.Goals.GetContributionPlansAsync(Ct));
        Assert.Equal(f.Goal.Id, saved.GoalId); Assert.Contains(saved.Id, new[] { first.Id, other.Id });
        Assert.Contains(saved.Amount, new long?[] { 1500, 2000 });
    }

    [Fact]
    public async Task Retained_reminder_can_stay_enabled_but_new_reminder_cannot_be_added_after_downgrade()
    {
        var f = await FixtureAsync(); f.Enable(); f.Plan.Amount = 1500;
        await f.Goals.SaveContributionPlanAsync(f.Goal.Id, f.Plan, Ct);
        Assert.True(Assert.Single(await f.Goals.GetContributionPlansAsync(Ct)).ReminderEnabled);
        f.Plan.ReminderEnabled = false; await f.Goals.SaveContributionPlanAsync(f.Goal.Id, f.Plan, Ct);
        var before = await SnapshotAsync(f.Provider); f.Plan.ReminderEnabled = true;
        var error = await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => f.Goals.SaveContributionPlanAsync(f.Goal.Id, f.Plan, Ct));
        Assert.Equal(CommercialFeature.ContributionReviewReminders, error.Feature); Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

    [Fact]
    public async Task History_correction_keeps_retained_plan_without_reactivating_goal_or_posting_money()
    {
        var f = await FixtureAsync(); f.Goal.State = GoalState.Completed;
        await f.Goals.SaveGoalAsync(f.Goal, Ct); f.Enable();
        f.Plan.Method = ContributionMethod.ShareOfIncome; f.Plan.Percent = 15;
        await f.Goals.SaveGoalWithContributionPlanAsync(f.Goal, f.Plan, Ct);
        Assert.Equal(GoalState.Completed, Assert.Single(await f.Goals.GetGoalsAsync(Ct)).State);
        Assert.Equal(15, Assert.Single(await f.Goals.GetContributionPlansAsync(Ct)).Percent);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct)); Assert.Single(await f.Goals.GetAllocationsAsync(cancellationToken: Ct));
    }
    private static async Task<string> SnapshotAsync(ServiceProvider provider)
    {
        await using var db = await provider.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        await db.Database.OpenConnectionAsync(Ct); var connection = db.Database.GetDbConnection(); var tables = new List<string>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
            using var reader = await command.ExecuteReaderAsync(Ct); while (await reader.ReadAsync(Ct)) tables.Add(reader.GetString(0));
        }
        var result = new Dictionary<string, List<string>>();
        foreach (var table in tables)
        {
            using var command = connection.CreateCommand(); command.CommandText = "SELECT * FROM \""+table+"\"";
            using var reader = await command.ExecuteReaderAsync(Ct); var rows = new List<string>();
            while (await reader.ReadAsync(Ct))
            {
                var values = new object?[reader.FieldCount];
                for (var i = 0; i < values.Length; i++) values[i] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                rows.Add(JsonSerializer.Serialize(values));
            }
            rows.Sort(StringComparer.Ordinal); result[table] = rows;
        }
        return JsonSerializer.Serialize(result);
    }

    /// <summary>Cached exact-scope facts plus a rendezvous before actual writer acquisition, never fake goals.</summary>
    private sealed class AccessSource(string path) : ICommercialWriteAccessSource
    {
        public CommercialWriteAccess Current = CommercialWriteAccess.Inactive;
        public Barrier? Rendezvous;
        public int Captures;
        public bool RetireAfterSave;
        public int RetiredWrites;
        public CommercialWriteAccess Capture(string databasePath)
        {
            Assert.Equal(System.IO.Path.GetFullPath(path), databasePath);
            if (Rendezvous is { } barrier && Interlocked.Increment(ref Captures) <= 2
                && !barrier.SignalAndWait(TimeSpan.FromSeconds(15), Ct)) throw new TimeoutException("The writers did not rendezvous.");
            return Current;
        }
    }

    /// <summary>Decorates actual SQLite connections for a trigger-driven cached-access change after a contribution INSERT.</summary>
    private sealed class RetiringConnections(IDbContextFactory<ZananceDbContext> inner, AccessSource source) : IDbContextFactory<ZananceDbContext>
    {
        public ZananceDbContext CreateDbContext()
        {
            var db = inner.CreateDbContext();
            ((SqliteConnection)db.Database.GetDbConnection()).CreateFunction("RetireGoalAccess", () =>
            {
                if (source.RetireAfterSave)
                {
                    source.Current = new(source.Current.DatabasePath!, Context(ProductPlan.Free));
                    source.RetireAfterSave = false; source.RetiredWrites++;
                }
                return 1;
            });
            return db;
        }
    }

    /// <summary>Stable local time keeps full audit metadata comparable across delete and Undo.</summary>
    private sealed class FixedTime : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
