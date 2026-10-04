using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Forecasts;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Rates;
using Vafadar.Zanance.Core.Reports;

namespace Vafadar.Zanance.Core.Tests.Zex;

/// <summary>Quantity goals, trend ETA, wealth history and forecast snapshots of enhancement ZEX phase 5.</summary>
public sealed class ZexPhase5Tests
{
    private static readonly DateOnly Today = new(2026, 10, 12);
    private readonly Guid _place = Guid.NewGuid();

    [Fact]
    public void G14_a_quantity_goal_counts_grams_not_prices()
    {
        var gold = new AssetType { Name = "Gold", PriceCurrencyCode = "EUR" };
        var goal = new Goal { Name = "50 g", CurrencyCode = "EUR", Type = GoalType.HoldingQuantity, AssetTypeId = gold.Id, TargetAmount = 50_000 };
        var events = new List<AssetEvent> { new() { AssetTypeId = gold.Id, LocationId = _place, Kind = AssetEventKind.Opening, Quantity = 20_000, Date = Today.AddDays(-30) } };
        var plan = new ContributionPlan { GoalId = goal.Id, Method = ContributionMethod.FixedAmount, Amount = 2_000, Rule = new RecurrenceRule { Frequency = Frequency.Monthly, Start = new DateOnly(2026, 11, 1) } };

        var progress = Assert.Single(GoalProgressService.Evaluate([goal], [], [], [], [plan], Today, events, [gold]));

        Assert.Equal((20_000, 30_000), (progress.Current, progress.Remaining));
        Assert.Equal(new DateOnly(2028, 1, 1), progress.Eta);

        // A new price alone changes nothing: progress is the quantity.
        var again = Assert.Single(GoalProgressService.Evaluate([goal], [], [], [], [plan], Today, events, [gold]));
        Assert.Equal(progress.Current, again.Current);
    }

    [Fact]
    public void A_quantity_goal_can_count_one_location_and_ignores_moves_in_its_pace()
    {
        var gold = new AssetType { Name = "Gold", PriceCurrencyCode = "EUR" };
        var bank = Guid.NewGuid();
        var goal = new Goal { Name = "Bank gold", CurrencyCode = "EUR", Type = GoalType.HoldingQuantity, AssetTypeId = gold.Id, LocationId = bank, TargetAmount = 50_000 };
        var events = new List<AssetEvent>
        {
            new() { AssetTypeId = gold.Id, LocationId = _place, Kind = AssetEventKind.Opening, Quantity = 30_000, Date = new DateOnly(2026, 1, 1) },
            new() { AssetTypeId = gold.Id, LocationId = _place, ToLocationId = bank, Kind = AssetEventKind.LocationTransfer, Quantity = 10_000, Date = new DateOnly(2026, 9, 5) },
        };

        Assert.Equal(10_000, GoalProgressService.HeldQuantity(goal, events, Today));
    }

    [Fact]
    public void S0702_the_pace_is_the_median_and_a_large_month_is_a_one_off()
    {
        var ledger = new LedgerBuilder();
        var savings = ledger.Account("Savings", 0, AccountType.Savings, openingDate: new DateOnly(2026, 6, 1));
        var checking = ledger.Account("Main", 10_000, openingDate: new DateOnly(2026, 6, 1));
        foreach (var (month, amount) in new[] { (6, 200m), (7, 250m), (8, 230m), (9, 900m) })
        {
            ledger.Transfer(checking, savings, amount, date: new DateOnly(2026, month, 10));
        }

        var goal = new Goal { Name = "Savings to 5,000", CurrencyCode = "EUR", Type = GoalType.AccountBalance, AccountId = savings.Id, TargetAmount = 500_000 };
        var trend = GoalTrendService.Compute(goal, 500_000 - 158_000, ledger.Accounts, ledger.Entries, [], [], Today, PeriodCalendar.Gregorian);

        Assert.Equal(TrendStatus.Ok, trend.Status);
        Assert.Equal(24_000, trend.Pace);
        Assert.Equal([false, false, false, true], trend.Periods.Select(p => p.IsOneOff));
        Assert.Equal(new DateOnly(2027, 12, 31), trend.Eta);
    }

