using Vafadar.Finance.Core.Accounts;
using Vafadar.Finance.Core.Ledger;

namespace Vafadar.Finance.Core.Tests.Ledger;

public sealed class ReconciliationTests
{
    private readonly LedgerBuilder _ledger = new();

    [Fact]
    [Trait("AT", "AT-11")]
    public void An_adjustment_closes_the_difference_without_touching_income_or_expense()
    {
        var checking = _ledger.Account("Checking", 1_000);
        _ledger.Add(EntryKind.Expense, checking, 100);
        _ledger.Add(EntryKind.Expense, checking, 40, review: ReviewState.Unreviewed);

        var result = Reconciliation.Analyze(checking, _ledger.Entries, LedgerBuilder.Minor(855), LedgerBuilder.Day1);
        var adjustment = Reconciliation.CreateAdjustment(checking, result, LedgerBuilder.Day1, "Bank fee not recorded")!;
        _ledger.Entries.Add(adjustment);

        Assert.Equal(LedgerBuilder.Minor(-5), result.Difference);
        Assert.Equal(1, result.UnreviewedCount);
        Assert.Equal(AdjustmentDirection.Decrease, adjustment.Direction);
        Assert.Equal("Bank fee not recorded", adjustment.Note);
        Assert.Equal(LedgerBuilder.Minor(855), _ledger.Balance(checking));
        Assert.Equal(LedgerBuilder.Minor(140), _ledger.Totals().GrossExpense);
    }

    [Fact]
    public void Possible_duplicates_are_suggested_and_a_match_needs_no_adjustment()
    {
        var checking = _ledger.Account("Checking", 1_000);
        _ledger.Add(EntryKind.Expense, checking, 25);
        _ledger.Add(EntryKind.Expense, checking, 25);

        var result = Reconciliation.Analyze(checking, _ledger.Entries, LedgerBuilder.Minor(950), LedgerBuilder.Day1);

        Assert.Equal(2, result.PossibleDuplicates);
        Assert.Equal(0, result.Difference);
        Assert.Null(Reconciliation.CreateAdjustment(checking, result, LedgerBuilder.Day1, "x"));
    }
}
