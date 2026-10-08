using System.Globalization;
using Vafadar.Zanance.Core.Receipts;
using Vafadar.Documents;

namespace Vafadar.Zanance.Core.Tests.Receipts;

/// <summary>Fictitious receipts protecting against confident but unrelated numeric suggestions.</summary>
public sealed class ReceiptEvidenceTests
{
    [Theory]
    [Trait("AT", "AT-71")]
    [InlineData("Brötchen 1,20\nKaffee 2,90\nSUMME 4,10 EUR (inkl. MwSt. 0,27)", "4.10")]
    [InlineData("Gesamtbetrag inkl. 19 % MwSt. 119,00 EUR", "119.00")]
    [InlineData("TOTAL 12,34 EUR A1", "12.34")]
    [InlineData("Total 12,34\nTotal points 999", "12.34")]
    [InlineData("SUMME 12 , 34", "12.34")]
    [InlineData("SUMME 1234", "1234")]
    [InlineData("TOTAL 0,00\nItem 99,00", "0.00")]
    public void Only_the_complete_purchase_total_is_suggested(string text, string expected) =>
        Assert.Equal(decimal.Parse(expected, CultureInfo.InvariantCulture), ReceiptParser.Parse(text).Amount);

    [Theory]
    [InlineData("SUMME\nBeleg Nr 998877")]
    [InlineData("SUMME\nTelefon 12345678")]
    [InlineData("SUMME\n19 %")]
    [InlineData("Kiosk\nZeitung 2,50\nWasser 1,20")]
    [InlineData("TOTAL 1O,99")]
    [InlineData("TOTAL 12,O9")]
    [InlineData("TOTAL -12,34")]
    [InlineData("TOTAL 12,34-")]
    [InlineData("TOTAL - 12,34")]
    [InlineData("TOTAL 12,34 -")]
    [InlineData("TOTAL (12,34)")]
    [InlineData("Total points 999")]
    [InlineData("Total liters 12.345")]
    [InlineData("TOTAL 1.899 EUR/l")]
    [InlineData("TOTAL 12.345 l")]
    [InlineData("TOTAL 12.345 kg")]
    [InlineData("TOTAL IVA 2,10")]
    [InlineData("TOTAL TVA 4,00")]
    [InlineData("TOTAL 12,34 99,00")]
    public void Insufficient_or_unsafe_evidence_leaves_the_amount_unset(string text) =>
        Assert.Null(ReceiptParser.Parse(text).Amount);

    [Theory]
    [InlineData("TOTAL 12,34 EUR", ReceiptAmountStatus.Found)]
    [InlineData("TOTAL 12,34\nTOTAL 12,34", ReceiptAmountStatus.Found)]
    [InlineData("TOTAL 12,34\nTOTAL 15,00", ReceiptAmountStatus.Review)]
    [InlineData("TOTAL 12,O9", ReceiptAmountStatus.Review)]
    [InlineData("TOTAL -12,34", ReceiptAmountStatus.Review)]
    [InlineData("TOTAL 12,34-", ReceiptAmountStatus.Review)]
    [InlineData("Item 12,34", ReceiptAmountStatus.NotFound)]
    [InlineData("Total paid 10,00\nChange 2,00", ReceiptAmountStatus.Review)]
    [InlineData("TOTAL 12,34\nBalance due 5,00", ReceiptAmountStatus.Found)]
    [InlineData("TOTAL 12,34 EUR 15,00 USD", ReceiptAmountStatus.Review)]
    [InlineData("TOTAL 12,34 $", ReceiptAmountStatus.Review)]
    public void Semantic_status_is_explicit_without_an_invented_confidence(string text, ReceiptAmountStatus status) =>
        Assert.Equal(status, ReceiptParser.Parse(text).AmountStatus);

    [Theory]
    [InlineData("TOTAL 12,34 EUR Beleg Nr 998877", "12.34")]
    [InlineData("TOTAL 12,34 EUR/l\nSUMME 24,90 EUR", "24.90")]
    [InlineData("جمع کل ۱۶۵٬۰۰۰ ریال", "165000")]
    [InlineData("جمع کل ۱۶۵٬۰۰۰ تومان", "165000")]
    [InlineData("TOTAL ١٢٫٣٤ EUR", "12.34")]
    public void Explicit_units_and_identifiers_do_not_change_the_purchase_number(string text, string expected) =>
        Assert.Equal(decimal.Parse(expected, CultureInfo.InvariantCulture), ReceiptParser.Parse(text).Amount);

