using System.Globalization;
using Vafadar.Zanance.App.Features.Forecast;
using Vafadar.Zanance.Core.Money;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-436: actual forecast native-effect boundary keeps the exact original prompt interpretation.</summary>
[Trait("AT", "AT-436")]
[Collection("Entry validation units")]
public sealed class ForecastMoneyPromptTests
{
    [Theory, InlineData(false), InlineData(true)]
    public async Task Original_prompt_unit_and_initial_value_remain_the_parser_after_native_await(bool startsWithUnit)
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set(startsWithUnit ? [new("EUR", "Original hundred EUR", 2)] : []);
            var result = await ForecastAmountPrompt.ReadAsync("EUR", CultureInfo.GetCultureInfo("en-US"), 123400,
                async (unit, initial) =>
                {
                    Assert.Equal(startsWithUnit ? "Original hundred EUR" : "EUR", unit);
                    Assert.Equal(startsWithUnit ? "12.34" : "1234.00", initial);
                    await Task.Yield(); DisplayUnits.Set(startsWithUnit ? [] : [new("EUR", "Later hundred EUR", 2)]);
                    return "12.34";
                });
            Assert.Equal(startsWithUnit ? 123400 : 1234, result);
        }
        finally { DisplayUnits.Set(units); }
    }

    [Fact]
    public async Task Mutable_culture_changes_during_the_native_prompt_cannot_replace_the_shown_parser()
    {
        var culture = (CultureInfo)CultureInfo.GetCultureInfo("de-DE").Clone();
        var result = await ForecastAmountPrompt.ReadAsync("EUR", culture, null, async (_, initial) =>
        {
            Assert.Empty(initial); await Task.Yield(); culture.NumberFormat.NumberDecimalSeparator = ".";
            culture.NumberFormat.NumberGroupSeparator = ","; return "1.234";
        });
        Assert.Equal(123400, result);
    }

    [Theory, InlineData(null), InlineData(""), InlineData("not money"), InlineData("0"), InlineData("-1"), InlineData("9223372036854775808")]
    public async Task Cancelled_or_invalid_native_input_does_not_supply_a_scenario_amount(string? raw)
    {
        var result = await ForecastAmountPrompt.ReadAsync("EUR", CultureInfo.GetCultureInfo("en-US"), 1234,
            (_, _) => Task.FromResult(raw));
        Assert.Null(result);
    }
}
