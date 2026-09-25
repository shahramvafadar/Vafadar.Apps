using Vafadar.Finance.Core.Ledger;

namespace Vafadar.Finance.Core.Tests.Ledger;

public sealed class EntryListTests
{
    private readonly LedgerBuilder _ledger = new();

    private Dictionary<Guid, Accounts.Account> AccountsById => _ledger.Accounts.ToDictionary(a => a.Id);

    [Fact]
    public void Kind_filter_groups_refunds_with_expenses_and_adjustments_with_transfers()
    {
        var checking = _ledger.Account("Checking", 100);
        var savings = _ledger.Account("Savings", 0);
        var purchase = _ledger.Add(EntryKind.Expense, checking, 30);
        var refund = _ledger.Refund(purchase, checking, 10);
        var salary = _ledger.Add(EntryKind.Income, checking, 500);
        var transfer = _ledger.Transfer(checking, savings, 50);

        Assert.Equal([purchase, refund], Filter(new EntryFilter(Kind: KindFilter.Expenses)));
        Assert.Equal([salary], Filter(new EntryFilter(Kind: KindFilter.Income)));
        Assert.Equal([transfer], Filter(new EntryFilter(Kind: KindFilter.Transfers)));
    }

    [Fact]
    public void Account_filter_includes_incoming_transfers()
    {
        var checking = _ledger.Account("Checking", 100);
        var savings = _ledger.Account("Savings", 0);
        var transfer = _ledger.Transfer(checking, savings, 50);
        _ledger.Add(EntryKind.Expense, checking, 5);

        Assert.Equal([transfer], Filter(new EntryFilter(AccountId: savings.Id)));
    }

    [Theory]
    [InlineData("rent")]
    [InlineData("RENT")]
    [InlineData("landlord")]
    [InlineData("12.5")]
    [InlineData("12.50")]
    [InlineData("۱۲.۵۰")]
    public void Text_search_matches_title_payee_and_amount(string text)
    {
        var checking = _ledger.Account("Checking", 100);
        var rent = _ledger.Add(EntryKind.Expense, checking, 12.50m);
        rent.Title = "Rent October";
        rent.Payee = "Landlord";
        _ledger.Add(EntryKind.Expense, checking, 3);

        Assert.Equal([rent], Filter(new EntryFilter(Text: text)));
    }

    [Fact]
    public void Text_search_matches_the_category_name_and_normalizes_arabic_letters()
    {
        var checking = _ledger.Account("Checking", 100);
        var category = Guid.CreateVersion7();
        var entry = _ledger.Add(EntryKind.Expense, checking, 1, categoryId: category);

        var result = EntrySearch.Apply(_ledger.Entries, new EntryFilter(Text: "كرايه"), id => id == category ? "کرایه" : null, AccountsById);

        Assert.Equal([entry], result);
    }

    [Fact]
    public void Days_are_newest_first_with_net_result_excluding_transfers()
    {
        var checking = _ledger.Account("Checking", 100);
        var savings = _ledger.Account("Savings", 0);
        var day2 = LedgerBuilder.Day1.AddDays(1);
        _ledger.Add(EntryKind.Expense, checking, 30);
        _ledger.Add(EntryKind.Income, checking, 100);
        _ledger.Transfer(checking, savings, 40, date: day2);
        _ledger.Add(EntryKind.Expense, checking, 5, date: day2);

        var days = EntrySearch.ByDay(_ledger.Entries, AccountsById);

        Assert.Equal([day2, LedgerBuilder.Day1], days.Select(d => d.Date));
        Assert.Equal(LedgerBuilder.Minor(-5), days[0].Net["EUR"]);
        Assert.Equal(LedgerBuilder.Minor(70), days[1].Net["EUR"]);
    }

    [Fact]
    public void Duplicate_is_a_new_confirmed_manual_entry_without_links()
    {
        var checking = _ledger.Account("Checking", 100);
        var original = _ledger.Add(EntryKind.Expense, checking, 20, review: ReviewState.Unreviewed);
        original.Title = "Groceries";
        original.ScheduleId = Guid.CreateVersion7();
        original.OccurrenceDate = LedgerBuilder.Day1;
        original.Source = EntrySource.Schedule;

        var copy = EntryActions.Duplicate(original, LedgerBuilder.Day1.AddDays(3));

        Assert.NotEqual(original.Id, copy.Id);
        Assert.Equal("Groceries", copy.Title);
        Assert.Equal(original.Amount, copy.Amount);
        Assert.Equal(LedgerBuilder.Day1.AddDays(3), copy.Date);
        Assert.Equal(ReviewState.Confirmed, copy.Review);
        Assert.Equal(EntrySource.Manual, copy.Source);
        Assert.Null(copy.ScheduleId);
        Assert.Null(copy.OccurrenceDate);
    }

