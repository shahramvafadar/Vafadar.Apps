using Vafadar.Documents;
using Vafadar.Zanance.Core.Receipts;

namespace Vafadar.Zanance.Core.Tests.Receipts;

public sealed class ReceiptParserTests
{
    [Fact]
    public void A_german_receipt_gives_the_total_the_date_and_the_shop()
    {
        const string text = """
            Bäckerei Sonnenschein
            Hauptstr. 12, 10115 Berlin
            28.09.2026 08:14
            2x Brötchen        1,20
            Kaffee             2,90
            Zwischensumme      4,10
            SUMME EUR          4,10
            Bar               10,00
            Rückgeld           5,90
            """;

        var receipt = ReceiptParser.Parse(text);

        Assert.Equal(4.10m, receipt.Amount);
        Assert.Equal(new DateOnly(2026, 9, 28), receipt.Date);
        Assert.Equal("Bäckerei Sonnenschein", receipt.Merchant);
    }

    [Fact]
    public void An_english_receipt_with_thousands_separators()
    {
        const string text = """
            ACME Electronics
            Receipt #5521
            2026-09-27
            Headphones      1,249.00
            Subtotal        1,249.00
            Total           1,249.00
            """;

        var receipt = ReceiptParser.Parse(text);

        Assert.Equal(1249.00m, receipt.Amount);
        Assert.Equal(new DateOnly(2026, 9, 27), receipt.Date);
        Assert.Equal("ACME Electronics", receipt.Merchant);
    }

    [Fact]
    public void A_persian_receipt_with_persian_digits_and_a_solar_hijri_date()
    {
        const string text = """
            فروشگاه نمونه
            تاریخ: ۱۴۰۵/۰۷/۰۶
            شیر ۲ عدد        ۱۲۰٬۰۰۰
            نان               ۴۵٬۰۰۰
            جمع کل           ۱۶۵٬۰۰۰
            """;

        var receipt = ReceiptParser.Parse(text);

        Assert.Equal(165_000m, receipt.Amount);
        Assert.Equal(new DateOnly(2026, 9, 28), receipt.Date);
        Assert.Equal("فروشگاه نمونه", receipt.Merchant);
    }

    [Fact]
    public void Without_a_total_line_the_largest_amount_is_suggested_and_dates_are_not_amounts()
    {
        var receipt = ReceiptParser.Parse("Kiosk\n12.03.2027\nZeitung 2,50\nWasser 1,20");

        Assert.Equal(2.50m, receipt.Amount);
        Assert.Equal(new DateOnly(2027, 3, 12), receipt.Date);
    }

    [Theory]
    [InlineData("12,50", 12.50)]
    [InlineData("12.5", 12.5)]
    [InlineData("1.234,56", 1234.56)]
    [InlineData("1,234.56", 1234.56)]
    [InlineData("125,000", 125000)]
    [InlineData("2.500.000", 2500000)]
    public void Amounts_in_common_notations(string text, double expected)
    {
        Assert.True(ReceiptParser.TryAmount(text, out var amount));
        Assert.Equal((decimal)expected, amount);
    }

    [Fact]
    public void Words_an_ocr_engine_returns_in_columns_are_joined_into_rows()
    {
        // Word boxes as Windows OCR returned them for a rendered receipt: prices come as a separate column, "2,90" split.
        LayoutWord[] words =
        [
            new(55, 81, 42, "Baeckerei"), new(55, 81, 240, "Sonnenschein"), new(236, 266, 399, "1,20"), new(296, 326, 398, "2,"),
            new(296, 321, 437, "90"), new(357, 386, 397, "4,"), new(356, 381, 438, "10"), new(416, 446, 379, "10,ee"),
            new(117, 148, 42, "Hauptstr."), new(116, 146, 240, "12,"), new(116, 141, 319, "10115"), new(176, 201, 42, "28.09.2026"),
            new(176, 201, 259, "08:14"), new(236, 260, 42, "2x"), new(235, 261, 102, "Broetchen"), new(297, 320, 42, "K"),
            new(302, 321, 62, "a"), new(295, 320, 81, "f"), new(295, 321, 101, "fee"), new(356, 381, 42, "SUMME"),
            new(357, 381, 162, "EUR"), new(417, 441, 42, "Bar"), new(115, 141, 438, "Berlin"),
        ];

        var text = TextLayout.Rows(words);
        var receipt = ReceiptParser.Parse(text);

        Assert.Contains("SUMME EUR 4, 10", text, StringComparison.Ordinal);
        Assert.Equal(4.10m, receipt.Amount);
        Assert.Equal(new DateOnly(2026, 9, 28), receipt.Date);
        Assert.Equal("Baeckerei Sonnenschein", receipt.Merchant);
    }

    [Fact]
    public void Tax_discount_and_change_lines_after_the_total_are_not_the_total()
    {
        var receipt = ReceiptParser.Parse("Markt\nSUMME EUR 4,10\nNettobetrag 3,83\nMwSt-Betrag 0,27\nGesamtersparnis 1,00\nBar 10,00\nRückgeld 5,90");

        Assert.Equal(4.10m, receipt.Amount);
    }

    [Fact]
    public void Without_a_total_line_postal_codes_phone_numbers_and_ids_are_not_amounts()
    {
        var receipt = ReceiptParser.Parse("Hotel Adlon\nHauptstr. 12, 10115 Berlin\nTel. 030 1234567\nUSt-IdNr DE123456789\nSUMNE 4,10");

        Assert.Equal(4.10m, receipt.Amount);
        Assert.Equal("Hotel Adlon", receipt.Merchant);
    }

    [Fact]
    public void A_shop_name_containing_a_keyword_is_still_the_shop()
    {
        var receipt = ReceiptParser.Parse("TotalEnergies\n10115 Berlin\nSuper E10 45,20\nTotal 45,20");

        Assert.Equal("TotalEnergies", receipt.Merchant);
        Assert.Equal(45.20m, receipt.Amount);
    }

    [Fact]
    public void A_persian_row_is_read_from_right_to_left() =>
        Assert.Equal("فروشگاه نمونه", TextLayout.Rows([new(0, 10, 50, "نمونه"), new(0, 10, 120, "فروشگاه")]));

    [Fact]
    public void An_invoice_total_is_the_gross_amount_not_the_net_or_the_tax()
    {
        var receipt = ReceiptParser.Parse("Musterfirma GmbH\nRechnungsdatum 28.09.2026\nNettobetrag 100,00 EUR\nUSt 19 % 19,00 EUR\nRechnungsbetrag 119,00 EUR\nZahlbar bis 12.10.2026");

        Assert.Equal(119.00m, receipt.Amount);
        Assert.Equal(new DateOnly(2026, 9, 28), receipt.Date);
        Assert.Equal("Musterfirma GmbH", receipt.Merchant);
    }

    [Fact]
    public void An_empty_text_suggests_nothing() => Assert.True(ReceiptParser.Parse("  ").IsEmpty);
}
