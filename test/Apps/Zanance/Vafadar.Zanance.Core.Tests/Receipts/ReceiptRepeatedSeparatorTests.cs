using Vafadar.Zanance.Core.Receipts;

namespace Vafadar.Zanance.Core.Tests.Receipts;

/// <summary>Repeated identical separators cannot hide an incomplete final thousands group.</summary>
public sealed class ReceiptRepeatedSeparatorTests
{
    /// <summary>Rejects a short final group instead of joining every digit into a larger amount.</summary>
    [Theory]
    [InlineData("1,234,56")]
    [InlineData("1.234.56")]
    [InlineData("1,234,5")]
    [InlineData("1.234.5")]
    [InlineData("12,345,67")]
    [InlineData("12.345.67")]
    [InlineData("1,234,567,89")]
    [InlineData("1.234.567.89")]
    public void A_repeated_single_separator_requires_complete_thousands_groups(string text)
    {
        Assert.False(ReceiptParser.TryAmount(text, out var amount));
        Assert.Equal(0m, amount);
    }

    /// <summary>Keeps the complete damaged total as review evidence without salvaging a candidate.</summary>
    [Theory]
    [InlineData("1,234,56")]
    [InlineData("1.234.56")]
    [InlineData("1,234,5")]
    [InlineData("1.234.5")]
    [InlineData("12,345,67")]
    [InlineData("12.345.67")]
    [InlineData("1,234,567,89")]
    [InlineData("1.234.567.89")]
    public void A_damaged_total_does_not_become_a_confident_large_amount(string amount)
    {
        var row = "TOTAL " + amount + " EUR";
        var result = ReceiptParser.Parse("NORTH HARBOR MARKET\n" + row, new DateOnly(2026, 11, 2));

        Assert.Null(result.Amount);
        Assert.Equal(ReceiptAmountStatus.Review, result.AmountStatus);
        Assert.Empty(result.Candidates);
        Assert.Equal(row, result.AmountSource);
        Assert.Equal("NORTH HARBOR MARKET", result.Merchant);
        Assert.Null(result.Date);
    }

    /// <summary>Preserves complete thousands groups and a separate decimal separator.</summary>
    [Theory]
    [InlineData("1,234,567.89", "1234567.89")]
    [InlineData("1.234.567,89", "1234567.89")]
    [InlineData("123,456,789", "123456789")]
    [InlineData("123.456.789", "123456789")]
    public void Complete_grouping_and_a_distinct_decimal_separator_keep_their_value(string text, string expected)
    {
        Assert.True(ReceiptParser.TryAmount(text, out var amount));
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), amount);
    }

    /// <summary>Retains damaged evidence even when a different complete total is also present.</summary>
    [Theory]
    [InlineData("1,234,56")]
    [InlineData("1.234.56")]
    public void A_later_valid_total_does_not_erase_the_damaged_original_row(string amount)
    {
        var damaged = "TOTAL " + amount + " EUR";
        var result = ReceiptParser.Parse(damaged + "\nTOTAL 7.10 EUR", new DateOnly(2026, 11, 2));

        Assert.Null(result.Amount);
        Assert.Equal(ReceiptAmountStatus.Review, result.AmountStatus);
        var candidate = Assert.Single(result.Candidates);
        Assert.Equal(7.10m, candidate.Amount);
        Assert.Equal("TOTAL 7.10 EUR", candidate.Source);
        Assert.Equal(damaged + "\nTOTAL 7.10 EUR", result.AmountSource);
    }

    /// <summary>Applies the same refusal after Persian and Arabic digit normalization.</summary>
    [Theory]
    [InlineData("جمع کل ۱٬۲۳۴٬۵۶ ریال")]
    [InlineData("TOTAL ١٬٢٣٤٬٥٦ EUR")]
    public void Native_digit_normalization_does_not_repair_a_damaged_group(string text)
    {
        var result = ReceiptParser.Parse(text, new DateOnly(2026, 11, 2));
        Assert.Null(result.Amount);
        Assert.Equal(ReceiptAmountStatus.Review, result.AmountStatus);
        Assert.Empty(result.Candidates);
    }
}
