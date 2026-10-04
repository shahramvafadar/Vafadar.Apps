using Syncfusion.Drawing;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Grid;
using Syncfusion.Pdf;

namespace Vafadar.Zanance.Reports;

/// <summary>A labelled figure of the report summary.</summary>
public sealed record ReportLine(string Label, string Value);

/// <summary>
/// A table of the report; <see cref="NumericColumns"/> are aligned like numbers. <see cref="Scope"/> names what the
/// numbers of this section were computed with (period, currency, accounts), printed under its title (ZEX-S0601).
/// </summary>
public sealed record ReportTable(string Title, IReadOnlyList<string> Columns, IReadOnlyList<IReadOnlyList<string>> Rows, IReadOnlySet<int> NumericColumns, string? Scope = null);

/// <summary>
/// The content of a PDF report, already translated and formatted by the app. The report states its period and scope and
/// that it is not an official statement (REP-07).
/// </summary>
public sealed record ReportDocument(
    string Title,
    string Period,
    string Scope,
    string Generated,
    string Disclaimer,
    bool IsRightToLeft,
    IReadOnlyList<ReportLine> Summary,
    IReadOnlyList<ReportTable> Tables);

/// <summary>
/// Writes a <see cref="ReportDocument"/> as an A4 PDF (REP-07) with the embedded Vazirmatn font, which covers Latin and
/// Persian text. In right-to-left documents text runs right to left and table columns are mirrored.
/// </summary>
public static class PdfReport
{
    private const float Margin = 36;

    /// <summary>Creates the PDF.</summary>
    public static byte[] Write(ReportDocument report)
    {
        ArgumentNullException.ThrowIfNull(report);
        using var regularStream = Font("Vazirmatn-Regular.ttf");
        using var boldStream = Font("Vazirmatn-Bold.ttf");
        using var document = new PdfDocument();
        document.PageSettings.Size = PdfPageSize.A4;
        document.PageSettings.Margins.All = Margin;
        document.DocumentInformation.Title = report.Title;
        document.DocumentInformation.Creator = "Zanance";

        var body = new PdfTrueTypeFont(regularStream, 10);
        var small = new PdfTrueTypeFont(regularStream, 8);
        var heading = new PdfTrueTypeFont(boldStream, 12);
        var title = new PdfTrueTypeFont(boldStream, 18);
        var rtl = report.IsRightToLeft;

        var page = document.Pages.Add();
        var width = page.GetClientSize().Width;
        float y = 0;

        y = Text(page, report.Title, title, y, width, rtl) + 4;
        y = Text(page, report.Period, body, y, width, rtl, PdfBrushes.DimGray);
        y = Text(page, report.Scope, body, y, width, rtl, PdfBrushes.DimGray);
        y = Text(page, report.Generated, small, y, width, rtl, PdfBrushes.Gray) + 12;

        if (report.Summary.Count > 0)
        {
            (page, y) = Table(page, y, new ReportTable(string.Empty, [string.Empty, string.Empty], [.. report.Summary.Select(l => (IReadOnlyList<string>)[l.Label, l.Value])], new HashSet<int> { 1 }), body, heading, rtl, header: false);
        }

        foreach (var table in report.Tables)
        {
            y += 14;
            if (y > page.GetClientSize().Height - 80)
            {
                page = document.Pages.Add();
                y = 0;
            }

            y = Text(page, table.Title, heading, y, width, rtl) + 2;
            y = Text(page, table.Scope ?? string.Empty, small, y, width, rtl, PdfBrushes.DimGray) + 4;
            (page, y) = Table(page, y, table, body, heading, rtl, header: true);
        }

        // The disclaimer on every page (REP-07).
        foreach (PdfPage each in document.Pages)
        {
            var size = each.GetClientSize();
            each.Graphics.DrawString(Clean(report.Disclaimer), small, PdfBrushes.Gray, new RectangleF(0, size.Height - 24, size.Width, 24), Format(rtl));
        }

        using var output = new MemoryStream();
        document.Save(output);
        return output.ToArray();
    }