    [Fact]
    public void S0702_two_complete_months_are_not_enough_history()
    {
        var ledger = new LedgerBuilder();
        var savings = ledger.Account("Savings", 0, AccountType.Savings, openingDate: new DateOnly(2026, 8, 1));
        var goal = new Goal { Name = "Savings", CurrencyCode = "EUR", Type = GoalType.AccountBalance, AccountId = savings.Id, TargetAmount = 100_000 };

        var trend = GoalTrendService.Compute(goal, 100_000, ledger.Accounts, ledger.Entries, [], [], Today, PeriodCalendar.Gregorian);

        Assert.Equal((TrendStatus.NotEnoughHistory, 2), (trend.Status, trend.CompletePeriods));
    }

    [Fact]
    public void S0703_capacity_buys_quantity_only_at_an_assumed_price()
    {
        Assert.Equal(2_500, CapacityCalculator.QuantityFor(15_000, 6_000_000));
        Assert.Equal(0, CapacityCalculator.QuantityFor(15_000, 0));
    }

    [Fact]
    public void S0801_a_new_price_today_leaves_last_month_unchanged()
    {
        var ledger = new LedgerBuilder();
        ledger.Account("Main", 1_000, openingDate: new DateOnly(2026, 1, 1));
        var gold = new AssetType { Name = "Gold", PriceCurrencyCode = "EUR" };
        var events = new List<AssetEvent> { new() { AssetTypeId = gold.Id, LocationId = _place, Kind = AssetEventKind.Opening, Quantity = 10_000, Date = new DateOnly(2026, 1, 1) } };
        var prices = new List<AssetValuation> { new() { AssetTypeId = gold.Id, CurrencyCode = "EUR", Date = new DateOnly(2026, 8, 1), PricePerUnitMilli = 10_000_000 } };

        var before = WealthHistory.MonthEnds(ledger.Accounts, ledger.Entries, [gold], events, prices, new RateTable([]), "EUR", Today, 1, PeriodCalendar.Gregorian);
        prices.Add(new AssetValuation { AssetTypeId = gold.Id, CurrencyCode = "EUR", Date = Today, PricePerUnitMilli = 12_000_000 });
        var after = WealthHistory.MonthEnds(ledger.Accounts, ledger.Entries, [gold], events, prices, new RateTable([]), "EUR", Today, 1, PeriodCalendar.Gregorian);

        Assert.Equal(new DateOnly(2026, 9, 30), after[0].Date);
        Assert.Equal(before[0].PerCurrency["EUR"], after[0].PerCurrency["EUR"]);
        Assert.Equal(220_000, after[1].PerCurrency["EUR"]);
    }

    [Fact]
    public void S0802_flows_price_and_fx_effects_add_up_to_the_change()
    {
        var ledger = new LedgerBuilder();
        var from = new DateOnly(2026, 9, 30);
        var to = new DateOnly(2026, 10, 31);
        var euro = ledger.Account("Main", 1_000, openingDate: new DateOnly(2026, 1, 1));
        ledger.Account("Dollar", 1_000, currency: "USD", openingDate: new DateOnly(2026, 1, 1));
        ledger.Add(EntryKind.Income, euro, 500, new DateOnly(2026, 10, 5));
        var gold = new AssetType { Name = "Gold", PriceCurrencyCode = "EUR" };
        var events = new List<AssetEvent> { new() { AssetTypeId = gold.Id, LocationId = _place, Kind = AssetEventKind.Opening, Quantity = 1_000, Date = new DateOnly(2026, 1, 1) } };
        var prices = new List<AssetValuation>
        {
            new() { AssetTypeId = gold.Id, CurrencyCode = "EUR", Date = new DateOnly(2026, 9, 1), PricePerUnitMilli = 10_000_000 },
            new() { AssetTypeId = gold.Id, CurrencyCode = "EUR", Date = new DateOnly(2026, 10, 20), PricePerUnitMilli = 20_000_000 },
        };
        var rates = new RateTable(
        [
            new ExchangeRate { FromCurrencyCode = "USD", ToCurrencyCode = "EUR", Rate = 0.92m, Date = from },
            new ExchangeRate { FromCurrencyCode = "USD", ToCurrencyCode = "EUR", Rate = 0.89m, Date = to },
        ]);

        var changes = WealthHistory.Explain(ledger.Accounts, ledger.Entries, [gold], events, prices, from, to);
        var (converted, missing) = WealthHistory.Convert(changes, rates, "EUR");

        Assert.Empty(missing);
        Assert.Equal((50_000, 10_000, -3_000), (converted.Flows, converted.PriceEffect, converted.FxEffect));
        Assert.Equal(57_000, converted.Change);
        Assert.Equal(0, converted.Remainder);
    }

