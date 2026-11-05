using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Forecasts;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Reports;
using Vafadar.Zanance.Core.Settings;

namespace Vafadar.Zanance.Core.Tests.Reports;

/// <summary>Liquidity must keep exact protected coverage and refuse wrapped projected money.</summary>
public sealed class LiquidityBoundaryTests
{
    private static readonly DateOnly Today = new(2026, 11, 5);

    /// <summary>A deficit below the supported range must not appear as positive headroom.</summary>
    [Theory]
    [InlineData(long.MinValue, 1)]
    [InlineData(-long.MaxValue, 2)]
    public void An_unrepresentable_headroom_is_refused(long minimum, long protectedMoney) =>
        Assert.Throws<OverflowException>(() => Result(minimum, protectedMoney).Headroom);

    /// <summary>Supported boundary values remain exact.</summary>
    [Theory]
    [InlineData(long.MinValue, 0, long.MinValue)]
    [InlineData(long.MaxValue, long.MaxValue, 0)]
    public void Representable_headroom_remains_exact(long minimum, long protectedMoney, long expected) =>
        Assert.Equal(expected, Result(minimum, protectedMoney).Headroom);

    /// <summary>The day-to-day estimate cannot wrap a projected balance into positive money.</summary>
    [Fact]
    public void A_projected_liquidity_underflow_is_refused()
    {
        var forecast = Forecast(new ForecastPoint(Today, long.MinValue), new ForecastPoint(Today.AddDays(1), long.MinValue));
        Assert.Throws<OverflowException>(() => LiquidityCalculator.Compute([forecast], new Dictionary<string, long>(), Today, 1, EstimatePeriod.Day, "EUR"));
    }

    /// <summary>An overflowing early day must not be discarded by the minimum selection.</summary>
    [Fact]
    public void Minimum_selection_cannot_hide_an_unrepresentable_day()
    {
        var forecast = Forecast(new ForecastPoint(Today, long.MinValue), new ForecastPoint(Today.AddDays(1), long.MinValue), new ForecastPoint(Today.AddDays(2), 0));
        Assert.Throws<OverflowException>(() => LiquidityCalculator.Compute([forecast], new Dictionary<string, long>(), Today, 1, EstimatePeriod.Day, "EUR"));
    }

    /// <summary>One exact deduction at the negative limit remains supported.</summary>
    [Fact]
    public void A_representable_projected_boundary_keeps_its_original_day()
    {
        var forecast = Forecast(new ForecastPoint(Today, long.MinValue + 1), new ForecastPoint(Today.AddDays(1), long.MinValue + 1));
        var result = Assert.Single(LiquidityCalculator.Compute([forecast], new Dictionary<string, long>(), Today, 1, EstimatePeriod.Day, "EUR"));
        Assert.Equal(long.MinValue, result.Minimum); Assert.Equal(Today.AddDays(1), result.MinimumDate); Assert.Equal(1, result.EssentialToMinimum);
    }

    /// <summary>An estimate in another currency is still omitted instead of subtracted.</summary>
    [Fact]
    public void Another_currency_estimate_remains_outside_the_result()
    {
        var forecast = Forecast(new ForecastPoint(Today, long.MinValue), new ForecastPoint(Today.AddDays(1), long.MinValue));
        var result = Assert.Single(LiquidityCalculator.Compute([forecast], new Dictionary<string, long>(), Today, 1, EstimatePeriod.Day, "GBP"));
        Assert.Equal(long.MinValue, result.Minimum); Assert.False(result.IncludesEssential); Assert.Equal(0, result.EssentialToMinimum);
    }

    /// <summary>Today's balance does not pay the estimate again when no future path exists.</summary>
    [Fact]
    public void No_future_day_keeps_the_original_start_without_a_deduction()
    {
        var forecast = Forecast(new ForecastPoint(Today, long.MinValue));
        var result = Assert.Single(LiquidityCalculator.Compute([forecast], new Dictionary<string, long>(), Today, long.MaxValue, EstimatePeriod.Day, "EUR"));
        Assert.Equal(long.MinValue, result.Minimum); Assert.Equal(Today, result.MinimumDate); Assert.Equal(0, result.EssentialToMinimum);
    }

    /// <summary>The sum of independently covered accounts cannot wrap into negative protection.</summary>
    [Theory]
    [InlineData("EUR")]
    [InlineData("eur")]
    public void Combined_currency_protection_outside_money_is_refused(string secondCurrency)
    {
        var first = Account(long.MaxValue); var second = Account(1, secondCurrency); var goal = Goal();
        var allocations = new[] { Allocation(goal, first, long.MaxValue), Allocation(goal, second, 1) };
        Assert.Throws<OverflowException>(() => LiquidityCalculator.ProtectedMoney([goal], allocations, [first, second], [], Today));
    }

