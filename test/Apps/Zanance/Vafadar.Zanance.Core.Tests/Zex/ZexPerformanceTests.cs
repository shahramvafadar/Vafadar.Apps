using System.Diagnostics;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Forecasts;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Rates;
using Vafadar.Zanance.Core.Reports;
using Vafadar.Zanance.Core.Settings;

namespace Vafadar.Zanance.Core.Tests.Zex;

/// <summary>
/// ZEX-S0906: the calculations enhancement ZEX added to Home, the reports R1–R6 and the holdings ledger stay within the
/// calculation budget of Q-02 (<see cref="PerformanceTests"/>) with the reference data set: 10,000 entries, 20 accounts,
/// 100 plans, 30 asset types, 500 holding events, 200 valuations and 10 goals. The limit is generous so slow CI machines
/// do not fail; it catches accidental quadratic behaviour.
/// </summary>
public sealed class ZexPerformanceTests
{
    private static readonly DateOnly Today = new(2027, 6, 15);
    private static readonly DateOnly Start = new(2024, 1, 1);
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(2);

    [Fact]
    public void The_reference_data_set_is_calculated_within_the_budget()
    {
        var random = new Random(42);
        var ledger = new LedgerBuilder();
        var accounts = Enumerable.Range(0, 20).Select(i => ledger.Account($"Account {i}", 1_000, openingDate: Start)).ToList();
        var categories = Enumerable.Range(0, 20).Select(_ => Guid.CreateVersion7()).ToList();
        for (var i = 0; i < 10_000; i++)
        {
            var kind = (i % 10) switch { 0 => EntryKind.Income, 1 => EntryKind.Refund, 2 => EntryKind.Transfer, _ => EntryKind.Expense };
            var account = accounts[random.Next(accounts.Count)];
            var entry = ledger.Add(kind, account, random.Next(1, 500), Start.AddDays(random.Next(0, 900)), categories[random.Next(categories.Count)]);
            if (kind == EntryKind.Transfer)
            {
                entry.CategoryId = null;
                entry.ToAccountId = accounts[(accounts.IndexOf(account) + 1) % accounts.Count].Id;
            }
        }

        var plans = Enumerable.Range(0, 100).Select(i => new Schedule
        {
            Name = $"Plan {i}",
            AccountId = accounts[i % accounts.Count].Id,
            Amount = 1_000,
            Rule = new RecurrenceRule { Frequency = i % 2 == 0 ? Frequency.Monthly : Frequency.Weekly, Start = new DateOnly(2025, 1, 1 + (i % 28)) },
        }).ToList();

        // Holdings: an opening per type, then purchases and small sales, so no type is ever sold short.
        var place = Guid.CreateVersion7();
        var types = Enumerable.Range(0, 30).Select(i => new AssetType { Name = $"Type {i}", PriceCurrencyCode = "EUR" }).ToList();
        var events = types.Select(t => new AssetEvent { AssetTypeId = t.Id, LocationId = place, Kind = AssetEventKind.Opening, Quantity = 100_000, BasisAmount = 50_000, Date = Start }).ToList();
        for (var i = events.Count; i < 500; i++)
        {
            var sale = i % 10 == 0;
            events.Add(new AssetEvent
            {
                AssetTypeId = types[random.Next(types.Count)].Id,
                LocationId = place,
                Kind = sale ? AssetEventKind.Sale : AssetEventKind.Purchase,
                Quantity = sale ? 500 : random.Next(100, 5_000),
                BasisAmount = random.Next(100, 5_000),
                Date = Start.AddDays(random.Next(1, 900)),
            });
        }

        var valuations = Enumerable.Range(0, 200).Select(i => new AssetValuation
        {
            AssetTypeId = types[i % types.Count].Id,
            CurrencyCode = "EUR",
            Date = Start.AddDays(i * 4),
            PricePerUnitMilli = random.Next(1_000_000, 20_000_000),
        }).ToList();

        // Ten goals: money set aside with allocations, and quantity goals on asset types.
        var goals = Enumerable.Range(0, 10).Select(i => i % 2 == 0
            ? new Goal { Name = $"Goal {i}", CurrencyCode = "EUR", Type = GoalType.Earmark, TargetAmount = 500_000, TargetDate = Today.AddMonths(12) }
            : new Goal { Name = $"Goal {i}", CurrencyCode = "EUR", Type = GoalType.HoldingQuantity, AssetTypeId = types[i].Id, TargetAmount = 200_000 }).ToList();
        var allocations = goals.Where(g => g.Type == GoalType.Earmark)
            .Select((g, i) => new GoalAllocation { GoalId = g.Id, AccountId = accounts[i].Id, Amount = 10_000, Date = Today.AddMonths(-3) }).ToList();
        var contributions = goals.Select(g => new ContributionPlan { GoalId = g.Id, Method = ContributionMethod.FixedAmount, Amount = 5_000, AssumedPricePerUnitMilli = 6_000_000 }).ToList();

        var rates = new RateTable([]);
        var month = new LedgerFilter(new DateOnly(2027, 6, 1), new DateOnly(2027, 6, 30));
        var range = KpiCatalog.Compare(new DateOnly(2027, 5, 1), new DateOnly(2027, 5, 31), new DateOnly(2027, 4, 1), new DateOnly(2027, 4, 30), Today);

        // Home: liquidity, capacity, goals and data status.
        var forecast = ForecastCalculator.Compute(ledger.Accounts, ledger.Entries, plans, [], Today, Today.AddDays(30));
        var protectedMoney = LiquidityCalculator.ProtectedMoney(goals, allocations, ledger.Accounts, ledger.Entries, Today);
        Measure(() => LiquidityCalculator.Compute(forecast, protectedMoney, Today, 2_000, EstimatePeriod.Day, "EUR"));
        Measure(() => CapacityCalculator.Compute(ledger.Accounts, ledger.Entries, plans, [], goals, contributions, "EUR", Today, PeriodCalendar.Gregorian));
        Measure(() => GoalProgressService.Evaluate(goals, allocations, ledger.Accounts, ledger.Entries, contributions, Today, events, types));
        Measure(() => goals.Select(g => GoalTrendService.Compute(g, 100_000, ledger.Accounts, ledger.Entries, allocations, events, Today, PeriodCalendar.Gregorian)).ToList());
        Measure(() => DataStatus.Check(ledger.Accounts, ledger.Entries, [], month, Today, Today.AddDays(-40)));

        // R1–R6: surplus and spending changes, commitments, debt and receivables, coverage, wealth and its history.
        Measure(() => KpiCatalog.Surplus(ledger.Accounts, ledger.Entries, month));
        Measure(() => KpiCatalog.SpendingChanges(ledger.Accounts, ledger.Entries, range, "EUR", id => id));
        Measure(() => KpiCatalog.Commitments(ledger.Accounts, plans, [], Today));
        Measure(() => KpiCatalog.Yearly(ledger.Accounts, [], plans, [], Today));
        Measure(() => KpiCatalog.Debt(ledger.Accounts, ledger.Entries, plans, [], month.From, month.To, Today));
        Measure(() => KpiCatalog.Receivables(ledger.Accounts, ledger.Entries, Today));
        Measure(() => KpiCatalog.Coverage(ledger.Accounts, ledger.Entries, [], [], "EUR", Today, PeriodCalendar.Gregorian));
        Measure(() => NetWorthCalculator.Compute(ledger.Accounts, ledger.Entries, types, events, valuations, Today));
        Measure(() => WealthHistory.MonthEnds(ledger.Accounts, ledger.Entries, types, events, valuations, rates, "EUR", Today, 12, PeriodCalendar.Gregorian));
        Measure(() => WealthHistory.Explain(ledger.Accounts, ledger.Entries, types, events, valuations, new DateOnly(2027, 4, 30), new DateOnly(2027, 5, 31)));

        // A saved forecast of 90 days compared with the ledger (CR03-02).
        var snapshot = ForecastSnapshot.From("Reference", forecast[0], accounts.Select(a => a.Id), Today.AddDays(-90), null, null);
        snapshot.Path = string.Join(',', Enumerable.Repeat("0", 91));
        Measure(() => SnapshotComparer.Compare(snapshot, ledger.Accounts, ledger.Entries, Today));

        // The holdings ledger: positions, values and cost basis of every type.
        Measure(() => HoldingsLedger.Positions(events, Today));
        Measure(() => AssetValuationService.Values(types, events, valuations, Today));
        Measure(() => types.Select(t => HoldingsLedger.Basis(events, t.Id)).ToList());
    }

    private static void Measure(Func<object> calculation)
    {
        var watch = Stopwatch.StartNew();
        Assert.NotNull(calculation());
        watch.Stop();
        Assert.True(watch.Elapsed < Limit, $"Took {watch.Elapsed.TotalMilliseconds:0} ms.");
    }
}
