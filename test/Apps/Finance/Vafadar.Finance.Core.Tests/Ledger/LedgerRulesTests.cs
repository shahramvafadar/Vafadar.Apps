using Vafadar.Finance.Core.Accounts;
using Vafadar.Finance.Core.Budgets;
using Vafadar.Finance.Core.Categories;
using Vafadar.Finance.Core.Ledger;

namespace Vafadar.Finance.Core.Tests.Ledger;

public sealed class LedgerRulesTests
{
    private readonly LedgerBuilder _ledger = new();

    [Fact]
    [Trait("AT", "AT-02")]
    public void Income_and_expense_change_balance_and_report_exactly_once()
    {
        var account = _ledger.Account("A", 100m);
        _ledger.Add(EntryKind.Income, account, 50m);
        _ledger.Add(EntryKind.Expense, account, 30m);

        Assert.Equal(LedgerBuilder.Minor(120m), _ledger.Balance(account));
        Assert.Equal(LedgerBuilder.Minor(50m), _ledger.Totals().Income);
        Assert.Equal(LedgerBuilder.Minor(30m), _ledger.Totals().NetExpense);
    }

    [Fact]
    [Trait("AT", "AT-05")]
    public void Transfer_keeps_the_total_and_is_neither_income_nor_expense()
    {
        var a = _ledger.Account("A", 100m);
        var b = _ledger.Account("B", 0m);
        _ledger.Transfer(a, b, 40m);

        Assert.Equal(LedgerBuilder.Minor(60m), _ledger.Balance(a));
        Assert.Equal(LedgerBuilder.Minor(40m), _ledger.Balance(b));
        Assert.Equal(0, _ledger.Totals().Income);
        Assert.Equal(0, _ledger.Totals().GrossExpense);
    }

    [Fact]
    [Trait("AT", "AT-06")]
    public void Report_on_the_source_account_only_shows_no_expense_for_a_transfer()
    {
        var a = _ledger.Account("A", 100m);
        var b = _ledger.Account("B", 0m);
        _ledger.Transfer(a, b, 40m);

        Assert.Equal(LedgerBuilder.Minor(60m), _ledger.Balance(a));
        Assert.Equal(0, _ledger.Totals([a.Id]).GrossExpense);
    }

    [Fact]
    [Trait("AT", "AT-07")]
    public void Card_purchase_is_one_expense_and_card_payment_is_a_transfer()
    {
        var checking = _ledger.Account("Checking", 500m);
        var card = _ledger.Account("Card", 0m, AccountType.CreditCard);
        _ledger.Add(EntryKind.Expense, card, 80m);
        _ledger.Transfer(checking, card, 80m);

        Assert.Equal(LedgerBuilder.Minor(80m), _ledger.Totals().GrossExpense);
        Assert.Equal(0, _ledger.Balance(card));
        Assert.Equal(LedgerBuilder.Minor(420m), _ledger.Balance(checking));
    }

    [Fact]
    [Trait("AT", "AT-08")]
    public void Only_the_transfer_fee_is_an_expense()
    {
        var a = _ledger.Account("A", 100m);
        var b = _ledger.Account("B", 0m);
        var transfer = _ledger.Transfer(a, b, 50m);
        var fee = _ledger.Add(EntryKind.Expense, a, 1m);
        fee.GroupId = transfer.GroupId = Guid.CreateVersion7();

        Assert.Equal(LedgerBuilder.Minor(1m), _ledger.Totals().GrossExpense);
        Assert.Equal(LedgerBuilder.Minor(49m), _ledger.Balance(a));
    }

    [Fact]
    [Trait("AT", "AT-09")]
    public void Entries_before_the_opening_date_are_not_counted_twice()
    {
        var account = _ledger.Account("A", 1_000m, openingDate: new DateOnly(2026, 10, 1));
        var old = _ledger.Add(EntryKind.Expense, account, 300m, new DateOnly(2026, 9, 15));

        Assert.True(LedgerCalculator.IsBeforeOpening(old, account));
        Assert.Equal(LedgerBuilder.Minor(1_000m), _ledger.Balance(account));
    }

