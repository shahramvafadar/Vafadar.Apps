using System.Text.Json;
using System.Text.Json.Serialization;
using Vafadar.Zanance.Core.Rates;

namespace Vafadar.Zanance.Core.Tests.Rates;

/// <summary>Completed converted nets retain exact cancellation and the original per-currency evidence and limits.</summary>
public sealed class ConvertedNetCompletionTests
{
    private static readonly DateOnly Day = new(2026, 11, 6);
    private static readonly int[][] Orders = [[0, 1, 2], [0, 2, 1], [1, 0, 2], [1, 2, 0], [2, 0, 1], [2, 1, 0]];

    /// <summary>All currency orders publish the supported signed net with their original dated evidence order.</summary>
    [Theory]
    [InlineData(false, 0)] [InlineData(false, 1)] [InlineData(false, 2)]
    [InlineData(false, 3)] [InlineData(false, 4)] [InlineData(false, 5)]
    [InlineData(true, 0)] [InlineData(true, 1)] [InlineData(true, 2)]
    [InlineData(true, 3)] [InlineData(true, 4)] [InlineData(true, 5)]
    public void Every_original_currency_order_completes(bool negative, int permutation)
    {
        string[] codes = ["EUR", "USD", "GBP"];
        long[] amounts = negative ? [long.MinValue, -long.MaxValue, long.MaxValue] : [long.MaxValue, long.MaxValue, -long.MaxValue];
        var values = Orders[permutation].ToDictionary(i => codes[i], i => amounts[i]);
        var rates = UnitRates();
        var before = Capture(values, rates);
        var result = new RateTable(rates).Combine(values, "eUr", Day);
        Assert.Equal(negative ? long.MinValue : long.MaxValue, result.Total);
        Assert.Equal("eUr", result.CurrencyCode);
        Assert.True(result.IsComplete);
        Assert.True(result.HasEstimate);
        Assert.True(result.IsOutdated);
        Assert.Equal(Day.AddDays(-40), result.OldestRateDate);
        Assert.Equal(values.Keys.Where(code => code != "EUR").ToArray(), result.Rates.Select(rate => rate.CurrencyCode).ToArray());
        Assert.Equal(new RateInfo("USD", Day.AddDays(-40), true, true), result.Rates.Single(rate => rate.CurrencyCode == "USD"));
        Assert.Equal(new RateInfo("GBP", Day.AddDays(-1), false, false), result.Rates.Single(rate => rate.CurrencyCode == "GBP"));
        Assert.Empty(result.MissingCurrencies);
        Assert.Equal(before, Capture(values, rates));
    }

    /// <summary>A genuinely unsupported known subtotal refuses even if a missing currency could offset a guess.</summary>
    [Theory]
    [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public void Unsupported_completed_known_net_still_refuses(bool negative, bool missing)
    {
        var values = new Dictionary<string, long> { ["EUR"] = negative ? long.MinValue : long.MaxValue, ["USD"] = negative ? -1 : 1 };
        if (missing) values.Add("CHF", negative ? long.MaxValue : -long.MaxValue);
        var rates = UnitRates();
        var before = Capture(values, rates);
        Assert.Throws<OverflowException>(() => new RateTable(rates).Combine(values, "EUR", Day));
        Assert.Equal(before, Capture(values, rates));
    }

    /// <summary>Each individual conversion must still fit before any other currency can offset it.</summary>
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void An_unsupported_individual_conversion_is_not_rescued(bool negative)
    {
        var values = new Dictionary<string, long> { ["EUR"] = negative ? long.MaxValue : -long.MaxValue, ["USD"] = negative ? long.MinValue : long.MaxValue };
        ExchangeRate[] rates = [Rate("USD", 2m, 0)];
        var before = Capture(values, rates);
        Assert.Throws<OverflowException>(() => new RateTable(rates).Combine(values, "EUR", Day));
        Assert.Equal(before, Capture(values, rates));
    }

    /// <summary>Missing conversion evidence still hides the total after valid known-currency cancellation.</summary>
    [Fact]
    public void Missing_rate_retains_original_incomplete_evidence()
    {
        var values = new Dictionary<string, long> { ["EUR"] = long.MaxValue, ["USD"] = long.MaxValue, ["GBP"] = -long.MaxValue, ["CHF"] = 1 };
        var rates = UnitRates();
        var before = Capture(values, rates);
        var result = new RateTable(rates).Combine(values, "EUR", Day);
        Assert.Null(result.Total);
        Assert.False(result.IsComplete);
        Assert.Equal(new[] { "CHF" }, result.MissingCurrencies.ToArray());
        Assert.Equal(new[] { "USD", "GBP", "CHF" }, result.Rates.Select(rate => rate.CurrencyCode).ToArray());
        Assert.Equal(new RateInfo("CHF", null, false, false), result.Rates.Last());
        Assert.Equal(Day.AddDays(-40), result.OldestRateDate);
        Assert.True(result.HasEstimate);
        Assert.Equal(before, Capture(values, rates));
    }

    /// <summary>Rounding remains per currency and future rates never fill even a zero-valued missing currency.</summary>
    [Fact]
    public void Per_currency_rounding_and_future_rate_refusal_remain()
    {
        var values = new Dictionary<string, long> { ["USD"] = 1, ["GBP"] = 1 };
        ExchangeRate[] rates = [Rate("USD", .5m, -2), Rate("GBP", .5m, -1), Rate("USD", 100m, 1), Rate("CHF", 1m, 1)];
        var before = Capture(values, rates);
        var table = new RateTable(rates);
        Assert.Equal(2, table.Combine(values, "EUR", Day).Total);
        Assert.Equal(before, Capture(values, rates));
        values.Add("CHF", 0);
        var withMissing = Capture(values, rates);
        var result = table.Combine(values, "EUR", Day);
        Assert.Null(result.Total);
        Assert.Equal(new[] { "CHF" }, result.MissingCurrencies.ToArray());
        Assert.Equal(withMissing, Capture(values, rates));
    }

    private static ExchangeRate[] UnitRates() => [Rate("USD", 1m, -40, true), Rate("GBP", 1m, -1)];
    private static ExchangeRate Rate(string from, decimal value, int offset, bool estimate = false) => new()
    { FromCurrencyCode = from, ToCurrencyCode = "EUR", Rate = value, Date = Day.AddDays(offset), IsEstimate = estimate };
    private static string Capture(Dictionary<string, long> values, ExchangeRate[] rates) =>
        JsonSerializer.Serialize(new ConvertedNetInputs(values, rates), ConvertedNetJson.Default.ConvertedNetInputs);
}

/// <summary>Complete in-memory original rate and ordered amount input used by the native source comparison.</summary>
internal sealed record ConvertedNetInputs(Dictionary<string, long> Values, ExchangeRate[] Rates);

/// <summary>Preserves original input properties without reflection-only metadata in native execution.</summary>
[JsonSerializable(typeof(ConvertedNetInputs))]
internal sealed partial class ConvertedNetJson : JsonSerializerContext;
