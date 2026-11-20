using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.Core.Tests.Plans;

public sealed class SecondDayTests
{
    [Fact]
    public void A_monthly_plan_can_occur_on_two_days_each_with_its_own_number()
    {
        var rule = new RecurrenceRule { Frequency = Frequency.Monthly, Start = new DateOnly(2027, 1, 1), SecondDay = 15 };

        var dates = Recurrence.Between(rule, new DateOnly(2027, 1, 1), new DateOnly(2027, 3, 1)).ToList();

        Assert.Equal([new DateOnly(2027, 1, 1), new DateOnly(2027, 1, 15), new DateOnly(2027, 2, 1), new DateOnly(2027, 2, 15), new DateOnly(2027, 3, 1)], dates.Select(d => d.Date));
        Assert.Equal([1, 2, 3, 4, 5], dates.Select(d => d.Number));
        Assert.Equal(24m, Recurrence.PerYear(rule));
    }

    [Fact]
    public void Count_end_date_and_missing_days_apply_per_date_without_duplicates()
    {
        var counted = new RecurrenceRule { Frequency = Frequency.Monthly, Start = new DateOnly(2027, 1, 10), SecondDay = 25, End = EndKind.AfterCount, Count = 3 };
        Assert.Equal([new DateOnly(2027, 1, 10), new DateOnly(2027, 1, 25), new DateOnly(2027, 2, 10)], Recurrence.Next(counted, new DateOnly(2027, 1, 1), 10).Select(d => d.Date));

        // Day 30 and day 31 both become 28 February: one occurrence, not two.
        var lastDays = new RecurrenceRule { Frequency = Frequency.Monthly, Start = new DateOnly(2027, 1, 30), SecondDay = 31 };
        Assert.Equal([new DateOnly(2027, 2, 28)], Recurrence.Between(lastDays, new DateOnly(2027, 2, 1), new DateOnly(2027, 2, 28)).Select(d => d.Date));

        // A second day before the start in the first month is not in the past of the plan.
        var late = new RecurrenceRule { Frequency = Frequency.Monthly, Start = new DateOnly(2027, 1, 20), SecondDay = 5 };
        Assert.Equal(new DateOnly(2027, 1, 20), Recurrence.Next(late, new DateOnly(2027, 1, 1), 1).Single().Date);

        Assert.Equal("SecondDayInvalid", Recurrence.Validate(new RecurrenceRule { Frequency = Frequency.Weekly, Start = new DateOnly(2027, 1, 1), SecondDay = 3 }));
        Assert.Equal("SecondDayInvalid", Recurrence.Validate(new RecurrenceRule { Frequency = Frequency.Monthly, Start = new DateOnly(2027, 1, 1), SecondDay = 32 }));
    }

    [Fact]
    public void Persian_months_use_their_own_days()
    {
        // 1 Farvardin 1406 = 21 March 2027; the second day is the 15th of each Persian month.
        var rule = new RecurrenceRule { Frequency = Frequency.Monthly, Calendar = PeriodCalendar.Persian, Start = new DateOnly(2027, 3, 21), SecondDay = 15 };

        Assert.Equal([new DateOnly(2027, 3, 21), new DateOnly(2027, 4, 4)], Recurrence.Next(rule, new DateOnly(2027, 3, 1), 2).Select(d => d.Date));
    }

    [Theory]
    [InlineData(PeriodCalendar.Gregorian, 2027)]
    [InlineData(PeriodCalendar.Persian, 1406)]
    [InlineData(PeriodCalendar.Hijri, 1448)]
    public void An_identical_fixed_second_day_does_not_double_monthly_commitments(PeriodCalendar calendar, int year)
    {
        var start = PeriodMath.ToDate(year, 1, 15, calendar);
        var rule = new RecurrenceRule
        {
            Frequency = Frequency.Monthly, Interval = 2, Calendar = calendar, Start = start, SecondDay = 15,
        };
        var plan = new Schedule { Name = "Synthetic commitment", Amount = 10_000, Rule = rule };

        Assert.Equal(PeriodMath.ToDate(year, 3, 15, calendar), Recurrence.Next(rule, start, 2)[1].Date);
        Assert.Equal(5_000, BudgetPlanning.MonthlyEquivalent(plan));

        rule.SecondDay = 20;
        Assert.Equal(PeriodMath.ToDate(year, 1, 20, calendar), Recurrence.Next(rule, start, 2)[1].Date);
        Assert.Equal(10_000, BudgetPlanning.MonthlyEquivalent(plan));
    }
}