    [Fact]
    [Trait("AT", "AT-12")]
    public void Refundable_amount_shrinks_with_each_linked_refund()
    {
        var checking = _ledger.Account("Checking", 100);
        var purchase = _ledger.Add(EntryKind.Expense, checking, 80);
        var first = _ledger.Refund(purchase, checking, 30);

        Assert.Equal(LedgerBuilder.Minor(50), EntryActions.Refundable(purchase, _ledger.Entries));
        Assert.Equal(LedgerBuilder.Minor(80), EntryActions.Refundable(purchase, _ledger.Entries, exceptRefundId: first.Id));

        var refund = EntryActions.CreateRefund(purchase, LedgerBuilder.Minor(50), checking.Id, LedgerBuilder.Day1.AddDays(2));
        Assert.Equal(EntryKind.Refund, refund.Kind);
        Assert.Equal(purchase.Id, refund.RefundOfId);
        Assert.Equal(purchase.CategoryId, refund.CategoryId);
    }

    [Fact]
    [Trait("AT", "AT-08")]
    public void Transfer_fee_follows_the_transfer_and_disappears_at_zero()
    {
        var checking = _ledger.Account("Checking", 100);
        var savings = _ledger.Account("Savings", 0);
        var transfer = _ledger.Transfer(checking, savings, 50);
        var category = Guid.CreateVersion7();

        var fee = EntryActions.SyncTransferFee(transfer, null, LedgerBuilder.Minor(1.5m), category);
        Assert.NotNull(fee);
        Assert.NotNull(transfer.GroupId);
        Assert.Equal(transfer.GroupId, fee.GroupId);
        Assert.Equal(EntryKind.Expense, fee.Kind);
        Assert.Equal(checking.Id, fee.AccountId);
        Assert.Equal(category, fee.CategoryId);

        transfer.Date = LedgerBuilder.Day1.AddDays(5);
        var updated = EntryActions.SyncTransferFee(transfer, fee, LedgerBuilder.Minor(2), category);
        Assert.Same(fee, updated);
        Assert.Equal(transfer.Date, fee.Date);
        Assert.Same(fee, EntryActions.FindTransferFee(transfer, [transfer, fee]));

        Assert.Null(EntryActions.SyncTransferFee(transfer, fee, 0, category));
    }

    [Fact]
    public void Persian_year_runs_from_nowruz_to_nowruz()
    {
        var (first, last) = Budgets.PeriodMath.YearRange(new DateOnly(2026, 9, 26), Budgets.PeriodCalendar.Persian);

        Assert.Equal(new DateOnly(2026, 3, 21), first);
        Assert.Equal(new DateOnly(2027, 3, 20), last);
        Assert.Equal((1405, 12), Budgets.PeriodMath.Previous(1406, 1));
    }

    [Fact]
    [Trait("AT", "AT-50")]
    public void Drill_down_list_adds_up_to_the_category_number()
    {
        var checking = _ledger.Account("Checking", 100);
        var hidden = _ledger.Account("Hidden", 0);
        hidden.IncludeInTotals = false;
        var food = Guid.CreateVersion7();
        var bread = Guid.CreateVersion7();
        var purchase = _ledger.Add(EntryKind.Expense, checking, 40, categoryId: food);
        _ledger.Add(EntryKind.Expense, checking, 10, categoryId: bread);
        _ledger.Refund(purchase, checking, 15);
        _ledger.Add(EntryKind.Expense, hidden, 99, categoryId: food);
        Guid? Group(Guid? id) => id == bread ? food : id;

        var report = LedgerCalculator.ExpenseByCategory(_ledger.Accounts, _ledger.Entries, new LedgerFilter(LedgerBuilder.Day1, LedgerBuilder.Day1), Group).Single();
        var list = EntrySearch.Apply(_ledger.Entries, new EntryFilter(LedgerBuilder.Day1, LedgerBuilder.Day1, KindFilter.Expenses, CategoryIds: [food, bread], InTotalsOnly: true), _ => null, AccountsById);

        Assert.Equal(food, report.CategoryId);
        Assert.Equal(LedgerBuilder.Minor(35), report.Net);
        Assert.Equal(-report.Net, EntrySearch.NetByCurrency(list, AccountsById)["EUR"]);
    }

    private List<LedgerEntry> Filter(EntryFilter filter) => [.. EntrySearch.Apply(_ledger.Entries, filter, _ => null, AccountsById)];
}
