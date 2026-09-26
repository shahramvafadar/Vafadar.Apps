using Vafadar.Finance.Core.Budgets;
using Vafadar.Finance.Core.Ledger;

namespace Vafadar.Finance.Core.Tests.Ledger;

public sealed class SplitTests
{
    private readonly LedgerBuilder _ledger = new();

    [Fact]
    [Trait("AT", "AT-66")]
    public void A_split_changes_the_balance_once_and_each_category_budget_sees_its_share()
    {
        var account = _ledger.Account("Checking", 500);
        var food = Guid.CreateVersion7();
        var household = Guid.CreateVersion7();
        var purchase = _ledger.Add(EntryKind.Expense, account, 120, categoryId: food);

        var (save, delete) = EntryActions.Split([purchase], [(food, LedgerBuilder.Minor(80)), (household, LedgerBuilder.Minor(40))]);

        Assert.Empty(delete);
        Assert.Equal(purchase.Id, save[0].Id);
        Assert.All(save, e => Assert.Equal(purchase.GroupId, e.GroupId));
        _ledger.Entries.Remove(purchase);
        _ledger.Entries.AddRange(save);

        var day = purchase.Date;
        Assert.Equal(LedgerBuilder.Minor(380), LedgerCalculator.Balance(account, _ledger.Entries, day));
        Assert.Equal(LedgerBuilder.Minor(80), BudgetCalculator.NetExpense(_ledger.Accounts, _ledger.Entries, day, day, "EUR", categoryIds: [food]));
        Assert.Equal(LedgerBuilder.Minor(40), BudgetCalculator.NetExpense(_ledger.Accounts, _ledger.Entries, day, day, "EUR", categoryIds: [household]));
        Assert.Equal(LedgerBuilder.Minor(120), BudgetCalculator.NetExpense(_ledger.Accounts, _ledger.Entries, day, day, "EUR"));
        Assert.True(EntryActions.IsSplit(save));
    }

    [Fact]
    public void Parts_must_add_up_exactly_and_links_and_fees_prevent_splitting()
    {
        var account = _ledger.Account("Checking", 500);
        var purchase = _ledger.Add(EntryKind.Expense, account, 100);

        Assert.Throws<ArgumentException>(() => EntryActions.Split([purchase], [(null, LedgerBuilder.Minor(60)), (null, LedgerBuilder.Minor(30))]));
        Assert.Throws<ArgumentException>(() => EntryActions.Split([purchase], [(null, LedgerBuilder.Minor(100))]));
        Assert.Throws<ArgumentException>(() => EntryActions.Split([purchase], [(null, LedgerBuilder.Minor(110)), (null, -LedgerBuilder.Minor(10))]));

        purchase.ScheduleId = Guid.CreateVersion7();
        Assert.False(EntryActions.CanSplit([purchase]));

        var transfer = _ledger.Add(EntryKind.Transfer, account, 50);
        var fee = EntryActions.SyncTransferFee(transfer, null, 150, Guid.CreateVersion7())!;
        Assert.False(EntryActions.CanSplit([fee]));
        Assert.False(EntryActions.CanSplit([transfer, fee]));
    }

    [Fact]
    public void A_split_can_be_changed_and_joined_again()
    {
        var account = _ledger.Account("Checking", 500);
        var purchase = _ledger.Add(EntryKind.Expense, account, 90);
        var (parts, _) = EntryActions.Split([purchase], [(null, LedgerBuilder.Minor(30)), (null, LedgerBuilder.Minor(30)), (null, LedgerBuilder.Minor(30))]);

        var (fewer, removed) = EntryActions.Split(parts, [(null, LedgerBuilder.Minor(50)), (null, LedgerBuilder.Minor(40))]);
        Assert.Equal(2, fewer.Count);
        Assert.Equal([parts[2].Id], removed);

        var (joined, deleted) = EntryActions.Join(fewer);
        Assert.Equal(purchase.Id, joined.Id);
        Assert.Equal(LedgerBuilder.Minor(90), joined.Amount);
        Assert.Null(joined.GroupId);
        Assert.Equal([fewer[1].Id], deleted);
    }
}