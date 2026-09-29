using System.Globalization;
using System.Text;

namespace Vafadar.Documents.Tests;

/// <summary>
/// Writes small, valid PDF files by hand, so the tests do not depend on a PDF generator (whose trial notice would add
/// text to every page without a license key).
/// </summary>
internal static class MinimalPdf
{
    /// <summary>A text run on a page: x and y from the top left corner, in points.</summary>
    public readonly record struct Run(string Text, float X, float Y);

    /// <summary>Creates a PDF with one A4 page per entry; a page without runs gets a grey box (like a scan) instead.</summary>
    public static byte[] Create(params Run[][] pages)
    {
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            string.Empty, // The page tree, filled in below.
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>",
        };

        var kids = new List<int>();
        foreach (var runs in pages)
        {
            var content = runs.Length == 0
                ? "0.5 g 40 500 300 200 re f"
                : string.Join('\n', runs.Select(r => string.Create(CultureInfo.InvariantCulture, $"BT /F1 12 Tf {r.X} {842 - r.Y} Td ({Escape(r.Text)}) Tj ET")));
            objects.Add($"<< /Length {Encoding.Latin1.GetByteCount(content)} >>\nstream\n{content}\nendstream");
            var contentId = objects.Count;
            objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 3 0 R >> >> /Contents {contentId} 0 R >>");
            kids.Add(objects.Count);
        }

        objects[1] = $"<< /Type /Pages /Kids [{string.Join(' ', kids.Select(k => $"{k} 0 R"))}] /Count {kids.Count} >>";

        var pdf = new StringBuilder("%PDF-1.4\n");
        var offsets = new List<int>();
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(Encoding.Latin1.GetByteCount(pdf.ToString()));
            pdf.Append(CultureInfo.InvariantCulture, $"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
        }

        var xref = Encoding.Latin1.GetByteCount(pdf.ToString());
        pdf.Append(CultureInfo.InvariantCulture, $"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets)
        {
            pdf.Append(CultureInfo.InvariantCulture, $"{offset:D10} 00000 n \n");
        }

        pdf.Append(CultureInfo.InvariantCulture, $"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return Encoding.Latin1.GetBytes(pdf.ToString());
    }

    private static string Escape(string text) => text.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
}
