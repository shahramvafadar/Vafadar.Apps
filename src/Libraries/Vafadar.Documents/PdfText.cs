using System.Text;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Parsing;

namespace Vafadar.Documents;

/// <summary>
/// The text layer of PDF files (invoices, statements, receipts sent by e-mail). Digital PDFs carry their text, so it is
/// read directly: exact, fast and without OCR. Scanned PDFs have no text layer and return <see langword="null"/>; their
/// pages are rendered and recognised instead.
/// </summary>
public static class PdfText
{
    // Pages are stacked below each other when their words are joined into rows.
    private const double PageOffset = 100_000;

    /// <summary>Gets a value indicating whether the content starts like a PDF file.</summary>
    public static bool IsPdf(ReadOnlySpan<byte> content) => content.StartsWith("%PDF-"u8);

    /// <summary>Gets a value indicating whether a MIME type or file content is a PDF.</summary>
    public static bool IsPdf(string? contentType, ReadOnlySpan<byte> content) =>
        string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase) || IsPdf(content);

    /// <summary>Returns the number of pages, or 0 when the file cannot be opened (damaged or password protected).</summary>
    public static int PageCount(byte[] pdf)
    {
        ArgumentNullException.ThrowIfNull(pdf);
        try
        {
            using var stream = new MemoryStream(pdf, writable: false);
            var document = new PdfLoadedDocument(stream);
            try
            {
                return document.Pages.Count;
            }
            finally
            {
                document.Close(true);
            }
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return 0;
        }
    }

    /// <summary>
    /// Reads the text of a PDF row by row, from the pages chosen by <see cref="TextLayout.PagesToRead"/>.
    /// </summary>
    /// <param name="pdf">The file content.</param>
    /// <param name="maxPages">The most pages to read.</param>
    /// <returns>The rows, or <see langword="null"/> when the file has no usable text layer or cannot be opened.</returns>
    public static string? Extract(byte[] pdf, int maxPages = 4)
    {
        ArgumentNullException.ThrowIfNull(pdf);
        try
        {
            using var stream = new MemoryStream(pdf, writable: false);
            var document = new PdfLoadedDocument(stream);
            try
            {
                var words = new List<LayoutWord>();
                foreach (var index in TextLayout.PagesToRead(document.Pages.Count, maxPages))
                {
                    if (document.Pages[index] is not PdfLoadedPage page)
                    {
                        continue;
                    }

                    page.ExtractText(out TextLineCollection lines);
                    var offset = index * PageOffset;
                    foreach (var line in lines.TextLine)
                    {
                        foreach (var word in line.WordCollection)
                        {
                            // Persian text in PDFs is often stored as presentation forms; compatibility normalisation
                            // turns them back into ordinary letters.
                            var text = word.Text?.Normalize(NormalizationForm.FormKC).Trim();
                            if (!string.IsNullOrEmpty(text))
                            {
                                var box = word.Bounds;
                                words.Add(new LayoutWord(offset + box.Top, offset + box.Bottom, box.Left, text));
                            }
                        }
                    }
                }

                var rows = TextLayout.Rows(words);
                return TextLayout.HasContent(rows) ? rows : null;
            }
            finally
            {
                document.Close(true);
            }
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // A damaged or password-protected file has nothing to read.
            System.Diagnostics.Debug.WriteLine($"PDF text extraction failed: {ex.GetType().Name}");
            return null;
        }
    }
}
