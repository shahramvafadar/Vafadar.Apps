using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.Core.Tests;

/// <summary>Completed balances and monthly contributions retain supported offsetting net values.</summary>
public sealed class CompletedNetReadTests
{
    private static readonly DateOnly Day = new(2026, 11, 1);

    /// <summary>Input identity/order is not an arithmetic limit on a supported completed net.</summary>
    [Theory]
    [InlineData(0, 0, false), InlineData(0, 1, false), InlineData(0, 2, false)]
    [InlineData(0, 3, false), InlineData(0, 4, false), InlineData(0, 5, false)]
    [InlineData(1, 0, false), InlineData(1, 1, false), InlineData(1, 2, false)]
    [InlineData(1, 3, false), InlineData(1, 4, false), InlineData(1, 5, false)]
    [InlineData(2, 0, false), InlineData(2, 1, false), InlineData(2, 2, false)]
    [InlineData(2, 3, false), InlineData(2, 4, false), InlineData(2, 5, false)]
    [InlineData(3, 0, false), InlineData(3, 1, false), InlineData(3, 2, false)]
    [InlineData(3, 3, false), InlineData(3, 4, false), InlineData(3, 5, false)]
    [InlineData(1, 0, true), InlineData(1, 1, true), InlineData(1, 2, true)]
    [InlineData(1, 3, true), InlineData(1, 4, true), InlineData(1, 5, true)]
    [InlineData(2, 0, true), InlineData(2, 1, true), InlineData(2, 2, true)]
    [InlineData(2, 3, true), InlineData(2, 4, true), InlineData(2, 5, true)]
    [InlineData(3, 0, true), InlineData(3, 1, true), InlineData(3, 2, true)]
    [InlineData(3, 3, true), InlineData(3, 4, true), InlineData(3, 5, true)]
    public void Supported_completed_net_is_independent_of_input_order(int reader, int permutation, bool completedMonth)
    {
        var start = completedMonth ? Day.AddMonths(-1) : Day;
        var fixture = Create(reader, [long.MaxValue, -long.MaxValue, long.MaxValue], start);
        int[][] orders = [[0, 1, 2], [0, 2, 1], [1, 0, 2], [1, 2, 0], [2, 0, 1], [2, 1, 0]];
        fixture.Entries = [.. orders[permutation].Select(i => fixture.Entries[i])];
        fixture.Events = [.. orders[permutation].Select(i => fixture.Events[i])];
        fixture.Allocations = [.. orders[permutation].Select(i => fixture.Allocations[i])];
        var before = State(fixture);
        if (reader == 2) Assert.Null(HoldingsLedger.FindConflict(fixture.Events));
        Assert.Equal(long.MaxValue, Read(fixture, reader, completedMonth));
        Assert.True(before.SequenceEqual(State(fixture)));
    }

    /// <summary>A completed value outside the public long range still fails rather than wrapping or clamping.</summary>
    [Theory]
    [InlineData(0, false), InlineData(0, true), InlineData(1, false), InlineData(1, true)]
    [InlineData(2, false), InlineData(2, true), InlineData(3, false), InlineData(3, true)]
    public void Unsupported_completed_net_still_refuses(int reader, bool negative)
    {
        var fixture = Create(reader, negative ? [-long.MaxValue, -2] : [long.MaxValue, 1], Day);
        var before = State(fixture);
        Assert.Throws<OverflowException>(() => Read(fixture, reader, false));
        Assert.True(before.SequenceEqual(State(fixture)));
    }

    /// <summary>Wider intermediate arithmetic does not admit future, pre-opening or unrelated source rows.</summary>
    [Theory]
    [InlineData(0), InlineData(1), InlineData(2), InlineData(3)]
    public void Original_period_and_identity_filters_remain(int reader)
    {
        var fixture = Create(reader, [1, long.MaxValue, long.MaxValue, long.MaxValue], Day);
        fixture.Entries[1].Date = fixture.Events[1].Date = fixture.Allocations[1].Date = Day.AddMonths(-1);
        fixture.Entries[2].Date = fixture.Events[2].Date = fixture.Allocations[2].Date = Day.AddDays(10);
        fixture.Entries[3].AccountId = Guid.CreateVersion7();
        fixture.Events[3].AssetTypeId = Guid.CreateVersion7();
        fixture.Allocations[3].GoalId = Guid.CreateVersion7();
        var before = State(fixture);
        Assert.Equal(1L, Read(fixture, reader, false));
        Assert.True(before.SequenceEqual(State(fixture)));
    }

