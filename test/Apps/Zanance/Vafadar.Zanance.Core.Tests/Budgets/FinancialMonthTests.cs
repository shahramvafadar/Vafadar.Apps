using System.Globalization;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Reports;

namespace Vafadar.Zanance.Core.Tests.Budgets;

/// <summary>Financial months with their own start day (§10.3 pay-cycle periods).</summary>
public sealed class FinancialMonthTests
{
    [Fact]
    public void A_month_starting_on_the_25th_runs_to_the_24th_of_the_next_month_and_is_named_after_its_start()
    {
        Assert.Equal((new DateOnly(2026, 9, 25), new DateOnly(2026, 10, 24)), PeriodMath.MonthRange(2026, 9, PeriodCalendar.Gregorian, 25));
        Assert.Equal((new DateOnly(2026, 12, 25), new DateOnly(2027, 1, 24)), PeriodMath.MonthRange(2026, 12, PeriodCalendar.Gregorian, 25));
        Assert.Equal((2026, 9), PeriodMath.MonthOf(new DateOnly(2026, 10, 24), PeriodCalendar.Gregorian, 25));
        Assert.Equal((2026, 10), PeriodMath.MonthOf(new DateOnly(2026, 10, 25), PeriodCalendar.Gregorian, 25));
        Assert.Equal((2025, 12), PeriodMath.MonthOf(new DateOnly(2026, 1, 3), PeriodCalendar.Gregorian, 25));
    }

    [Fact]
    public void Consecutive_financial_months_leave_no_gap_even_around_february()
    {
        var (_, januaryEnd) = PeriodMath.MonthRange(2027, 1, PeriodCalendar.Gregorian, 28);
        var (februaryStart, februaryEnd) = PeriodMath.MonthRange(2027, 2, PeriodCalendar.Gregorian, 28);
        var (marchStart, _) = PeriodMath.MonthRange(2027, 3, PeriodCalendar.Gregorian, 28);

        Assert.Equal(januaryEnd.AddDays(1), februaryStart);
        Assert.Equal(februaryEnd.AddDays(1), marchStart);
        Assert.Equal(new DateOnly(2027, 3, 27), februaryEnd);
    }

    [Fact]
    public void Persian_financial_months_start_on_the_day_in_the_persian_calendar()
    {
        var persian = new PersianCalendar();
        var (first, last) = PeriodMath.MonthRange(1405, 6, PeriodCalendar.Persian, 25);

        Assert.Equal((1405, 6, 25), (persian.GetYear(first.ToDateTime(TimeOnly.MinValue)), persian.GetMonth(first.ToDateTime(TimeOnly.MinValue)), persian.GetDayOfMonth(first.ToDateTime(TimeOnly.MinValue))));
        Assert.Equal((1405, 7, 24), (persian.GetYear(last.ToDateTime(TimeOnly.MinValue)), persian.GetMonth(last.ToDateTime(TimeOnly.MinValue)), persian.GetDayOfMonth(last.ToDateTime(TimeOnly.MinValue))));
        Assert.Equal((1405, 6), PeriodMath.MonthOf(last, PeriodCalendar.Persian, 25));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(31)]
    public void Day_one_or_an_invalid_day_is_the_calendar_month(int startDay)
    {
        var expected = startDay > PeriodMath.MaxStartDay
            ? (new DateOnly(2026, 9, 28), new DateOnly(2026, 10, 27))
            : (new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        Assert.Equal(expected, PeriodMath.MonthRange(2026, 9, PeriodCalendar.Gregorian, startDay));
    }

    [Fact]
    public void The_monthly_trend_follows_the_financial_month()
    {
        var account = new Account { Name = "Checking", CurrencyCode = "EUR", OpeningDate = new DateOnly(2026, 1, 1) };
        LedgerEntry[] entries =
        [
            new() { Kind = EntryKind.Income, AccountId = account.Id, Amount = 300_000, Date = new DateOnly(2026, 9, 25) },
            new() { Kind = EntryKind.Expense, AccountId = account.Id, Amount = 5_000, Date = new DateOnly(2026, 10, 20) },
        ];

        var month = ReportCalculator.MonthlyTrend([account], entries, new DateOnly(2026, 10, 20), 1, PeriodCalendar.Gregorian, "EUR", startDay: 25).Single();

        Assert.Equal((new DateOnly(2026, 9, 25), new DateOnly(2026, 10, 24)), (month.From, month.To));
        Assert.Equal(300_000, month.NetIncome);
        Assert.Equal(5_000, month.NetExpense);
    }
}
