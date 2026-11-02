using Vafadar.Zanance.Core.Receipts;

namespace Vafadar.Zanance.Core.Tests.Receipts;

/// <summary>Exact joined total/currency evidence is separated without fuzzy labels or rewritten source rows.</summary>
public sealed class ReceiptJoinedUnitTests
{
    /// <summary>Only a complete existing strong label and an exact known ISO code can supply joined evidence.</summary>
    [Theory]
    [InlineData("TOTALEUR", "EUR")]
    [InlineData("TOTALUSD", "USD")]
    [InlineData("TOTALGBP", "GBP")]
    [InlineData("TOTALCHF", "CHF")]
    [InlineData("TOTALCAD", "CAD")]
    [InlineData("TOTALAUD", "AUD")]
    [InlineData("SUMMEEUR", "EUR")]
    [InlineData("TOTALEEUR", "EUR")]
    [InlineData("GRAND TOTALEUR", "EUR")]
    [InlineData("IMPORTE TOTALEUR", "EUR")]
    public void An_exact_joined_total_retains_its_original_source_and_unit(string label, string code)
    {
        var line = label + " 7.10";
        var result = ReceiptParser.Parse("28.10.2026\nBREAD EUR 3.20\n" + line, new DateOnly(2026, 11, 2));

        Assert.Equal(ReceiptAmountStatus.Found, result.AmountStatus);
        Assert.Equal(7.10m, result.Amount);
        var candidate = Assert.Single(result.Candidates);
        Assert.Equal(code, candidate.CurrencyCode);
        Assert.Equal(line, candidate.Source);
        Assert.Equal(line, result.AmountSource);
        Assert.Null(result.Merchant);
    }

    /// <summary>Unknown labels, compound names, unknown codes and incomplete unit boundaries are not repaired.</summary>
    [Theory]
    [InlineData("TOTALXYZ")]
    [InlineData("TOTALEURSHOP")]
    [InlineData("TOTALENERGIES")]
    [InlineData("SUBTOTALEUR")]
    [InlineData("TOTALUSDT")]
    [InlineData("SUMNEEUR")]
    [InlineData("TOTALEUR_")]
    [InlineData("TOTALEUR7")]
    public void A_partial_or_unknown_join_never_promotes_an_item_price(string label)
    {
        var result = ReceiptParser.Parse("28.10.2026\nBREAD EUR 3.20\n" + label + " 7.10", new DateOnly(2026, 11, 2));

        Assert.Null(result.Amount);
        Assert.Equal(ReceiptAmountStatus.NotFound, result.AmountStatus);
        Assert.Empty(result.Candidates);
    }

    /// <summary>Original tax, subtotal, nonmoney and payment exclusions still apply after separating an exact unit.</summary>
    [Theory]
    [InlineData("SUB TOTALEUR 7.10", ReceiptAmountStatus.NotFound)]
    [InlineData("TOTALEUR TAX 7.10", ReceiptAmountStatus.NotFound)]
    [InlineData("TOTALEUR POINTS 7.10", ReceiptAmountStatus.NotFound)]
    [InlineData("TOTALEUR PAID 7.10", ReceiptAmountStatus.Review)]
    public void Original_exclusions_remain_authoritative(string line, ReceiptAmountStatus status)
    {
        var result = ReceiptParser.Parse("28.10.2026\n" + line, new DateOnly(2026, 11, 2));

        Assert.Null(result.Amount);
        Assert.Equal(status, result.AmountStatus);
        Assert.Empty(result.Candidates);
    }

    /// <summary>Recognizing an exact joined label does not salvage a damaged or signed amount.</summary>
    [Theory]
    [InlineData("TOTALEUR 7.O0")]
    [InlineData("TOTALEUR -7.10")]
    public void Damaged_or_signed_joined_amounts_need_review(string line)
    {
        var result = ReceiptParser.Parse("28.10.2026\n" + line, new DateOnly(2026, 11, 2));

        Assert.Null(result.Amount);
        Assert.Equal(ReceiptAmountStatus.Review, result.AmountStatus);
    }

    /// <summary>A joined total row is not a merchant header.</summary>
    [Fact]
    public void A_joined_total_without_a_header_does_not_supply_a_merchant()
    {
        Assert.Null(ReceiptParser.Parse("TOTALEUR 7.10").Merchant);
    }
}
