using System.Globalization;
using Vafadar.Finance.Core.Budgets;
using Vafadar.Finance.Core.Plans;

namespace Vafadar.Finance.Core.Tests.Plans;

public sealed class RecurrenceTests
{
    private static readonly PersianCalendar Persian = new();

    [Fact]
    [Trait("AT", "AT-17")]
    public void Every_two_weeks_is_not_twice_a_month()
    {
        var rule = new RecurrenceRule { Frequency = Frequency.Weekly, Interval = 2, Start = new DateOnly(2027, 1, 1) };

        var january = Dates(rule, new DateOnly(2027, 1, 1), new DateOnly(2027, 1, 31));

        Assert.Equal([new DateOnly(2027, 1, 1), new DateOnly(2027, 1, 15), new DateOnly(2027, 1, 29)], january);
    }

    [Fact]
    [Trait("AT", "AT-19")]
    public void Day_31_uses_the_last_valid_day_and_returns_to_the_31st()
    {
        var rule = new RecurrenceRule { Frequency = Frequency.Monthly, Start = new DateOnly(2027, 1, 31) };

        var dates = Dates(rule, new DateOnly(2027, 1, 1), new DateOnly(2027, 4, 30));

        Assert.Equal([new DateOnly(2027, 1, 31), new DateOnly(2027, 2, 28), new DateOnly(2027, 3, 31), new DateOnly(2027, 4, 30)], dates);
    }

    [Fact]
    [Trait("AT", "AT-20")]
    public void Skip_policy_leaves_out_invalid_months_and_keeps_the_anchor()
    {
        var rule = new RecurrenceRule { Frequency = Frequency.Monthly, Start = new DateOnly(2027, 1, 31), MissingDay = MissingDayPolicy.Skip };

        var dates = Dates(rule, new DateOnly(2027, 1, 1), new DateOnly(2027, 5, 31));

        Assert.Equal([new DateOnly(2027, 1, 31), new DateOnly(2027, 3, 31), new DateOnly(2027, 5, 31)], dates);
    }

    [Fact]
    [Trait("AT", "AT-21")]
    public void Last_day_of_month_follows_the_month_length()
    {
        var rule = new RecurrenceRule { Frequency = Frequency.Monthly, Start = new DateOnly(2028, 1, 15), DayRule = MonthDayRule.LastDayOfMonth };

        var dates = Dates(rule, new DateOnly(2028, 1, 1), new DateOnly(2028, 4, 30));

        Assert.Equal([new DateOnly(2028, 1, 31), new DateOnly(2028, 2, 29), new DateOnly(2028, 3, 31), new DateOnly(2028, 4, 30)], dates);
    }

    [Theory]
    [Trait("AT", "AT-22")]
    [InlineData(MissingDayPolicy.LastValidDay, "2029-02-28")]
    [InlineData(MissingDayPolicy.Skip, null)]
    public void February_29_in_a_common_year_follows_the_policy(MissingDayPolicy policy, string? expected)
    {
        var rule = new RecurrenceRule { Frequency = Frequency.Yearly, Start = new DateOnly(2028, 2, 29), MissingDay = policy };

        var dates = Dates(rule, new DateOnly(2029, 1, 1), new DateOnly(2029, 12, 31));

        Assert.Equal(expected is null ? [] : [DateOnly.Parse(expected, CultureInfo.InvariantCulture)], dates);
        Assert.Contains(new DateOnly(2032, 2, 29), Dates(rule, new DateOnly(2032, 1, 1), new DateOnly(2032, 12, 31)));
    }

    [Fact]
    [Trait("AT", "AT-22")]
    public void Esfand_30_in_a_common_persian_year_uses_the_last_valid_day()
    {
        // 1403 is a leap year (Esfand has 30 days), 1404 is not.
        var start = FromPersian(1403, 12, 30);
        var rule = new RecurrenceRule { Frequency = Frequency.Yearly, Start = start, Calendar = PeriodCalendar.Persian };

        var next = Recurrence.Next(rule, start.AddDays(1), 1).Single().Date;

        Assert.Equal(FromPersian(1404, 12, 29), next);
    }