    private static float Text(PdfPage page, string text, PdfFont font, float y, float width, bool rtl, PdfBrush? brush = null)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return y;
        }

        var element = new PdfTextElement(Clean(text), font, brush ?? PdfBrushes.Black) { StringFormat = Format(rtl, top: true) };
        var result = element.Draw(page, new RectangleF(0, y, width, page.GetClientSize().Height - y));
        return result.Bounds.Bottom;
    }

    private static (PdfPage Page, float Y) Table(PdfPage page, float y, ReportTable table, PdfFont font, PdfFont bold, bool rtl, bool header)
    {
        var count = table.Columns.Count;
        int Map(int column) => rtl ? count - 1 - column : column;

        var grid = new PdfGrid();
        grid.Columns.Add(count);
        grid.Style.Font = font;
        grid.Style.CellPadding = new PdfPaddings(4, 4, 3, 3);
        var line = new PdfPen(new PdfColor(210, 214, 210), 0.5f);
        var none = new PdfPen(new PdfColor(255, 255, 255), 0);
        void Borders(PdfGridCell cell)
        {
            // A summary without a header is a plain list with separators; tables get a light grid.
            cell.Style.Borders = header
                ? new PdfBorders { All = line }
                : new PdfBorders { Left = none, Right = none, Top = none, Bottom = line };
        }
        if (header)
        {
            var headerRow = grid.Headers.Add(1)[0];
            for (var i = 0; i < count; i++)
            {
                var cell = headerRow.Cells[Map(i)];
                cell.Value = Clean(table.Columns[i]);
                cell.Style.Font = bold;
                cell.Style.BackgroundBrush = new PdfSolidBrush(new PdfColor(236, 239, 236));
                Borders(cell);
                cell.StringFormat = Format(rtl, numeric: false, alignEnd: table.NumericColumns.Contains(i));
            }
        }

        foreach (var values in table.Rows)
        {
            var row = grid.Rows.Add();
            for (var i = 0; i < count; i++)
            {
                var cell = row.Cells[Map(i)];
                var value = i < values.Count ? Clean(values[i]) : string.Empty;
                cell.Value = value;
                Borders(cell);

                // A cell that starts with a Latin word (an account name, a #tag) keeps its left-to-right order in RTL.
                cell.StringFormat = Format(rtl, table.NumericColumns.Contains(i) || StartsLatin(value), alignEnd: table.NumericColumns.Contains(i));
            }
        }

        var result = grid.Draw(page, new PointF(0, y), new PdfGridLayoutFormat { Layout = PdfLayoutType.Paginate });
        return (result.Page, result.Bounds.Bottom);
    }

    // Text runs right to left in RTL documents; cells that hold only a number and a currency code stay left to right so
    // their order never changes. Number columns align to the end of the reading direction in LTR and to the right in RTL.
    private static PdfStringFormat Format(bool rtl, bool numeric = false, bool alignEnd = false, bool top = false) => new()
    {
        TextDirection = rtl && !numeric ? PdfTextDirection.RightToLeft : PdfTextDirection.None,
        Alignment = rtl || alignEnd ? PdfTextAlignment.Right : PdfTextAlignment.Left,
        LineAlignment = top ? PdfVerticalAlignment.Top : PdfVerticalAlignment.Middle,
    };

    // Directional marks and isolates keep amounts in place on screen; the PDF font has no glyphs for them.
    private static string Clean(string text) =>
        new([.. text.Where(c => c is not ('‎' or '‏' or '⁦' or '⁧' or '⁨' or '⁩'))]);

    private static bool StartsLatin(string text) => text.FirstOrDefault(char.IsLetter) is >= 'A' and <= 'z' or >= '\u00C0' and <= '\u024F';

    private static Stream Font(string name) =>
        typeof(PdfReport).Assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException($"Font {name} is missing.");
}
