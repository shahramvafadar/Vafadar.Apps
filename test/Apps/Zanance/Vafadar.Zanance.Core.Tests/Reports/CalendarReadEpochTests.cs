using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Reports;

namespace Vafadar.Zanance.Core.Tests.Reports;

/// <summary>Missing earlier calendar time must not prevent original read-only summaries.</summary>
public sealed class CalendarReadEpochTests
{
    /// <summary>No complete preceding month exists at the first supported month of any calendar.</summary>
    [Theory]
    [InlineData(PeriodCalendar.Gregorian)]
    [InlineData(PeriodCalendar.Persian)]
    [InlineData(PeriodCalendar.Hijri)]
    public void First_calendar_month_has_no_due_review(PeriodCalendar calendar)
    {
        var first = PeriodMath.ToDate(1, 1, 1, calendar);
        Assert.Null(PeriodReview.Due(null, first, calendar, 1, DateOnly.MinValue));
    }

    /// <summary>The first actual completed month remains reviewable with its exact identity.</summary>
    [Theory]
    [InlineData(PeriodCalendar.Gregorian)]
    [InlineData(PeriodCalendar.Persian)]
    [InlineData(PeriodCalendar.Hijri)]
    public void Second_calendar_month_retains_first_month_review(PeriodCalendar calendar)
    {
        var first = PeriodMath.ToDate(1, 1, 1, calendar);
        var today = PeriodMath.ToDate(1, 2, 1, calendar);
        var review = Assert.IsType<PeriodReviewState>(PeriodReview.Due(null, today, calendar, 1, first));
        Assert.Equal((1, 1), (review.Year, review.Month));
        Assert.Null(PeriodReview.Due(PeriodReview.Finish(review), today, calendar, 1, first));
    }

    /// <summary>There is no balance before the earliest representable day; a later payment still keeps its split.</summary>
    [Fact]
    public void Earliest_debt_period_does_not_require_an_unrepresentable_previous_day()
    {
        var first = DateOnly.MinValue;
        var cash = new Account { Name = "Epoch payment source", Type = AccountType.Checking, CurrencyCode = "EUR", OpeningDate = first, OpeningBalance = 0, IncludeInTotals = true, UsableForPayments = true };
        var loan = new Account { Name = "Epoch loan", Type = AccountType.Loan, CurrencyCode = "EUR", OpeningDate = first, OpeningBalance = -10000, InterestRate = 12, UsableForPayments = false };
        var plan = new Schedule { Name = "Epoch installment", AccountId = cash.Id, ToAccountId = loan.Id, Kind = EntryKind.Transfer, Amount = 11, Rule = new RecurrenceRule { Frequency = Frequency.Once, Start = first.AddDays(2) } };
        var result = Assert.Single(KpiCatalog.Debt([cash, loan], [], [plan], [], first, first.AddDays(30), first));
        Assert.Equal(11, result.Installments);
        Assert.Equal(0, result.Interest);
        Assert.Equal(0, result.Principal);
        Assert.Equal(first.AddDays(2), result.NextDate);
        Assert.Equal(-10000, loan.OpeningBalance);
    }
}
