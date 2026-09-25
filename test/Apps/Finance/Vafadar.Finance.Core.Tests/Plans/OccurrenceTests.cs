using Vafadar.Finance.Core.Accounts;
using Vafadar.Finance.Core.Ledger;
using Vafadar.Finance.Core.Plans;

namespace Vafadar.Finance.Core.Tests.Plans;

public sealed class OccurrenceTests
{
    private static readonly DateOnly Today = new(2027, 3, 15);
    private readonly Account _account = new() { Name = "Checking", CurrencyCode = "EUR", OpeningDate = new DateOnly(2027, 1, 1) };

    [Fact]
    [Trait("AT", "AT-16")]
    public void A_future_one_time_plan_does_not_change_the_balance()
    {
        var plan = Plan(new RecurrenceRule { Frequency = Frequency.Once, Start = new DateOnly(2027, 5, 1) }, 30_000);

        var occurrence = Occurrences.Between(plan, [], Today, new DateOnly(2027, 12, 31), Today).Single();

        Assert.Equal(OccurrenceView.Future, occurrence.Status);
        Assert.Equal(0, LedgerCalculator.Balance(_account, [], Today));
    }

    [Fact]
    public void Status_is_derived_from_the_due_date_and_today()
    {
        var plan = Plan(Monthly(new DateOnly(2027, 1, 15)), 1_000);

        var statuses = Occurrences.Between(plan, [], new DateOnly(2027, 2, 1), new DateOnly(2027, 4, 30), Today).Select(o => o.Status);

        Assert.Equal([OccurrenceView.Overdue, OccurrenceView.Due, OccurrenceView.Future], statuses);
    }

    [Fact]
    [Trait("AT", "AT-26")]
    public void Moving_one_due_date_changes_only_that_occurrence()
    {
        var plan = Plan(Monthly(new DateOnly(2027, 1, 15)), 1_000);
        var moved = new OccurrenceState { ScheduleId = plan.Id, OriginalDate = new DateOnly(2027, 4, 15), DueDate = new DateOnly(2027, 5, 2) };

        var dues = Occurrences.Between(plan, [moved], new DateOnly(2027, 4, 1), new DateOnly(2027, 6, 30), Today).Select(o => o.DueDate);

        Assert.Equal([new DateOnly(2027, 5, 2), new DateOnly(2027, 5, 15), new DateOnly(2027, 6, 15)], dues);
    }

    [Fact]
    [Trait("AT", "AT-27")]
    public void Skipping_in_a_twelve_occurrence_plan_adds_no_thirteenth()
    {
        var rule = Monthly(new DateOnly(2027, 1, 10));
        rule.End = EndKind.AfterCount;
        rule.Count = 12;
        var plan = Plan(rule, 1_000);
        var skipped = new OccurrenceState { ScheduleId = plan.Id, OriginalDate = new DateOnly(2027, 5, 10), Status = OccurrenceStatus.Skipped };

        var all = Occurrences.Between(plan, [skipped], new DateOnly(2027, 1, 1), new DateOnly(2029, 12, 31), Today);

        Assert.Equal(12, all.Count);
        Assert.Equal(11, all.Count(o => o.Status != OccurrenceView.Skipped));
        Assert.Equal(new DateOnly(2027, 12, 10), all[^1].DueDate);
    }

    [Fact]
    [Trait("AT", "AT-25")]
    public void This_and_future_split_keeps_the_past_and_continues_the_series()
    {
        var plan = Plan(Monthly(new DateOnly(2027, 1, 31)), 1_000);
        var settled = new OccurrenceState { ScheduleId = plan.Id, OriginalDate = new DateOnly(2027, 1, 31), Status = OccurrenceStatus.Settled };

        var next = PlanActions.SplitFrom(plan, new DateOnly(2027, 4, 30));
        next.Amount = 1_200;

        var old = Occurrences.Between(plan, [settled], new DateOnly(2027, 1, 1), new DateOnly(2027, 12, 31), Today);
        var future = Occurrences.Between(next, [], new DateOnly(2027, 1, 1), new DateOnly(2027, 6, 30), Today);

        Assert.Equal([new DateOnly(2027, 1, 31), new DateOnly(2027, 2, 28), new DateOnly(2027, 3, 31)], old.Select(o => o.DueDate));
        Assert.All(old, o => Assert.Equal(1_000, o.Amount));
        Assert.Equal([new DateOnly(2027, 4, 30), new DateOnly(2027, 5, 31), new DateOnly(2027, 6, 30)], future.Select(o => o.DueDate));
        Assert.All(future, o => Assert.Equal(1_200, o.Amount));
        Assert.Equal(4, future[0].Number);
        Assert.Equal(plan.Id, next.PreviousScheduleId);
    }

