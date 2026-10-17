using Vafadar.Zanance.Core.Receipts;

namespace Vafadar.Zanance.Core.Tests.Receipts;

/// <summary>Observed OCR accent drift changes only keyword matching, never numeric or currency evidence.</summary>
public sealed class ReceiptAccentTests
{
    [Theory]
    [Trait("AT", "AT-71")]
    [InlineData("NET Å PAYER")]
    [InlineData("NET A\u030A PAYER")]
    [InlineData("NÉT À PAYER")]
    [InlineData("MÖNTANT À PAYER")]
    public void Latin_keyword_accents_keep_the_complete_original_total_source(string label)
    {
        var row = label + " 24,50 EUR";
        var receipt = ReceiptParser.Parse("Maison Café\n2026-10-17\n" + row, new(2026, 10, 17));

        Assert.Equal(24.50m, receipt.Amount);
        Assert.Equal(ReceiptAmountStatus.Found, receipt.AmountStatus);
        var candidate = Assert.Single(receipt.Candidates);
        Assert.Equal(row, candidate.Source);
        Assert.Equal("EUR", candidate.CurrencyCode);
        Assert.Equal("Maison Café", receipt.Merchant);
        Assert.Equal(new DateOnly(2026, 10, 17), receipt.Date);
    }

    [Theory]
    [Trait("AT", "AT-71")]
    [InlineData("TÖTAL ESPÈCES REÇÜES 50.00 EUR")]
    [InlineData("TÖTAL TVA 4.00 EUR")]
    [InlineData("NET Å PAYFR 24.50 EUR")]
    [InlineData("NET Å PAYER -24.50 EUR")]
    [InlineData("NET Å PAYER 24.5O EUR")]
    [InlineData("NET Å PAYER 24.50 EUR\nTOTAL 18.00 USD")]
    public void Accent_tolerance_never_guesses_payment_tax_spelling_numbers_or_currency(string rows)
    {
        var receipt = ReceiptParser.Parse("Maison Café\n" + rows, new(2026, 10, 17));

        Assert.Null(receipt.Amount);
        Assert.NotEqual(ReceiptAmountStatus.Found, receipt.AmountStatus);
    }
}
