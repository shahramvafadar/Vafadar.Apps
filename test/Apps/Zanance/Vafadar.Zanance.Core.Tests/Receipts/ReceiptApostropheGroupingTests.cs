using System.Globalization;
using Vafadar.Zanance.Core.Receipts;

namespace Vafadar.Zanance.Core.Tests.Receipts;

/// <summary>A complete grouped total cannot become equal shorter fragments (D-815).</summary>
public sealed class ReceiptApostropheGroupingTests
{
    [Theory]
    [InlineData("1'001.00", 1001)]
    [InlineData("12'012.00", 12012)]
    [InlineData("1\u2019001.00", 1001)]
    [InlineData("12\u2019012.00", 12012)]
    public void An_apostrophe_grouped_total_keeps_all_its_digits(string printed, int expected)
    {
        var receipt = ReceiptParser.Parse("ALPEN MARKET\nSUMME CHF " + printed, new DateOnly(2026, 11, 4));

        Assert.Equal((decimal)expected, receipt.Amount);
        Assert.Equal(ReceiptAmountStatus.Found, receipt.AmountStatus);
        Assert.Equal("ALPEN MARKET", receipt.Merchant);
        Assert.Contains(printed, receipt.AmountSource, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("1'234.56", "1234.56")]
    [InlineData("1\u2019234,56", "1234.56")]
    [InlineData("1'234'567.89", "1234567.89")]
    [InlineData("1\u2019234\u2019567,89", "1234567.89")]
    [InlineData("0'001.00", "1.00")]
    [InlineData("123'456.00", "123456.00")]
    [InlineData("1\u2019234'567.89", "1234567.89")]
    public void Complete_three_digit_groups_support_both_printed_apostrophes(string printed, string expected)
    {
        var receipt = ReceiptParser.Parse("ALPEN MARKET\nTOTAL CHF " + printed);

        Assert.Equal(decimal.Parse(expected, CultureInfo.InvariantCulture), receipt.Amount);
        Assert.Equal(ReceiptAmountStatus.Found, receipt.AmountStatus);
        Assert.Equal("CHF", Assert.Single(receipt.Candidates).CurrencyCode);
        Assert.Contains(printed, receipt.AmountSource, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("1'001.00")]
    [InlineData("1\u2019001.00")]
    public void A_detached_grouped_amount_stays_bound_to_its_explicit_total(string printed)
    {
        var receipt = ReceiptParser.Parse("ALPEN MARKET\nTOTAL\nCHF " + printed);

        Assert.Equal(1001m, receipt.Amount);
        Assert.Equal(ReceiptAmountStatus.Found, receipt.AmountStatus);
        Assert.Contains("TOTAL\nCHF " + printed, receipt.AmountSource, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("1'23.45")]
    [InlineData("1''001.00")]
    [InlineData("1234'567.89")]
    [InlineData("1'234'56")]
    [InlineData("1\u2019001,0O")]
    [InlineData("1'001.00A")]
    [InlineData("1'001.00'")]
    [InlineData("1'23.45 7.10")]
    public void A_damaged_group_never_authorizes_a_shorter_fragment(string printed)
    {
        var receipt = ReceiptParser.Parse("ALPEN MARKET\nTOTAL CHF " + printed);

        Assert.Null(receipt.Amount);
        Assert.Equal(ReceiptAmountStatus.Review, receipt.AmountStatus);
        Assert.Contains(printed, receipt.AmountSource, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("1 '001.00")]
    [InlineData("1' 001.00")]
    [InlineData("1' 001.00 7.10")]
    public void Whitespace_does_not_turn_a_broken_group_into_a_confident_fragment(string printed)
    {
        var receipt = ReceiptParser.Parse("ALPEN MARKET\nTOTAL CHF " + printed);

        Assert.Null(receipt.Amount);
        Assert.Equal(ReceiptAmountStatus.Review, receipt.AmountStatus);
    }

    [Fact]
    public void A_merchant_apostrophe_is_unrelated_to_its_ordinary_total()
    {
        var receipt = ReceiptParser.Parse("L'ALPEN MARKET\nTOTAL CHF 7.10");

        Assert.Equal(7.10m, receipt.Amount);
        Assert.Equal("L'ALPEN MARKET", receipt.Merchant);
    }

    [Theory]
    [InlineData("1'001.00")]
    [InlineData("1\u2019001.00")]
    public void An_item_price_still_cannot_substitute_for_a_total(string printed)
    {
        var receipt = ReceiptParser.Parse("ALPEN MARKET\nItem CHF " + printed);

        Assert.Null(receipt.Amount);
        Assert.Empty(receipt.Candidates);
    }
}