    /// <summary>Unordered allocation history may exceed long temporarily before exact release cancellation.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Allocation_cancellation_precedes_the_current_account_coverage_cap(bool reversePositiveRows)
    {
        var account = Account(long.MaxValue); var goal = Goal();
        var original = Allocation(goal, account, long.MaxValue, Today.AddDays(-3));
        var released = Allocation(goal, account, -2, Today.AddDays(-2));
        var restored = Allocation(goal, account, 2, Today.AddDays(-1));
        var allocations = reversePositiveRows ? new[] { restored, original, released } : [original, restored, released];
        var expense = new LedgerEntry { AccountId = account.Id, Amount = long.MaxValue - 100, Kind = EntryKind.Expense, Date = Today };
        var result = LiquidityCalculator.ProtectedMoney([goal], allocations, [account], [expense], Today);
        Assert.Equal(100, result["EUR"]); Assert.Equal(long.MaxValue, original.Amount); Assert.Equal(-2, released.Amount); Assert.Equal(2, restored.Amount);
    }

    /// <summary>A complete release still reserves no money.</summary>
    [Fact]
    public void Released_protection_remains_zero()
    {
        var account = Account(long.MaxValue); var goal = Goal();
        var result = LiquidityCalculator.ProtectedMoney([goal], [Allocation(goal, account, long.MaxValue), Allocation(goal, account, -long.MaxValue)], [account], [], Today);
        Assert.Equal(0, result["EUR"]);
    }

    /// <summary>Archived goals and unusable accounts remain excluded.</summary>
    [Fact]
    public void Original_protection_exclusions_are_preserved()
    {
        var account = Account(long.MaxValue); var goal = Goal(); goal.State = GoalState.Archived;
        Assert.Empty(LiquidityCalculator.ProtectedMoney([goal], [Allocation(goal, account, long.MaxValue)], [account], [], Today));
        goal.State = GoalState.Active; account.UsableForPayments = false;
        Assert.Empty(LiquidityCalculator.ProtectedMoney([goal], [Allocation(goal, account, long.MaxValue)], [account], [], Today));
    }

    /// <summary>A positive earmark remains bounded by the actual account balance.</summary>
    [Fact]
    public void Protection_keeps_the_existing_positive_balance_cap()
    {
        var account = Account(50); var goal = Goal();
        Assert.Equal(50, LiquidityCalculator.ProtectedMoney([goal], [Allocation(goal, account, 100)], [account], [], Today)["EUR"]);
    }

    /// <summary>The explanation partitions original forecast objects without altering their order or source.</summary>
    [Fact]
    public void Explanation_items_keep_original_identity_and_unknown_policy()
    {
        var unknown = new ForecastItem(Today.AddDays(1), "Unknown original plan", "EUR", null, ForecastSource.Plan, false, Guid.NewGuid(), Today.AddDays(1));
        var later = new ForecastItem(Today.AddDays(2), "Later original income", "EUR", 1, ForecastSource.FutureEntry, false, null, null);
        var forecast = Forecast(new ForecastPoint(Today, 100), new ForecastPoint(Today.AddDays(1), 99), new ForecastPoint(Today.AddDays(2), 100)) with { Items = [unknown, later] };
        var result = Assert.Single(LiquidityCalculator.Compute([forecast], new Dictionary<string, long>(), Today));
        Assert.Same(unknown, Assert.Single(result.Items)); Assert.Same(later, Assert.Single(result.ItemsAfter)); Assert.True(result.IsIncomplete); Assert.Equal(1, result.UnknownCount);
        Assert.Same(unknown, forecast.Items[0]); Assert.Same(later, forecast.Items[1]);
    }

    private static LiquidityResult Result(long minimum, long protectedMoney) => new("EUR", minimum, minimum, Today, protectedMoney, null, 0, 0, [], []);
    private static CurrencyForecast Forecast(params ForecastPoint[] path) => new("EUR", path[0].Balance, path[^1].Balance, path.Min(p => p.Balance), path.MinBy(p => p.Balance).Date, path, []);
    private static Account Account(long balance, string currency = "EUR") => new() { Name = "Original protected account", Type = AccountType.Checking, CurrencyCode = currency, OpeningDate = Today.AddDays(-10), OpeningBalance = balance, UsableForPayments = true };
    private static Goal Goal() => new() { Name = "Original protected target", CurrencyCode = "EUR", TargetAmount = long.MaxValue, Type = GoalType.Earmark, Protect = true };
    private static GoalAllocation Allocation(Goal goal, Account account, long amount, DateOnly? date = null) => new() { GoalId = goal.Id, AccountId = account.Id, Amount = amount, Date = date ?? Today };
}
