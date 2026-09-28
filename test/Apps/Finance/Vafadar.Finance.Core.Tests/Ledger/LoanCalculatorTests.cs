using Vafadar.Finance.Core.Accounts;
using Vafadar.Finance.Core.Ledger;

namespace Vafadar.Finance.Core.Tests.Ledger;

public sealed class LoanCalculatorTests
{
    private static readonly DateOnly First = new(2026, 1, 31);

    [Fact]
    public void An_annuity_repays_the_principal_in_the_given_months_with_a_smaller_last_installment()
    {
        // 10,000.00 at 6 % a year over 12 months: 860.67 a month (rounded up), 50.00 interest in the first month.
        var installment = LoanCalculator.Installment(1_000_000, 6m, 12);
        Assert.Equal(86_067, installment);

        var schedule = LoanCalculator.Schedule(1_000_000, 6m, installment, First);
        Assert.True(schedule.PaysOff);
        Assert.Equal(12, schedule.Installments.Count);
        Assert.Equal(5_000, schedule.Installments[0].Interest);
        Assert.Equal(1_000_000, schedule.Installments.Sum(i => i.Principal));
        Assert.Equal(schedule.TotalInterest, schedule.Installments.Sum(i => i.Interest));
        Assert.All(schedule.Installments, i => Assert.Equal(i.Interest + i.Principal, i.Payment));
        Assert.Equal(0, schedule.Installments[^1].Remaining);
        Assert.True(schedule.Installments[^1].Payment <= installment);
        Assert.InRange(schedule.TotalInterest, 32_700, 32_900);
    }

    [Fact]
    public void Installment_dates_keep_the_day_of_the_first_date_and_clamp_to_the_month_end()
    {
        var schedule = LoanCalculator.Schedule(300_000, 0m, 100_000, First);
        Assert.Equal([new DateOnly(2026, 1, 31), new DateOnly(2026, 2, 28), new DateOnly(2026, 3, 31)], schedule.Installments.Select(i => i.Date));
        Assert.Equal(new DateOnly(2026, 3, 31), schedule.LastDate);
        Assert.Equal(0, schedule.TotalInterest);
        Assert.Equal(34, LoanCalculator.Installment(100, 0m, 3));
    }

    [Fact]
    public void An_installment_that_does_not_cover_the_interest_never_pays_off()
    {
        // 1 % a month on 100,000.00 is 1,000.00 interest; 1,000.00 a month only pays interest.
        var schedule = LoanCalculator.Schedule(10_000_000, 12m, 100_000, First);
        Assert.False(schedule.PaysOff);
        Assert.Null(schedule.LastDate);
        Assert.Empty(schedule.Installments);

        // Barely more than the interest would take longer than the limit.
        Assert.False(LoanCalculator.Schedule(10_000_000, 12m, 100_001, First).PaysOff);
        Assert.True(LoanCalculator.Schedule(0, 12m, 100, First).PaysOff);
    }

    [Fact]
    public void A_payment_pays_interest_first_and_never_more_principal_than_is_outstanding()
    {
        Assert.Equal((500L, 9_500L), LoanCalculator.Split(100_000, 6m, 10_000));
        Assert.Equal((5L, 1_000L), LoanCalculator.Split(1_000, 6m, 10_000));
        Assert.Equal((500L, 0L), LoanCalculator.Split(100_000, 6m, 500));
        Assert.Equal((0L, 0L), LoanCalculator.Split(0, 6m, 500));
    }

    [Fact]
    public void A_loan_installment_is_a_principal_transfer_plus_an_interest_expense()
    {
        var ledger = new LedgerBuilder();
        var checking = ledger.Account("Checking", 5_000);
        var loan = ledger.Account("Car loan", -1_000, AccountType.Loan);
        var category = Guid.NewGuid();

        var entries = LoanCalculator.CreateInstallment(loan, checking.Id, 100_000, 6m, 10_000, LedgerBuilder.Day1, category, "Interest");
        Assert.Equal(2, entries.Count);
        var transfer = entries.Single(e => e.Kind == EntryKind.Transfer);
        Assert.Equal((checking.Id, loan.Id, 9_500L), (transfer.AccountId, transfer.ToAccountId!.Value, transfer.Amount));
        var interest = entries.Single(e => e.Kind == EntryKind.Expense);
        Assert.Equal((checking.Id, category, 500L), (interest.AccountId, interest.CategoryId!.Value, interest.Amount));

        // Only the interest counts as spending.
        ledger.Entries.AddRange(entries);
        var totals = LedgerCalculator.Totals(ledger.Accounts, ledger.Entries, new LedgerFilter(LedgerBuilder.Day1, LedgerBuilder.Day1)).Single();
        Assert.Equal(500, totals.NetExpense);
        Assert.Equal(0, totals.NetIncome);
    }

    [Fact]
    public void Money_lent_comes_back_as_a_transfer_and_its_interest_is_income()
    {
        var ledger = new LedgerBuilder();
        var checking = ledger.Account("Checking", 0);
        var lent = ledger.Account("Friend", 1_000, AccountType.Lent);

        var entries = LoanCalculator.CreateInstallment(lent, checking.Id, 100_000, 0m, 10_000, First, Guid.NewGuid(), "Interest");
        var transfer = Assert.Single(entries);
        Assert.Equal((lent.Id, checking.Id, 10_000L), (transfer.AccountId, transfer.ToAccountId!.Value, transfer.Amount));

        var withInterest = LoanCalculator.CreateInstallment(lent, checking.Id, 100_000, 6m, 10_000, First, Guid.NewGuid(), "Interest");
        Assert.Equal(EntryKind.Income, withInterest.Single(e => e.Kind != EntryKind.Transfer).Kind);
        Assert.Throws<ArgumentException>(() => LoanCalculator.CreateInstallment(checking, lent.Id, 1, 0m, 1, First, Guid.NewGuid(), "x"));
    }
}