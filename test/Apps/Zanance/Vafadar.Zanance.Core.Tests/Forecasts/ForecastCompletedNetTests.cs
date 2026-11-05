using System.Text.Json;
using System.Text.Json.Serialization;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Forecasts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.Core.Tests.Forecasts;

/// <summary>Forecasts narrow completed currency/day results and exact transfer ratios, never arbitrary storage prefixes.</summary>
public sealed class ForecastCompletedNetTests
{
    private static readonly DateOnly Day = new(2026, 11, 5);
    private static Account Account(long opening = 0, string currency = "EUR") => new()
    { Name = "Forecast boundary", CurrencyCode = currency, Type = AccountType.Checking, OpeningDate = Day.AddDays(-1), OpeningBalance = opening };
    private static readonly int[][] Orders = [[0, 1, 2], [0, 2, 1], [1, 0, 2], [1, 2, 0], [2, 0, 1], [2, 1, 0]];

    /// <summary>All account source permutations retain the exact completed currency balance.</summary>
    [Theory]
    [InlineData(0, false)] [InlineData(1, false)] [InlineData(2, false)] [InlineData(3, false)] [InlineData(4, false)] [InlineData(5, false)]
    [InlineData(0, true)] [InlineData(1, true)] [InlineData(2, true)] [InlineData(3, true)] [InlineData(4, true)] [InlineData(5, true)]
    public void Starting_currency_balance_uses_the_complete_account_net(int permutation, bool negative)
    {
        long[] amounts = negative ? [long.MinValue, -1, 1] : [long.MaxValue, long.MaxValue, -long.MaxValue];
        var accounts = Orders[permutation].Select(index => Account(amounts[index])).ToArray();
        var before = JsonSerializer.Serialize(accounts, ForecastCompletedReadJson.Default.AccountArray);
        var forecast = Assert.Single(ForecastCalculator.Compute(accounts, [], [], [], Day, Day));
        var expected = negative ? long.MinValue : long.MaxValue;
        Assert.Equal(expected, forecast.StartBalance); Assert.Equal(expected, forecast.EndBalance);
        Assert.Equal(expected, forecast.Minimum); Assert.Equal(expected, Assert.Single(forecast.Path).Balance);
        Assert.Equal(before, JsonSerializer.Serialize(accounts, ForecastCompletedReadJson.Default.AccountArray));
    }

    /// <summary>Every source order gives the same completed signed day net without changing rows.</summary>
    [Theory]
    [InlineData(0, false)] [InlineData(1, false)] [InlineData(2, false)] [InlineData(3, false)] [InlineData(4, false)] [InlineData(5, false)]
    [InlineData(0, true)] [InlineData(1, true)] [InlineData(2, true)] [InlineData(3, true)] [InlineData(4, true)] [InlineData(5, true)]
    public void Same_day_effects_use_the_complete_net_in_every_source_order(int permutation, bool negative)
    {
        var account = Account();
        var kinds = negative ? new[] { EntryKind.Expense, EntryKind.Expense, EntryKind.Income }
            : new[] { EntryKind.Income, EntryKind.Income, EntryKind.Expense };
        var source = kinds.Select(kind => new LedgerEntry { AccountId = account.Id, Kind = kind, Amount = long.MaxValue, Date = Day.AddDays(1) }).ToArray();
        var entries = Orders[permutation].Select(index => source[index]).ToArray();
        var before = Capture([account], entries, []);
        var forecast = Assert.Single(ForecastCalculator.Compute([account], entries, [], [], Day, Day.AddDays(1)));
        var expected = negative ? -long.MaxValue : long.MaxValue;
        Assert.Equal(0, forecast.StartBalance); Assert.Equal(expected, forecast.EndBalance);
        Assert.Equal(new[] { 0L, expected }, forecast.Path.Select(point => point.Balance));
        Assert.Equal(entries.Select(entry => entry.Kind == EntryKind.Income ? entry.Amount : -entry.Amount), forecast.Items.Select(item => item.Effect!.Value));
        Assert.Equal(before, Capture([account], entries, []));
    }

    /// <summary>Only the final day balance, not its unpublished delta, must fit the public money type.</summary>
    [Theory, InlineData(false), InlineData(true)]
    public void Unpublished_day_delta_can_exceed_long_when_the_published_balance_fits(bool negative)
    {
        var account = Account(negative ? long.MaxValue : -long.MaxValue);
        var entries = Enumerable.Range(0, 2).Select(_ => new LedgerEntry { AccountId = account.Id,
            Kind = negative ? EntryKind.Expense : EntryKind.Income, Amount = long.MaxValue, Date = Day.AddDays(1) }).ToArray();
        var before = Capture([account], entries, []);
        var forecast = Assert.Single(ForecastCalculator.Compute([account], entries, [], [], Day, Day.AddDays(1)));
        Assert.Equal(negative ? -long.MaxValue : long.MaxValue, forecast.EndBalance);
        Assert.Equal(account.OpeningBalance, forecast.Path[0].Balance);
        Assert.Equal(before, Capture([account], entries, []));
    }

