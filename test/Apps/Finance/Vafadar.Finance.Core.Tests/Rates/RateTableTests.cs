using Vafadar.Finance.Core.Rates;

namespace Vafadar.Finance.Core.Tests.Rates;

public sealed class RateTableTests
{
    private static readonly DateOnly Day = new(2027, 3, 15);

    [Fact]
    public void The_latest_rate_on_or_before_the_date_is_used()
    {
        var table = new RateTable([Rate("USD", "EUR", 0.90m, Day.AddDays(-10)), Rate("USD", "EUR", 0.92m, Day.AddDays(-1)), Rate("USD", "EUR", 0.95m, Day.AddDays(3))]);

        Assert.True(table.TryConvert(10_000, "USD", "EUR", Day, out var converted, out var rateDate));
        Assert.Equal(9_200, converted);
        Assert.Equal(Day.AddDays(-1), rateDate);
    }

    [Fact]
    public void A_rate_in_the_other_direction_is_inverted()
    {
        var table = new RateTable([Rate("EUR", "USD", 1.25m, Day)]);

        Assert.True(table.TryConvert(12_500, "USD", "EUR", Day, out var converted, out _));
        Assert.Equal(10_000, converted);
    }

    [Fact]
    [Trait("AT", "AT-43")]
    public void Without_a_rate_there_is_no_conversion_and_no_implicit_one_to_one()
    {
        var table = new RateTable([Rate("USD", "EUR", 0.9m, Day.AddDays(1))]);

        Assert.False(table.TryConvert(10_000, "USD", "EUR", Day, out _, out _));
        Assert.False(table.TryConvert(10_000, "GBP", "EUR", Day, out _, out _));
    }

    [Fact]
    [Trait("AT", "AT-46")]
    public void Combined_totals_are_incomplete_when_a_rate_is_missing()
    {
        var table = new RateTable([Rate("USD", "EUR", 0.9m, Day.AddDays(-2))]);
        var balances = new Dictionary<string, long> { ["EUR"] = 100_00, ["USD"] = 50_00, ["GBP"] = 10_00 };

        var combined = table.Combine(balances, "EUR", Day);
        var withoutGbp = table.Combine(new Dictionary<string, long> { ["EUR"] = 100_00, ["USD"] = 50_00 }, "EUR", Day);

        Assert.False(combined.IsComplete);
        Assert.Equal(["GBP"], combined.MissingCurrencies);
        Assert.Equal(145_00, withoutGbp.Total);
        Assert.Equal(Day.AddDays(-2), withoutGbp.OldestRateDate);
    }

    [Fact]
    [Trait("AT", "AT-47")]
    public void Conversion_respects_the_minor_digits_of_both_currencies()
    {
        var table = new RateTable([Rate("EUR", "JPY", 161.5m, Day), Rate("KWD", "EUR", 2.95m, Day)]);

        Assert.True(table.TryConvert(1_050, "EUR", "JPY", Day, out var yen, out _));
        Assert.Equal(1_696, yen);
        Assert.True(table.TryConvert(1_234, "KWD", "EUR", Day, out var euro, out _));
        Assert.Equal(364, euro);
    }

    private static ExchangeRate Rate(string from, string to, decimal rate, DateOnly date) =>
        new() { FromCurrencyCode = from, ToCurrencyCode = to, Rate = rate, Date = date };
}
