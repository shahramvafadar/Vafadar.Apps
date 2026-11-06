using System.Text.Json;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Reports;

namespace Vafadar.Zanance.Core.Tests.Reports;

/// <summary>Prospective monthly costs must not retain an exhausted or retired recurrence slice.</summary>
public sealed class CurrentMonthlyShareTests
{
    private static readonly DateOnly Today = new(2026, 11, 6);

    /// <summary>Capacity and essential coverage agree on only the remaining owned recurrence costs.</summary>
    [Theory]
    [InlineData(false, 0, 10000)]
    [InlineData(true, 0, 10000)]
    [InlineData(false, 1, 15000)]
    [InlineData(true, 1, 15000)]
    [InlineData(false, 2, 0)]
    [InlineData(true, 2, 0)]
    [InlineData(false, 3, 0)]
    [InlineData(true, 3, 0)]
    [InlineData(false, 4, 0)]
    [InlineData(true, 4, 0)]
    public void Prospective_cost_excludes_retired_or_exhausted_slices(bool coverage, int shape, long expected)
    {
        var (account, category, first) = Source(); var schedules = new List<Schedule> { first };
        if (shape is 0 or 1)
        {
            var second = PlanActions.SplitFrom(first, Today.AddMonths(-1)); second.Amount = 120000; schedules.Add(second);
            if (shape == 1) { var third = PlanActions.SplitFrom(second, Today); third.Amount = 180000; schedules.Add(third); }
        }
        if (shape == 2) { first.Rule.End = EndKind.OnDate; first.Rule.EndDate = Today.AddDays(-1); }
        if (shape == 3) { first.Rule.End = EndKind.AfterCount; first.Rule.Count = 2; }
        if (shape == 4) PlanActions.Pause(first, Today);
        var before = JsonSerializer.Serialize(schedules);
        Assert.Equal(expected, Cost(coverage, account, category, schedules, []));
        Assert.Equal(before, JsonSerializer.Serialize(schedules));
    }

    /// <summary>The last owned date is inclusive, including counted and explicitly paused boundaries.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void A_real_remaining_boundary_occurrence_retains_its_share(int shape)
    {
        var (account, category, plan) = Source();
        if (shape == 0) { plan.Rule.End = EndKind.OnDate; plan.Rule.EndDate = Today; }
        if (shape == 1) { plan.Rule.End = EndKind.AfterCount; plan.Rule.Count = 3; }
        if (shape == 2) PlanActions.Pause(plan, Today.AddDays(1));
        Assert.Equal(5000, Cost(false, account, category, [plan], []));
        Assert.Equal(5000, Cost(true, account, category, [plan], []));
    }

    /// <summary>A future slice cannot re-anchor or borrow an occurrence after its own upper ownership bound.</summary>
    [Fact]
    public void A_slice_with_no_owned_future_original_date_contributes_nothing()
    {
        var (account, category, plan) = Source(); plan.ActiveFrom = Today.AddDays(1); plan.ActiveUntil = Today.AddMonths(2);
        Assert.Equal(0, Cost(false, account, category, [plan], []));
        Assert.Equal(0, Cost(true, account, category, [plan], []));
    }

    /// <summary>Removing an obsolete annualized share must not count its existing partial payments as ordinary spending.</summary>
    [Fact]
    public void Historical_settlement_ids_remain_excluded_after_the_current_share_changes()
    {
        var (account, category, first) = Source(); first.Rule.Start = new DateOnly(2024, 5, 1);
        var next = PlanActions.SplitFrom(first, Today); next.Amount = 120000;
        var entries = new[] { 8, 9, 10 }.Select(month => new LedgerEntry
        {
            AccountId = account.Id, CategoryId = category.Id, Kind = EntryKind.Expense, Amount = 10000,
            Date = new DateOnly(2026, month, 1), ScheduleId = first.Id, OccurrenceDate = new DateOnly(2026, 5, 1),
            IsPartialPayment = true, Source = EntrySource.Schedule,
        }).ToArray();
        var before = JsonSerializer.Serialize(entries);
        Assert.Equal(10000, Cost(false, account, category, [first, next], entries));
        Assert.Equal(10000, Cost(true, account, category, [first, next], entries));
        Assert.Equal(before, JsonSerializer.Serialize(entries));
    }

    /// <summary>A separate future plan still contributes its original annual equivalent without creating a payment.</summary>
    [Fact]
    public void A_future_plan_keeps_the_original_comparison_formula()
    {
        var (account, category, plan) = Source(); plan.ActiveFrom = Today.AddYears(2);
        Assert.Equal(5000, Cost(false, account, category, [plan], []));
        Assert.Equal(5000, Cost(true, account, category, [plan], []));
    }

    private static long Cost(bool coverage, Account account, Category category, IReadOnlyCollection<Schedule> schedules, IReadOnlyCollection<LedgerEntry> entries) =>
        coverage ? KpiCatalog.Coverage([account], entries, [category], schedules, "EUR", Today, PeriodCalendar.Gregorian).MonthlyEssential
            : CapacityCalculator.Compute([account], entries, schedules, [], [], [], "EUR", Today, PeriodCalendar.Gregorian).NonMonthly;

    private static (Account Account, Category Category, Schedule Schedule) Source()
    {
        var account = new Account { Name = "Current cost source", CurrencyCode = "EUR", Type = AccountType.Checking, OpeningBalance = 1000000,
            OpeningDate = Today.AddYears(-3), UsableForPayments = true, IncludeInTotals = true };
        var category = new Category { Name = "Current essential cost", Kind = CategoryKind.Expense, IsEssential = true };
        var plan = new Schedule { Name = "Current annual cost", AccountId = account.Id, CategoryId = category.Id, Kind = EntryKind.Expense,
            Amount = 60000, Rule = new RecurrenceRule { Start = Today.AddYears(-2), Frequency = Frequency.Yearly } };
        return (account, category, plan);
    }
}
