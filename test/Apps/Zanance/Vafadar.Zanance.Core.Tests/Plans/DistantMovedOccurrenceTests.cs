using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Forecasts;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Reminders;
using Vafadar.Zanance.Core.Reports;

namespace Vafadar.Zanance.Core.Tests.Plans;

/// <summary>Explicit due-date overrides have no artificial distance limit (REC-14 / AT-26).</summary>
public sealed class DistantMovedOccurrenceTests
{
    private static readonly DateOnly Today = new(2026, 11, 15);

    [Theory]
    [InlineData(-800)]
    [InlineData(-401)]
    [InlineData(-400)]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(800)]
    public void A_moved_once_occurrence_keeps_its_original_identity_at_any_distance(int offset)
    {
        var original = Today.AddDays(offset);
        var plan = Once(original);
        var state = Move(plan, original);

        var occurrence = Assert.Single(Occurrences.Between(plan, [state], Today, Today, Today));

        Assert.Equal(original, occurrence.OriginalDate);
        Assert.Equal(Today, occurrence.DueDate);
        Assert.Equal(1, occurrence.Number);
        Assert.Same(plan, occurrence.Schedule);
        Assert.Same(state, occurrence.State);
        Assert.Equal(original, state.OriginalDate);
        Assert.Equal(Today, state.DueDate);
    }

    [Fact]
    public void A_moved_finite_series_occurrence_keeps_its_original_number_after_the_series_end()
    {
        var plan = Monthly();
        plan.Rule.End = EndKind.AfterCount;
        plan.Rule.Count = 12;
        var original = new DateOnly(2024, 2, 15);

        var occurrence = Assert.Single(Occurrences.Between(plan, [Move(plan, original)], Today, Today, Today));

        Assert.Equal(2, occurrence.Number);
        Assert.Equal(original, occurrence.OriginalDate);
    }

    [Fact]
    public void Distant_and_regular_occurrences_with_the_same_due_date_keep_original_number_order()
    {
        var plan = Monthly();
        var original = new DateOnly(2024, 2, 15);

        var occurrences = Occurrences.Between(plan, [Move(plan, original)], Today, Today, Today);

        Assert.Equal([2, 35], occurrences.Select(item => item.Number));
        Assert.Equal([original, Today], occurrences.Select(item => item.OriginalDate));
        Assert.All(occurrences, item => Assert.Equal(Today, item.DueDate));
    }

    [Fact]
    public void A_future_original_moved_into_the_forecast_is_counted_without_a_ledger_entry()
    {
        var account = new Account { Name = "Checking", CurrencyCode = "EUR", OpeningDate = Today.AddDays(-1), OpeningBalance = 10_000 };
        var original = Today.AddDays(800);
        var plan = Once(original);
        plan.AccountId = account.Id;

        var forecast = Assert.Single(ForecastCalculator.Compute([account], [], [plan], [Move(plan, original)], Today, Today.AddDays(5)));

        Assert.Equal(10_000, forecast.StartBalance);
        Assert.Equal(8_800, forecast.EndBalance);
        Assert.Equal(original, Assert.Single(forecast.Items).OriginalDate);
    }

    [Fact]
    public void An_override_cannot_invent_an_original_date_absent_from_the_rule()
    {
        var plan = Once(Today.AddDays(-800));
        var invalid = Move(plan, plan.Rule.Start.AddDays(1));

        Assert.Empty(Occurrences.Between(plan, [invalid], Today, Today, Today));
    }

    [Fact]
    public void An_override_cannot_reclaim_an_original_date_owned_by_another_series_slice()
    {
        var original = Today.AddDays(-800);
        var plan = Once(original);
        plan.ActiveFrom = original.AddDays(1);

        Assert.Empty(Occurrences.Between(plan, [Move(plan, original)], Today, Today, Today));
    }

    [Theory]
    [InlineData(OccurrenceStatus.Open, OccurrenceView.Due)]
    [InlineData(OccurrenceStatus.Skipped, OccurrenceView.Skipped)]
    [InlineData(OccurrenceStatus.Settled, OccurrenceView.Settled)]
    public void Distant_override_status_and_partial_amount_are_not_replaced(OccurrenceStatus status, OccurrenceView expected)
    {
        var original = Today.AddDays(-800);
        var plan = Once(original);
        var state = Move(plan, original);
        state.Status = status;
        state.Amount = 900;
        state.PaidAmount = 200;
        state.AutoPostSuppressed = true;

        var occurrence = Assert.Single(Occurrences.Between(plan, [state], Today, Today, Today));

        Assert.Equal(expected, occurrence.Status);
        Assert.Equal(900, occurrence.Amount);
        Assert.Equal(200, occurrence.Paid);
        Assert.Equal(700, occurrence.Outstanding);
        Assert.Same(state, occurrence.State);
        Assert.True(state.AutoPostSuppressed);
    }

    [Fact]
    public void Complete_due_work_starts_at_the_earliest_valid_explicit_due_date()
    {
        var original = Today.AddDays(800);
        var plan = Once(original);
        var state = Move(plan, original);
        state.DueDate = Today.AddDays(-2);

        var since = Occurrences.DueRangeStart(plan, [state]);
        var occurrence = Assert.Single(Occurrences.OpenUpTo(plan, [state], Today, since));

        Assert.Equal(state.DueDate, since);
        Assert.Equal(OccurrenceView.Overdue, occurrence.Status);
        Assert.Equal(original, occurrence.OriginalDate);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Invalid_or_foreign_overrides_do_not_expand_the_complete_due_range(int invalidKind)
    {
        var original = Today.AddDays(800);
        var plan = Once(original);
        var state = Move(plan, original);
        if (invalidKind == 0) state.ScheduleId = Guid.NewGuid();
        if (invalidKind == 1) state.OriginalDate = original.AddDays(1);
        if (invalidKind == 2) plan.ActiveUntil = original.AddDays(-1);

        Assert.Equal(original, Occurrences.DueRangeStart(plan, [state]));
    }

    [Fact]
    public void An_early_moved_overdue_plan_is_assumed_today_in_the_forecast()
    {
        var account = new Account { Name = "Checking", CurrencyCode = "EUR", OpeningDate = Today.AddDays(-5), OpeningBalance = 10_000 };
        var original = Today.AddDays(800);
        var plan = Once(original);
        plan.AccountId = account.Id;
        var state = Move(plan, original);
        state.DueDate = Today.AddDays(-2);

        var forecast = Assert.Single(ForecastCalculator.Compute([account], [], [plan], [state], Today, Today.AddDays(5)));

        var item = Assert.Single(forecast.Items);
        Assert.Equal(Today, item.Date);
        Assert.Equal(ForecastSource.OverduePlan, item.Source);
        Assert.Equal(8_800, forecast.EndBalance);
    }

    [Fact]
    public void Commitments_keep_an_early_moved_overdue_plan_and_its_original_schedule()
    {
        var account = new Account { Name = "Checking", CurrencyCode = "EUR", OpeningDate = Today.AddDays(-5) };
        var original = Today.AddDays(800);
        var plan = Once(original);
        plan.AccountId = account.Id;
        var state = Move(plan, original);
        state.DueDate = Today.AddDays(-2);

        var total = Assert.Single(KpiCatalog.Commitments([account], [plan], [state], Today));

        Assert.Equal(1_200, total.Fixed);
        var item = Assert.Single(total.Items);
        Assert.Equal(plan.Id, item.ScheduleId);
        Assert.Equal(state.DueDate, item.DueDate);
        Assert.True(item.IsOverdue);
    }

    [Theory]
    [InlineData(-800)]
    [InlineData(800)]
    public void A_distant_moved_reminder_keeps_the_stable_original_occurrence_id(int offset)
    {
        var original = Today.AddDays(offset);
        var plan = Once(original);
        plan.ReminderEnabled = true;
        plan.ReminderDaysBefore = 0;
        plan.ReminderTime = new TimeOnly(9, 0);
        var state = Move(plan, original);
        state.DueDate = Today.AddDays(1);

        var reminder = Assert.Single(ReminderPlanner.Plan([plan], [state], Today.ToDateTime(new TimeOnly(12, 0))));

        Assert.Equal(ReminderPlanner.StableId(plan.Id, original), reminder.Id);
        Assert.Equal(state.DueDate.Value.ToDateTime(plan.ReminderTime, DateTimeKind.Local), reminder.NotifyAt);
        Assert.Equal(original, reminder.Occurrence.OriginalDate);
    }

    private static Schedule Once(DateOnly original) => new()
    {
        Name = "Moved original", Amount = 1_200,
        Rule = new RecurrenceRule { Frequency = Frequency.Once, Start = original },
    };

    private static Schedule Monthly() => new()
    {
        Name = "Monthly original", Amount = 1_200,
        Rule = new RecurrenceRule { Frequency = Frequency.Monthly, Start = new DateOnly(2024, 1, 15) },
    };

    private static OccurrenceState Move(Schedule plan, DateOnly original) => new()
    {
        ScheduleId = plan.Id, OriginalDate = original, DueDate = Today,
    };
}