    private static long Read(Fixture fixture, int reader, bool completedMonth)
    {
        if (reader == 0) return LedgerCalculator.Balance(fixture.Account, fixture.Entries, Day.AddDays(3));
        var trend = GoalTrendService.Compute(fixture.Goal, 0, [fixture.Account], fixture.Entries,
            fixture.Allocations, fixture.Events, Day.AddDays(3), PeriodCalendar.Gregorian);
        return completedMonth ? Assert.Single(trend.Periods).Contribution : trend.SoFar;
    }

    private static Fixture Create(int reader, long[] effects, DateOnly start)
    {
        var account = new Account { Name = "Fictitious completed net", Type = AccountType.Checking,
            CurrencyCode = "EUR", OpeningDate = start, OpeningBalance = 0 };
        var goal = new Goal { Name = "Fictitious completed contribution", CurrencyCode = "EUR",
            TargetAmount = long.MaxValue, Type = reader switch
            { 2 => GoalType.HoldingQuantity, 3 => GoalType.Earmark, _ => GoalType.AccountBalance },
            AccountId = account.Id, AssetTypeId = Guid.CreateVersion7() };
        var location = Guid.CreateVersion7();
        var entries = effects.Select((effect, i) => new LedgerEntry
        {
            AccountId = account.Id, Kind = effect >= 0 ? EntryKind.Income : EntryKind.Expense,
            Amount = Math.Abs(effect), Date = start.AddDays(i), Note = "Fictitious in-memory source",
            Tags = ["source ownership"], CreatedAt = DateTimeOffset.UnixEpoch, UpdatedAt = DateTimeOffset.UnixEpoch
        }).ToArray();
        var events = effects.Select((effect, i) => new AssetEvent
        {
            AssetTypeId = goal.AssetTypeId!.Value, LocationId = location,
            Kind = effect >= 0 ? AssetEventKind.Opening : AssetEventKind.Outflow, Quantity = Math.Abs(effect),
            Date = start.AddDays(i), Note = "Fictitious in-memory source", CreatedAt = DateTimeOffset.UnixEpoch,
            UpdatedAt = DateTimeOffset.UnixEpoch
        }).ToArray();
        var allocations = effects.Select((effect, i) => new GoalAllocation
        { GoalId = goal.Id, AccountId = account.Id, Amount = effect, Date = start.AddDays(i) }).ToArray();
        return new Fixture(account, goal, entries, events, allocations);
    }

    // Typed tuples keep the preservation check usable in original trimmed/AOT calculations without reflection.
    private static object[] State(Fixture f) => [
        (f.Account.Id, f.Account.Name, f.Account.Type, f.Account.CurrencyCode, f.Account.OpeningDate, f.Account.OpeningBalance),
        (f.Goal.Id, f.Goal.Name, f.Goal.Type, f.Goal.TargetAmount, f.Goal.AccountId, f.Goal.AssetTypeId, f.Goal.LocationId),
        .. f.Entries.Select(e => (object)(e.Id, e.Kind, e.Date, e.AccountId, e.Amount, e.ToAccountId, e.ToAmount,
            e.Direction, e.CategoryId, e.Title, e.Payee, e.Note, e.Icon, e.Review, e.Source, e.OriginalAmount,
            e.OriginalCurrencyCode, e.RefundOfId, e.GroupId, e.IsPartialPayment, e.ReimbursableAmount,
            e.ReimbursedBy, e.ReimbursementDueDate, e.IsAggregated, e.AggregatedFrom, e.AggregatedTo,
            string.Join("\0", e.Tags), e.ScheduleId, e.OccurrenceDate, e.ImportBatchId, e.CreatedAt, e.UpdatedAt)),
        .. f.Events.Select(e => (object)(e.Id, e.AssetTypeId, e.LocationId, e.ToLocationId, e.Kind, e.Date,
            e.Quantity, e.BasisAmount, e.ProceedsAmount, e.IsIncrease, e.GroupId, e.Reason, e.Note, e.CreatedAt, e.UpdatedAt)),
        .. f.Allocations.Select(a => (object)(a.Id, a.GoalId, a.AccountId, a.Amount, a.Date, a.CreatedAt, a.UpdatedAt))];

    private sealed record Fixture(Account Account, Goal Goal, LedgerEntry[] InitialEntries,
        AssetEvent[] InitialEvents, GoalAllocation[] InitialAllocations)
    {
        public LedgerEntry[] Entries { get; set; } = InitialEntries;
        public AssetEvent[] Events { get; set; } = InitialEvents;
        public GoalAllocation[] Allocations { get; set; } = InitialAllocations;
    }
}
