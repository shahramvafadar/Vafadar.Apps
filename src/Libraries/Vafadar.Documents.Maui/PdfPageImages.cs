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
    public static async Task<IReadOnlyList<byte[]>> RenderAsync(byte[] pdf, int maxPages = 4)
    {
        ArgumentNullException.ThrowIfNull(pdf);
#if IOS || MACCATALYST
        return await Task.Run(() => RenderApple(pdf, maxPages));
#elif WINDOWS
        return await RenderWindowsAsync(pdf, maxPages);
#elif ANDROID
        return await Task.Run(() => RenderAndroid(pdf, maxPages));
#else
        await Task.CompletedTask;
        return [];
#endif
    }

    private static (int Width, int Height) SizeOf(double width, double height)
    {
        var scale = Math.Min(Scale, MaxSide / Math.Max(1, Math.Max(width, height)));
        return ((int)Math.Max(1, width * scale), (int)Math.Max(1, height * scale));
    }

#if IOS || MACCATALYST
    private static List<byte[]> RenderApple(byte[] pdf, int maxPages)
    {
        using var data = Foundation.NSData.FromArray(pdf);
        using var document = new PdfKit.PdfDocument(data);
        if (document.IsLocked)
        {
            return [];
        }

        var images = new List<byte[]>();
        foreach (var index in TextLayout.PagesToRead((int)document.PageCount, maxPages))
        {
            using var page = document.GetPage(index);
            if (page is null)
            {
                continue;
            }

            var bounds = page.GetBoundsForBox(PdfKit.PdfDisplayBox.Media);
            var (width, height) = SizeOf(bounds.Width, bounds.Height);
            using var image = page.GetThumbnail(new CoreGraphics.CGSize(width, height), PdfKit.PdfDisplayBox.Media);
            if (image.AsJPEG(0.9f) is { } jpeg)
            {
                images.Add(jpeg.ToArray());
            }
        }

        return images;
    }
#endif

#if ANDROID
    private static List<byte[]> RenderAndroid(byte[] pdf, int maxPages)
    {
        // PdfRenderer needs a seekable file descriptor; the copy lives in the app's cache only while rendering.
        var path = Path.Combine(Android.App.Application.Context.CacheDir!.AbsolutePath, $"render-{Guid.NewGuid():N}.pdf");
        try
        {
            File.WriteAllBytes(path, pdf);

            // Dispose only releases the .NET handles; the renderer, its pages and the descriptor are closed explicitly.
            using var descriptor = Android.OS.ParcelFileDescriptor.Open(new Java.IO.File(path), Android.OS.ParcelFileMode.ReadOnly)!;
            try
            {
                using var renderer = new Android.Graphics.Pdf.PdfRenderer(descriptor);
                try
                {
                    var images = new List<byte[]>();
                    foreach (var index in TextLayout.PagesToRead(renderer.PageCount, maxPages))
                    {
                        using var page = renderer.OpenPage(index);
                        using var bitmap = RenderPage(page);
                        using var output = new MemoryStream();
                        bitmap.Compress(Android.Graphics.Bitmap.CompressFormat.Jpeg!, 90, output);
                        bitmap.Recycle();
                        images.Add(output.ToArray());
                    }

                    return images;
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
            return [];
        }
        finally
        {
            File.Delete(path);
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
    private static async Task<IReadOnlyList<byte[]>> RenderWindowsAsync(byte[] pdf, int maxPages)
    {
        using var source = new global::Windows.Storage.Streams.InMemoryRandomAccessStream();
        using (var writer = new global::Windows.Storage.Streams.DataWriter(source.GetOutputStreamAt(0)))
        {
            writer.WriteBytes(pdf);
            await writer.StoreAsync();
            await writer.FlushAsync();
            writer.DetachStream();
        }

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
            return [];
        }

        var images = new List<byte[]>();
        foreach (var index in TextLayout.PagesToRead((int)document.PageCount, maxPages))
        {
            using var page = document.GetPage((uint)index);
            var (width, height) = SizeOf(page.Size.Width, page.Size.Height);
            using var output = new global::Windows.Storage.Streams.InMemoryRandomAccessStream();

            // The default encoding is PNG on a white background.
            await page.RenderToStreamAsync(output, new global::Windows.Data.Pdf.PdfPageRenderOptions { DestinationWidth = (uint)width, DestinationHeight = (uint)height });
            var bytes = new byte[output.Size];
            using var reader = new global::Windows.Storage.Streams.DataReader(output.GetInputStreamAt(0));
            await reader.LoadAsync((uint)output.Size);
            reader.ReadBytes(bytes);
            images.Add(bytes);
        }

        return images;
    }
#endif
}
