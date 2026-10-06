using System.Globalization;
using Vafadar.Zanance.Core.Receipts;

namespace Vafadar.Zanance.Core.Tests.Receipts;

/// <summary>
/// Italian receipts: "totale", "importo totale" and "netto a pagare" are the total; "totale IVA", the taxable amount,
/// discounts, cash handed over and change are not. All shops and amounts are fictitious.
/// </summary>
public sealed class ItalianReceiptTests
{
    private static readonly DateOnly Today = new(2026, 10, 6);

    [Theory]
    [InlineData("EMPORIO LUNA\n25/09/2026\nTOTALE 24,50\nCONTANTI RICEVUTI 50,00\nRESTO 25,50", "24.50", "2026-09-25", "EMPORIO LUNA")]
    [InlineData("BOTTEGA ALBA\n25/09/2026\nIMPORTO TOTALE 1.234,56", "1234.56", "2026-09-25", "BOTTEGA ALBA")]
    [InlineData("EMPORIO LUNA\nTOTALE IVA INCLUSA 24,00\nIMPORTO IVA 4,33", "24.00", null, "EMPORIO LUNA")]
    [InlineData("EMPORIO LUNA\nTOTALE IVA COMPRESA 24,00 (di cui IVA 4,33)", "24.00", null, "EMPORIO LUNA")]
    [InlineData("BOTTEGA ALBA\nNETTO A PAGARE 45,60", "45.60", null, "BOTTEGA ALBA")]
    [InlineData("EMPORIO LUNA\nSUBTOTALE 150,00\nSCONTO TOTALE 30,00\nTOTALE DA PAGARE 120,00", "120.00", null, "EMPORIO LUNA")]
    [InlineData("EMPORIO LUNA\nTOTALE COMPLESSIVO 61,00\nTOTALE IMPONIBILE 50,00\nTOTALE IVA 11,00", "61.00", null, "EMPORIO LUNA")]
    [InlineData("BOTTEGA ALBA\nTOTALE DOCUMENTO\n67,80\nRESTO 2,20", "67.80", null, "BOTTEGA ALBA")]
    [InlineData("DOCUMENTO COMMERCIALE\nBAR DEL BORGO\n25/09/2026\nTOTALE 8,50", "8.50", "2026-09-25", "BAR DEL BORGO")]
    [InlineData("EMPORIO LUNA\nTOTALE 1 234,56", "1234.56", null, "EMPORIO LUNA")]
    [InlineData("EMPORIO LUNA\nTOTALE 1 234,56", "1234.56", null, "EMPORIO LUNA")]
    [InlineData("BOTTEGA ALBA\nTOTALE CONTANTI 15,00\nCONTANTI CONSEGNATI 20,00\nRESTO 5,00", "15.00", null, "BOTTEGA ALBA")]
    [InlineData("EMPORIO LUNA\nPARTITA IVA 12.345.678.901\nTOTALE 23,40", "23.40", null, "EMPORIO LUNA")]
    [InlineData("BOTTEGA ALBA\nSUBTOTALE 70,00\nSCONTO 10,00\nDA PAGARE 60,00", "60.00", null, "BOTTEGA ALBA")]
    [InlineData("BOTTEGA ALBA\nProdotto 12,50\nSCONTO 120,00\nRESTO 20,00", "12.50", null, "BOTTEGA ALBA")]
    public void The_total_the_date_and_the_shop_are_read(string text, string amount, string? date, string merchant)
    {
        var receipt = ReceiptParser.Parse(text, Today);

        Assert.Equal(decimal.Parse(amount, CultureInfo.InvariantCulture), receipt.Amount);
        Assert.Equal(date is null ? null : DateOnly.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture), receipt.Date);
        Assert.Equal(merchant, receipt.Merchant);
    }

    [Fact]
    public void Cash_change_and_tax_alone_give_no_total()
    {
        Assert.Null(ReceiptParser.Parse("CONTANTI RICEVUTI 100,00\nRESTO 12,00\nIMPORTO IVA 15,87", Today).Amount);
    }

    [Fact]
    public void A_German_net_amount_is_still_not_the_total()
    {
        // "netto a pagare" names the Italian total; German "Netto" alone stays excluded.
        Assert.Equal(11.90m, ReceiptParser.Parse("Laden Beispiel\nNetto 10,00\nSumme 11,90", Today).Amount);
        Assert.Equal(11.90m, ReceiptParser.Parse("Laden Beispiel\nSumme 11,90\nNetto 10,00", Today).Amount);
    }
}
