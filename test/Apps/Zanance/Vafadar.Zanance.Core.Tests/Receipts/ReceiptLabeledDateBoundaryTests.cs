using Vafadar.Zanance.Core.Receipts;

namespace Vafadar.Zanance.Core.Tests.Receipts;

/// <summary>Printed date-field metadata cannot supply a merchant from a clipped receipt body.</summary>
public sealed class ReceiptLabeledDateBoundaryTests
{
    /// <summary>The actual first native OCR keeps missing total evidence rather than borrowing an item price.</summary>
    [Fact]
    public void The_actual_joined_native_total_preserves_manual_amount_review()
    {
        const string text = "Date: 28/10\nBREAD EUR 7.10\nTOTALEUR 7.10";
        var result = ReceiptParser.Parse(text, new DateOnly(2026, 11, 2));

        Assert.Null(result.Merchant);
        Assert.Null(result.Date);
        Assert.Null(result.Amount);
        Assert.Equal(ReceiptAmountStatus.NotFound, result.AmountStatus);
        Assert.Empty(result.Candidates);
    }

    /// <summary>Yearless explicitly labelled dates end header search without inventing a purchase year.</summary>
    [Theory]
    [InlineData("Datum: 28.10.")]
    [InlineData("Date: 28/10")]
    [InlineData("Fecha: 28-10")]
    [InlineData("Data: 28 ott")]
    [InlineData("Date: 28 OCT")]
    [InlineData("Datum: 28. Oktober")]
    [InlineData("Date: 28 octobre 13:42")]
    [InlineData("تاریخ: ۰۶/۰۸")]
    public void A_labeled_yearless_date_does_not_supply_a_merchant_or_year(string printed)
    {
        var result = ReceiptParser.Parse(printed + "\nBREAD EUR 7.10\nTOTAL EUR 7.10", new DateOnly(2026, 10, 28));

        Assert.Null(result.Merchant);
        Assert.Null(result.Date);
        Assert.Equal(ReceiptAmountStatus.Found, result.AmountStatus);
        Assert.Equal(7.10m, result.Amount);
        Assert.True(Assert.Single(result.Candidates).TryMinor("EUR", out var minor));
        Assert.Equal(710, minor);
    }

    /// <summary>Labelled complete named dates retain the original manual-date review policy.</summary>
    [Theory]
    [InlineData("Date: 28 OCT 2026")]
    [InlineData("Datum: 28. Oktober 2026")]
    [InlineData("Fecha: 28 octubre 2026")]
    [InlineData("Date: 28 octobre 2026")]
    [InlineData("Data: 28 ottobre 2026")]
    public void A_labeled_complete_named_date_cannot_be_a_merchant(string printed)
    {
        var result = ReceiptParser.Parse(printed + "\nBREAD EUR 7.10\nTOTAL EUR 7.10", new DateOnly(2026, 10, 28));

        Assert.Null(result.Merchant);
        Assert.Null(result.Date);
        Assert.Equal(7.10m, result.Amount);
    }

    /// <summary>The existing numeric date suggestion and total are not changed by merchant metadata recognition.</summary>
    [Fact]
    public void A_labeled_numeric_timestamp_preserves_its_original_date_and_amount()
    {
        var result = ReceiptParser.Parse("DATE : 28.10.2026 13:42\nBREAD EUR 7.10\nTOTAL EUR 7.10", new DateOnly(2026, 10, 28));

        Assert.Null(result.Merchant);
        Assert.Equal(new DateOnly(2026, 10, 28), result.Date);
        Assert.Equal(7.10m, result.Amount);
    }

    /// <summary>Real headers with date-like words or additional merchant text remain unchanged.</summary>
    [Theory]
    [InlineData("DATE PALM MARKET")]
    [InlineData("DATA MARKET")]
    [InlineData("DATUM STORES")]
    [InlineData("Date: 28 OCT MARKET")]
    [InlineData("MY DATE: 28 OCT")]
    [InlineData("Date: 28 Unknown")]
    [InlineData("Date: 32 OCT")]
    [InlineData("Date: 28/13")]
    [InlineData("28 OCT")]
    public void A_name_without_complete_labeled_date_evidence_is_preserved(string merchant)
    {
        var result = ReceiptParser.Parse(merchant + "\n28 OCT 2026\nTOTAL EUR 7.10", new DateOnly(2026, 10, 28));

        Assert.Equal(merchant, result.Merchant);
        Assert.Equal(new DateOnly(2026, 10, 28), result.Date);
        Assert.Equal(7.10m, result.Amount);
    }
}
