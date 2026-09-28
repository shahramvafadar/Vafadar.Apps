using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.Core.Tests.Plans;

public sealed class AdvanceSettlementTests
{
    private static readonly DateOnly From = new(2026, 1, 1);
    private static readonly DateOnly To = new(2026, 12, 31);
    private readonly LedgerBuilder _ledger = new();

    [Fact]
    public void An_extra_payment_is_only_the_difference_so_spending_equals_the_bill()
    {
        var (account, plan) = Advances(monthly: 100);

        var result = AdvanceSettlement.Compute(plan, _ledger.Entries, From, To, LedgerBuilder.Minor(1_350));
        Assert.Equal(LedgerBuilder.Minor(1_200), result.Paid);
        Assert.Equal(LedgerBuilder.Minor(150), result.Difference);

        var entries = AdvanceSettlement.CreateEntries(plan, result, _ledger.Entries, new DateOnly(2027, 2, 1), "Electricity: final bill");
        var extra = Assert.Single(entries);
        Assert.Equal(EntryKind.Expense, extra.Kind);
        _ledger.Entries.AddRange(entries);

        Assert.Equal(LedgerBuilder.Minor(1_350), BudgetCalculator.NetExpense(_ledger.Accounts, _ledger.Entries, From, new DateOnly(2027, 12, 31), "EUR"));
        Assert.Null(extra.ScheduleId);
        Assert.NotNull(account);
    }

    [Fact]
    [Trait("AT", "AT-66")]
    public void Money_back_becomes_refunds_of_the_advances_and_never_income()
    {
        var (_, plan) = Advances(monthly: 100);

        var result = AdvanceSettlement.Compute(plan, _ledger.Entries, From, To, LedgerBuilder.Minor(1_050));
        var refunds = AdvanceSettlement.CreateEntries(plan, result, _ledger.Entries, new DateOnly(2027, 2, 1), "Electricity: money back");

        // 150 back: 100 from December, 50 from November – each within its advance.
        Assert.Equal([LedgerBuilder.Minor(100), LedgerBuilder.Minor(50)], refunds.Select(r => r.Amount));
        Assert.All(refunds, r => Assert.Equal(EntryKind.Refund, r.Kind));
        _ledger.Entries.AddRange(refunds);

        Assert.Equal(LedgerBuilder.Minor(1_050), BudgetCalculator.NetExpense(_ledger.Accounts, _ledger.Entries, From, new DateOnly(2027, 12, 31), "EUR"));
        Assert.Equal(0, LedgerCalculator.Totals(_ledger.Accounts, _ledger.Entries, new LedgerFilter(From, new DateOnly(2027, 12, 31))).Single().NetIncome);

        // A second settlement of the same period sees the refunds already received.
        Assert.Equal(LedgerBuilder.Minor(1_050), AdvanceSettlement.Compute(plan, _ledger.Entries, From, To, LedgerBuilder.Minor(1_050)).Paid);
    }

    [Fact]
    public void A_matching_bill_creates_nothing_and_too_much_money_back_is_refused()
    {
        var (_, plan) = Advances(monthly: 100);

        var exact = AdvanceSettlement.Compute(plan, _ledger.Entries, From, To, LedgerBuilder.Minor(1_200));
        Assert.Empty(AdvanceSettlement.CreateEntries(plan, exact, _ledger.Entries, To, "x"));

        var impossible = AdvanceSettlement.Compute(plan, _ledger.Entries, From, To, -LedgerBuilder.Minor(10));
        Assert.Throws<InvalidOperationException>(() => AdvanceSettlement.CreateEntries(plan, impossible, _ledger.Entries, To, "x"));
    }

    private (Accounts.Account Account, Schedule Plan) Advances(decimal monthly)
    {
        var account = _ledger.Account("Checking", 5_000, openingDate: new DateOnly(2025, 12, 1));
        var plan = new Schedule { Name = "Electricity", Kind = EntryKind.Expense, AccountId = account.Id, Amount = LedgerBuilder.Minor(monthly), Rule = new RecurrenceRule { Frequency = Frequency.Monthly, Start = From } };
        for (var month = 1; month <= 12; month++)
        {
            var advance = _ledger.Add(EntryKind.Expense, account, monthly, new DateOnly(2026, month, 5));
            advance.ScheduleId = plan.Id;
            advance.OccurrenceDate = new DateOnly(2026, month, 1);
        }

        return (account, plan);
    }
}
