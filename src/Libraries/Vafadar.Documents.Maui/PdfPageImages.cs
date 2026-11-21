using System.Security.Cryptography;

namespace Vafadar.Documents.Maui;

/// <summary>
/// Renders pages of a PDF file to images with the system's own PDF renderer (PdfRenderer on Android, PDFKit on iOS,
/// Windows.Data.Pdf on Windows), so scanned PDFs can be recognised like photos. No extra library is shipped.
/// </summary>
public static class PdfPageImages
{
    // About 200 dpi for text recognition, at most 2400 pixels on the long side.
    private const double Scale = 200d / 72d;
    private const double MaxSide = 2400;

    /// <summary>Gets a value indicating whether this device can render PDF pages.</summary>
    public static bool IsSupported =>
#if IOS || MACCATALYST || WINDOWS || ANDROID
        true;
#else
        false;
#endif

    /// <summary>Renders the pages chosen by <see cref="TextLayout.PagesToRead"/> as encoded images.</summary>
    /// <returns>One image per page; empty when the file cannot be opened (damaged or password protected).</returns>
    public static Task<IReadOnlyList<byte[]>> RenderAsync(byte[] pdf, int maxPages = 4) =>
        RenderAsync(pdf, maxPages, CancellationToken.None);

    /// <summary>Renders the chosen pages while their original reader remains active.</summary>
    public static Task<IReadOnlyList<byte[]>> RenderAsync(byte[] pdf, int maxPages, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(pdf);
        return RenderCoreAsync(pdf, maxPages, selected: null, observe: null, ct);
    }

    /// <summary>Renders one selected page and reports the real count, without sampling or loading every page.</summary>
    public static async Task<PdfPageImage?> ReadPageAsync(byte[] pdf, int pageIndex)
    {
        ArgumentNullException.ThrowIfNull(pdf);
        if (pageIndex < 0) throw new ArgumentOutOfRangeException(nameof(pageIndex));
        var count = 0;
        var images = await RenderCoreAsync(pdf, 1, pageIndex, value => count = value, CancellationToken.None);
        return images.Count == 1 ? new PdfPageImage(count, images[0]) : null;
    }

    private static async Task<IReadOnlyList<byte[]>> RenderCoreAsync(byte[] pdf, int maxPages,
        int? selected, Action<int>? observe, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var images = new List<byte[]>();
        var transferred = false;
        try
        {
#if IOS || MACCATALYST
            var rendered = await Task.Run(() => RenderApple(pdf, maxPages, selected, observe, images, ct), ct);
#elif WINDOWS
            var rendered = await RenderWindowsAsync(pdf, maxPages, selected, observe, images, ct);
#elif ANDROID
            var rendered = await Task.Run(() => RenderAndroid(pdf, maxPages, selected, observe, images, ct), ct);
#else
            await Task.CompletedTask;
            var rendered = false;
#endif
            ct.ThrowIfCancellationRequested();
            transferred = rendered;
            return rendered ? images : [];
        }
        finally
        {
            if (!transferred)
                foreach (var image in images) CryptographicOperations.ZeroMemory(image);
        }
    }

    private static IEnumerable<int> SelectedPages(int count, int maximum, int? selected, Action<int>? observe)
    {
        observe?.Invoke(count);
        if (selected is { } index)
        {
            if (index < 0 || index >= count) throw new ArgumentOutOfRangeException(nameof(selected));
            return [index];
        }
        return TextLayout.PagesToRead(count, maximum);
    }

