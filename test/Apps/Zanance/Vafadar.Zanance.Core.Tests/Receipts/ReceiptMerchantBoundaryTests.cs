using Vafadar.Zanance.Core.Receipts;

namespace Vafadar.Zanance.Core.Tests.Receipts;

/// <summary>Preserves missing merchant evidence when the original header is outside a captured frame.</summary>
public sealed class ReceiptMerchantBoundaryTests
{
    /// <summary>Uses the actual D-615 camera text without constructing another camera or financial operation.</summary>
    [Fact]
    public void The_original_clipped_camera_text_does_not_turn_its_date_into_a_merchant()
    {
        const string text = "28 OCT 2026\nBREAD EUR 12.00\nFRUIT EUR 11.45\nTOTAL EUR 23.45\nCARD EUR 23.45";
        var result = ReceiptParser.Parse(text, new DateOnly(2026, 10, 28));

        Assert.Null(result.Merchant);
        Assert.Equal(ReceiptAmountStatus.Found, result.AmountStatus);
        Assert.Equal(23.45m, result.Amount);
        Assert.True(Assert.Single(result.Candidates).TryMinor("EUR", out var minor));
        Assert.Equal(2345, minor);
        // D-621 extends the former boundary-only recognition to this complete source date, still only an unsaved suggestion.
        Assert.Equal(new DateOnly(2026, 10, 28), result.Date);
    }

    /// <summary>Never guesses a merchant from item rows after a complete date in the supported source languages.</summary>
    [Theory]
    [InlineData("28 OCT 2026")]
    [InlineData("28. Oktober 2026")]
    [InlineData("28 octubre 2026")]
    [InlineData("28 octobre 2026")]
    [InlineData("28 ottobre 2026")]
    [InlineData("۱۴۰۵/۰۸/۰۶")]
    public void A_date_before_any_header_ends_the_merchant_search(string date)
    {
        var result = ReceiptParser.Parse(date + "\nBREAD EUR 12.00\nTOTAL EUR 12.00", new DateOnly(2026, 10, 28));

        Assert.Null(result.Merchant);
        Assert.Equal(12m, result.Amount);
        Assert.Equal(ReceiptAmountStatus.Found, result.AmountStatus);
    }

    /// <summary>Actual names, including numeric or date-like names, remain authoritative before a receipt body.</summary>
    [Theory]
    [InlineData("NORTH HARBOR MARKET")]
    [InlineData("Studio 54")]
    [InlineData("28 OCT 2026 MARKET")]
    public void A_real_header_before_the_date_is_preserved(string merchant)
    {
        var result = ReceiptParser.Parse(merchant + "\n28 OCT 2026\nTOTAL EUR 23.45", new DateOnly(2026, 10, 28));

        Assert.Equal(merchant, result.Merchant);
        Assert.Equal(23.45m, result.Amount);
    }
}
