using Vafadar.Zanance.Core.Receipts;

namespace Vafadar.Zanance.App.Presentation;

/// <summary>
/// On-device text recognition of receipt photos (D-31): Vision on iOS, Windows.Media.Ocr on Windows and ML Kit with its
/// bundled Latin model on Android (owner decision; Android has no system engine). Nothing leaves the device: the model
/// ships in the app and the network permissions ML Kit asks for are removed from the release manifest.
/// </summary>
internal static class ReceiptReader
{
    /// <summary>Gets a value indicating whether this device can read receipts.</summary>
    public static bool IsSupported =>
#if IOS || MACCATALYST
        OperatingSystem.IsIOSVersionAtLeast(13) || OperatingSystem.IsMacCatalystVersionAtLeast(13);
#elif WINDOWS || ANDROID
        true;
#else
        false;
#endif

    /// <summary>Returns the text of an image, line by line, or <see langword="null"/> when nothing could be read.</summary>
    public static async Task<string?> ReadAsync(byte[] image)
    {
        ArgumentNullException.ThrowIfNull(image);
        try
        {
#if IOS || MACCATALYST
            return await Task.Run(() => ReadApple(image));
#elif WINDOWS
            return await ReadWindowsAsync(image);
#elif ANDROID
            return await ReadAndroidAsync(image);
#else
            await Task.CompletedTask;
            return null;
#endif
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // An unreadable or unsupported image is "nothing found", never a crash.
            System.Diagnostics.Debug.WriteLine($"Receipt reading failed: {ex.GetType().Name}");
            return null;
        }
    }

#if IOS || MACCATALYST
    private static string? ReadApple(byte[] image)
    {
        var words = new List<ReceiptWord>();
        using var request = new Vision.VNRecognizeTextRequest((request, error) =>
        {
            foreach (var observation in request.GetResults<Vision.VNRecognizedTextObservation>() ?? [])
            {
                if (observation.TopCandidates(1).FirstOrDefault()?.String is { Length: > 0 } text)
                {
                    // Vision's origin is the lower left corner; read from the top.
                    var box = observation.BoundingBox;
                    words.Add(new ReceiptWord(1 - box.Y - box.Height, 1 - box.Y, box.X, text));
                }
            }
        })
        {
            RecognitionLevel = Vision.VNRequestTextRecognitionLevel.Accurate,
            UsesLanguageCorrection = true,
        };

        using var data = Foundation.NSData.FromArray(image);
        using var handler = new Vision.VNImageRequestHandler(data, new Foundation.NSDictionary());
        if (!handler.Perform([request], out _))
        {
            return null;
        }

        return ReceiptParser.Rows(words);
    }
#endif

#if ANDROID
    private static async Task<string?> ReadAndroidAsync(byte[] image)
    {
        using var bitmap = Android.Graphics.BitmapFactory.DecodeByteArray(image, 0, image.Length);
        if (bitmap is null)
        {
            return null;
        }

        using var input = Xamarin.Google.MLKit.Vision.Common.InputImage.FromBitmap(bitmap, 0);
        using var recognizer = Xamarin.Google.MLKit.Vision.Text.TextRecognition.GetClient(Xamarin.Google.MLKit.Vision.Text.Latin.TextRecognizerOptions.DefaultOptions);
        var result = await Android.Gms.Extensions.TasksExtensions.AsAsync<Xamarin.Google.MLKit.Vision.Text.Text>(recognizer.Process(input));
        var words = new List<ReceiptWord>();
        foreach (var block in result.TextBlocks)
        {
            foreach (var line in block.Lines)
            {
                foreach (var element in line.Elements)
                {
                    if (element.BoundingBox is { } box && !string.IsNullOrEmpty(element.Text))
                    {
                        words.Add(new ReceiptWord(box.Top, box.Bottom, box.Left, element.Text));
                    }
                }
            }
        }

        return ReceiptParser.Rows(words);
    }
#endif

#if WINDOWS
    private static async Task<string?> ReadWindowsAsync(byte[] image)
    {
        var engine = global::Windows.Media.Ocr.OcrEngine.TryCreateFromUserProfileLanguages();
        if (engine is null)
        {
            return null;
        }

        using var stream = new global::Windows.Storage.Streams.InMemoryRandomAccessStream();
        using (var writer = new global::Windows.Storage.Streams.DataWriter(stream.GetOutputStreamAt(0)))
        {
            writer.WriteBytes(image);
            await writer.StoreAsync();
            await writer.FlushAsync();
            writer.DetachStream();
        }

        stream.Seek(0);
        var decoder = await global::Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(stream);
        var scale = Math.Min(1d, global::Windows.Media.Ocr.OcrEngine.MaxImageDimension / (double)Math.Max(decoder.PixelWidth, decoder.PixelHeight));
        var transform = new global::Windows.Graphics.Imaging.BitmapTransform
        {
            ScaledWidth = (uint)(decoder.PixelWidth * scale),
            ScaledHeight = (uint)(decoder.PixelHeight * scale),
        };
        using var bitmap = await decoder.GetSoftwareBitmapAsync(
            global::Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8,
            global::Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied,
            transform,
            global::Windows.Graphics.Imaging.ExifOrientationMode.RespectExifOrientation,
            global::Windows.Graphics.Imaging.ColorManagementMode.DoNotColorManage);
        var result = await engine.RecognizeAsync(bitmap);
        return ReceiptParser.Rows(result.Lines.SelectMany(l => l.Words).Select(w => new ReceiptWord(w.BoundingRect.Y, w.BoundingRect.Y + w.BoundingRect.Height, w.BoundingRect.X, w.Text)));
    }
#endif
}