    [Fact]
    [Trait("AT", "AT-28")]
    public void Early_confirmation_keeps_the_real_payment_date()
    {
        var plan = Plan(Monthly(new DateOnly(2027, 1, 15)), 1_000);
        var april = Occurrences.Between(plan, [], new DateOnly(2027, 4, 1), new DateOnly(2027, 4, 30), Today).Single();

        var entry = Occurrences.CreateEntry(april, 1_000, Today, ReviewState.Confirmed);

        Assert.Equal(Today, entry.Date);
        Assert.Equal(new DateOnly(2027, 4, 15), entry.OccurrenceDate);
        Assert.Equal(plan.Id, entry.ScheduleId);
        Assert.Equal("Rent", entry.Title);
        Assert.Equal(EntrySource.Schedule, entry.Source);
    }

    [Fact]
    [Trait("AT", "AT-32")]
    public void Two_equal_plans_keep_independent_occurrences()
    {
        var first = Plan(Monthly(new DateOnly(2027, 1, 15)), 1_000);
        var second = Plan(Monthly(new DateOnly(2027, 1, 15)), 1_000);
        var settledFirst = new OccurrenceState { ScheduleId = first.Id, OriginalDate = new DateOnly(2027, 3, 15), Status = OccurrenceStatus.Settled };

        var a = Occurrences.Between(first, [settledFirst], Today, Today, Today).Single();
        var b = Occurrences.Between(second, [settledFirst], Today, Today, Today).Single();

        Assert.Equal(OccurrenceView.Settled, a.Status);
        Assert.Equal(OccurrenceView.Due, b.Status);
    }

    [Fact]
    public void Pause_hides_occurrences_and_resume_continues_from_the_chosen_date()
    {
        var plan = Plan(Monthly(new DateOnly(2027, 1, 15)), 1_000);
        PlanActions.Pause(plan, new DateOnly(2027, 3, 1));

        Assert.Empty(Occurrences.Between(plan, [], new DateOnly(2027, 3, 1), new DateOnly(2027, 12, 31), Today));

        var continuation = PlanActions.Resume(plan, new DateOnly(2027, 6, 1))!;

        Assert.Equal([new DateOnly(2027, 1, 15), new DateOnly(2027, 2, 15)], Occurrences.Between(plan, [], DateOnly.MinValue, new DateOnly(2027, 12, 31), Today).Select(o => o.DueDate));
        Assert.Equal(new DateOnly(2027, 6, 15), Occurrences.Between(continuation, [], DateOnly.MinValue, new DateOnly(2027, 12, 31), Today)[0].DueDate);
        Assert.Equal(ScheduleState.Ended, plan.State);
        Assert.Equal(ScheduleState.Active, continuation.State);
    }

    [Fact]
    public void Only_fixed_plans_with_available_accounts_post_automatically()
    {
        var accounts = new Dictionary<Guid, Account> { [_account.Id] = _account };
        var plan = Plan(Monthly(new DateOnly(2027, 1, 15)), 1_000);
        Assert.Equal(AutoPostBlocker.Disabled, PlanActions.AutoPostBlockedBy(plan, accounts));

        plan.AutoPost = true;
        Assert.Null(PlanActions.AutoPostBlockedBy(plan, accounts));

        plan.AmountMode = AmountMode.Estimated;
        Assert.Equal(AutoPostBlocker.AmountNotFixed, PlanActions.AutoPostBlockedBy(plan, accounts));

        plan.AmountMode = AmountMode.Fixed;
        _account.IsArchived = true;
        Assert.Equal(AutoPostBlocker.AccountUnavailable, PlanActions.AutoPostBlockedBy(plan, accounts));
    }

    [Fact]
    [Trait("AT", "AT-29")]
    public void Link_candidates_are_unlinked_entries_of_the_same_kind_and_account_near_the_due_date()
    {
        var plan = Plan(Monthly(new DateOnly(2027, 1, 15)), 1_000);
        var occurrence = Occurrences.Between(plan, [], Today, Today, Today).Single();
        var close = new LedgerEntry { Kind = EntryKind.Expense, AccountId = _account.Id, Amount = 1_000, Date = Today.AddDays(-2) };
        var other = new LedgerEntry { Kind = EntryKind.Expense, AccountId = _account.Id, Amount = 5_000, Date = Today };
        var linked = new LedgerEntry { Kind = EntryKind.Expense, AccountId = _account.Id, Amount = 1_000, Date = Today, ScheduleId = Guid.CreateVersion7() };
        var far = new LedgerEntry { Kind = EntryKind.Expense, AccountId = _account.Id, Amount = 1_000, Date = Today.AddDays(30) };
        var income = new LedgerEntry { Kind = EntryKind.Income, AccountId = _account.Id, Amount = 1_000, Date = Today };

        Assert.Equal([close, other], PlanActions.LinkCandidates(occurrence, [other, linked, far, income, close]));
    }

    private Schedule Plan(RecurrenceRule rule, long amount) => new()
    {
        Name = "Rent",
        Kind = EntryKind.Expense,
        AccountId = _account.Id,
        AmountMode = AmountMode.Fixed,
        Amount = amount,
        Rule = rule,
    };

    private static RecurrenceRule Monthly(DateOnly start) => new() { Frequency = Frequency.Monthly, Start = start };
}