    [Fact]
    public void Different_currencies_stay_attached_to_their_candidates()
    {
        var receipt = ReceiptParser.Parse("TOTAL 12,34 EUR 15,00 USD");

        Assert.Null(receipt.Amount);
        Assert.Collection(receipt.Candidates,
            c => { Assert.Equal(12.34m, c.Amount); Assert.Equal("EUR", c.CurrencyCode); },
            c => { Assert.Equal(15m, c.Amount); Assert.Equal("USD", c.CurrencyCode); });
    }

    [Fact]
    public void Toman_is_ten_rials_and_never_a_foreign_exchange_conversion()
    {
        var candidate = Assert.Single(ReceiptParser.Parse("جمع کل ۱۲٬۳۴۵ تومان").Candidates);

        Assert.Equal("IRR", candidate.CurrencyCode);
        Assert.True(candidate.TryMinor("IRR", out var minor));
        Assert.Equal(12_345_000, minor);
        Assert.False(candidate.TryMinor("EUR", out _));
    }

    [Fact]
    public void Account_conflicts_ambiguous_symbols_and_excess_precision_cannot_fill_the_form()
    {
        Assert.False(new ReceiptAmountCandidate(12.34m, "TOTAL", "USD").TryMinor("EUR", out _));
        Assert.False(new ReceiptAmountCandidate(12.34m, "TOTAL", UnitLabel: "$").TryMinor("USD", out _));
        Assert.False(new ReceiptAmountCandidate(12.34m, "TOTAL", "JPY").TryMinor("JPY", out _));
        Assert.False(new ReceiptAmountCandidate(12.34m, "TOTAL").TryMinor(null, out _));
        Assert.True(new ReceiptAmountCandidate(12.34m, "TOTAL", "EUR").TryMinor("EUR", out var minor));
        Assert.Equal(1234, minor);
    }

    [Fact]
    public void Legacy_skew_uncertainty_prevents_a_confident_tax_suggestion()
    {
        var layout = TextLayout.Reconstruct([
            new(90, 110, 10, "NETTO"), new(115, 135, 10, "MwSt"), new(140, 160, 10, "SUMME"),
            new(115, 135, 610, "20,92"), new(140, 160, 610, "3,98"), new(165, 185, 610, "24,90"),
        ]);

        var receipt = ReceiptParser.Parse(layout.Text, uncertainLayout: layout.IsAmbiguous);
        Assert.Equal(ReceiptAmountStatus.Review, receipt.AmountStatus);
        Assert.Null(receipt.Amount);
    }

    [Fact]
    public void Correct_source_geometry_gives_the_skewed_gross_total()
    {
        var angle = Math.Atan2(25, 600) * 180 / Math.PI;
        var layout = TextLayout.Reconstruct([
            new(90, 110, 10, "NETTO") { Angle = angle, BlockId = 0, LineId = 0 },
            new(115, 135, 10, "MwSt") { Angle = angle, BlockId = 0, LineId = 1 },
            new(140, 160, 10, "SUMME") { Angle = angle, BlockId = 0, LineId = 2 },
            new(115, 135, 610, "20,92") { Angle = angle, BlockId = 1, LineId = 0 },
            new(140, 160, 610, "3,98") { Angle = angle, BlockId = 1, LineId = 1 },
            new(165, 185, 610, "24,90") { Angle = angle, BlockId = 1, LineId = 2 },
        ]);
        var receipt = ReceiptParser.Parse(layout.Text, uncertainLayout: layout.IsAmbiguous);

        Assert.Equal(24.90m, receipt.Amount);
        Assert.Equal(ReceiptAmountStatus.Found, receipt.AmountStatus);
    }

    [Fact]
    public void A_total_label_cannot_borrow_a_number_from_the_next_page() =>
        Assert.Null(ReceiptParser.Parse("Shop\nSUMME\f24,90").Amount);

    [Fact]
    public void A_single_separator_in_a_three_decimal_currency_requires_a_locale_decision()
    {
        var receipt = ReceiptParser.Parse("TOTAL 12.345 KWD");

        Assert.Null(receipt.Amount);
        Assert.Equal(ReceiptAmountStatus.Review, receipt.AmountStatus);
        Assert.Equal(new[] { 12345m, 12.345m }, receipt.Candidates.Select(c => c.Amount));
    }

    [Fact]
    public void An_external_candidate_cannot_overflow_minor_units() =>
        Assert.False(new ReceiptAmountCandidate(decimal.MaxValue, "TOTAL", "EUR").TryMinor("EUR", out _));
}
