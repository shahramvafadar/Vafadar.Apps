using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Forecasts;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Rates;
using Vafadar.Zanance.Core.Reports;
using Vafadar.Zanance.Core.Settings;

namespace Vafadar.Zanance.Core.Tests.Zex;

/// <summary>The examples of the KPI catalog (04 §2, §3) for enhancement ZEX phase 4.</summary>
public sealed class ZexPhase4KpiTests
{
    private static readonly DateOnly Oct1 = new(2026, 10, 1);

    [Fact]
    public void K05_K06_surplus_and_rate_leave_out_transfers_and_capital_movements()
    {
        var ledger = new LedgerBuilder();
        var checking = ledger.Account("Main", 0);
        var savings = ledger.Account("Savings", 0, AccountType.Savings);
        ledger.Add(EntryKind.Income, checking, 3_000);
        var expense = ledger.Add(EntryKind.Expense, checking, 2_400);
        ledger.Refund(expense, checking, 100);
        ledger.Transfer(checking, savings, 500);
        ledger.Add(EntryKind.AssetPurchase, checking, 1_000);

        var result = Assert.Single(KpiCatalog.Surplus(ledger.Accounts, ledger.Entries, new LedgerFilter(Oct1, Oct1.AddDays(30))));

        Assert.Equal(70_000, result.Surplus);
        Assert.Equal(23.3m, result.RatePercent);
        Assert.Equal(50_000, result.Transfers);
        Assert.Equal(100_000, result.CapitalPurchases);
    }

    [Fact]
    public void G17_without_income_the_rate_is_not_available_and_the_surplus_a_deficit()
    {
        var ledger = new LedgerBuilder();
        var checking = ledger.Account("Main", 1_000);
        ledger.Add(EntryKind.Expense, checking, 450);

        var result = Assert.Single(KpiCatalog.Surplus(ledger.Accounts, ledger.Entries, new LedgerFilter(Oct1, Oct1.AddDays(30))));

        Assert.Equal(-45_000, result.Surplus);
        Assert.Null(result.RatePercent);
    }

    [Fact]
    public void K11_a_running_month_is_compared_with_the_same_days_of_the_previous_one()
    {
        var ledger = new LedgerBuilder();
        var checking = ledger.Account("Main", 5_000, openingDate: new DateOnly(2026, 8, 1));
        var groceries = Guid.NewGuid();
        ledger.Add(EntryKind.Expense, checking, 150, new DateOnly(2026, 9, 5), groceries);
        ledger.Add(EntryKind.Expense, checking, 400, new DateOnly(2026, 9, 25), groceries);
        ledger.Add(EntryKind.Expense, checking, 180, new DateOnly(2026, 10, 10), groceries);
        var today = new DateOnly(2026, 10, 12);

        var range = KpiCatalog.Compare(Oct1, new DateOnly(2026, 10, 31), new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30), today);
        var change = Assert.Single(KpiCatalog.SpendingChanges(ledger.Accounts, ledger.Entries, range, "EUR", id => id));

