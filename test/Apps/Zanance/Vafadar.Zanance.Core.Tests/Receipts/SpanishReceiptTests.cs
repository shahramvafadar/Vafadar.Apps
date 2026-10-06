using Vafadar.Zanance.Core.Receipts;

namespace Vafadar.Zanance.Core.Tests.Receipts;

/// <summary>Spanish receipts from Spain and Latin America: the purchase total, never a subtotal, tax, cash or change.</summary>
public sealed class SpanishReceiptTests
{
    [Theory]
    [InlineData("TIENDA EJEMPLO\n25/09/2026\nSUBTOTAL 10,00\nIVA 2,10\nTOTAL A PAGAR 12,10\nEFECTIVO ENTREGADO 20,00\nCAMBIO 7,90", "12.10")]
    [InlineData("COMERCIO EJEMPLO\n25/09/2026\nSUBTOTAL 100.00\nIVA 16.00\nMONTO TOTAL 116.00\nRECIBIDO 200.00\nVUELTO 84.00", "116.00")]
    [InlineData("COMERCIO EJEMPLO\nTOTAL IVA INCLUIDO 121,00\nEFECTIVO ENTREGADO 200,00\nCAMBIO 79,00", "121.00")]
    [InlineData("COMERCIO EJEMPLO\nIMPORTE TOTAL 1.234,56", "1234.56")]
    [InlineData("COMERCIO EJEMPLO\nMONTO A PAGAR 1,234.56", "1234.56")]
    [InlineData("COMERCIO EJEMPLO\nTOTAL A PAGAR\n54,90", "54.90")]
    [InlineData("COMERCIO EJEMPLO\nTOTAL 35,00\nTOTAL DESCUENTOS 5,00", "35.00")]
    [InlineData("COMERCIO EJEMPLO\nTOTAL $ 116.00\nRECIBIDO $ 200.00\nVUELTO $ 84.00", "116.00")]
    public void The_total_is_read(string text, string expected)
    {
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), ReceiptParser.Parse(text, new DateOnly(2026, 10, 6)).Amount);
    }

    [Fact]
    public void Cash_handed_over_and_change_are_never_the_total()
    {
        Assert.Null(ReceiptParser.Parse("EFECTIVO ENTREGADO 100,00\nCAMBIO 12,00", new DateOnly(2026, 10, 6)).Amount);
    }

    [Fact]
    public void The_date_and_the_shop_are_read()
    {
        var receipt = ReceiptParser.Parse("TIENDA EJEMPLO\nFACTURA SIMPLIFICADA\n25/09/2026\nTOTAL A PAGAR 12,10", new DateOnly(2026, 10, 6));

        Assert.Equal(new DateOnly(2026, 9, 25), receipt.Date);
        Assert.Equal("TIENDA EJEMPLO", receipt.Merchant);
    }

    [Fact]
    public void A_phone_number_is_no_price()
    {
        // No total word: the largest price, not the phone number or the tax id.
        var receipt = ReceiptParser.Parse("TIENDA EJEMPLO\nTELÉFONO 912.345.678\nNIF B12.345.678\nPAN 1,20\nLECHE 0,95", new DateOnly(2026, 10, 6));

        Assert.Equal(1.20m, receipt.Amount);
    }
}
