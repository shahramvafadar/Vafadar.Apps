using System.Globalization;
using System.Text.Json;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Sync;

namespace Vafadar.Zanance.Core.Tests.Budgets;

/// <summary>Bounded read ranges never authorize incomplete synchronized budgets (D-813).</summary>
public sealed class SyncBudgetCompletenessTests
{
    [Theory]
    [InlineData(BudgetPeriod.Month, PeriodCalendar.Gregorian, 25)]
    [InlineData(BudgetPeriod.Month, PeriodCalendar.Hijri, 1)]
    [InlineData(BudgetPeriod.Month, PeriodCalendar.Persian, 25)]
    [InlineData(BudgetPeriod.Week, PeriodCalendar.Gregorian, 1)]
    [InlineData(BudgetPeriod.TwoWeeks, PeriodCalendar.Gregorian, 1)]
    public void An_incomplete_synchronized_period_is_refused_without_changing_rows(BudgetPeriod period, PeriodCalendar calendar, int startDay)
    {
        var graph = Graph(period, calendar, DateOnly.MaxValue, startDay);
        var before = graph.Select(row => row.Values!.Value.GetRawText()).ToArray();

        var issue = Assert.Single(SyncFinancialValidation.Validate([], graph));

        Assert.Equal("InvalidBudgetPeriod", issue.Reason);
        Assert.Equal(graph[0].Record, issue.Record);
        Assert.Equal(before, graph.Select(row => row.Values!.Value.GetRawText()));
    }

    [Theory]
    [InlineData(BudgetPeriod.Month, 1, 0)]
    [InlineData(BudgetPeriod.Month, 25, 61)]
    [InlineData(BudgetPeriod.Week, 1, 6)]
    [InlineData(BudgetPeriod.TwoWeeks, 1, 13)]
    public void A_complete_synchronized_period_keeps_its_original_acceptance(BudgetPeriod period, int startDay, int daysBeforeEnd)
    {
        var graph = Graph(period, PeriodCalendar.Gregorian, DateOnly.MaxValue.AddDays(-daysBeforeEnd), startDay);

        Assert.Empty(SyncFinancialValidation.Validate([], graph));
    }

    private static SyncRecord[] Graph(BudgetPeriod period, PeriodCalendar calendar, DateOnly date, int startDay)
    {
        var (year, month) = PeriodMath.MonthOf(date, calendar);
        var id = Guid.NewGuid().ToString("D").ToUpperInvariant();
        var budget = JsonSerializer.SerializeToElement(new
        {
            AccountIds = "[]", AlertsEnabled = 0L, Calendar = (long)calendar, CreatedAt = 0L,
            CurrencyCode = "EUR", Id = id, Method = 0L, Month = month, Period = (long)period,
            PeriodStart = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), Rollover = 0L,
            TotalLimit = 0L, Year = year,
        });
        var settings = JsonSerializer.SerializeToElement(new
        {
            BudgetCalendar = 0L, DefaultAccountId = (string?)null, DefaultCurrencyCode = "EUR",
            DisplayUnits = (string?)null, EssentialEstimate = (long?)null, EssentialEstimateCurrency = (string?)null,
            EssentialEstimatePeriod = 0L, FortnightStart = (string?)null, HomeBudgetCurrencyCode = (string?)null,
            HomeLayout = (string?)null, Mode = 0L, MonthStartDay = startDay, RateFreshnessDays = 1L,
            ReportCurrencyCode = "EUR", ReviewProgress = (string?)null, ValuationCurrencyEnabled = 0L,
        });
        return [new(new("Budgets", JsonSerializer.Serialize(new[] { id })), 0, budget), new(new("Settings", "[]"), 0, settings)];
    }
}
