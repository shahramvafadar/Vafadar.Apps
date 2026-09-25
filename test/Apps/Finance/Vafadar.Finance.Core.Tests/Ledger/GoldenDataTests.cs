using Vafadar.Finance.Core.Accounts;
using Vafadar.Finance.Core.Budgets;
using Vafadar.Finance.Core.Ledger;

namespace Vafadar.Finance.Core.Tests.Ledger;

/// <summary>AT-62: the reference data set of specification §24.2 must produce exactly the reference results.</summary>
[Trait("AT", "AT-62")]
public sealed class GoldenDataTests
{
    private readonly LedgerBuilder _ledger = new();
    private readonly Account _checking;
    private readonly Account _savings;
    private readonly Account _card;

    public GoldenDataTests()
    {
        _checking = _ledger.Account("Checking", 1_000m);
        _savings = _ledger.Account("Savings", 200m, AccountType.Savings);
        _card = _ledger.Account("Card", 0m, AccountType.CreditCard);

        var food = Guid.CreateVersion7();
        _ledger.Add(EntryKind.Income, _checking, 3_000m);                // salary
        _ledger.Add(EntryKind.Expense, _checking, 800m);                 // rent
        _ledger.Add(EntryKind.Expense, _checking, 92.37m);               // electricity
        var groceries = _ledger.Add(EntryKind.Expense, _card, 100m, categoryId: food);
        _ledger.Transfer(_checking, _card, 100m);                        // card payment
        _ledger.Transfer(_checking, _savings, 500m);
        _ledger.Refund(groceries, _card, 20m);
    }

    [Fact]
    public void Account_balances_match_the_reference()
    {
        Assert.Equal(LedgerBuilder.Minor(2_507.63m), _ledger.Balance(_checking));
        Assert.Equal(LedgerBuilder.Minor(700m), _ledger.Balance(_savings));
        Assert.Equal(LedgerBuilder.Minor(20m), _ledger.Balance(_card));
    }

    [Fact]
    public void Total_of_recorded_balances_matches_the_reference()
    {
        var totals = LedgerCalculator.TotalBalances(_ledger.Accounts, _ledger.Entries, new DateOnly(2026, 10, 31));

        Assert.Equal(LedgerBuilder.Minor(3_227.63m), totals["EUR"]);
    }

    [Fact]
    public void Income_and_expense_match_the_reference()
    {
        var totals = _ledger.Totals();

        Assert.Equal(LedgerBuilder.Minor(3_000m), totals.Income);
        Assert.Equal(LedgerBuilder.Minor(992.37m), totals.GrossExpense);
        Assert.Equal(LedgerBuilder.Minor(20m), totals.Refunds);
        Assert.Equal(LedgerBuilder.Minor(972.37m), totals.NetExpense);
        Assert.Equal(LedgerBuilder.Minor(2_027.63m), totals.Result);
    }

    [Fact]
    public void Budget_of_1000_matches_the_reference()
    {
        var spent = BudgetCalculator.NetExpense(_ledger.Accounts, _ledger.Entries, LedgerBuilder.Day1, new DateOnly(2026, 10, 31), "EUR");
        var status = new BudgetStatus(LedgerBuilder.Minor(1_000m), spent);

        Assert.Equal(LedgerBuilder.Minor(27.63m), status.Remaining);
        Assert.Equal(97.237m, status.UsagePercent);
        Assert.Equal("97.24", decimal.Round(status.UsagePercent!.Value, 2).ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Unreviewed_automatic_expense_is_visible_separately()
    {
        _ledger.Add(EntryKind.Expense, _checking, 10m, review: ReviewState.Unreviewed);

        Assert.Equal(LedgerBuilder.Minor(2_497.63m), _ledger.Balance(_checking));
        Assert.Equal(LedgerBuilder.Minor(2_507.63m), _ledger.Balance(_checking, confirmedOnly: true));
        var unreviewed = Assert.Single(LedgerCalculator.Unreviewed(_ledger.Accounts, _ledger.Entries));
        Assert.Equal(1, unreviewed.Count);
        Assert.Equal(LedgerBuilder.Minor(-10m), unreviewed.NetEffect);
    }
}
