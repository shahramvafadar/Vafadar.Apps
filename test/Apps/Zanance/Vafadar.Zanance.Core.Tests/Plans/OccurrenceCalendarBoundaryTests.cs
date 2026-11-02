using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.Core.Tests.Plans;

/// <summary>Original next-open and implicit shifts must respect the representable calendar endpoints.</summary>
public sealed class OccurrenceCalendarBoundaryTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(30)]
    [InlineData(62)]
    [InlineData(100)]
    public void Next_open_includes_the_last_representable_date(int daysBefore)
    {
        var plan = Plan();
        var next = Occurrences.NextOpen(plan, [], DateOnly.MaxValue.AddDays(-daysBefore), DateOnly.MaxValue);

        Assert.NotNull(next);
        Assert.Same(plan, next.Schedule);
        Assert.Equal(1, next.Number);
        Assert.Equal(DateOnly.MaxValue, next.OriginalDate);
        Assert.Equal(DateOnly.MaxValue, next.DueDate);
        Assert.Equal(OccurrenceView.Due, next.Status);
        Assert.Equal(123L, next.Outstanding);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void Exhausted_search_at_calendar_end_returns_no_open_occurrence(int daysBefore)
    {
        var plan = Plan();
        var state = new OccurrenceState { ScheduleId = plan.Id, OriginalDate = DateOnly.MaxValue, Status = OccurrenceStatus.Settled };

        Assert.Null(Occurrences.NextOpen(plan, [state], DateOnly.MaxValue.AddDays(-daysBefore), DateOnly.MaxValue));
        Assert.Equal(OccurrenceStatus.Settled, state.Status);
        Assert.Equal(DateOnly.MaxValue, state.OriginalDate);
        Assert.Null(state.DueDate);
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 1)]
    [InlineData(true, 0)]
    [InlineData(true, 1)]
    public void Weekend_shift_stops_at_the_calendar_boundary(bool forward, int daysInside)
    {
        var edge = forward ? DateOnly.MaxValue : DateOnly.MinValue;
        var date = edge.AddDays(forward ? -daysInside : daysInside);
        var mask = (1 << (int)edge.DayOfWeek) | (1 << (int)date.DayOfWeek);
        var rule = new RecurrenceRule { Start = date, Frequency = Frequency.Once,
            WeekendDays = mask, WeekendShift = forward ? WeekendShift.After : WeekendShift.Before };

        Assert.Equal(edge, rule.ApplyWeekend(date));
        Assert.Equal(date, rule.Start);
        Assert.Equal(mask, rule.WeekendDays);
    }

    private static Schedule Plan() => new()
    {
        Name = "Calendar boundary", Kind = EntryKind.Expense, AmountMode = AmountMode.Fixed,
        Amount = 123, Rule = new RecurrenceRule { Frequency = Frequency.Once, Start = DateOnly.MaxValue },
    };
}
