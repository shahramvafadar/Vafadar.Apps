using System.Globalization;
using Vafadar.Zanance.Core.Money;

namespace Vafadar.Zanance.Core.Tests.Money;

/// <summary>French amounts: decimal comma and a space, no-break space or narrow no-break space between thousands.</summary>
[Trait("AT", "AT-47")]
public sealed class FrenchAmountTests
{
    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr");

    [Theory]
    [InlineData("12,50", "EUR", 1250)]
    [InlineData("1234,56", "EUR", 123456)]
    [InlineData("1 234,56", "EUR", 123456)]
    [InlineData("1 234,56", "EUR", 123456)]
    [InlineData("1 234,56", "EUR", 123456)]
    [InlineData("0,001", "KWD", 1)]
    [InlineData("100", "JPY", 100)]
    public void Parses(string text, string currency, long expected)
    {
        Assert.True(MoneyAmount.TryParse(text, Currencies.Get(currency), French, out var minor));
        Assert.Equal(expected, minor);
    }

    [Theory]
    [InlineData("1,234")]   // three decimals with a decimal comma: never silently 1234
    [InlineData("12,345")]
    [InlineData("-12,50")]  // the entry kind carries the direction
    public void Rejects(string text)
    {
        Assert.False(MoneyAmount.TryParse(text, Currencies.Euro, French, out _));
    }

    [Theory]
    [InlineData(123456L, "EUR")]
    [InlineData(100L, "JPY")]
    [InlineData(1L, "KWD")]
    public void An_amount_shown_for_input_reads_back_unchanged(long minor, string currency)
    {
        var text = MoneyText.ForInput(minor, currency, French);

        Assert.True(MoneyAmount.TryParse(text, Currencies.Get(currency), French, out var parsed));
        Assert.Equal(minor, parsed);
    }
}
