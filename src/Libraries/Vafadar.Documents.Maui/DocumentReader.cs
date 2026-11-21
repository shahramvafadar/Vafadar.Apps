using System.Security.Cryptography;

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
    public static async Task<string?> ReadAsync(byte[] content, string? contentType) =>
        (await ReadDocumentAsync(content, contentType)).Text;

    /// <summary>Reads text while preserving whether OCR column alignment needs human review.</summary>
    public static Task<TextLayoutResult> ReadDocumentAsync(byte[] content, string? contentType) =>
        ReadDocumentAsync(content, contentType, CancellationToken.None);

    /// <summary>Reads text and stops before the next extraction step when the reader is cancelled.</summary>
    public static async Task<TextLayoutResult> ReadDocumentAsync(byte[] content, string? contentType, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(content);
        ct.ThrowIfCancellationRequested();
        try
        {
            if (!PdfText.IsPdf(contentType, content))
            {
                if (!TextRecognizer.IsSupported)
                {
                    return new(null);
                }

                // Recognition gets its own bounded upright pixels, not the app's 1600 px / quality-80 stored copy.
                var image = await DocumentImages.PrepareAsync(content, DocumentImages.RecognitionEdge, 0.95f);
                try
                {
                    return await RecognizePageAsync(image);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(image);
                }
            }

            // Digital PDFs carry their text; OCR is only needed for scanned pages.
            var text = await Task.Run(() => PdfText.Extract(content, MaxPages), ct);
            ct.ThrowIfCancellationRequested();
            if (text is not null)
            {
                return new(text);
            }

            if (!TextRecognizer.IsSupported || !PdfPageImages.IsSupported)
            {
                return new(await ReadSparsePdfAsync());
            }

            var pages = new List<string>();
            var ambiguous = false;
            var images = await PdfPageImages.RenderAsync(content, MaxPages);
            try
            {
                foreach (var image in images)
                {
                    var layout = await RecognizePageAsync(image);
                    ambiguous |= layout.IsAmbiguous;
                    if (layout.Text is { } rows)
                    {
                        pages.Add(rows);
                    }
                }
            }
            finally
            {
                // An OCR error can leave later pages unread, but the renderer already allocated their images.
                foreach (var image in images) CryptographicOperations.ZeroMemory(image);
            }

            // Nothing recognised: a sparse text layer is still better than nothing.
            return new(pages.Count > 0
                ? string.Join('\f', pages)
                : await ReadSparsePdfAsync(), ambiguous);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            ct.ThrowIfCancellationRequested();
            // An unreadable or unsupported file is "nothing found", never a crash.
            System.Diagnostics.Debug.WriteLine($"Document reading failed: {ex.GetType().Name}");
            return new(null);
        }

        async Task<TextLayoutResult> RecognizePageAsync(byte[] image)
        {
            ct.ThrowIfCancellationRequested();
            // The native recognizer may still own these bytes. Wait for it to settle before clearing them.
            var words = await TextRecognizer.RecognizeAsync(image);
            ct.ThrowIfCancellationRequested();
            return TextLayout.Reconstruct(words);
        }

        async Task<string?> ReadSparsePdfAsync()
        {
            var sparse = await Task.Run(() => PdfText.Extract(content, MaxPages, minimumPerPage: 0), ct);
            ct.ThrowIfCancellationRequested();
            return sparse;
        }
    }

    private static bool IsPdf(string? contentType) => string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase);
}
