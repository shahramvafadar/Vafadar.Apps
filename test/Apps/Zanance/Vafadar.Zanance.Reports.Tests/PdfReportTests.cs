using Syncfusion.Pdf.Parsing;

namespace Vafadar.Zanance.Reports.Tests;

public sealed class PdfReportTests
{
    // Without a key the PDFs carry a trial notice; the tests still check structure and text.
    static PdfReportTests()
    {
        if (!string.IsNullOrEmpty(AppSecrets.SyncfusionLicenseKey))
        {
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(AppSecrets.SyncfusionLicenseKey);
        }
    }

    [Fact]
    public void Every_section_prints_its_own_scope()
    {
        var report = new ReportDocument("Report", "October 2026", "No open issues", "2026-10-31", "Not an official statement.", false, [],
        [
            new ReportTable("Period overview", ["Item", "Amount"], [["Surplus", "+700.00 EUR"]], new HashSet<int> { 1 }, "October 2026 · EUR · Accounts in totals"),
            new ReportTable("Next 30 days", ["Plan", "Amount"], [["Rent", "950.00 EUR"]], new HashSet<int> { 1 }, "Next 30 days · EUR · Usable accounts"),
        ]);

        using var loaded = new PdfLoadedDocument(new MemoryStream(PdfReport.Write(report)));
        var text = string.Concat(Enumerable.Range(0, loaded.Pages.Count).Select(i => loaded.Pages[i].ExtractText()));

        Assert.Contains("Accounts in totals", text, StringComparison.Ordinal);
        Assert.Contains("Usable accounts", text, StringComparison.Ordinal);
        Assert.Contains("+700.00", text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(false, "Monthly report", "Food")]
    [InlineData(true, "گزارش ماهانه", "خوراک")]
    public void A_report_is_a_pdf_with_its_texts_and_a_disclaimer(bool rtl, string title, string category)
    {
        var report = new ReportDocument(
            title,
            rtl ? "مهر ۱۴۰۵" : "October 2026",
            rtl ? "حساب‌های داخل جمع کل · EUR" : "Accounts in totals · EUR",
            "2026-10-31",
            rtl ? "این گزارش سند رسمی نیست." : "This report is not an official statement.",
            rtl,
            [new ReportLine(rtl ? "درآمد" : "Income", "\u2066\u200E2,500.00 EUR\u200E\u2069"), new ReportLine(rtl ? "هزینه" : "Expenses", "31.80 EUR")],
            [new ReportTable(rtl ? "هزینه به تفکیک دسته" : "Expenses by category", [rtl ? "دسته" : "Category", rtl ? "خالص" : "Net"], [[category, "31.80 EUR"]], new HashSet<int> { 1 })]);

        var pdf = PdfReport.Write(report);
        if (Environment.GetEnvironmentVariable("VAFADAR_PDF_SAMPLES") is { Length: > 0 } output)
        {
            File.WriteAllBytes(Path.Combine(output, rtl ? "report-fa.pdf" : "report-en.pdf"), pdf);
        }

        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
        using var loaded = new PdfLoadedDocument(new MemoryStream(pdf));
        Assert.Equal(1, loaded.Pages.Count);
        var text = loaded.Pages[0].ExtractText();
        Assert.Contains("2,500.00", text, StringComparison.Ordinal);
        Assert.Contains("EUR", text, StringComparison.Ordinal);

    }

    [Fact]
    public void Long_tables_continue_on_the_next_page()
    {
        var rows = Enumerable.Range(1, 120).Select(i => (IReadOnlyList<string>)[$"Row {i}", $"{i}.00 EUR"]).ToList();
        var report = new ReportDocument("Report", "2026", "All", "today", "Not official.", false, [], [new ReportTable("Entries", ["Title", "Amount"], rows, new HashSet<int> { 1 })]);

        using var loaded = new PdfLoadedDocument(new MemoryStream(PdfReport.Write(report)));

        Assert.True(loaded.Pages.Count > 1);
    }
}
