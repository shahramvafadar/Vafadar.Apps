using System.Globalization;
using Vafadar.Finance.Core.Budgets;
using Vafadar.Finance.Core.Plans;

namespace Vafadar.Finance.Core.Tests.Plans;

public sealed class WeekdayRuleTests
{
    private static List<DateOnly> Dates(RecurrenceRule rule, int count) => [.. Recurrence.Next(rule, rule.Start, count).Select(d => d.Date)];

    [Fact]
    public void The_second_monday_stays_the_second_monday_every_month()
    {
        // 12 October 2026 is the second Monday of October.
        var rule = new RecurrenceRule { Frequency = Frequency.Monthly, Start = new DateOnly(2026, 10, 12), DayRule = MonthDayRule.NthWeekday };
        Assert.Equal([new DateOnly(2026, 10, 12), new DateOnly(2026, 11, 9), new DateOnly(2026, 12, 14)], Dates(rule, 3));
    }

    [Fact]
    public void The_last_friday_is_found_in_months_with_four_or_five_fridays()
    {
        var rule = new RecurrenceRule { Frequency = Frequency.Monthly, Start = new DateOnly(2026, 10, 30), DayRule = MonthDayRule.LastWeekday };
        Assert.Equal([new DateOnly(2026, 10, 30), new DateOnly(2026, 11, 27), new DateOnly(2026, 12, 25), new DateOnly(2027, 1, 29)], Dates(rule, 4));
    }

    [Fact]
    public void A_start_in_the_fifth_week_means_the_last_such_weekday()
    {
        // 29 October 2026 is the fifth Thursday; November has only four.
        var rule = new RecurrenceRule { Frequency = Frequency.Monthly, Start = new DateOnly(2026, 10, 29), DayRule = MonthDayRule.NthWeekday };
        Assert.Equal([new DateOnly(2026, 10, 29), new DateOnly(2026, 11, 26)], Dates(rule, 2));
        Assert.Equal(5, MonthDayRules.WeekOf(29));
        Assert.Equal(1, MonthDayRules.WeekOf(7));
    }

    [Fact]
    public void Weekday_rules_follow_persian_months()
    {
        // 1 Mehr 1405 is a Wednesday: every plan date is the first Wednesday of its Persian month.
        var persian = new PersianCalendar();
        var start = DateOnly.FromDateTime(persian.ToDateTime(1405, 7, 1, 0, 0, 0, 0));
        var rule = new RecurrenceRule { Frequency = Frequency.Monthly, Start = start, Calendar = PeriodCalendar.Persian, DayRule = MonthDayRule.NthWeekday };
        var dates = Dates(rule, 12);
        Assert.All(dates, d =>
        {
            Assert.Equal(DayOfWeek.Wednesday, d.DayOfWeek);
            Assert.InRange(persian.GetDayOfMonth(d.ToDateTime(TimeOnly.MinValue)), 1, 7);
        });
        Assert.Equal(12, dates.Select(d => persian.GetMonth(d.ToDateTime(TimeOnly.MinValue))).Distinct().Count());
    }

    [Fact]
    public void A_yearly_weekday_rule_keeps_the_weekday_of_its_month()
    {
        // The fourth Thursday of November.
        var rule = new RecurrenceRule { Frequency = Frequency.Yearly, Start = new DateOnly(2026, 11, 26), DayRule = MonthDayRule.NthWeekday };
        Assert.Equal([new DateOnly(2026, 11, 26), new DateOnly(2027, 11, 25), new DateOnly(2028, 11, 23)], Dates(rule, 3));
    }

    [Fact]
    public void A_second_day_cannot_be_combined_with_a_weekday_rule()
    {
        var rule = new RecurrenceRule { Frequency = Frequency.Monthly, Start = new DateOnly(2026, 10, 12), DayRule = MonthDayRule.NthWeekday, SecondDay = 20 };
        Assert.Equal("SecondDayInvalid", Recurrence.Validate(rule));
    }
}