    [Fact]
    [Trait("AT", "AT-11")]
    public void Adjustment_changes_the_balance_but_not_income_or_expense()
    {
        var account = _ledger.Account("A", 100m);
        var adjustment = _ledger.Add(EntryKind.Adjustment, account, 5m);
        adjustment.Direction = AdjustmentDirection.Decrease;

        Assert.Equal(LedgerBuilder.Minor(95m), _ledger.Balance(account));
        Assert.Equal(new PeriodTotals("EUR", 0, 0, 0, 0), _ledger.Totals());
    }

    [Fact]
    [Trait("AT", "AT-12")]
    public void Partial_refund_reduces_net_expense_and_increases_the_balance()
    {
        var account = _ledger.Account("A", 100m);
        var purchase = _ledger.Add(EntryKind.Expense, account, 60m);
        _ledger.Refund(purchase, account, 15m);

        Assert.Equal(LedgerBuilder.Minor(45m), _ledger.Totals().NetExpense);
        Assert.Equal(LedgerBuilder.Minor(55m), _ledger.Balance(account));
    }

    [Fact]
    [Trait("AT", "AT-13")]
    public void Refund_of_last_month_gives_a_valid_negative_net_expense()
    {
        var account = _ledger.Account("A", 100m, openingDate: new DateOnly(2026, 9, 1));
        var purchase = _ledger.Add(EntryKind.Expense, account, 40m, new DateOnly(2026, 9, 20));
        _ledger.Refund(purchase, account, 40m, LedgerBuilder.Day1);

        var october = _ledger.Totals();
        Assert.Equal(0, october.GrossExpense);
        Assert.Equal(LedgerBuilder.Minor(-40m), october.NetExpense);
    }

    [Fact]
    [Trait("AT", "AT-14")]
    public void Refund_to_another_account_changes_the_receiving_account_and_keeps_the_category()
    {
        var card = _ledger.Account("Card", 0m, AccountType.CreditCard);
        var checking = _ledger.Account("Checking", 0m);
        var category = Guid.CreateVersion7();
        var purchase = _ledger.Add(EntryKind.Expense, card, 30m, categoryId: category);
        var refund = _ledger.Refund(purchase, checking, 30m);

        Assert.Equal(LedgerBuilder.Minor(30m), _ledger.Balance(checking));
        Assert.Equal(LedgerBuilder.Minor(-30m), _ledger.Balance(card));
        Assert.Equal(category, refund.CategoryId);
    }

    [Fact]
    [Trait("AT", "AT-15")]
    public void Refund_larger_than_the_purchase_is_rejected()
    {
        var account = _ledger.Account("A", 0m);
        var purchase = _ledger.Add(EntryKind.Expense, account, 30m);
        var refund = new LedgerEntry { Kind = EntryKind.Refund, AccountId = account.Id, Amount = LedgerBuilder.Minor(20m), RefundOfId = purchase.Id };

        var errors = LedgerValidator.Validate(refund, Accounts(), new Dictionary<Guid, Category>(), purchase, otherRefundsOfOriginal: LedgerBuilder.Minor(15m));

        Assert.Contains(LedgerError.RefundExceedsPurchase, errors);
    }

    [Fact]
    public void Zero_amount_and_transfer_to_the_same_account_are_rejected()
    {
        var account = _ledger.Account("A", 0m);
        var zero = new LedgerEntry { Kind = EntryKind.Expense, AccountId = account.Id, Amount = 0 };
        var self = new LedgerEntry { Kind = EntryKind.Transfer, AccountId = account.Id, ToAccountId = account.Id, Amount = 1 };

        Assert.Contains(LedgerError.AmountMustBePositive, LedgerValidator.Validate(zero, Accounts(), new Dictionary<Guid, Category>()));
        Assert.Contains(LedgerError.SameAccountTransfer, LedgerValidator.Validate(self, Accounts(), new Dictionary<Guid, Category>()));
    }

    [Fact]
    public void Transfer_between_currencies_needs_both_amounts()
    {
        var eur = _ledger.Account("EUR", 0m);
        var usd = _ledger.Account("USD", 0m, currency: "USD");
        var transfer = new LedgerEntry { Kind = EntryKind.Transfer, AccountId = eur.Id, ToAccountId = usd.Id, Amount = 100 };

        Assert.Contains(LedgerError.DestinationAmountRequired, LedgerValidator.Validate(transfer, Accounts(), new Dictionary<Guid, Category>()));

        transfer.ToAmount = 108;
        Assert.Empty(LedgerValidator.Validate(transfer, Accounts(), new Dictionary<Guid, Category>()));
    }

