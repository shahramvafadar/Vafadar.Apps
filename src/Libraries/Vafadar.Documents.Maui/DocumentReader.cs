namespace Vafadar.Documents.Maui;

/// <summary>
/// Reads the text of a photo or a PDF file on the device, row by row. A PDF with a text layer is read directly (exact,
/// no OCR); a scanned PDF is rendered page by page and recognised like a photo. Interpreting the text (amounts,
/// dates, names) is left to the app.
/// </summary>
public static class DocumentReader
{
    /// <summary>The most pages read from a PDF: the first ones and the last (where totals usually are).</summary>
    public const int MaxPages = 4;

    /// <summary>Gets a value indicating whether a file of this type can be read on this device.</summary>
    /// <param name="contentType">The MIME type, e.g. <c>image/jpeg</c> or <c>application/pdf</c>.</param>
    public static bool CanRead(string? contentType) =>
        IsPdf(contentType)
            ? true // The text layer is read everywhere; scanned pages need OCR, checked when reading.
            : contentType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true && TextRecognizer.IsSupported;

    /// <summary>Returns the text rows of a document, or <see langword="null"/> when nothing could be read.</summary>
    /// <param name="content">The file content.</param>
    /// <param name="contentType">The MIME type; the content is checked as well.</param>
    public static async Task<string?> ReadAsync(byte[] content, string? contentType)
    {
        ArgumentNullException.ThrowIfNull(content);
        try
        {
            if (!PdfText.IsPdf(contentType, content))
            {
                return TextRecognizer.IsSupported ? TextLayout.Rows(await TextRecognizer.RecognizeAsync(content)) : null;
            }

            // Digital PDFs carry their text; OCR is only needed for scanned pages.
            if (await Task.Run(() => PdfText.Extract(content, MaxPages)) is { } text)
            {
                return text;
            }

            if (!TextRecognizer.IsSupported || !PdfPageImages.IsSupported)
            {
                return null;
            }

            var pages = new List<string>();
            foreach (var image in await PdfPageImages.RenderAsync(content, MaxPages))
            {
                if (TextLayout.Rows(await TextRecognizer.RecognizeAsync(image)) is { } rows)
                {
                    pages.Add(rows);
                }
            }

            return pages.Count == 0 ? null : string.Join('\n', pages);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // An unreadable or unsupported file is "nothing found", never a crash.
            System.Diagnostics.Debug.WriteLine($"Document reading failed: {ex.GetType().Name}");
            return null;
        }
    }

    private static bool IsPdf(string? contentType) => string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase);
}