    [Fact]
    [Trait("AT", "AT-24")]
    public void Persian_monthly_rule_moves_by_persian_months_not_by_days()
    {
        // 1 Farvardin, 1 Ordibehesht, 1 Khordad: 31-day months, not 30-day steps.
        var rule = new RecurrenceRule { Frequency = Frequency.Monthly, Start = FromPersian(1406, 1, 1), Calendar = PeriodCalendar.Persian };

        var dates = Recurrence.Next(rule, rule.Start, 3).Select(d => d.Date);

        Assert.Equal([FromPersian(1406, 1, 1), FromPersian(1406, 2, 1), FromPersian(1406, 3, 1)], dates);
    }

    [Fact]
    [Trait("AT", "AT-24")]
    public void Persian_day_31_falls_back_in_30_day_months()
    {
        var rule = new RecurrenceRule { Frequency = Frequency.Monthly, Start = FromPersian(1406, 6, 31), Calendar = PeriodCalendar.Persian };

        var dates = Recurrence.Next(rule, rule.Start, 3).Select(d => d.Date);

        Assert.Equal([FromPersian(1406, 6, 31), FromPersian(1406, 7, 30), FromPersian(1406, 8, 30)], dates);
    }

    [Fact]
    [Trait("AT", "AT-23")]
    public void Gregorian_rule_is_unaffected_by_calendars_of_other_rules()
    {
        var gregorian = new RecurrenceRule { Frequency = Frequency.Monthly, Start = new DateOnly(2027, 1, 5) };

        Assert.All(Dates(gregorian, new DateOnly(2027, 1, 1), new DateOnly(2027, 12, 31)), d => Assert.Equal(5, d.Day));
    }

    [Fact]
    public void After_count_ends_after_the_given_number_of_occurrences()
    {
        var rule = new RecurrenceRule { Frequency = Frequency.Monthly, Start = new DateOnly(2027, 1, 10), End = EndKind.AfterCount, Count = 12 };

        var dates = Recurrence.Between(rule, DateOnly.MinValue, new DateOnly(2030, 1, 1)).ToList();

        Assert.Equal(12, dates.Count);
        Assert.Equal(new DateOnly(2027, 12, 10), dates[^1].Date);
        Assert.Equal(12, dates[^1].Number);
    }

    [Fact]
    public void End_date_is_inclusive_and_once_has_one_date()
    {
        var rule = new RecurrenceRule { Frequency = Frequency.Daily, Interval = 3, Start = new DateOnly(2027, 1, 1), End = EndKind.OnDate, EndDate = new DateOnly(2027, 1, 7) };
        Assert.Equal([new DateOnly(2027, 1, 1), new DateOnly(2027, 1, 4), new DateOnly(2027, 1, 7)], Dates(rule, DateOnly.MinValue, DateOnly.MaxValue));

        var once = new RecurrenceRule { Frequency = Frequency.Once, Start = new DateOnly(2027, 6, 1) };
        Assert.Equal([new DateOnly(2027, 6, 1)], Dates(once, DateOnly.MinValue, DateOnly.MaxValue));
    }

    [Theory]
    [InlineData(0, EndKind.Never, null, "IntervalMustBePositive")]
    [InlineData(1, EndKind.AfterCount, 0, "CountMustBePositive")]
    [InlineData(1, EndKind.Never, null, null)]
    public void Invalid_rules_are_reported_and_generate_nothing(int interval, EndKind end, int? count, string? problem)
    {
        var rule = new RecurrenceRule { Frequency = Frequency.Monthly, Interval = interval, Start = new DateOnly(2027, 1, 1), End = end, Count = count };

        Assert.Equal(problem, Recurrence.Validate(rule));
        if (problem is not null)
        {
            Assert.Empty(Dates(rule, DateOnly.MinValue, DateOnly.MaxValue));
        }
    }

    [Fact]
    public void Occurrences_per_year_give_monthly_equivalents()
    {
        Assert.Equal(12m, Recurrence.PerYear(new RecurrenceRule { Frequency = Frequency.Monthly }));
        Assert.Equal(4m, Recurrence.PerYear(new RecurrenceRule { Frequency = Frequency.Monthly, Interval = 3 }));
        Assert.Equal(0m, Recurrence.PerYear(new RecurrenceRule { Frequency = Frequency.Once }));
    }

    private static List<DateOnly> Dates(RecurrenceRule rule, DateOnly from, DateOnly to) =>
        [.. Recurrence.Between(rule, from, to).Select(d => d.Date)];

    private static DateOnly FromPersian(int year, int month, int day) =>
        DateOnly.FromDateTime(Persian.ToDateTime(year, month, day, 0, 0, 0, 0));
}
