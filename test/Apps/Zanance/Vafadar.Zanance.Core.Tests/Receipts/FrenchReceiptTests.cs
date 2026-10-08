using System.Globalization;
using Vafadar.Zanance.Core.Receipts;

namespace Vafadar.Zanance.Core.Tests.Receipts;

/// <summary>French receipts: TTC and "net à payer" are the total; HT, tax, cash handed over and change are not.</summary>
public sealed class FrenchReceiptTests
{
    private static readonly DateOnly Today = new(2026, 10, 6);

    [Theory]
    [InlineData("Commerce Exemple\n25/09/2026\nSous-total 20,00\nTVA 4,00\nTOTAL TTC 24,00\nEspèces reçues 50,00\nMonnaie rendue 26,00", "24.00", "Commerce Exemple")]
    [InlineData("Atelier Exemple\n25/09/2026\nNet à payer 28,50", "28.50", "Atelier Exemple")]
    [InlineData("Boutique Exemple\n25/09/2026\nTOTAL TVA COMPRISE 24,00\nDont TVA 4,00", "24.00", "Boutique Exemple")]
    [InlineData("Boutique Exemple\n25/09/2026\nTotal TTC 24,00 (dont TVA 4,00)", "24.00", "Boutique Exemple")]
    [InlineData("Magasin Exemple\n25/09/2026\nTOTAL À PAYER 1 234,56", "1234.56", "Magasin Exemple")]
    [InlineData("Magasin Exemple\n25/09/2026\nTOTAL À PAYER 1 234,56", "1234.56", "Magasin Exemple")]
    [InlineData("Magasin Exemple\n25/09/2026\nTOTAL À PAYER 1 234,56", "1234.56", "Magasin Exemple")]
    [InlineData("Atelier Exemple\n25/09/2026\nMONTANT A PAYER 19,90\nESPECES RECUES 20,00\nA RENDRE 0,10", "19.90", "Atelier Exemple")]
    [InlineData("Commerce Exemple\n25/09/2026\nTOTAL 12.50 $", "12.50", "Commerce Exemple")]
    [InlineData("FACTURE\nAtelier Exemple\n25/09/2026\nTotal TTC 75,00", "75.00", "Atelier Exemple")]
    [InlineData("Café Exemple\n25/09/2026\nBoisson 3,50\nEspèces reçues 10,00\nMonnaie rendue 6,50", null, "Café Exemple")]
    [InlineData("Commerce Exemple\n25/09/2026\nTotal HT 20,00\nTotal TTC 24,00", "24.00", "Commerce Exemple")]
    public void The_total_the_date_and_the_shop_are_read(string text, string? amount, string merchant)
    {
        var receipt = ReceiptParser.Parse(text, Today);

        // Cash/change and a single item still do not establish a purchase total.
        if (text.Contains('$'))
        {
            // A complete number with an ambiguous symbol remains a review choice, never an automatic total.
            Assert.Null(receipt.Amount);
            Assert.Equal(ReceiptAmountStatus.Review, receipt.AmountStatus);
            Assert.Equal(decimal.Parse(amount!, CultureInfo.InvariantCulture), Assert.Single(receipt.Candidates).Amount);
        }
        else
        {
            Assert.Equal(amount is null ? null : decimal.Parse(amount, CultureInfo.InvariantCulture), receipt.Amount);
        }
        Assert.Equal(new DateOnly(2026, 9, 25), receipt.Date);
        Assert.Equal(merchant, receipt.Merchant);
    }

    [Fact]
    public void A_plain_space_joins_digits_only_on_a_total_line()
    {
        // Without a total word "2 100,00" is a quantity and a price, not 2,100.00.
        Assert.Null(ReceiptParser.Parse("Magasin Exemple\nCafé 2 100,00", Today).Amount);
    }
}
