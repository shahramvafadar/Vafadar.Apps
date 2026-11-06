using System.Globalization;
using Vafadar.Zanance.Core.Money;

namespace Vafadar.Zanance.Core.Tests.Money;

/// <summary>Explicit signed balances keep the original positive grammar, full signed bounds and captured units.</summary>
public sealed class SignedMoneyInputBoundaryTests
{
    [Theory]
    [InlineData("en-US", "EUR", "-92233720368547758.08", long.MinValue)]
    [InlineData("de-DE", "EUR", "−92.233.720.368.547.758,08", long.MinValue)]
    [InlineData("fa-IR", "EUR", "−۹۲٬۲۳۳٬۷۲۰٬۳۶۸٬۵۴۷٬۷۵۸٫۰۸", long.MinValue)]
    [InlineData("en-US", "JPY", "-9223372036854775808", long.MinValue)]
    [InlineData("en-US", "KWD", "-9223372036854775.808", long.MinValue)]
    [InlineData("en-US", "EUR", "92233720368547758.07", long.MaxValue)]
    public void Signed_balance_retains_exact_original_minor_units(string culture, string code, string raw, long expected)
    {
        var context = MoneyInputContext.Capture(code, CultureInfo.GetCultureInfo(culture));
        Assert.True(context.TryParseSigned(raw, out var actual));
        Assert.Equal(expected, actual);
        if (raw.StartsWith('-') || raw.StartsWith('−')) Assert.False(context.TryParse(raw, out _));
    }

    [Theory]
    [InlineData("-92233720368547758.09")]
    [InlineData("92233720368547758.08")]
    [InlineData("+1")]
    [InlineData("--1")]
    [InlineData("−−1")]
    [InlineData("1-")]
    [InlineData("-1.001")]
    [InlineData("-1,23,45")]
    [InlineData("")]
    public void Invalid_signed_input_does_not_guess_round_or_wrap(string raw)
    {
        var context = MoneyInputContext.Capture("EUR", CultureInfo.GetCultureInfo("en-US"));
        Assert.False(context.TryParseSigned(raw, out var value));
        Assert.Equal(0, value);
    }

    [Fact]
    public void Original_unit_parser_stays_immutable_while_the_owner_must_recheck_changed_units()
    {
        var original = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([new("EUR", "Fictitious hundreds", 2)]);
            var context = MoneyInputContext.Capture("EUR", CultureInfo.GetCultureInfo("en-US"));
            var raw = "−" + context.ForInput(long.MinValue);
            DisplayUnits.Set([new("EUR", "Fictitious thousands", 3)]);
            Assert.False(context.MatchesCurrent("EUR"));
            Assert.True(context.TryParseSigned(raw, out var value));
            Assert.Equal(long.MinValue, value);
            Assert.False(context.TryParse(raw, out _));
        }
        finally { DisplayUnits.Set(original); }
    }
}
