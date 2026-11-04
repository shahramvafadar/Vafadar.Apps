using System.Text.Json;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Forecasts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Rates;
using Vafadar.Zanance.Core.Reports;

namespace Vafadar.Zanance.Core.Tests.Reports;

/// <summary>Valid first/last chronological reads do not invent dates outside the supported calendar.</summary>
public sealed class ChronologicalReadBoundaryTests
{
    private static Account Account(DateOnly opening, string currency = "EUR") => new()
    { Name = "Boundary source", Type = AccountType.Checking, CurrencyCode = currency, OpeningDate = opening, OpeningBalance = 100 };

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Forecast_includes_the_final_date_exactly_once(int days)
    {
        var start = DateOnly.MaxValue.AddDays(-days);
        var account = Account(start);
        var entry = new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense, Amount = 25, Date = DateOnly.MaxValue };
        var before = JsonSerializer.Serialize(new { account, entry });
        var forecast = Assert.Single(ForecastCalculator.Compute([account], [entry], [], [], start, DateOnly.MaxValue));
        Assert.Equal(days + 1, forecast.Path.Count);
        Assert.Equal(DateOnly.MaxValue, forecast.Path[^1].Date);
        Assert.Equal(75, forecast.EndBalance);
        Assert.Equal(days == 0 ? 75 : 100, forecast.StartBalance);
        Assert.Equal(before, JsonSerializer.Serialize(new { account, entry }));
    }

    [Fact]
    public void Final_day_transfer_retains_both_original_currency_effects()
    {
        var start = DateOnly.MaxValue.AddDays(-1);var euro = Account(start);var dollar = Account(start, "USD");
        var transfer = new LedgerEntry { AccountId = euro.Id, ToAccountId = dollar.Id, Kind = EntryKind.Transfer,
            Amount = 10, ToAmount = 12, Date = DateOnly.MaxValue };
        var result = ForecastCalculator.Compute([euro, dollar], [transfer], [], [], start, DateOnly.MaxValue);
        Assert.Equal(90, result.Single(r => r.CurrencyCode == "EUR").EndBalance);
        Assert.Equal(112, result.Single(r => r.CurrencyCode == "USD").EndBalance);
        Assert.All(result, r => Assert.Equal(2, r.Path.Count));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(1, 1)]
    public void First_day_report_keeps_opening_money_inside_the_period(int opensAfter, int endsAfter)
    {
        var account = Account(DateOnly.MinValue.AddDays(opensAfter));
        var income = new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Income, Amount = 5, Date = account.OpeningDate };
        var expense = new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense, Amount = 3, Date = account.OpeningDate.AddDays(1) };
        var before = JsonSerializer.Serialize(new { account, income, expense });
        var movement = Assert.Single(ReportCalculator.AccountMovements([account], [income, expense], DateOnly.MinValue, DateOnly.MinValue.AddDays(endsAfter)));
        var opened = opensAfter <= endsAfter;
        Assert.Equal(0, movement.Opening);
        Assert.Equal(opened ? 100 : 0, movement.OpeningBalanceAdded);
        Assert.Equal(opened ? 5 : 0, movement.Income);
        Assert.Equal(endsAfter > opensAfter ? 3 : 0, movement.Expense);
        Assert.Equal(opened ? (endsAfter > opensAfter ? 102 : 105) : 0, movement.Closing);
        Assert.Equal(before, JsonSerializer.Serialize(new { account, income, expense }));
    }

    [Theory]
    [InlineData(0, 7, false)]
    [InlineData(3, 7, false)]
    [InlineData(7, 7, false)]
    [InlineData(8, 7, true)]
    [InlineData(30, 90, false)]
    public void Rate_age_uses_elapsed_days_without_constructing_an_unavailable_date(int elapsed, int threshold, bool expected) =>
        Assert.Equal(expected, new RateFreshness(threshold).IsOutdated(DateOnly.MinValue, DateOnly.MinValue.AddDays(elapsed)));

    [Fact]
    public void Current_financial_month_still_exempts_an_old_rate() =>
        Assert.False(new RateFreshness(7, DateOnly.MinValue).IsOutdated(DateOnly.MinValue, DateOnly.MinValue.AddDays(8)));

    [Theory]
    [InlineData(AccountType.Checking)]
    [InlineData(AccountType.Loan)]
    public void Same_final_day_wealth_has_no_flow_or_change(AccountType type)
    {
        var account = Account(DateOnly.MaxValue);account.Type = type;
        if (type == AccountType.Loan) account.OpeningBalance = -100;
        var before = JsonSerializer.Serialize(account);
        var change = Assert.Single(WealthHistory.Explain([account], [], [], [], [], DateOnly.MaxValue, DateOnly.MaxValue));
        Assert.Equal(change.Start, change.End);
        Assert.Equal(0, change.Change);Assert.Equal(0, change.Flows);Assert.Equal(0, change.Remainder);
        Assert.Equal(before, JsonSerializer.Serialize(account));
    }

    [Fact]
    public void An_ordinary_same_day_wealth_comparison_still_has_no_flow()
    {
        var day = new DateOnly(2026, 11, 4);var account = Account(day);
        var change = Assert.Single(WealthHistory.Explain([account], [], [], [], [], day, day));
        Assert.Equal(100, change.Start);Assert.Equal(100, change.End);Assert.Equal(0, change.Flows);
    }

    [Theory]
    [InlineData(PeriodCalendar.Gregorian, 1)]
    [InlineData(PeriodCalendar.Persian, 1)]
    [InlineData(PeriodCalendar.Hijri, 1)]
    [InlineData(PeriodCalendar.Gregorian, 28)]
    [InlineData(PeriodCalendar.Persian, 28)]
    [InlineData(PeriodCalendar.Hijri, 28)]
    public void Monthly_history_returns_only_representable_earliest_periods(PeriodCalendar calendar, int startDay)
    {
        var today = PeriodMath.ToDate(1, 1, Math.Max(10, startDay), calendar);
        var account = Account(PeriodMath.ToDate(1, 1, 1, calendar));
        var point = Assert.Single(ReportCalculator.MonthlyTrend([account], [], today, 12, calendar, "EUR", startDay));
        Assert.Equal(1, point.Year);Assert.Equal(1, point.Month);
        Assert.Equal(PeriodMath.MonthRange(1, 1, calendar, startDay).First, point.From);
        Assert.True(point.IsPartial);Assert.Equal(0, point.NetIncome);Assert.Equal(0, point.NetExpense);
    }

    [Theory]
    [InlineData(PeriodCalendar.Gregorian, 1)]
    [InlineData(PeriodCalendar.Persian, 1)]
    [InlineData(PeriodCalendar.Hijri, 1)]
    [InlineData(PeriodCalendar.Gregorian, 28)]
    [InlineData(PeriodCalendar.Persian, 28)]
    [InlineData(PeriodCalendar.Hijri, 28)]
    public void Wealth_history_has_today_but_no_unrepresentable_previous_month(PeriodCalendar calendar, int startDay)
    {
        var today = PeriodMath.ToDate(1, 1, Math.Max(10, startDay), calendar);
        var account = Account(PeriodMath.ToDate(1, 1, 1, calendar));
        var point = Assert.Single(WealthHistory.MonthEnds([account], [], [], [], [], new RateTable([]), "EUR", today, 12, calendar, startDay));
        Assert.Equal(today, point.Date);Assert.Equal(100, point.Converted);Assert.False(point.IsIncomplete);
    }

    [Theory]
    [InlineData(PeriodCalendar.Gregorian)]
    [InlineData(PeriodCalendar.Persian)]
    [InlineData(PeriodCalendar.Hijri)]
    public void A_financial_period_start_before_the_calendar_still_refuses(PeriodCalendar calendar)
    {
        var today = PeriodMath.ToDate(1, 1, 1, calendar);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            WealthHistory.MonthEnds([], [], [], [], [], new RateTable([]), "EUR", today, 12, calendar, 28));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ReportCalculator.MonthlyTrend([], [], today, 12, calendar, "EUR", 28));
    }

}
