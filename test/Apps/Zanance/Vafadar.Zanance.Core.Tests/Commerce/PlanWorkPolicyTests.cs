using System.Text.Json;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Forecasts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Reminders;

namespace Vafadar.Zanance.Core.Tests.Commerce;

/// <summary>AT-141: scoped future work remains separate from original financial/calculation inputs.</summary>
public sealed class PlanWorkPolicyTests
{
    private static readonly DateOnly Today = new(2026, 10, 11);
    private static readonly Guid ScopeId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private static QuotaScope Scope => new(QuotaScopeKind.PersonalProfile, ScopeId);
    private static CapabilityContext Free => new(ProductPlan.Free, new(EntitlementScopeKind.PersonalProfile, ScopeId));
    private static Schedule Plan() => new()
    {
        Name = "Owned monthly bill", AccountId = Guid.NewGuid(), Amount = 1000, ReminderEnabled = true,
        Rule = new() { Frequency = Frequency.Monthly, Start = Today.AddMonths(-1) }
    };

    [Fact, Trait("AT", "AT-141")]
    public void Missing_over_capacity_choice_generates_nothing_and_preserves_past_and_settled_history()
    {
        var plans = Enumerable.Range(0, 6).Select(_ => Plan()).ToList();
        var work = PlanWorkPolicy.Resolve(Free, Scope, plans);
        Assert.All(plans, plan => { Assert.False(work.CanGenerate(plan.Id)); Assert.False(work.CanRemind(plan.Id)); });
        var future = Occurrences.Between(plans[0], [], Today, Today.AddMonths(1), Today).Last();
        Assert.False(work.AllowsOccurrence(future, Today));
        Assert.False(work.AllowsOccurrence(future with { DueDate = Today }, Today));
        Assert.True(work.AllowsOccurrence(future with { DueDate = Today.AddDays(-1) }, Today));
        Assert.True(work.AllowsOccurrence(future with { Status = OccurrenceView.Settled }, Today));
        Assert.True(work.AllowsOccurrence(future with { Status = OccurrenceView.Skipped }, Today));
    }

    [Theory, Trait("AT", "AT-141")]
    [InlineData("missing"), InlineData("oversized"), InlineData("stale"), InlineData("ended")]
    public void An_unreviewed_choice_never_invents_selected_plan_identities(string choice)
    {
        var plans = Enumerable.Range(0, 6).Select(_ => Plan()).ToList();
        var ended = Plan(); ended.State = ScheduleState.Ended; plans.Add(ended);
        ResourceSelection? selection = choice == "missing" ? null : new(QuotaKind.RecurringPlans, Scope,
            choice == "oversized" ? plans.Take(6).Select(plan => plan.Id) : [choice == "ended" ? ended.Id : Guid.NewGuid()]);
        var work = PlanWorkPolicy.Resolve(Free, Scope, plans, selection);
        Assert.All(plans, plan => Assert.False(work.CanGenerate(plan.Id)));
    }

    [Fact, Trait("AT", "AT-141")]
    public void Explicit_choice_keeps_five_original_slots_including_a_pause_without_mutating_dates_or_states()
    {
        var plans = Enumerable.Range(0, 6).Select(_ => Plan()).ToList();
        plans[0].State = ScheduleState.Paused; plans[0].PausedFrom = Today.AddDays(10);
        var before = JsonSerializer.Serialize(plans);
        var work = PlanWorkPolicy.Resolve(Free, Scope, plans, new(QuotaKind.RecurringPlans, Scope, plans.Take(5).Select(plan => plan.Id)));
        Assert.All(plans.Take(5), plan => Assert.True(work.CanGenerate(plan.Id)));
        Assert.False(work.CanGenerate(plans[5].Id)); Assert.Equal(before, JsonSerializer.Serialize(plans));
        Assert.Empty(Occurrences.Between(plans[0], [], Today.AddDays(11), Today.AddMonths(2), Today));
    }

    [Theory, Trait("AT", "AT-141")]
    [InlineData(ProductPlan.Plus), InlineData(ProductPlan.Pro)]
    public void Unlimited_upgrades_restore_original_new_work_despite_old_limited_choices(ProductPlan tier)
    {
        var plans = Enumerable.Range(0, 8).Select(_ => Plan()).ToList();
        var context = new CapabilityContext(tier, Free.Scope);
        var work = PlanWorkPolicy.Resolve(context, Scope, plans, new(QuotaKind.RecurringPlans, Scope, [Guid.NewGuid()]));
        Assert.All(plans, plan => { Assert.True(work.CanGenerate(plan.Id)); Assert.True(work.CanRemind(plan.Id, contract: true)); });
    }

    [Theory, Trait("AT", "AT-141")]
    [InlineData(false, true), InlineData(true, false)]
    public void Paid_recurrence_is_separate_from_retained_contract_or_automatic_post_choices(bool advancedRule, bool allowed)
    {
        var plan = Plan(); plan.AutoPost = true; plan.ContractProvider = "Owned retained contract";
        if (advancedRule) plan.Rule.DayRule = MonthDayRule.NthWeekday;
        var work = PlanWorkPolicy.Resolve(Free, Scope, [plan]);
        Assert.Equal(allowed, work.CanGenerate(plan.Id)); Assert.Equal(allowed, work.CanRemind(plan.Id));
        Assert.False(work.CanRemind(plan.Id, contract: true));
    }

