using Vafadar.Finance.Core.Accounts;
using Vafadar.Finance.Core.Ledger;

namespace Vafadar.Finance.Core.Tests.Ledger;

public sealed class LoanTests
{
    private readonly LedgerBuilder _ledger = new();

    [Fact]
    public void Receiving_and_repaying_a_loan_is_neither_income_nor_spending_but_interest_is()
    {
        var checking = _ledger.Account("Checking", 100);
        var loan = _ledger.Account("Car loan", 0, AccountType.Loan);
        loan.IncludeInTotals = false;

        var principal = _ledger.Add(EntryKind.Transfer, loan, 5_000);
        principal.ToAccountId = checking.Id;
        var repayment = _ledger.Add(EntryKind.Transfer, checking, 400);
        repayment.ToAccountId = loan.Id;
        _ledger.Add(EntryKind.Expense, checking, 25);

        var day = principal.Date;
        Assert.Equal(LedgerBuilder.Minor(-4_600), LedgerCalculator.Balance(loan, _ledger.Entries, day));
        Assert.Equal(LedgerBuilder.Minor(4_675), LedgerCalculator.Balance(checking, _ledger.Entries, day));

        // Only the interest counts as spending; the loan itself is never income.
        var totals = LedgerCalculator.Totals(_ledger.Accounts, _ledger.Entries, new LedgerFilter(day, day)).Single();
        Assert.Equal(0, totals.NetIncome);
        Assert.Equal(LedgerBuilder.Minor(25), totals.NetExpense);
        Assert.True(AccountType.Loan.IsDebt());
        Assert.True(AccountType.Lent.IsDebt());
        Assert.False(AccountType.Savings.IsDebt());
        Assert.True(AccountType.Asset.IsOutsideCash());
        Assert.False(AccountType.Asset.IsDebt());
        Assert.False(AccountType.Checking.IsOutsideCash());
    }
}