    [Fact]
    public void Archived_account_accepts_no_new_entries()
    {
        var account = _ledger.Account("A", 0m);
        account.IsArchived = true;
        var entry = new LedgerEntry { Kind = EntryKind.Expense, AccountId = account.Id, Amount = 1 };

        Assert.Contains(LedgerError.AccountArchived, LedgerValidator.Validate(entry, Accounts(), new Dictionary<Guid, Category>()));
        Assert.DoesNotContain(LedgerError.AccountArchived, LedgerValidator.Validate(entry, Accounts(), new Dictionary<Guid, Category>(), isNew: false));
    }

    [Fact]
    [Trait("AT", "AT-45")]
    public void Foreign_purchase_keeps_the_original_amount_and_books_the_charged_amount()
    {
        var account = _ledger.Account("EUR", 200m);
        var purchase = _ledger.Add(EntryKind.Expense, account, 92m);
        purchase.OriginalAmount = LedgerBuilder.Minor(100m, "USD");
        purchase.OriginalCurrencyCode = "USD";
        _ledger.Add(EntryKind.Expense, account, 1m); // fee

        Assert.Empty(LedgerValidator.Validate(purchase, Accounts(), new Dictionary<Guid, Category>()));
        Assert.Equal(LedgerBuilder.Minor(107m), _ledger.Balance(account));
        Assert.Equal(10_000, purchase.OriginalAmount);
    }

    [Fact]
    public void Totals_are_separate_per_currency()
    {
        var eur = _ledger.Account("EUR", 0m);
        var jpy = _ledger.Account("JPY", 0m, currency: "JPY");
        _ledger.Add(EntryKind.Expense, eur, 10m);
        _ledger.Add(EntryKind.Expense, jpy, 1_500m);

        var totals = LedgerCalculator.Totals(_ledger.Accounts, _ledger.Entries, new LedgerFilter(LedgerBuilder.Day1, LedgerBuilder.Day1));

        Assert.Equal([("EUR", 1_000L), ("JPY", 1_500L)], totals.Select(t => (t.CurrencyCode, t.GrossExpense)));
    }

    [Theory]
    [Trait("AT", "AT-39")]
    [InlineData(0, 10, null, true)]
    [InlineData(100, 100, 100.0, false)]
    [InlineData(100, 130, 130.0, true)]
    public void Budget_status_handles_zero_exact_and_exceeded_limits(long limit, long spent, double? percent, bool over)
    {
        var status = new BudgetStatus(limit, spent);

        Assert.Equal(percent is null ? null : (decimal)percent.Value, status.UsagePercent);
        Assert.Equal(over, status.IsOver);
        Assert.Equal(limit - spent, status.Remaining);
    }

    [Fact]
    [Trait("AT", "AT-40")]
    public void Category_limits_analyse_the_same_expenses_and_parents_include_children()
    {
        var account = _ledger.Account("A", 0m);
        var food = new Category { Kind = CategoryKind.Expense, Name = "Food" };
        var restaurant = new Category { Kind = CategoryKind.Expense, Name = "Restaurant", ParentId = food.Id };
        _ledger.Add(EntryKind.Expense, account, 30m, categoryId: food.Id);
        _ledger.Add(EntryKind.Expense, account, 20m, categoryId: restaurant.Id);

        var total = BudgetCalculator.NetExpense(_ledger.Accounts, _ledger.Entries, LedgerBuilder.Day1, LedgerBuilder.Day1, "EUR");
        var foodTotal = BudgetCalculator.NetExpense(_ledger.Accounts, _ledger.Entries, LedgerBuilder.Day1, LedgerBuilder.Day1, "EUR", categoryIds: [food.Id], categories: [food, restaurant]);

        Assert.Equal(LedgerBuilder.Minor(50m), total);
        Assert.Equal(LedgerBuilder.Minor(50m), foodTotal);
    }

    [Fact]
    public void Persian_month_range_follows_the_persian_calendar()
    {
        var (first, last) = PeriodMath.MonthRange(1405, 7, PeriodCalendar.Persian);

        Assert.Equal(new DateOnly(2026, 9, 23), first);
        Assert.Equal(new DateOnly(2026, 10, 22), last);
    }

    private Dictionary<Guid, Account> Accounts() => _ledger.Accounts.ToDictionary(a => a.Id);
}
