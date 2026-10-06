using System.Globalization;
using Vafadar.Zanance.Core.Money;

namespace Vafadar.Zanance.Core.Tests.Money;

/// <summary>Italian amounts: decimal comma, dots or (no-break) spaces between thousands, no sign, no guessing.</summary>
[Trait("AT", "AT-47")]
public sealed class ItalianAmountTests
{
    private static readonly CultureInfo Italian = CultureInfo.GetCultureInfo("it");

    [Theory]
    [InlineData("1234,56", "EUR", 123456)]
    [InlineData("1.234,56", "EUR", 123456)]
    [InlineData("1 234,56", "EUR", 123456)]
    [InlineData("1 234,56", "EUR", 123456)]
    [InlineData("1 234,56", "EUR", 123456)]
    [InlineData("1.234", "EUR", 123400)]
    [InlineData("1.234,567", "KWD", 1234567)]
    [InlineData("1.234", "JPY", 1234)]
    [InlineData("0", "EUR", 0)]
    public void Parses(string text, string currency, long expected)
    {
        Assert.True(MoneyAmount.TryParse(text, Currencies.Get(currency), Italian, out var minor));
        Assert.Equal(expected, minor);
    }

    [Theory]
    [InlineData("1,234", "EUR")]      // three decimals for a two-decimal currency: never silently 1234
    [InlineData("12.34,56", "EUR")]   // broken thousands groups
    [InlineData("12,50", "JPY")]      // the yen has no decimals
    [InlineData("-12,50", "EUR")]     // the entry kind carries the direction
    [InlineData("12,50 EUR", "EUR")]  // the currency is chosen, not typed
    [InlineData("12,505", "EUR")]     // never rounded
    [InlineData("99999999999999999999999999", "EUR")]
    public void Rejects(string text, string currency)
    {
        Assert.False(MoneyAmount.TryParse(text, Currencies.Get(currency), Italian, out _));
    }

    [Theory]
    [InlineData(123456L, "EUR")]
    [InlineData(1234L, "JPY")]
    [InlineData(1234567L, "KWD")]
    public void An_amount_shown_for_input_reads_back_unchanged(long minor, string currency)
    {
        var text = MoneyText.ForInput(minor, currency, Italian);

        Assert.True(MoneyAmount.TryParse(text, Currencies.Get(currency), Italian, out var parsed));
        Assert.Equal(minor, parsed);
    }
}
