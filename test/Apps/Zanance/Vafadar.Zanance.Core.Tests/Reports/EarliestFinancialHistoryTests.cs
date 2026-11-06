using System.Text.Json;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Reports;

namespace Vafadar.Zanance.Core.Tests.Reports;

/// <summary>Three-month suggestions distinguish unavailable history from a valid current financial period.</summary>
public sealed class EarliestFinancialHistoryTests
{
    /// <summary>All supported calendars and both ordinary and shifted financial-month starts.</summary>
    public static IEnumerable<object[]> Contexts()
    {
        foreach (var calendar in Enum.GetValues<PeriodCalendar>())
        foreach (var start in new[] { 1, 28 })
        foreach (var month in new[] { 1, 2, 3, 4 })
            yield return [calendar, start, month];
    }

    [Theory, MemberData(nameof(Contexts))]
    public void Current_reports_require_three_representable_complete_months(PeriodCalendar calendar, int start, int month)
    {
        var today = PeriodMath.ToDate(1, month, start, calendar);
        var account = new Account { Name = "Fictitious earliest history", Type = AccountType.Checking,
            CurrencyCode = "EUR", OpeningDate = PeriodMath.ToDate(1, 1, 1, calendar), OpeningBalance = 10000 };
        var before = JsonSerializer.Serialize(account);
        var capacity = CapacityCalculator.Compute([account], [], [], [], [], [], "EUR", today, calendar, start);
        Assert.Equal(month == 4, capacity.EnoughHistory);
        Assert.Equal(month == 4 ? 0L : (long?)null, capacity.Amount);
        var coverage = KpiCatalog.Coverage([account], [], [], [], "EUR", today, calendar, start);
        Assert.Equal(month == 4 ? CoverageStatus.NotAvailable : CoverageStatus.NotEnoughHistory, coverage.Status);
        Assert.Equal(10000, coverage.UsableMoney);
        Assert.Equal(month == 4 ? 0L : (long?)null,
            LiquidityCalculator.SuggestPerDay([account], [], "EUR", today, calendar, start));
        Assert.Equal(before, JsonSerializer.Serialize(account));
    }

    [Theory]
    [InlineData(PeriodCalendar.Gregorian)]
    [InlineData(PeriodCalendar.Persian)]
    [InlineData(PeriodCalendar.Hijri)]
    public void An_invalid_selected_current_period_still_refuses(PeriodCalendar calendar)
    {
        var today = PeriodMath.ToDate(1, 1, 1, calendar);
        var account = new Account { Name = "Fictitious invalid current period", CurrencyCode = "EUR", OpeningDate = today };
        Assert.Throws<ArgumentOutOfRangeException>(() => CapacityCalculator.Compute([account], [], [], [], [], [], "EUR", today, calendar, 28));
        Assert.Throws<ArgumentOutOfRangeException>(() => KpiCatalog.Coverage([account], [], [], [], "EUR", today, calendar, 28));
        Assert.Throws<ArgumentOutOfRangeException>(() => LiquidityCalculator.SuggestPerDay([account], [], "EUR", today, calendar, 28));
    }
}