        Assert.Equal((Oct1, today, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 12), true), (range.From, range.To, range.CompareFrom, range.CompareTo, range.IsPartial));
        Assert.Equal(3_000, change.Delta);
        Assert.Equal(20m, change.DeltaPercent);
    }

    [Fact]
    public void K04_next_30_days_split_fixed_estimated_and_unknown_and_count_only_what_is_outstanding()
    {
        var ledger = new LedgerBuilder();
        var checking = ledger.Account("Main", 3_000);
        var loan = ledger.Account("Car loan", -9_000, AccountType.Loan);
        loan.UsableForPayments = false;
        var today = new DateOnly(2026, 10, 2);
        Schedule Plan(string name, long? amount, AmountMode mode, int day, EntryKind kind = EntryKind.Expense, Guid? to = null) => new()
        {
            Name = name, Kind = kind, AccountId = checking.Id, ToAccountId = to, Amount = amount, AmountMode = mode,
            Rule = new RecurrenceRule { Frequency = Frequency.Once, Start = new DateOnly(2026, 10, day) },
        };
        var installment = Plan("Loan", 45_000, AmountMode.Fixed, 5, EntryKind.Transfer, loan.Id);
        var plans = new[] { Plan("Rent", 95_000, AmountMode.Fixed, 3), Plan("Phone", 3_000, AmountMode.Estimated, 20), Plan("Insurance", null, AmountMode.Unknown, 25), installment };
        var partlyPaid = new OccurrenceState { ScheduleId = installment.Id, OriginalDate = new DateOnly(2026, 10, 5), PaidAmount = 20_000 };

        var total = Assert.Single(KpiCatalog.Commitments(ledger.Accounts, plans, [partlyPaid], today));

        Assert.Equal((120_000, 3_000, 1), (total.Fixed, total.Estimated, total.UnknownCount));
    }

    [Fact]
    public void K08_twelve_months_of_regular_payments_by_group_with_monthly_shares()
    {
        var ledger = new LedgerBuilder();
        var checking = ledger.Account("Main", 3_000);
        var today = new DateOnly(2026, 10, 1);
        Schedule Monthly(string name, long amount, int interval = 1, AmountMode mode = AmountMode.Fixed) => new()
        {
            Name = name, Kind = EntryKind.Expense, AccountId = checking.Id, Amount = amount, AmountMode = mode,
            Rule = new RecurrenceRule { Frequency = Frequency.Monthly, Interval = interval, Start = new DateOnly(2026, 10, 15) },
        };

        var year = Assert.Single(KpiCatalog.Yearly(ledger.Accounts, [], [Monthly("Rent", 95_000), Monthly("Car insurance", 60_000, 12), Monthly("Phone", 3_000, mode: AmountMode.Estimated)], [], today));

        Assert.Equal((1_140_000, 60_000, 36_000), (year.Fixed, year.NonMonthly, year.Estimated));
        Assert.Equal(5_000, year.Plans.Single(p => p.Schedule.Name == "Car insurance").MonthlyShare);
    }

    [Fact]
    public void K02_headroom_example_subtracts_day_to_day_spending_and_protected_money_once()
    {
        var ledger = new LedgerBuilder();
        var today = new DateOnly(2026, 9, 30);
        var checking = ledger.Account("Main", 2_500, openingDate: new DateOnly(2026, 9, 1));
        var card = ledger.Account("Card", 0, AccountType.CreditCard, openingDate: new DateOnly(2026, 9, 1));
        card.UsableForPayments = false;
        ledger.Add(EntryKind.Expense, card, 400, today);
        Schedule Once(string name, EntryKind kind, long amount, DateOnly date, Guid? to = null) => new()
        {
            Name = name, Kind = kind, AccountId = checking.Id, ToAccountId = to, Amount = amount,
            Rule = new RecurrenceRule { Frequency = Frequency.Once, Start = date },
        };
        var plans = new[]
        {
            Once("Rent", EntryKind.Expense, 95_000, new DateOnly(2026, 10, 3)),
            Once("Card payment", EntryKind.Transfer, 40_000, new DateOnly(2026, 10, 10), card.Id),
            Once("Salary", EntryKind.Income, 280_000, new DateOnly(2026, 10, 25)),
        };
        var goal = new Goal { Name = "Car repair", CurrencyCode = "EUR", TargetAmount = 150_000, Protect = true };
        var protectedMoney = LiquidityCalculator.ProtectedMoney([goal], [new GoalAllocation { GoalId = goal.Id, AccountId = checking.Id, Amount = 100_000 }], ledger.Accounts, ledger.Entries, today);
        var forecast = ForecastCalculator.Compute(ledger.Accounts, ledger.Entries, plans, [], today, new DateOnly(2026, 10, 31));

        var result = Assert.Single(LiquidityCalculator.Compute(forecast, protectedMoney, today, 2_000, EstimatePeriod.Day, "EUR"));

        Assert.Equal(250_000, result.Start);
        Assert.Equal((67_000, new DateOnly(2026, 10, 24)), (result.Minimum, result.MinimumDate));
        Assert.Equal(-33_000, result.Headroom);
        Assert.Equal(48_000, result.EssentialToMinimum);
        Assert.Contains(result.ItemsAfter, i => i.Name == "Salary");
    }

    [Fact]
    public void K02_without_an_estimate_says_day_to_day_spending_is_not_included_and_a_balance_goal_protects_nothing()
    {
        var ledger = new LedgerBuilder();
        var today = new DateOnly(2026, 9, 30);
        var checking = ledger.Account("Main", 1_000, openingDate: new DateOnly(2026, 9, 1));
        var balanceGoal = new Goal { Name = "Main to 5,000", CurrencyCode = "EUR", TargetAmount = 500_000, Protect = true, Type = GoalType.AccountBalance, AccountId = checking.Id };

        var protectedMoney = LiquidityCalculator.ProtectedMoney([balanceGoal], [], ledger.Accounts, ledger.Entries, today);
        var result = Assert.Single(LiquidityCalculator.Compute(ForecastCalculator.Compute(ledger.Accounts, ledger.Entries, [], [], today, today.AddDays(30)), protectedMoney, today));

        Assert.False(result.IncludesEssential);
        Assert.Equal(0, result.Protected);
        Assert.Equal(100_000, result.Headroom);
    }

    [Fact]
    public void K07_essential_coverage_is_usable_money_over_the_median_essential_month()
    {
        var ledger = new LedgerBuilder();
        var checking = ledger.Account("Main", 6_000 + 1_450 + 1_500 + 1_620, openingDate: new DateOnly(2026, 6, 1));
        var housing = new Category { Kind = CategoryKind.Expense, Name = "Housing", IsEssential = true };
        var leisure = new Category { Kind = CategoryKind.Expense, Name = "Leisure" };
        ledger.Add(EntryKind.Expense, checking, 1_450, new DateOnly(2026, 7, 3), housing.Id);
        ledger.Add(EntryKind.Expense, checking, 1_500, new DateOnly(2026, 8, 3), housing.Id);
        ledger.Add(EntryKind.Expense, checking, 1_620, new DateOnly(2026, 9, 3), housing.Id);
        var today = new DateOnly(2026, 10, 12);

        var coverage = KpiCatalog.Coverage(ledger.Accounts, ledger.Entries, [housing, leisure], [], "EUR", today, PeriodCalendar.Gregorian);

        Assert.Equal(CoverageStatus.Ok, coverage.Status);
        Assert.Equal(150_000, coverage.MonthlyEssential);
        Assert.Equal(4.0m, coverage.Months);

        housing.IsEssential = false;
        Assert.Equal(CoverageStatus.NotAvailable, KpiCatalog.Coverage(ledger.Accounts, ledger.Entries, [housing, leisure], [], "EUR", today, PeriodCalendar.Gregorian).Status);
    }

    [Fact]
    public void K07_needs_three_complete_months()
    {
        var ledger = new LedgerBuilder();
        ledger.Account("Main", 6_000, openingDate: new DateOnly(2026, 9, 1));

        var coverage = KpiCatalog.Coverage(ledger.Accounts, ledger.Entries, [], [], "EUR", new DateOnly(2026, 10, 12), PeriodCalendar.Gregorian);

        Assert.Equal(CoverageStatus.NotEnoughHistory, coverage.Status);
        Assert.Null(coverage.Months);
    }

    [Fact]
    public void K09_installments_are_15_percent_of_income_with_70_interest()
    {
        var ledger = new LedgerBuilder();
        var checking = ledger.Account("Main", 5_000, openingDate: new DateOnly(2026, 9, 1));
        var loan = ledger.Account("Car loan", -16_800, AccountType.Loan, openingDate: new DateOnly(2026, 9, 1));
        loan.InterestRate = 5m;
        ledger.Add(EntryKind.Income, checking, 3_000, new DateOnly(2026, 10, 1));
        var plan = new Schedule
        {
            Name = "Installment", Kind = EntryKind.Transfer, AccountId = checking.Id, ToAccountId = loan.Id, Amount = 45_000,
            Rule = new RecurrenceRule { Frequency = Frequency.Monthly, Start = new DateOnly(2026, 10, 5) },
        };

        var burden = Assert.Single(KpiCatalog.Debt(ledger.Accounts, ledger.Entries, [plan], [], Oct1, new DateOnly(2026, 10, 31), Oct1));

        Assert.Equal((45_000, 7_000, 38_000), (burden.Installments, burden.Interest, burden.Principal));
        Assert.Equal(15m, burden.RatioPercent);
        Assert.Equal(new DateOnly(2026, 10, 5), burden.NextDate);
    }

    [Fact]
    public void K10_net_worth_example_and_an_unvalued_holding_makes_it_incomplete()
    {
        var ledger = new LedgerBuilder();
        ledger.Account("Main", 2_980);
        ledger.Account("Car loan", -4_000, AccountType.Loan);
        ledger.Account("Sara", 300, AccountType.Lent);
        var gold = new AssetType { Name = "Gold", PriceCurrencyCode = "EUR" };
        var coin = new AssetType { Name = "Coins", Dimension = AssetDimension.Count, PriceCurrencyCode = "EUR" };
        var place = Guid.NewGuid();
        var events = new List<AssetEvent>
        {
            new() { AssetTypeId = gold.Id, LocationId = place, Kind = AssetEventKind.Opening, Quantity = 10_000, Date = Oct1 },
            new() { AssetTypeId = coin.Id, LocationId = place, Kind = AssetEventKind.Opening, Quantity = 5_000, Date = Oct1 },
        };
        var prices = new List<AssetValuation> { new() { AssetTypeId = gold.Id, CurrencyCode = "EUR", Date = Oct1, PricePerUnitMilli = 10_000_000 } };

        var worth = NetWorthCalculator.Compute(ledger.Accounts, ledger.Entries, [gold, coin], events, prices, Oct1);

        Assert.Equal(28_000, Assert.Single(worth.Totals).Total);
        Assert.True(worth.IsIncomplete);
        Assert.Equal(5_000, Assert.Single(worth.Unvalued).Quantity);
    }

    [Fact]
    public void K13_composition_shares_the_known_part_and_lists_a_currency_without_rate()
    {
        var ledger = new LedgerBuilder();
        ledger.Account("Main", 2_000);
        ledger.Account("Dollar", 4_000, currency: "USD");
        var gold = new AssetType { Name = "Gold", PriceCurrencyCode = "EUR" };
        var events = new List<AssetEvent> { new() { AssetTypeId = gold.Id, LocationId = Guid.NewGuid(), Kind = AssetEventKind.Opening, Quantity = 10_000, Date = Oct1 } };
        var prices = new List<AssetValuation> { new() { AssetTypeId = gold.Id, CurrencyCode = "EUR", Date = Oct1, PricePerUnitMilli = 10_000_000 } };
        var worth = NetWorthCalculator.Compute(ledger.Accounts, ledger.Entries, [gold], events, prices, Oct1);
        var rate = new ExchangeRate { FromCurrencyCode = "USD", ToCurrencyCode = "EUR", Rate = 0.92m, Date = Oct1 };

        var composition = NetWorthCalculator.Compose(worth, new RateTable([rate]), "EUR");
        var withoutRate = NetWorthCalculator.Compose(worth, new RateTable([]), "EUR");

        Assert.Equal([55m, 30m, 15m], composition.Slices.Select(s => Math.Round(s.SharePercent, 0, MidpointRounding.AwayFromZero)));
        Assert.Equal(["EUR", "Gold"], withoutRate.Slices.Select(s => s.Label).Order());
        Assert.Equal(["USD"], withoutRate.MissingRates);
    }

    [Fact]
    public void K12_open_receivables_with_age_buckets()
    {
        var ledger = new LedgerBuilder();
        var today = new DateOnly(2026, 10, 13);
        var checking = ledger.Account("Main", 2_000, openingDate: new DateOnly(2026, 6, 1));
        var sara = ledger.Account("Sara", 0, AccountType.Lent, openingDate: new DateOnly(2026, 6, 1));
        sara.Counterparty = "Sara";
        var hotel = ledger.Add(EntryKind.Expense, checking, 180, today.AddDays(-12));
        hotel.ReimbursableAmount = 18_000;
        hotel.ReimbursedBy = "Employer";
        ledger.Transfer(checking, sara, 300, date: today.AddDays(-95));

        var total = Assert.Single(KpiCatalog.Receivables(ledger.Accounts, ledger.Entries, today));

        Assert.Equal(48_000, total.Total);
        Assert.Equal(1, total.Over90Count);
        Assert.Equal((18_000, 0, 30_000), (total.UpTo30Days, total.UpTo90Days, total.Over90Days));
    }

    [Fact]
    public void G15_suggestions_never_exceed_the_capacity_and_follow_priority_and_date()
    {
        var today = new DateOnly(2026, 10, 1);
        var needs = new[]
        {
            new GoalNeed(Guid.NewGuid(), GoalPriority.Normal, today.AddMonths(6), 15_000),
            new GoalNeed(Guid.NewGuid(), GoalPriority.High, today.AddMonths(12), 20_000),
            new GoalNeed(Guid.NewGuid(), GoalPriority.Normal, today.AddMonths(3), 10_000),
        };

        var split = CapacityCalculator.Distribute(30_000, needs);

        Assert.True(split.Sum(s => s.Suggested) <= 30_000);
        Assert.Equal([20_000, 10_000, 0], split.Select(s => s.Suggested));
        Assert.Equal(needs[1].GoalId, split[0].GoalId);
    }

    [Fact]
    public void Capacity_is_income_minus_spending_non_monthly_shares_and_other_goal_plans()
    {
        var ledger = new LedgerBuilder();
        var checking = ledger.Account("Main", 1_000, openingDate: new DateOnly(2026, 6, 1));
        foreach (var month in new[] { 7, 8, 9 })
        {
            ledger.Add(EntryKind.Income, checking, 3_000, new DateOnly(2026, month, 1));
            ledger.Add(EntryKind.Expense, checking, 2_000, new DateOnly(2026, month, 10));
        }

        var insurance = new Schedule
        {
            Name = "Insurance", Kind = EntryKind.Expense, AccountId = checking.Id, Amount = 60_000,
            Rule = new RecurrenceRule { Frequency = Frequency.Monthly, Interval = 12, Start = new DateOnly(2027, 1, 15) },
        };
        var travel = new Goal { Name = "Travel", CurrencyCode = "EUR", TargetAmount = 100_000 };
        var plan = new ContributionPlan { GoalId = travel.Id, Method = ContributionMethod.FixedAmount, Amount = 10_000, Rule = new RecurrenceRule { Frequency = Frequency.Monthly, Start = new DateOnly(2026, 10, 1) } };

        var capacity = CapacityCalculator.Compute(ledger.Accounts, ledger.Entries, [insurance], [], [travel], [plan], "EUR", new DateOnly(2026, 10, 12), PeriodCalendar.Gregorian);

        Assert.Equal(300_000 - 200_000 - 5_000 - 10_000, capacity.Amount);
        Assert.Null(CapacityCalculator.Compute(ledger.Accounts, ledger.Entries, [], [], [], [], "EUR", new DateOnly(2026, 8, 12), PeriodCalendar.Gregorian).Amount);
    }

    [Fact]
    public void K14_lists_an_old_backup_and_an_account_not_reconciled_for_70_days()
    {
        var ledger = new LedgerBuilder();
        var today = new DateOnly(2026, 10, 12);
        var checking = ledger.Account("Main", 1_000, openingDate: new DateOnly(2026, 1, 1));
        checking.LastReconciledOn = today.AddDays(-70);
        ledger.Add(EntryKind.Expense, checking, 10, today, review: ReviewState.Unreviewed);

        var issues = DataStatus.Check(ledger.Accounts, ledger.Entries, [], new LedgerFilter(Oct1, today), today, today.AddDays(-34));

        Assert.Equal([DataIssueKind.BackupOld, DataIssueKind.Unreviewed, DataIssueKind.NotReconciled, DataIssueKind.WithoutCategory], issues.Select(i => i.Kind));

        checking.LastReconciledOn = today;
        ledger.Entries.Clear();
        Assert.Empty(DataStatus.Check(ledger.Accounts, ledger.Entries, [], new LedgerFilter(Oct1, today), today, today));
    }
}
