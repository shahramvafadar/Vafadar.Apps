using Syncfusion.Drawing;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Security;

namespace Vafadar.Documents.Tests;

public sealed class PdfTextTests
{
    [Fact]
    public void The_text_layer_of_a_digital_invoice_is_read_in_rows()
    {
        // The label and the price are separate text runs far apart on the same line, as invoice generators write them.
        var pdf = MinimalPdf.Create([[new("Musterfirma GmbH", 40, 40), new("Rechnungsdatum 28.09.2026", 40, 90), new("Rechnungsbetrag", 40, 300), new("119,00 EUR", 420, 300)]]);

        var text = PdfText.Extract(pdf);

        Assert.NotNull(text);
        var rows = text.Split('\n');
        Assert.Equal("Musterfirma GmbH", rows[0]);
        Assert.Contains("Rechnungsbetrag 119,00 EUR", rows);
    }

    [Fact]
    public void Pages_follow_each_other_and_the_last_page_of_a_long_file_is_always_read()
    {
        var pdf = MinimalPdf.Create([.. Enumerable.Range(1, 7).Select(n => new MinimalPdf.Run[] { new($"Seite {n} von sieben", 40, 40) })]);

        var rows = PdfText.Extract(pdf, maxPages: 3)!.Split('\n');

        Assert.Equal(["Seite 1 von sieben", "Seite 2 von sieben", "Seite 7 von sieben"], rows);
        Assert.Equal(7, PdfText.PageCount(pdf));
    }

    [Fact]
    public void A_scanned_pdf_without_a_text_layer_has_no_text()
    {
        var pdf = MinimalPdf.Create([[]]);

        Assert.Null(PdfText.Extract(pdf));
    }

    [Fact]
    public void Damaged_and_password_protected_files_have_nothing_to_read()
    {
        var locked = Create(
            page => Draw(page, "Vertrauliche Rechnung 119,00 EUR", 40, 40),
            security: s =>
            {
                s.UserPassword = "secret";
                s.OwnerPassword = "owner";
            });

        // The protected file comes from Syncfusion: its trial notice does not matter, the file cannot be opened at all.
        Assert.Null(PdfText.Extract("%PDF-1.7 broken"u8.ToArray()));
        Assert.Null(PdfText.Extract(locked));
        Assert.Equal(0, PdfText.PageCount(locked));
    }

    [Theory]
    [InlineData("application/pdf", "x", true)]
    [InlineData("application/octet-stream", "%PDF-1.4", true)]
    [InlineData("image/jpeg", "ÿØ", false)]
    public void Pdf_files_are_recognised_by_type_or_content(string contentType, string start, bool expected) =>
        Assert.Equal(expected, PdfText.IsPdf(contentType, System.Text.Encoding.Latin1.GetBytes(start)));

    private static void Draw(PdfPage page, string text, float x, float y) =>
        page.Graphics.DrawString(text, new PdfStandardFont(PdfFontFamily.Helvetica, 12), PdfBrushes.Black, new PointF(x, y));

    private static byte[] Create(Action<PdfPage> page, Action<PdfSecurity> security) => Create([page], security);

    private static byte[] Create(Action<PdfPage>[] pages, Action<PdfSecurity>? security)
    {
        var document = new PdfDocument();
        try
        {
            foreach (var draw in pages)
            {
                draw(document.Pages.Add());
            }

            security?.Invoke(document.Security);
            using var stream = new MemoryStream();
            document.Save(stream);
            return stream.ToArray();
        }
        finally
        {
            document.Close(true);
        }
    }
}
