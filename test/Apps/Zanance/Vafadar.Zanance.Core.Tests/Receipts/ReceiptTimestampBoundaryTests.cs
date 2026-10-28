using Vafadar.Zanance.Core.Receipts;

namespace Vafadar.Zanance.Core.Tests.Receipts;

/// <summary>Complete printed dates with bounded clock suffixes cannot stand in for a missing merchant.</summary>
public sealed class ReceiptTimestampBoundaryTests
{
    /// <summary>Preserves the complete printed date and total while leaving an absent merchant empty.</summary>
    [Theory]
    [InlineData("27 OCT 2025 13:42")]
    [InlineData("27. Oktober 2025 13:42:59")]
    [InlineData("27 octobre 2025 00:00")]
    [InlineData("27 ottobre 2025 23:59:59")]
    [InlineData("27 octubre 2025 9:05")]
    [InlineData("27.10.2025 13:42")]
    [InlineData("2025-10-27 13:42:59")]
    [InlineData("27/10/2025 00:00")]
    public void A_complete_date_and_clock_before_items_is_not_a_merchant(string printed)
    {
        var result = ReceiptParser.Parse(printed + "\nBREAD EUR 7.10\nTOTAL EUR 7.10", new DateOnly(2026, 10, 28));

        Assert.Null(result.Merchant);
        Assert.Equal(new DateOnly(2025, 10, 27), result.Date);
        Assert.Equal(ReceiptAmountStatus.Found, result.AmountStatus);
        Assert.Equal(7.10m, result.Amount);
        Assert.True(Assert.Single(result.Candidates).TryMinor("EUR", out var minor));
        Assert.Equal(710, minor);
    }

    /// <summary>A real header stays intact even when it happens to end with a valid clock.</summary>
    [Theory]
    [InlineData("BAY MARKET")]
    [InlineData("Studio 54 13:42")]
    [InlineData("27 OCT 2025 MARKET 13:42")]
    public void A_real_merchant_before_a_timestamp_is_not_trimmed(string merchant)
    {
        var result = ReceiptParser.Parse(merchant + "\n27 OCT 2025 13:42\nTOTAL EUR 7.10");

        Assert.Equal(merchant, result.Merchant);
        Assert.Equal(new DateOnly(2025, 10, 27), result.Date);
        Assert.Equal(7.10m, result.Amount);
    }

    /// <summary>Never repairs an invalid clock or contextual row to invent a complete source-named date.</summary>
    [Theory]
    [InlineData("27 OCT 2025 24:00")]
    [InlineData("27 OCT 2025 13:60")]
    [InlineData("27 OCT 2025 13:42:60")]
    [InlineData("27 OCT 2025 13:42 CET")]
    [InlineData("Date: 27 OCT 2025 13:42")]
    [InlineData("31 February 2025 13:42")]
    public void Unsupported_timestamp_rows_preserve_manual_date_review(string source)
    {
        var result = ReceiptParser.Parse("BAY MARKET\n" + source + "\nTOTAL EUR 7.10");

        Assert.Null(result.Date);
        Assert.Equal("BAY MARKET", result.Merchant);
        Assert.Equal(7.10m, result.Amount);
    }

    /// <summary>Timestamp support must preserve the original first-valid-numeric precedence.</summary>
    [Fact]
    public void A_numeric_date_still_precedes_a_different_named_timestamp()
    {
        var result = ReceiptParser.Parse("BAY MARKET\n27 OCT 2025 13:42\n28.09.2026\nTOTAL EUR 7.10");
        Assert.Equal(new DateOnly(2026, 9, 28), result.Date);
    }

    /// <summary>Different full timestamp dates remain conflicting rather than choosing the first row.</summary>
    [Fact]
    public void Conflicting_timestamp_dates_have_no_suggestion()
    {
        var result = ReceiptParser.Parse("BAY MARKET\n27 OCT 2025 13:42\n28 OCT 2025 13:42\nTOTAL EUR 7.10");
        Assert.Null(result.Date);
    }
}