    [Theory, Trait("AT", "AT-141")]
    [InlineData("weekday"), InlineData("second"), InlineData("shift")]
    public void Each_paid_recurrence_tool_needs_the_paid_capability_for_future_generation(string tool)
    {
        var plan = Plan();
        if (tool == "weekday") plan.Rule.DayRule = MonthDayRule.LastWeekday;
        else if (tool == "second") plan.Rule.SecondDay = 20;
        else plan.Rule.WeekendShift = WeekendShift.After;
        Assert.True(PlanWorkPolicy.RequiresAdvancedRule(plan));
        Assert.False(PlanWorkPolicy.Resolve(Free, Scope, [plan]).CanGenerate(plan.Id));
        Assert.True(PlanWorkPolicy.Resolve(new(ProductPlan.Plus, Free.Scope), Scope, [plan]).CanGenerate(plan.Id));
    }

    [Theory, Trait("AT", "AT-141")]
    [InlineData(true, true), InlineData(false, true), InlineData(true, false)]
    public void Exact_shared_membership_and_active_host_are_required_even_for_a_personally_paid_user(bool member, bool host)
    {
        var plan = Plan(); var scope = new QuotaScope(QuotaScopeKind.SharedSpace, ScopeId);
        var context = new CapabilityContext(ProductPlan.Pro, new(EntitlementScopeKind.SharedSpace, ScopeId), new(ScopeId, member, host));
        var work = PlanWorkPolicy.Resolve(context, scope, [plan], new(QuotaKind.RecurringPlans, scope, [plan.Id]));
        Assert.Equal(member && host, work.CanGenerate(plan.Id)); Assert.Equal(member && host, work.CanRemind(plan.Id, true));
    }

    [Fact, Trait("AT", "AT-141")]
    public void Earlier_ended_slices_use_only_the_actual_unique_selected_continuation()
    {
        var old = Plan();
        var middle = PlanActions.SplitFrom(old, Today.AddDays(4));
        var next = PlanActions.SplitFrom(middle, Today.AddDays(8));
        var others = Enumerable.Range(0, 5).Select(_ => Plan()).ToList();
        var all = new[] { old, middle, next }.Concat(others).ToList();
        var work = PlanWorkPolicy.Resolve(Free, Scope, all, new(QuotaKind.RecurringPlans, Scope, [next.Id]));
        Assert.True(work.CanGenerate(old.Id)); Assert.True(work.CanGenerate(middle.Id)); Assert.True(work.CanGenerate(next.Id));
        Assert.All(others, item => Assert.False(work.CanGenerate(item.Id)));
        var branch = Plan(); branch.PreviousScheduleId = old.Id; all.Add(branch);
        work = PlanWorkPolicy.Resolve(new(ProductPlan.Plus, Free.Scope), Scope, all);
        Assert.False(work.CanGenerate(old.Id)); Assert.True(work.CanGenerate(next.Id));
    }

    [Fact, Trait("AT", "AT-141")]
    public void Cyclic_or_uncontinued_ended_slices_do_not_acquire_new_work()
    {
        var a = Plan(); var b = Plan(); a.State = b.State = ScheduleState.Ended;
        a.PreviousScheduleId = b.Id; b.PreviousScheduleId = a.Id;
        var stopped = Plan(); stopped.State = ScheduleState.Ended;
        var work = PlanWorkPolicy.Resolve(new(ProductPlan.Plus, Free.Scope), Scope, [a, b, stopped]);
        Assert.False(work.CanGenerate(a.Id)); Assert.False(work.CanGenerate(b.Id)); Assert.False(work.CanGenerate(stopped.Id));
    }

    [Fact, Trait("AT", "AT-141")]
    public void Evaluating_work_keeps_original_forecasts_reminder_dates_and_complete_serialized_data()
    {
        var account = new Account { Name = "Owned cash", CurrencyCode = "EUR", OpeningDate = Today.AddMonths(-1), OpeningBalance = 100000 };
        var plans = Enumerable.Range(0, 6).Select(_ => Plan()).ToList(); plans.ForEach(plan => plan.AccountId = account.Id);
        var before = JsonSerializer.Serialize(plans);
        var expected = JsonSerializer.Serialize(ForecastCalculator.Compute([account], [], plans, [], Today, Today.AddMonths(1)));
        var expectedReminders = ReminderPlanner.Plan(plans, [], Today.ToDateTime(new TimeOnly(8, 0)));
        var work = PlanWorkPolicy.Resolve(Free, Scope, plans, new(QuotaKind.RecurringPlans, Scope, [plans[0].Id]));
        Assert.Equal(before, JsonSerializer.Serialize(plans));
        Assert.Equal(expected, JsonSerializer.Serialize(ForecastCalculator.Compute([account], [], plans, [], Today, Today.AddMonths(1))));
        Assert.Equal(expectedReminders.Where(reminder => reminder.Occurrence.Schedule.Id == plans[0].Id),
            ReminderPlanner.Plan(plans.Where(plan => work.CanRemind(plan.Id)), [], Today.ToDateTime(new TimeOnly(8, 0))));
    }
}