    /// <summary>Positive and negative genuinely invalid starting balances remain explicit failures.</summary>
    [Theory, InlineData(false), InlineData(true)]
    public void A_genuinely_unrepresentable_starting_currency_still_refuses(bool negative)
    {
        var accounts = new[] { Account(negative ? long.MinValue : long.MaxValue), Account(negative ? -1 : 1) };
        var before = JsonSerializer.Serialize(accounts, ForecastCompletedReadJson.Default.AccountArray);
        Assert.Throws<OverflowException>(() => ForecastCalculator.Compute(accounts, [], [], [], Day, Day));
        Assert.Equal(before, JsonSerializer.Serialize(accounts, ForecastCompletedReadJson.Default.AccountArray));
    }

    /// <summary>Large exact transfer products preserve the original nearest-minor midpoint-away rule.</summary>
    [Theory, InlineData(0), InlineData(1), InlineData(2), InlineData(3), InlineData(4), InlineData(5)]
    public void Transfer_ratio_divides_exactly_before_nearest_minor_away_rounding(int shape)
    {
        var amount = shape < 3 ? long.MaxValue : shape == 3 ? 2 : 3;
        var destination = shape == 1 ? long.MaxValue - 1 : shape < 4 ? long.MaxValue : shape == 4 ? 2 : 1;
        var used = shape == 2 ? long.MaxValue - 1 : shape < 3 ? long.MaxValue : 1;
        var expected = shape switch { 0 => long.MaxValue, 1 or 2 => long.MaxValue - 1, 3 => 4611686018427387904L, 4 => 1L, _ => 0L };
        var (accounts, plan) = Transfer(amount, destination);
        var scenario = new ForecastScenario(); scenario.SetAmount(plan.Id, plan.Rule.Start, used);
        var before = Capture(accounts, [], [plan]);
        var forecast = Assert.Single(ForecastCalculator.Compute(accounts, [], [plan], [], Day, Day.AddDays(1), [accounts[1].Id], scenario));
        Assert.Equal("USD", forecast.CurrencyCode); Assert.Equal(expected, forecast.EndBalance);
        var item = Assert.Single(forecast.Items); Assert.Equal(expected, item.Effect); Assert.True(item.IsAmountAssumed);
        Assert.Equal(before, Capture(accounts, [], [plan]));
    }

    /// <summary>Division cannot rescue a final transfer result outside the public money range.</summary>
    [Fact]
    public void A_genuinely_unrepresentable_transfer_ratio_still_refuses()
    {
        var (accounts, plan) = Transfer(1, long.MaxValue);
        var scenario = new ForecastScenario(); scenario.SetAmount(plan.Id, plan.Rule.Start, 2);
        var before = Capture(accounts, [], [plan]);
        Assert.Throws<OverflowException>(() => ForecastCalculator.Compute(accounts, [], [plan], [], Day, Day.AddDays(1), [accounts[1].Id], scenario));
        Assert.Equal(before, Capture(accounts, [], [plan]));
    }

    private static string Capture(Account[] accounts, LedgerEntry[] entries, Schedule[] plans) =>
        JsonSerializer.Serialize(accounts, ForecastCompletedReadJson.Default.AccountArray) +
        JsonSerializer.Serialize(entries, ForecastCompletedReadJson.Default.LedgerEntryArray) +
        JsonSerializer.Serialize(plans, ForecastCompletedReadJson.Default.ScheduleArray);

    private static (Account[] Accounts, Schedule Plan) Transfer(long amount, long destination)
    {
        var accounts = new[] { Account(), Account(currency: "USD") };
        return (accounts, new Schedule { Name = "Forecast transfer", Kind = EntryKind.Transfer, AccountId = accounts[0].Id,
            ToAccountId = accounts[1].Id, Amount = amount, ToAmount = destination,
            Rule = new RecurrenceRule { Start = Day.AddDays(1), Frequency = Frequency.Once } });
    }
}

[JsonSerializable(typeof(Account[]))]
[JsonSerializable(typeof(LedgerEntry[]))]
[JsonSerializable(typeof(Schedule[]))]
/// <summary>Provides bounded source-generated snapshots of the synthetic complete forecast inputs.</summary>
internal partial class ForecastCompletedReadJson : JsonSerializerContext { }
