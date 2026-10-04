using System.Globalization;
using Vafadar.Zanance.Core.Holdings;

namespace Vafadar.Zanance.Core.Tests.CodeReview;

/// <summary>Regression tests of the code review, part 4: goals, holdings, reports and files.</summary>
public sealed class CodeReviewPart4Tests
{
    private static readonly AssetType Gold = new() { Name = "Gold", PriceCurrencyCode = "IRR" };

    [Theory]
    [InlineData("de-DE", "1.5", 1_500)]
    [InlineData("de-DE", "1,5", 1_500)]
    [InlineData("de-DE", "1.500", 1_500_000)]
    [InlineData("de-DE", "1.250,5", 1_250_500)]
    [InlineData("en-US", "1.5", 1_500)]
    [InlineData("en-US", "1,500", 1_500_000)]
    [InlineData("en-US", "1.500", 1_500)]
    [InlineData("fa-IR", "۱٫۵", 1_500)]
    [InlineData("fa-IR", "1,500", 1_500_000)]
    [InlineData("de-DE", "50", 50_000)]
    public void A_typed_quantity_follows_the_conventions_of_the_language(string culture, string text, long expectedMg)
    {
        // CR04-01: "1.5" in German was 15 g – the group separator was removed before the decimal point was read.
        Assert.True(Quantities.TryParse(text, QuantityUnit.Gram, Gold, CultureInfo.GetCultureInfo(culture), out var mg));
        Assert.Equal(expectedMg, mg);
    }

    [Theory]
    [InlineData("1.5.5")]
    [InlineData("1,2345")]
    [InlineData("12,34,5")]
    [InlineData("1.5,5,5")]
    [InlineData("-2")]
    [InlineData("0")]
    public void An_ambiguous_or_invalid_quantity_is_refused(string text) =>
        Assert.False(Quantities.TryParse(text, QuantityUnit.Gram, Gold, CultureInfo.GetCultureInfo("en-US"), out _));

    [Fact]
    public void The_price_per_gram_of_a_holding_in_rials_does_not_overflow()
    {
        // CR04-02: 100 g bought for 500 billion rials; basis × 1,000,000 does not fit a 64-bit number.
        var purchase = new AssetEvent { AssetTypeId = Gold.Id, Kind = AssetEventKind.Purchase, Quantity = 100_000, BasisAmount = 50_000_000_000_000, Date = new DateOnly(2026, 10, 1) };

        var basis = HoldingsLedger.Basis([purchase], Gold.Id);

        Assert.Equal(500_000_000_000_000, basis.PerUnitMilli);
    }
}