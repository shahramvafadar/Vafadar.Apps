using System.Collections;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.Core.Tests.Goals;

/// <summary>Goal evaluation keeps financial results while avoiding redundant full-ledger reads (D-92).</summary>
public sealed class GoalProgressSnapshotTests
{
    private static readonly DateOnly Today = new(2026, 10, 9);

    [Fact, Trait("AT", "AT-98")]
    public void No_goals_do_not_read_the_ledger()
    {
        var ledger = new LedgerBuilder();
        ledger.Account("Existing account", 100);
        var entries = new ObservedEntries(ledger.Entries);

        Assert.Empty(GoalProgressService.Evaluate([], [], ledger.Accounts, entries, [], Today));
        Assert.Equal(0, entries.Enumerations);
    }

    [Theory, Trait("AT", "AT-98")]
    [InlineData(GoalState.Completed), InlineData(GoalState.Archived)]
    public void Inactive_goals_do_not_read_the_ledger(GoalState state)
    {
        var ledger = new LedgerBuilder();
        var account = ledger.Account("Existing account", 100);
        ledger.Add(EntryKind.Income, account, 20);
        var entries = new ObservedEntries(ledger.Entries);
        var goal = BalanceGoal(account);
        goal.State = state;

        Assert.Empty(GoalProgressService.Evaluate([goal], [], ledger.Accounts, entries, [], Today));
        Assert.Equal(0, entries.Enumerations);
    }

    [Fact, Trait("AT", "AT-98")]
    public void Many_balance_goals_read_one_original_ledger_snapshot()
    {
        var ledger = new LedgerBuilder();
        for (var i = 0; i < 100; i++)
        {
            var account = ledger.Account("Account " + i, 100);
            for (var j = 0; j < 10; j++) { ledger.Add(EntryKind.Income, account, 1); }
        }
        var entries = new ObservedEntries(ledger.Entries);
        var progress = GoalProgressService.Evaluate(ledger.Accounts.Select(BalanceGoal), [], ledger.Accounts, entries, [], Today);

        Assert.Equal(100, progress.Count);
        Assert.All(progress, item => Assert.Equal(11_000, item.Current));
        Assert.Equal(1, entries.Enumerations);
        Assert.Equal(1_000, entries.Visited);
        Assert.Equal(1_000, ledger.Entries.Count);
    }

    [Fact, Trait("AT", "AT-98")]
    public void Mixed_goals_keep_cross_currency_transfers_refunds_dates_and_funding_priority()
    {
        var ledger = new LedgerBuilder();
        var euro = ledger.Account("EUR", 100);
        var dollar = ledger.Account("USD", 200, currency: "USD");
        ledger.Add(EntryKind.Income, euro, 20, review: ReviewState.Unreviewed);
        var purchase = ledger.Add(EntryKind.Expense, euro, 40);
        ledger.Refund(purchase, euro, 5);
        ledger.Transfer(euro, dollar, 30, 33);
        ledger.Transfer(euro, euro, 1, 2);
        ledger.Add(EntryKind.Adjustment, euro, 7).Direction = AdjustmentDirection.Increase;
        ledger.Add(EntryKind.AssetPurchase, euro, 2);
        ledger.Add(EntryKind.AssetSale, euro, 3);
        ledger.Add(EntryKind.Income, euro, 999, LedgerBuilder.Day1.AddDays(-1));
        ledger.Add(EntryKind.Income, euro, 999, Today.AddDays(1));
        var high = new Goal { Name = "Priority", CurrencyCode = "EUR", TargetAmount = 10_000, Priority = GoalPriority.High };
        var low = new Goal { Name = "Later", CurrencyCode = "EUR", TargetAmount = 10_000, Priority = GoalPriority.Low };
        var allocations = new[]
        {
            new GoalAllocation { GoalId = high.Id, AccountId = euro.Id, Amount = 5_000, Date = Today },
            new GoalAllocation { GoalId = low.Id, AccountId = euro.Id, Amount = 4_000, Date = Today },
        };
        var euroGoal = BalanceGoal(euro);
        var dollarGoal = BalanceGoal(dollar);
        dollarGoal.State = GoalState.Paused;
        var entries = new ObservedEntries(ledger.Entries);
        var progress = GoalProgressService.Evaluate([euroGoal, dollarGoal, high, low], allocations, ledger.Accounts, entries, [], Today).ToDictionary(p => p.Goal.Id);

        Assert.Equal(6_400, progress[euroGoal.Id].Current);
        Assert.Equal(23_300, progress[dollarGoal.Id].Current);
        Assert.Equal(GoalNotice.Paused, progress[dollarGoal.Id].Notice);
        Assert.Equal((5_000L, 0L), (progress[high.Id].Current, progress[high.Id].Unfunded));
        Assert.Equal((1_400L, 2_600L), (progress[low.Id].Current, progress[low.Id].Unfunded));
        Assert.Equal(1, entries.Enumerations);
        Assert.Equal(10, ledger.Entries.Count);
    }

    [Fact, Trait("AT", "AT-98")]
    public void Archived_and_negative_account_notices_remain_distinct()
    {
        var ledger = new LedgerBuilder();
        var archived = ledger.Account("Archived", 100);
        archived.IsArchived = true;
        var negative = ledger.Account("Debt", -100);
        var goals = new[] { BalanceGoal(archived), BalanceGoal(negative) };
        var progress = GoalProgressService.Evaluate(goals, [], ledger.Accounts, ledger.Entries, [], Today);

        Assert.Equal((0L, GoalNotice.AccountUnavailable), (progress[0].Current, progress[0].Notice));
        Assert.Equal((-10_000L, GoalNotice.NegativeBalance), (progress[1].Current, progress[1].Notice));
    }

    [Fact, Trait("AT", "AT-98")]
    public void Active_balance_goals_keep_checked_overflow()
    {
        var ledger = new LedgerBuilder();
        var account = ledger.Account("Full", 0);
        account.OpeningBalance = long.MaxValue;
        ledger.Add(EntryKind.Income, account, 1);

        Assert.Throws<OverflowException>(() => GoalProgressService.Evaluate([BalanceGoal(account)], [], ledger.Accounts, ledger.Entries, [], Today));
    }

    private static Goal BalanceGoal(Account account) => new()
    {
        Name = "Goal " + account.Name, CurrencyCode = account.CurrencyCode, Type = GoalType.AccountBalance,
        AccountId = account.Id, TargetAmount = 100_000,
    };

    /// <summary>Observes full source passes, which must not grow with the number of account goals.</summary>
    private sealed class ObservedEntries(IReadOnlyCollection<LedgerEntry> entries) : IReadOnlyCollection<LedgerEntry>
    {
        public int Count => entries.Count;
        public int Enumerations { get; private set; }
        public int Visited { get; private set; }

        public IEnumerator<LedgerEntry> GetEnumerator()
        {
            Enumerations++;
            foreach (var entry in entries) { Visited++; yield return entry; }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
