using Vafadar.Finance.Core.Budgets;
using Vafadar.Finance.Core.Ledger;
using Vafadar.Finance.Core.Plans;
using Vafadar.Finance.Core.Reports;

namespace Vafadar.Finance.Core.Tests.Reports;

public sealed class ReportCalculatorTests
{
    private readonly LedgerBuilder _ledger = new();

    [Fact]
    public void Account_movement_explains_the_whole_balance_change()
    {
        var checking = _ledger.Account("Checking", 500, openingDate: new DateOnly(2026, 9, 15));
        var savings = _ledger.Account("Savings", 0, openingDate: new DateOnly(2026, 9, 1));
        _ledger.Add(EntryKind.Income, checking, 2_000);
        var purchase = _ledger.Add(EntryKind.Expense, checking, 300);
        _ledger.Refund(purchase, checking, 50);
        _ledger.Transfer(checking, savings, 400);
        var adjustment = _ledger.Add(EntryKind.Adjustment, checking, 10);
        adjustment.Direction = AdjustmentDirection.Decrease;

        var movement = ReportCalculator.AccountMovements([checking], _ledger.Entries, new DateOnly(2026, 9, 1), new DateOnly(2026, 10, 31)).Single();

        Assert.Equal(0, movement.Opening);
        Assert.Equal(LedgerBuilder.Minor(500), movement.OpeningBalanceAdded);
        var explained = movement.Opening + movement.OpeningBalanceAdded + movement.Income + movement.Refunds - movement.Expense
            - movement.IncomeReversals + movement.TransfersIn - movement.TransfersOut + movement.Adjustments;
        Assert.Equal(movement.Closing, explained);
        Assert.Equal(LedgerBuilder.Minor(1_840), movement.Closing);
    }

    [Fact]
    [Trait("AT", "AT-13")]
    public void A_refund_of_last_month_gives_negative_net_expense_but_no_negative_gross()
    {
        var checking = _ledger.Account("Checking", 100);
        var food = Guid.CreateVersion7();
        var purchase = _ledger.Add(EntryKind.Expense, checking, 80, date: new DateOnly(2026, 10, 20), categoryId: food);
        _ledger.Refund(purchase, checking, 30, date: new DateOnly(2026, 11, 3));

        var november = LedgerCalculator.ExpenseByCategory(_ledger.Accounts, _ledger.Entries, new LedgerFilter(new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30)), id => id).Single();

        Assert.Equal(0, november.GrossExpense);
        Assert.Equal(LedgerBuilder.Minor(30), november.Refunds);
        Assert.Equal(LedgerBuilder.Minor(-30), november.Net);
    }

    [Fact]
    public void Trend_marks_the_current_month_as_partial()
    {
        var checking = _ledger.Account("Checking", 0, openingDate: new DateOnly(2026, 1, 1));
        _ledger.Add(EntryKind.Income, checking, 1_000, date: new DateOnly(2026, 9, 5));
        _ledger.Add(EntryKind.Expense, checking, 200, date: new DateOnly(2026, 10, 2));

        var trend = ReportCalculator.MonthlyTrend(_ledger.Accounts, _ledger.Entries, new DateOnly(2026, 10, 15), 3, PeriodCalendar.Gregorian, "EUR");

        Assert.Equal([8, 9, 10], trend.Select(t => t.Month));
        Assert.Equal(LedgerBuilder.Minor(1_000), trend[1].NetIncome);
        Assert.Equal(LedgerBuilder.Minor(200), trend[2].NetExpense);
        Assert.Equal([false, false, true], trend.Select(t => t.IsPartial));
    }

    [Fact]
    public void Plan_versus_actual_compares_planned_occurrences_with_their_settlements()
    {
        var checking = _ledger.Account("Checking", 0, openingDate: new DateOnly(2026, 1, 1));
        var plan = new Schedule { Name = "Phone", AccountId = checking.Id, AmountMode = AmountMode.Estimated, Amount = 3_000, Rule = new RecurrenceRule { Frequency = Frequency.Monthly, Start = new DateOnly(2026, 9, 10) } };
        var settlement = _ledger.Add(EntryKind.Expense, checking, 32.5m, date: new DateOnly(2026, 9, 11));
        settlement.ScheduleId = plan.Id;
        settlement.OccurrenceDate = new DateOnly(2026, 9, 10);
        var state = new OccurrenceState { ScheduleId = plan.Id, OriginalDate = new DateOnly(2026, 9, 10), Status = OccurrenceStatus.Settled, EntryId = settlement.Id };

        var result = ReportCalculator.PlanVsActual([plan], [state], _ledger.Entries, new DateOnly(2026, 9, 1), new DateOnly(2026, 10, 31), new DateOnly(2026, 10, 1)).Single();

        Assert.Equal(6_000, result.Planned);
        Assert.Equal(2, result.PlannedCount);
        Assert.Equal(3_250, result.Actual);
        Assert.Equal(1, result.SettledCount);
        Assert.Equal(1, result.OpenCount);

        // Only the settled occurrence counts toward the variance; the open one is not a saving.
        Assert.Equal(250, result.Variance);
    }
}
