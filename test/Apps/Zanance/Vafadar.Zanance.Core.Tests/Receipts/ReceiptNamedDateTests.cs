using Vafadar.Zanance.Core.Receipts;

namespace Vafadar.Zanance.Core.Tests.Receipts;

/// <summary>Full source-named dates remain suggestions without replacing original numeric date precedence.</summary>
public sealed class ReceiptNamedDateTests
{
    /// <summary>Names the source calendar and full year rather than guessing from the UI culture or current day.</summary>
    [Theory]
    [InlineData("28 OCT 2026", 2026, 10, 28)]
    [InlineData("27 OCT 2025", 2025, 10, 27)]
    [InlineData("27 October 2025", 2025, 10, 27)]
    [InlineData("27. Oktober 2025", 2025, 10, 27)]
    [InlineData("27 M\u00e4rz 2025", 2025, 3, 27)]
    [InlineData("27 octubre 2025", 2025, 10, 27)]
    [InlineData("27 octobre 2025", 2025, 10, 27)]
    [InlineData("27 ottobre 2025", 2025, 10, 27)]
    [InlineData("\u06f6 \u0645\u0647\u0631 \u06f1\u06f4\u06f0\u06f5", 2026, 9, 28)]
    [InlineData("\u0662\u0667 OCT \u0662\u0660\u0662\u0665", 2025, 10, 27)]
    [InlineData("29 February 2024", 2024, 2, 29)]
    public void A_complete_named_date_is_suggested_independently_of_today(string source, int year, int month, int day)
    {
        var result = ReceiptParser.Parse("BAY MARKET\n" + source + "\nTOTAL EUR 7.10", new DateOnly(2027, 1, 2));

        Assert.Equal(new DateOnly(year, month, day), result.Date);
        Assert.Equal("BAY MARKET", result.Merchant);
        Assert.Equal(ReceiptAmountStatus.Found, result.AmountStatus);
        Assert.Equal(7.10m, result.Amount);
    }

    /// <summary>Rejects invalid, incomplete, contextual and unsupported-year strings instead of filling a financial date.</summary>
    [Theory]
    [InlineData("31 February 2025")]
    [InlineData("29 February 2025")]
    [InlineData("28 OCT 26")]
    [InlineData("Date: 28 OCT 2025")]
    [InlineData("28 OCT 2025 MARKET")]
    [InlineData("28 OCT 1999")]
    [InlineData("28 OCT 2101")]
    [InlineData("28 OCT 1447")]
    public void A_noncomplete_named_source_does_not_invent_a_date(string source)
    {
        var result = ReceiptParser.Parse("BAY MARKET\n" + source + "\nTOTAL EUR 7.10", new DateOnly(2027, 1, 2));

        Assert.Null(result.Date);
        Assert.Equal(7.10m, result.Amount);
    }

    /// <summary>Repeated equivalent printed rows are one source date rather than a conflict.</summary>
    [Fact]
    public void Equivalent_named_rows_keep_one_date()
    {
        var result = ReceiptParser.Parse("BAY MARKET\n27 OCT 2025\n27 October 2025\nTOTAL EUR 7.10", new DateOnly(2027, 1, 2));
        Assert.Equal(new DateOnly(2025, 10, 27), result.Date);
    }

    /// <summary>Different named dates need manual review; neither row silently wins.</summary>
    [Fact]
    public void Conflicting_named_rows_have_no_date_suggestion()
    {
        var result = ReceiptParser.Parse("BAY MARKET\n27 OCT 2025\n28 OCT 2025\nTOTAL EUR 7.10", new DateOnly(2027, 1, 2));
        Assert.Null(result.Date);
    }

    /// <summary>Preserves the existing first valid numeric date regardless of a named row's position.</summary>
    [Theory]
    [InlineData("27 OCT 2025\n28.09.2026")]
    [InlineData("28.09.2026\n27 OCT 2025")]
    public void Original_numeric_priority_is_unchanged(string rows)
    {
        var result = ReceiptParser.Parse("BAY MARKET\n" + rows + "\nTOTAL EUR 7.10", new DateOnly(2027, 1, 2));
        Assert.Equal(new DateOnly(2026, 9, 28), result.Date);
    }

    /// <summary>An invalid numeric-looking row does not prevent the independent full named fallback.</summary>
    [Fact]
    public void An_invalid_numeric_row_does_not_hide_a_complete_named_date()
    {
        var result = ReceiptParser.Parse("BAY MARKET\n31.02.2025\n27 OCT 2025\nTOTAL EUR 7.10", new DateOnly(2027, 1, 2));
        Assert.Equal(new DateOnly(2025, 10, 27), result.Date);
    }
}
