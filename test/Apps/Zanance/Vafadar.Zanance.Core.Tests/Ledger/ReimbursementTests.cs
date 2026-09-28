using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.Core.Tests.Ledger;

public sealed class ReimbursementTests
{
    private readonly LedgerBuilder _ledger = new();

    [Fact]
    public void Collecting_a_reimbursement_settles_the_receivable_and_is_never_income()
    {
        var account = _ledger.Account("Checking", 500);
        var hotel = _ledger.Add(EntryKind.Expense, account, 300);
        hotel.ReimbursableAmount = LedgerBuilder.Minor(250);
        hotel.ReimbursedBy = "Employer";

        Assert.Equal(LedgerBuilder.Minor(250), EntryActions.OpenReimbursement(hotel, _ledger.Entries));

        var first = EntryActions.CreateReimbursement(hotel, LedgerBuilder.Minor(100), account.Id, hotel.Date);
        _ledger.Entries.Add(first);
        Assert.Equal(EntryKind.Refund, first.Kind);
        Assert.Equal("Employer", first.Payee);
        Assert.Equal(LedgerBuilder.Minor(150), Assert.Single(EntryActions.OpenReimbursements(_ledger.Entries)).Open);

        _ledger.Entries.Add(EntryActions.CreateReimbursement(hotel, LedgerBuilder.Minor(150), account.Id, hotel.Date));
        Assert.Empty(EntryActions.OpenReimbursements(_ledger.Entries));

        // Only the personal part remains as spending; nothing counts as income.
        var day = hotel.Date;
        Assert.Equal(LedgerBuilder.Minor(50), BudgetCalculator.NetExpense(_ledger.Accounts, _ledger.Entries, day, day, "EUR"));
        var totals = LedgerCalculator.Totals(_ledger.Accounts, _ledger.Entries, new LedgerFilter(day, day)).Single();
        Assert.Equal(0, totals.NetIncome);
        Assert.Equal(LedgerBuilder.Minor(450), LedgerCalculator.Balance(account, _ledger.Entries, day));
    }

    [Fact]
    public void Ordinary_expenses_have_nothing_open()
    {
        var account = _ledger.Account("Checking", 500);
        var lunch = _ledger.Add(EntryKind.Expense, account, 20);

        Assert.Equal(0, EntryActions.OpenReimbursement(lunch, _ledger.Entries));
        Assert.Empty(EntryActions.OpenReimbursements(_ledger.Entries));
    }
}
