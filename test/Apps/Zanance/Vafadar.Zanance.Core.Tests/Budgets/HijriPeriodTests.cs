using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.DataFiles;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Receipts;

namespace Vafadar.Zanance.Core.Tests.Budgets;

/// <summary>The lunar Hijri calendar in periods, plans, imports and receipts (Umm al-Qura, see LunarHijri).</summary>
public sealed class HijriPeriodTests
{
    [Fact]
    public void A_Hijri_month_is_a_budget_month_of_29_or_30_days()
    {
        Assert.Equal((new DateOnly(2025, 3, 1), new DateOnly(2025, 3, 29)), PeriodMath.MonthRange(1446, 9, PeriodCalendar.Hijri));
        Assert.Equal((1446, 9), PeriodMath.MonthOf(new DateOnly(2025, 3, 15), PeriodCalendar.Hijri));
        Assert.Equal(15, PeriodMath.DayOf(new DateOnly(2025, 3, 15), PeriodCalendar.Hijri));
    }

    [Fact]
    public void A_Hijri_financial_month_with_a_start_day_runs_to_the_day_before_it()
    {
        // From the 25th: "Ramadan" is 25 Ramadan to 24 Shawwal.
        var (first, last) = PeriodMath.MonthRange(1446, 9, PeriodCalendar.Hijri, 25);

        Assert.Equal(PeriodMath.ToDate(1446, 9, 25, PeriodCalendar.Hijri), first);
        Assert.Equal(PeriodMath.ToDate(1446, 10, 24, PeriodCalendar.Hijri), last);
        Assert.Equal((1446, 9), PeriodMath.MonthOf(last, PeriodCalendar.Hijri, 25));
    }

    [Fact]
    public void A_Hijri_year_has_twelve_months_and_follows_the_lunar_year()
    {
        var (first, last) = PeriodMath.YearRange(new DateOnly(2025, 3, 15), PeriodCalendar.Hijri);

        Assert.Equal(new DateOnly(2024, 7, 7), first);
        Assert.Equal(new DateOnly(2025, 6, 25), last);
    }

    [Fact]
    public void A_monthly_plan_on_day_30_uses_the_last_day_of_29_day_months()
    {
        // 30 Safar 1446 (Safar has 30 days); Ramadan 1446 has 29, so that occurrence falls on the 29th.
        var rule = new RecurrenceRule
        {
            Frequency = Frequency.Monthly,
            Interval = 1,
            Start = PeriodMath.ToDate(1446, 2, 30, PeriodCalendar.Hijri),
            Calendar = PeriodCalendar.Hijri,
            MissingDay = MissingDayPolicy.LastValidDay,
        };

        var dates = Recurrence.Next(rule, rule.Start, 8).Select(d => d.Date).ToList();

        Assert.Equal(PeriodMath.ToDate(1446, 9, 29, PeriodCalendar.Hijri), dates[7]);
        Assert.All(dates, d => Assert.True(PeriodMath.DayOf(d, PeriodCalendar.Hijri) >= 29));
    }

    [Theory]
    [InlineData("1446/09/01", "yyyy/MM/dd")]
    [InlineData("۰۱.۰۹.۱۴۴۶", "dd.MM.yyyy")]
    public void A_CSV_date_can_be_read_in_the_Hijri_calendar(string text, string format)
    {
        Assert.True(CsvImport.TryDate(text, format, PeriodCalendar.Hijri, out var date));
        Assert.Equal(new DateOnly(2025, 3, 1), date);
        Assert.False(CsvImport.TryDate("1446/09/30", "yyyy/MM/dd", PeriodCalendar.Hijri, out _));
    }

    [Theory]
    [InlineData("Datum 1447/03/12", 2025, 9, 4)]
    [InlineData("تاریخ 1404/07/13", 2025, 10, 5)]
    public void A_receipt_year_from_1300_to_1500_is_read_in_the_Hijri_calendar_nearer_to_today(string line, int year, int month, int day)
    {
        // CR: 1447 read as a solar year would be 2068; as a lunar year it is September 2025.
        var receipt = ReceiptParser.Parse($"Market{Environment.NewLine}{line}{Environment.NewLine}Total 12.50", new DateOnly(2025, 10, 5));

        Assert.Equal(new DateOnly(year, month, day), receipt.Date);
    }
}