    [Fact]
    public void S0802_a_missing_valuation_is_named_as_the_cause_of_the_remainder()
    {
        var ledger = new LedgerBuilder();
        ledger.Account("Main", 1_000, openingDate: new DateOnly(2026, 1, 1));
        var gold = new AssetType { Name = "Gold", PriceCurrencyCode = "EUR" };
        var events = new List<AssetEvent> { new() { AssetTypeId = gold.Id, LocationId = _place, Kind = AssetEventKind.Opening, Quantity = 1_000, Date = new DateOnly(2026, 1, 1) } };
        var prices = new List<AssetValuation> { new() { AssetTypeId = gold.Id, CurrencyCode = "EUR", Date = new DateOnly(2026, 10, 20), PricePerUnitMilli = 10_000_000 } };

        var change = Assert.Single(WealthHistory.Explain(ledger.Accounts, ledger.Entries, [gold], events, prices, new DateOnly(2026, 9, 30), new DateOnly(2026, 10, 31)));

        Assert.Equal(10_000, change.Remainder);
        Assert.Contains(RemainderCause.MissingValuation, change.Causes);
    }

    [Fact]
    public void S0803_S0804_a_snapshot_keeps_its_path_and_an_entry_recorded_later_is_named()
    {
        var ledger = new LedgerBuilder();
        var baseDate = new DateOnly(2026, 10, 1);
        var checking = ledger.Account("Main", 1_000, openingDate: new DateOnly(2026, 9, 1));
        var rent = new Schedule { Name = "Rent", Kind = EntryKind.Expense, AccountId = checking.Id, Amount = 50_000, Rule = new RecurrenceRule { Frequency = Frequency.Once, Start = new DateOnly(2026, 10, 3) } };
        var forecast = Assert.Single(ForecastCalculator.Compute(ledger.Accounts, ledger.Entries, [rent], [], baseDate, baseDate.AddDays(10)));
        var snapshot = ForecastSnapshot.From("Before October", forecast, [checking.Id], baseDate, null, "1.0");
        snapshot.CreatedAt = new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.Zero);

        // After saving: a coffee of 30 September recorded late, rent paid at 480, an unplanned purchase.
        var late = ledger.Add(EntryKind.Expense, checking, 20, new DateOnly(2026, 9, 30));
        late.CreatedAt = snapshot.CreatedAt.AddDays(2);
        var paid = ledger.Add(EntryKind.Expense, checking, 480, new DateOnly(2026, 10, 3));
        paid.ScheduleId = rent.Id;
        paid.CreatedAt = snapshot.CreatedAt.AddDays(2);
        var shoes = ledger.Add(EntryKind.Expense, checking, 60, new DateOnly(2026, 10, 4));
        shoes.CreatedAt = snapshot.CreatedAt.AddDays(3);

        var comparison = SnapshotComparer.Compare(snapshot, ledger.Accounts, ledger.Entries, new DateOnly(2026, 10, 5));

        Assert.Equal(50_000, snapshot.Points()[^1].Balance);
        Assert.Equal((-2_000, -6_000, 0), (comparison.RecordedLater, comparison.UnplannedSpending, comparison.UnplannedIncome));
        Assert.Equal(-6_000, comparison.Difference);
        Assert.Equal(2_000, comparison.PlansChanged);
        Assert.Equal(comparison.Difference, comparison.RecordedLater + comparison.UnplannedSpending + comparison.UnplannedIncome + comparison.PlansChanged);
    }
}