    private static (int Width, int Height) SizeOf(double width, double height)
    {
        var scale = Math.Min(Scale, MaxSide / Math.Max(1, Math.Max(width, height)));
        return ((int)Math.Max(1, width * scale), (int)Math.Max(1, height * scale));
    }

#if IOS || MACCATALYST
    private static bool RenderApple(byte[] pdf, int maxPages, int? selected, Action<int>? observe,
        List<byte[]> images, CancellationToken ct)
    {
        using var data = Foundation.NSData.FromArray(pdf);
        using var document = new PdfKit.PdfDocument(data);
        if (document.IsLocked)
        {
            return false;
        }

        foreach (var index in SelectedPages(checked((int)document.PageCount), maxPages, selected, observe))
        {
            ct.ThrowIfCancellationRequested();
            using var page = document.GetPage(index);
            if (page is null)
            {
                continue;
            }

            var bounds = page.GetBoundsForBox(PdfKit.PdfDisplayBox.Media);
            var (width, height) = SizeOf(bounds.Width, bounds.Height);
            using var image = page.GetThumbnail(new CoreGraphics.CGSize(width, height), PdfKit.PdfDisplayBox.Media);
            ct.ThrowIfCancellationRequested();
            using var jpeg = image.AsJPEG(0.9f);
            if (jpeg is not null)
            {
                images.Add(jpeg.ToArray());
            }
        }

        return true;
    }
#endif

#if ANDROID
    private static bool RenderAndroid(byte[] pdf, int maxPages, int? selected, Action<int>? observe,
        List<byte[]> images, CancellationToken ct)
    {
        // PdfRenderer needs a seekable descriptor. Unlink the private path before rendering so an interrupted
        // render cannot leave the complete PDF behind in the app cache.
        var path = Path.Combine(Android.App.Application.Context.CacheDir!.AbsolutePath, $"render-{Guid.NewGuid():N}.pdf");
        var fileCreated = false;
        try
        {
            using (var staging = new FileStream(path, FileMode.CreateNew, FileAccess.Write))
            {
                fileCreated = true;
                staging.Write(pdf);
            }

            ct.ThrowIfCancellationRequested();
            // Dispose only releases the .NET handles; the renderer, its pages and the descriptor are closed explicitly.
            using var descriptor = Android.OS.ParcelFileDescriptor.Open(new Java.IO.File(path), Android.OS.ParcelFileMode.ReadOnly)!;
            try
            {
                File.Delete(path);
                fileCreated = false;
                using var renderer = new Android.Graphics.Pdf.PdfRenderer(descriptor);
                try
                {
                    foreach (var index in SelectedPages(renderer.PageCount, maxPages, selected, observe))
                    {
                        ct.ThrowIfCancellationRequested();
                        using var page = renderer.OpenPage(index);
                        using var bitmap = RenderPage(page);
                        try
                        {
                            ct.ThrowIfCancellationRequested();
                            using var output = new MemoryStream();
                            bitmap.Compress(Android.Graphics.Bitmap.CompressFormat.Jpeg!, 90, output);
                            images.Add(output.ToArray());
                        }
                        finally { bitmap.Recycle(); }
                    }

                    return true;
                }
                finally
                {
                    renderer.Close();
                }
            }
            finally
            {
                descriptor.Close();
            }
        }
        catch (Exception ex) when (ex is Java.Lang.Exception or IOException)
        {
            // A damaged or password-protected file has no pages to render.
            System.Diagnostics.Debug.WriteLine($"PDF rendering failed: {ex.GetType().Name}");
            return false;
        }
        finally
        {
            if (fileCreated) File.Delete(path);
        }
    }
#endif

#if ANDROID
    private static Android.Graphics.Bitmap RenderPage(Android.Graphics.Pdf.PdfRenderer.Page page)
    {
        try
        {
            var (width, height) = SizeOf(page.Width, page.Height);
            var bitmap = Android.Graphics.Bitmap.CreateBitmap(width, height, Android.Graphics.Bitmap.Config.Argb8888!);
            bitmap.EraseColor(Android.Graphics.Color.White);
            page.Render(bitmap, null, null, Android.Graphics.Pdf.PdfRenderMode.ForDisplay);
            return bitmap;
        }
        finally
        {
            page.Close();
        }
    }
#endif

#if WINDOWS
    private static async Task<bool> RenderWindowsAsync(byte[] pdf, int maxPages, int? selected, Action<int>? observe,
        List<byte[]> images, CancellationToken ct)
    {
        using var source = new global::Windows.Storage.Streams.InMemoryRandomAccessStream();
        using (var writer = new global::Windows.Storage.Streams.DataWriter(source.GetOutputStreamAt(0)))
        {
            writer.WriteBytes(pdf);
            await writer.StoreAsync();
            await writer.FlushAsync();
            writer.DetachStream();
        }

        ct.ThrowIfCancellationRequested();
        source.Seek(0);
        global::Windows.Data.Pdf.PdfDocument document;
        try
        {
            document = await global::Windows.Data.Pdf.PdfDocument.LoadFromStreamAsync(source);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // A damaged or password-protected file has no pages to render.
            System.Diagnostics.Debug.WriteLine($"PDF rendering failed: {ex.GetType().Name}");
            return false;
        }

        foreach (var index in SelectedPages(checked((int)document.PageCount), maxPages, selected, observe))
        {
            ct.ThrowIfCancellationRequested();
            using var page = document.GetPage((uint)index);
            var (width, height) = SizeOf(page.Size.Width, page.Size.Height);
            using var output = new global::Windows.Storage.Streams.InMemoryRandomAccessStream();

            // The default encoding is PNG on a white background.
            await page.RenderToStreamAsync(output, new global::Windows.Data.Pdf.PdfPageRenderOptions { DestinationWidth = (uint)width, DestinationHeight = (uint)height });
            ct.ThrowIfCancellationRequested();
            using var reader = new global::Windows.Storage.Streams.DataReader(output.GetInputStreamAt(0));
            await reader.LoadAsync((uint)output.Size);
            ct.ThrowIfCancellationRequested();
            var bytes = new byte[output.Size];
            images.Add(bytes);
            reader.ReadBytes(bytes);
        }

        return true;
    }
#endif
}
